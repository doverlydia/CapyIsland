// Capy Music Studio's little local server: serves the studio window, keeps the songs in songs/,
// and exports a song straight into the game (Assets/Resources/Music/<name>.wav).
//
//   node Tools/MusicStudio/server.js            # then open http://127.0.0.1:5178
//   "Capy Music Studio.bat"                      # starts this and opens the studio in its own window
//
// With --auto-exit (the launcher passes it) the server stops by itself a minute after the
// studio window is closed.
import http from "node:http";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { songToJson } from "./app/engine.js";
import { SONGS, renderToFile, gameFile } from "./render.js";

const here = path.dirname(fileURLToPath(import.meta.url));
const APP = path.join(here, "app");
const TRASH = path.join(SONGS, "trash");
const PORT = Number(process.env.PORT ?? 5178);
const autoExit = process.argv.includes("--auto-exit");

const TYPES = { ".html": "text/html; charset=utf-8", ".js": "text/javascript; charset=utf-8", ".css": "text/css; charset=utf-8", ".svg": "image/svg+xml", ".png": "image/png", ".ico": "image/x-icon", ".json": "application/json" };

fs.mkdirSync(SONGS, { recursive: true });

// song files are plain names: letters, digits, _ and -
const songPath = file => {
  const name = decodeURIComponent(file).replace(/\.json$/, "");
  if (!/^[\w-]{1,64}$/.test(name)) throw Object.assign(new Error("Song names can use letters, numbers, _ and -"), { status: 400 });
  return path.join(SONGS, name + ".json");
};

function send(res, status, body, type = "application/json") {
  res.writeHead(status, { "Content-Type": type, "Cache-Control": "no-store" });
  res.end(typeof body === "string" || body instanceof Uint8Array ? body : JSON.stringify(body));
}

const readBody = req => new Promise((ok, fail) => {
  let data = "";
  req.on("data", c => { data += c; if (data.length > 20e6) fail(new Error("too big")); });
  req.on("end", () => ok(data));
  req.on("error", fail);
});

let lastPing = Date.now();

async function api(req, res, parts) {
  const [, , what, file] = parts;
  if (what === "ping") { lastPing = Date.now(); return send(res, 200, { ok: true }); }

  if (what === "songs" && !file && req.method === "GET") {
    const list = fs.readdirSync(SONGS).filter(f => f.endsWith(".json")).map(f => {
      const full = path.join(SONGS, f);
      try {
        const s = JSON.parse(fs.readFileSync(full, "utf8"));
        return { file: f.replace(/\.json$/, ""), title: s.title ?? f, name: s.name, bpm: s.bpm, bars: s.bars, modified: fs.statSync(full).mtimeMs };
      } catch { return { file: f.replace(/\.json$/, ""), title: f + " (unreadable)", modified: 0 }; }
    }).sort((a, b) => b.modified - a.modified);
    return send(res, 200, list);
  }

  if (what === "songs" && file) {
    const p = songPath(file);
    if (req.method === "GET") {
      if (!fs.existsSync(p)) return send(res, 404, { error: "No such song" });
      return send(res, 200, fs.readFileSync(p, "utf8"));
    }
    if (req.method === "PUT" || req.method === "POST") {   // POST: the window's last save as it closes
      const song = JSON.parse(await readBody(req));
      fs.writeFileSync(p, songToJson(song));
      return send(res, 200, { ok: true });
    }
    if (req.method === "DELETE") {
      // never really gone: deleted songs wait in songs/trash
      if (fs.existsSync(p)) { fs.mkdirSync(TRASH, { recursive: true }); fs.renameSync(p, path.join(TRASH, Date.now() + "-" + path.basename(p))); }
      return send(res, 200, { ok: true });
    }
  }

  if (what === "export" && file && req.method === "POST") {
    const body = await readBody(req);
    const song = body ? JSON.parse(body) : JSON.parse(fs.readFileSync(songPath(file), "utf8"));
    if (!/^[\w-]{1,64}$/.test(song.name ?? "")) return send(res, 400, { error: "Give the song a game name first (like music_day)" });
    const target = gameFile(song);
    const seconds = renderToFile(song, target);
    console.log(`exported ${song.name} -> ${target} (${seconds.toFixed(1)}s)`);
    return send(res, 200, { ok: true, path: path.relative(path.resolve(here, "../.."), target).replace(/\\/g, "/"), seconds });
  }

  send(res, 404, { error: "Unknown request" });
}

const server = http.createServer(async (req, res) => {
  try {
    const url = new URL(req.url, "http://localhost");
    const parts = url.pathname.split("/");
    if (parts[1] === "api") return await api(req, res, parts);

    let rel = url.pathname === "/" ? "index.html" : decodeURIComponent(url.pathname.slice(1));
    const full = path.normalize(path.join(APP, rel));
    if (!full.startsWith(APP) || !fs.existsSync(full) || fs.statSync(full).isDirectory()) return send(res, 404, "Not found", "text/plain");
    send(res, 200, fs.readFileSync(full), TYPES[path.extname(full)] ?? "application/octet-stream");
  } catch (e) {
    console.error(e);
    send(res, e.status ?? 500, { error: e.message });
  }
});

server.on("error", e => {
  if (e.code === "EADDRINUSE") { console.log(`The studio is already running on http://localhost:${PORT}`); process.exit(0); }
  throw e;
});

server.listen(PORT, "127.0.0.1", () => console.log(`Capy Music Studio: http://localhost:${PORT}`));

if (autoExit) {
  setInterval(() => {
    if (Date.now() - lastPing > 60000) { console.log("Studio window closed; stopping."); process.exit(0); }
  }, 10000);
}
