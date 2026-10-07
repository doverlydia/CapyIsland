#!/usr/bin/env node
// Crops a PNG to its visible pixels plus a small transparent margin. Wardrobe pieces need this:
// the game sizes each piece by its picture's width, so empty margins would shrink the piece.
//
// Usage: node Tools/ChatGPT/trim.mjs <in.png> <out.png> [--pad 4] [--alpha 16]

import fs from "node:fs";
import path from "node:path";
import { decode, encode } from "./pngio.mjs";

const args = process.argv.slice(2);
const [src, dst] = args.filter((a) => !a.startsWith("--") && !/^\d+$/.test(a));
const opt = (name, fallback) => {
  const i = args.indexOf("--" + name);
  return i >= 0 ? parseInt(args[i + 1], 10) : fallback;
};
if (!src || !dst) {
  console.error("Usage: trim.mjs <in.png> <out.png> [--pad 4] [--alpha 16]");
  process.exit(1);
}
const pad = opt("pad", 4), threshold = opt("alpha", 16);

const { w, h, data } = decode(fs.readFileSync(src));
let minX = w, minY = h, maxX = -1, maxY = -1;
for (let y = 0; y < h; y++)
  for (let x = 0; x < w; x++)
    if (data[(y * w + x) * 4 + 3] > threshold) {
      if (x < minX) minX = x; if (x > maxX) maxX = x;
      if (y < minY) minY = y; if (y > maxY) maxY = y;
    }
if (maxX < 0) { console.error("nothing visible in " + src); process.exit(1); }

const x0 = Math.max(0, minX - pad), y0 = Math.max(0, minY - pad);
const x1 = Math.min(w - 1, maxX + pad), y1 = Math.min(h - 1, maxY + pad);
const ow = x1 - x0 + 1, oh = y1 - y0 + 1;
const out = Buffer.alloc(ow * oh * 4);
for (let y = 0; y < oh; y++)
  data.copy(out, y * ow * 4, ((y + y0) * w + x0) * 4, ((y + y0) * w + x1 + 1) * 4);

fs.mkdirSync(path.dirname(dst), { recursive: true });
fs.writeFileSync(dst, encode(ow, oh, out));
console.log(`${path.basename(src)} ${w}x${h} -> ${path.basename(dst)} ${ow}x${oh}`);
