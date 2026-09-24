#!/usr/bin/env node
// Contact sheet for judging generated art: each PNG is fitted into a square cell and composited
// over the island's grass green (#7FAF4F), since a sprite on a dark background hides halos and
// wrong-coloured edges.
//
// Usage: node Tools/ChatGPT/contact.mjs <out.png> <cell> <a.png> [b.png ...]

import fs from "node:fs";
import { decode, encode } from "./pngio.mjs";

const [, , out, cellArg, ...inputs] = process.argv;
if (!out || !cellArg || inputs.length === 0) {
  console.error("Usage: contact.mjs <out.png> <cell> <a.png> [b.png ...]");
  process.exit(1);
}
const cell = parseInt(cellArg, 10);
const cols = Math.min(4, inputs.length), rows = Math.ceil(inputs.length / cols);
const W = cols * cell, H = rows * cell;
const img = Buffer.alloc(W * H * 4);
for (let i = 0; i < W * H; i++) { img[i * 4] = 0x7f; img[i * 4 + 1] = 0xaf; img[i * 4 + 2] = 0x4f; img[i * 4 + 3] = 255; }

inputs.forEach((file, n) => {
  const { w, h, data } = decode(fs.readFileSync(file));
  const scale = Math.min(cell / w, cell / h);
  const cx = (n % cols) * cell, cy = Math.floor(n / cols) * cell;
  for (let y = 0; y < cell; y++) for (let x = 0; x < cell; x++) {
    const sx = Math.floor(x / scale), sy = Math.floor(y / scale);
    if (sx >= w || sy >= h) continue;
    const s = (sy * w + sx) * 4, A = data[s + 3] / 255;
    const o = ((cy + y) * W + cx + x) * 4;
    for (let c = 0; c < 3; c++) img[o + c] = Math.round(data[s + c] * A + img[o + c] * (1 - A));
  }
});
fs.writeFileSync(out, encode(W, H, img));
console.log(`wrote ${out} ${W}x${H}`);
