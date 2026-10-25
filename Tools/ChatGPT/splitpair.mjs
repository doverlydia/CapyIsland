#!/usr/bin/env node
// Splits a two-frame sheet (a flap drawn twice, side by side, in one image) into two frames that
// line up. Each half is cropped to the SAME box — the union of both halves' visible pixels, in each
// half's own coordinates — so swapping the frames in the game moves the wings, not the bird.
// The split is the emptiest column near the middle, not the exact middle, since the model rarely
// centres the two drawings perfectly.
//
// Usage: node Tools/ChatGPT/splitpair.mjs <sheet.png> <left-out.png> <right-out.png> [--pad 6] [--alpha 40]

import fs from "node:fs";
import path from "node:path";
import { decode, encode } from "./pngio.mjs";

const args = process.argv.slice(2);
const files = args.filter((a, i) => !a.startsWith("--") && !(i > 0 && args[i - 1].startsWith("--")));
const [src, leftOut, rightOut] = files;
const opt = (name, fallback) => {
  const i = args.indexOf("--" + name);
  return i >= 0 ? parseInt(args[i + 1], 10) : fallback;
};
if (!src || !leftOut || !rightOut) {
  console.error("Usage: splitpair.mjs <sheet.png> <left-out.png> <right-out.png> [--pad 6] [--alpha 40]");
  process.exit(1);
}
const pad = opt("pad", 6), threshold = opt("alpha", 40);
const { w, h, data } = decode(fs.readFileSync(src));
const solid = (x, y) => data[(y * w + x) * 4 + 3] > threshold;

// The gap between the two drawings: the column with the fewest visible pixels in the middle third.
let split = Math.floor(w / 2), fewest = Infinity;
for (let x = Math.floor(w / 3); x < Math.floor((2 * w) / 3); x++) {
  let n = 0;
  for (let y = 0; y < h; y++) if (solid(x, y)) n++;
  if (n < fewest || (n === fewest && Math.abs(x - w / 2) < Math.abs(split - w / 2))) { fewest = n; split = x; }
}

const bbox = (from, to) => {
  let x0 = Infinity, y0 = Infinity, x1 = -1, y1 = -1;
  for (let y = 0; y < h; y++) for (let x = from; x < to; x++) if (solid(x, y)) {
    const lx = x - from;
    if (lx < x0) x0 = lx; if (lx > x1) x1 = lx; if (y < y0) y0 = y; if (y > y1) y1 = y;
  }
  return { x0, y0, x1, y1 };
};
const a = bbox(0, split), b = bbox(split, w);
if (a.x1 < 0 || b.x1 < 0) { console.error("one half is empty in " + src); process.exit(1); }

// One box for both, in half-local coordinates.
const box = {
  x0: Math.max(0, Math.min(a.x0, b.x0) - pad), y0: Math.max(0, Math.min(a.y0, b.y0) - pad),
  x1: Math.max(a.x1, b.x1) + pad, y1: Math.min(h - 1, Math.max(a.y1, b.y1) + pad),
};
const ow = box.x1 - box.x0 + 1, oh = box.y1 - box.y0 + 1;

const cut = (from, to, dst) => {
  const out = Buffer.alloc(ow * oh * 4);
  for (let y = 0; y < oh; y++) for (let x = 0; x < ow; x++) {
    const sx = from + box.x0 + x, sy = box.y0 + y;
    if (sx < from || sx >= to || sy >= h) continue;   // outside this half: stays transparent
    data.copy(out, (y * ow + x) * 4, (sy * w + sx) * 4, (sy * w + sx) * 4 + 4);
  }
  fs.mkdirSync(path.dirname(dst), { recursive: true });
  fs.writeFileSync(dst, encode(ow, oh, out));
};
cut(0, split, leftOut);
cut(split, w, rightOut);
console.log(`${path.basename(src)} ${w}x${h}, split at ${split} -> two frames ${ow}x${oh}`);
