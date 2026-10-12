// Renders songs without the window — for scripts (and for Claude).
//
//   node Tools/MusicStudio/render.js                       # every song in songs/ into the game
//   node Tools/MusicStudio/render.js music_day             # one song into the game
//   node Tools/MusicStudio/render.js music_day --out x.wav # somewhere else, to listen to
//
// Into the game means Assets/Resources/Music/<song.name>.wav (32 kHz mono, 16-bit), which
// MusicPlayer.cs loads: music_day by day, music_night at night.
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createEngine, encodeWav, DEFAULT_SR } from "./app/engine.js";

const here = path.dirname(fileURLToPath(import.meta.url));
export const SONGS = path.join(here, "songs");
export const GAME_MUSIC = path.resolve(here, "../../Assets/Resources/Music");

/** Renders one song to a WAV file; returns its length in seconds. */
export function renderToFile(song, file) {
  const engine = createEngine(DEFAULT_SR);
  const samples = engine.renderSong(song);
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, encodeWav(samples, DEFAULT_SR));
  return samples.length / DEFAULT_SR;
}

export const gameFile = song => path.join(GAME_MUSIC, song.name.replace(/[^\w-]/g, "_") + ".wav");

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const args = process.argv.slice(2);
  const outAt = args.indexOf("--out");
  const out = outAt >= 0 ? args[outAt + 1] : null;
  const names = args.filter((a, i) => !a.startsWith("--") && (outAt < 0 || i !== outAt + 1));
  const files = names.length ? names.map(n => path.join(SONGS, n.endsWith(".json") ? n : n + ".json"))
                             : fs.readdirSync(SONGS).filter(f => f.endsWith(".json")).map(f => path.join(SONGS, f));
  for (const f of files) {
    const song = JSON.parse(fs.readFileSync(f, "utf8"));
    const target = out ?? gameFile(song);
    const secs = renderToFile(song, target);
    console.log(`${path.basename(f)} -> ${path.relative(process.cwd(), target)}  ${secs.toFixed(1)}s`);
  }
}
