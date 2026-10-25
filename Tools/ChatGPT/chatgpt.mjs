#!/usr/bin/env node
// Bridge to OpenAI so Claude Code can consult ChatGPT and generate 2D assets
// for this project. Lives outside Assets/ so Unity never imports it.
//
// Usage:
//   node Tools/ChatGPT/chatgpt.mjs ask "<question>"
//   node Tools/ChatGPT/chatgpt.mjs image "<prompt>" --out Assets/Art/Generated/name.png [--size 1024x1024] [--transparent] [--quality medium] [--ref a.png,b.png]
//
// --ref passes existing PNGs as style/character references (images/edits endpoint),
// which keeps new sprites consistent with sprites we already have.
//
// Two backends (--via codex|api, or env CHATGPT_VIA; default codex when it is installed):
//   codex - drives the Codex desktop app's CLI, signed in with the user's ChatGPT account, so it
//           spends the ChatGPT plan's allowance instead of API credits.
//   api   - the OpenAI API. Requires OPENAI_API_KEY. Optional: OPENAI_MODEL (default gpt-4o),
//           OPENAI_IMAGE_MODEL (default gpt-image-1).

import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { spawn } from "node:child_process";

const API = "https://api.openai.com/v1";
const key = process.env.OPENAI_API_KEY;

const SYSTEM = `You are a collaborator on "CapyIsland", a 2D Unity game.
Answer concisely and decisively. Skip preamble and restating the question.
When asked for an image prompt, return ONLY a single production-ready prompt for an
image model, no commentary.`;

function parseArgs(argv) {
  const out = { _: [] };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a.startsWith("--")) {
      const k = a.slice(2);
      const next = argv[i + 1];
      if (next === undefined || next.startsWith("--")) out[k] = true;
      else { out[k] = next; i++; }
    } else out._.push(a);
  }
  return out;
}

async function post(endpoint, body) {
  const res = await fetch(`${API}${endpoint}`, {
    method: "POST",
    headers: { "Authorization": `Bearer ${key}`, "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`${endpoint} -> HTTP ${res.status}: ${await res.text()}`);
  return res.json();
}

// Image requests can run for minutes and connections occasionally drop: retry those, and say why.
async function withRetry(what, attempt) {
  for (let i = 1; ; i++) {
    try { return await attempt(); }
    catch (e) {
      const transient = e instanceof TypeError || /fetch failed|ECONNRESET|socket/i.test(String(e.message));
      if (!transient || i >= 3) throw e;
      console.error(`${what}: ${e.message}${e.cause ? " (" + (e.cause.code || e.cause.message) + ")" : ""} - retrying (${i}/2)`);
      await new Promise((r) => setTimeout(r, 4000 * i));
    }
  }
}

async function postForm(endpoint, form) {
  return withRetry(endpoint, () => postFormOnce(endpoint, form));
}

async function postFormOnce(endpoint, form) {
  const res = await fetch(`${API}${endpoint}`, {
    method: "POST",
    headers: { "Authorization": `Bearer ${key}` },
    body: form,
  });
  if (!res.ok) throw new Error(`${endpoint} -> HTTP ${res.status}: ${await res.text()}`);
  return res.json();
}

async function ask(prompt) {
  const data = await post("/chat/completions", {
    model: process.env.OPENAI_MODEL || "gpt-4o",
    messages: [
      { role: "system", content: SYSTEM },
      { role: "user", content: prompt },
    ],
  });
  process.stdout.write(data.choices[0].message.content.trim() + "\n");
}

async function image(prompt, opts) {
  const model = process.env.OPENAI_IMAGE_MODEL || "gpt-image-1";
  const out = opts.out || `Assets/Art/Generated/${Date.now()}.png`;
  let b64;

  if (opts.ref) {
    // reference images -> edits endpoint (multipart)
    const form = new FormData();
    form.append("model", model);
    form.append("prompt", prompt);
    form.append("n", "1");
    form.append("size", opts.size || "1024x1024");
    form.append("quality", opts.quality || "medium");
    if (opts.transparent) form.append("background", "transparent");
    for (const ref of String(opts.ref).split(",")) {
      const p = path.resolve(ref.trim());
      form.append("image[]", new Blob([fs.readFileSync(p)], { type: "image/png" }), path.basename(p));
    }
    const data = await postForm("/images/edits", form);
    b64 = data.data[0].b64_json;
  } else {
    const body = {
      model,
      prompt,
      n: 1,
      size: opts.size || "1024x1024",
      quality: opts.quality || "medium",
    };
    if (opts.transparent) body.background = "transparent";
    const data = await post("/images/generations", body);
    b64 = data.data[0].b64_json;
  }

  fs.mkdirSync(path.dirname(out), { recursive: true });
  fs.writeFileSync(out, Buffer.from(b64, "base64"));
  console.log(`Saved ${out}`);
}

// ---- Codex backend ----

const CODEX_HOME = process.env.CODEX_HOME || path.join(os.homedir(), ".codex");

// The desktop app keeps its CLI under a hashed folder that changes on update: take the newest.
function findCodex() {
  if (process.env.CODEX_CLI) return process.env.CODEX_CLI;
  const bin = path.join(process.env.LOCALAPPDATA || "", "OpenAI", "Codex", "bin");
  const found = (fs.existsSync(bin) ? fs.readdirSync(bin) : [])
    .map((d) => path.join(bin, d, "codex.exe"))
    .filter((p) => fs.existsSync(p))
    .sort((a, b) => fs.statSync(b).mtimeMs - fs.statSync(a).mtimeMs);
  return found[0] || null;
}

// One non-interactive Codex turn in a throwaway read-only folder. The prompt goes in on stdin
// so long prompts and quotes survive Windows argument parsing.
function runCodex(prompt, refs) {
  const exe = findCodex();
  if (!exe) throw new Error("Codex CLI not found. Install the Codex app, or set CODEX_CLI, or use --via api.");
  const work = fs.mkdtempSync(path.join(os.tmpdir(), "capy-codex-"));
  const lastFile = path.join(work, "last.txt");
  const argv = ["exec", "--ephemeral", "--skip-git-repo-check", "-s", "read-only", "-C", work,
    "--json", "-o", lastFile, ...refs.map((r) => `--image=${path.resolve(r)}`), "-"];
  return new Promise((resolve, reject) => {
    const child = spawn(exe, argv, { stdio: ["pipe", "pipe", "pipe"] });
    let stdout = "", stderr = "";
    child.stdout.on("data", (d) => (stdout += d));
    child.stderr.on("data", (d) => (stderr += d));
    child.on("error", reject);
    child.on("close", (code) => {
      const events = stdout.split("\n").filter((l) => l.startsWith("{")).map((l) => JSON.parse(l));
      const threadId = events.find((e) => e.type === "thread.started")?.thread_id;
      const failure = events.find((e) => e.type === "turn.failed" || e.type === "error");
      const last = fs.existsSync(lastFile) ? fs.readFileSync(lastFile, "utf8").trim() : "";
      fs.rmSync(work, { recursive: true, force: true });
      if (code !== 0 || failure)
        reject(new Error(`codex exited ${code}: ${failure ? JSON.stringify(failure) : stderr.trim().slice(-800)}`));
      else resolve({ threadId, last });
    });
    child.stdin.end(prompt);
  });
}

function refList(opts) {
  return opts.ref ? String(opts.ref).split(",").map((r) => r.trim()) : [];
}

async function askCodex(prompt, opts) {
  const refs = refList(opts);
  const { last } = await runCodex(
    `${SYSTEM}\nAnswer from your own knowledge: do not run commands or read files.` +
    (refs.length ? " The attached images are for reference." : "") + `\n\n${prompt}`, refs);
  process.stdout.write(last + "\n");
}

const SHAPES = { "1024x1024": "square", "1536x1024": "landscape 3:2", "1024x1536": "portrait 2:3" };

// Codex's built-in image tool takes no size, quality or path arguments: shape and background go
// in the prompt, and the result is collected from $CODEX_HOME/generated_images/<thread>.
async function imageCodex(prompt, opts) {
  const out = opts.out || `Assets/Art/Generated/${Date.now()}.png`;
  const refs = refList(opts);
  const spec = [
    `Call your built-in image generation tool exactly once with the prompt below. Do not read skills or files, run commands, or save anything; the image is collected automatically.`,
    `Shape: ${SHAPES[opts.size || "1024x1024"] || opts.size}.`,
    opts.transparent ? "Background: genuinely transparent (alpha), no backdrop." : "",
    refs.length ? `Use the ${refs.length} attached image(s) as style/character references, in the order given (FIRST, SECOND...).` : "",
    `\nPrompt:\n${prompt}`,
  ].filter(Boolean).join("\n");
  const { threadId, last } = await runCodex(spec, refs);
  const dir = path.join(CODEX_HOME, "generated_images", threadId || "");
  const pngs = threadId && fs.existsSync(dir)
    ? fs.readdirSync(dir).filter((f) => f.endsWith(".png")).map((f) => path.join(dir, f))
        .sort((a, b) => fs.statSync(b).mtimeMs - fs.statSync(a).mtimeMs)
    : [];
  if (!pngs.length) throw new Error(`Codex made no image. It said: ${last || "(nothing)"}`);
  fs.mkdirSync(path.dirname(out), { recursive: true });
  fs.copyFileSync(pngs[0], out);
  const png = fs.readFileSync(out);
  console.log(`Saved ${out} (${png.readUInt32BE(16)}x${png.readUInt32BE(20)}, via Codex)`);
}

const args = parseArgs(process.argv.slice(2));
const [cmd, ...rest] = args._;
const text = rest.join(" ");
const via = args.via || process.env.CHATGPT_VIA || (findCodex() ? "codex" : "api");
if (via === "api" && !key) {
  console.error("OPENAI_API_KEY is not set (and the Codex app was not found). See Tools/ChatGPT/README.md");
  process.exit(2);
}

try {
  if (cmd === "ask" && text) await (via === "codex" ? askCodex(text, args) : ask(text));
  else if (cmd === "image" && text) await (via === "codex" ? imageCodex(text, args) : image(text, args));
  else {
    console.error('Usage:\n  ask "<question>" [--ref a.png]\n  image "<prompt>" --out <path.png> [--size WxH] [--transparent] [--quality low|medium|high] [--ref a.png,b.png]\n  either: [--via codex|api]');
    process.exit(1);
  }
} catch (e) {
  console.error(e.message + (e.cause ? " (cause: " + (e.cause.code || e.cause.message) + ")" : ""));
  process.exit(1);
}
