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
// Requires env var OPENAI_API_KEY. Optional: OPENAI_MODEL (default gpt-4o), OPENAI_IMAGE_MODEL (default gpt-image-1).

import fs from "node:fs";
import path from "node:path";

const API = "https://api.openai.com/v1";
const key = process.env.OPENAI_API_KEY;
if (!key) {
  console.error("OPENAI_API_KEY is not set. See Tools/ChatGPT/README.md");
  process.exit(2);
}

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

async function postForm(endpoint, form) {
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

const args = parseArgs(process.argv.slice(2));
const [cmd, ...rest] = args._;
const text = rest.join(" ");

try {
  if (cmd === "ask" && text) await ask(text);
  else if (cmd === "image" && text) await image(text, args);
  else {
    console.error('Usage:\n  ask "<question>"\n  image "<prompt>" --out <path.png> [--size WxH] [--transparent] [--quality low|medium|high] [--ref a.png,b.png]');
    process.exit(1);
  }
} catch (e) {
  console.error(e.message);
  process.exit(1);
}
