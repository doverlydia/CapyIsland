#!/usr/bin/env node
// Mirrors PNGs left to right, in place: for a drawing that came back facing the wrong way
// (the wildlife flight frames must face the same way as the creature's standing picture).
//
// Usage: node Tools/ChatGPT/mirror.mjs <file.png> [...]

import fs from "node:fs";
import { decode, encode } from "./pngio.mjs";

for (const file of process.argv.slice(2)) {
  const { w, h, data } = decode(fs.readFileSync(file));
  const out = Buffer.alloc(w * h * 4);
  for (let y = 0; y < h; y++)
    for (let x = 0; x < w; x++)
      data.copy(out, (y * w + (w - 1 - x)) * 4, (y * w + x) * 4, (y * w + x) * 4 + 4);
  fs.writeFileSync(file, encode(w, h, out));
  console.log("mirrored " + file);
}
