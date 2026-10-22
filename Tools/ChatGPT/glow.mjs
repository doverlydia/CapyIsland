#!/usr/bin/env node
// Bakes a soft golden glow under a transparent sprite: blurs its alpha, tints it warm gold and
// lays it behind, so the piece shines on any background (Horaira's halo). Trim the result with a
// low alpha threshold (trim.mjs --alpha 6) or the glow is cut off.
//
// Usage: node Tools/ChatGPT/glow.mjs <in.png> <out.png> [radius=26] [strength=0.9]
import fs from "node:fs";
import { decode, encode } from "./pngio.mjs";

const [src, out, radiusArg, strengthArg] = process.argv.slice(2);
const img = decode(fs.readFileSync(src));
const { w, h, data } = img;
const radius = Number(radiusArg || 26), strength = Number(strengthArg || 0.9);

// alpha mask, padded so the glow has room
const pad = radius * 3;
const W = w + pad * 2, H = h + pad * 2;
let a = new Float32Array(W * H);
for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) a[(y + pad) * W + x + pad] = data[(y * w + x) * 4 + 3] / 255;

function boxH(src, r) {
  const dst = new Float32Array(src.length);
  for (let y = 0; y < H; y++) {
    let s = 0; const row = y * W;
    for (let x = -r; x <= r; x++) s += src[row + Math.min(W - 1, Math.max(0, x))];
    for (let x = 0; x < W; x++) {
      dst[row + x] = s / (2 * r + 1);
      s += src[row + Math.min(W - 1, x + r + 1)] - src[row + Math.max(0, x - r)];
    }
  }
  return dst;
}
function boxV(src, r) {
  const dst = new Float32Array(src.length);
  for (let x = 0; x < W; x++) {
    let s = 0;
    for (let y = -r; y <= r; y++) s += src[Math.min(H - 1, Math.max(0, y)) * W + x];
    for (let y = 0; y < H; y++) {
      dst[y * W + x] = s / (2 * r + 1);
      s += src[Math.min(H - 1, y + r + 1) * W + x] - src[Math.max(0, y - r) * W + x];
    }
  }
  return dst;
}
let g = a;
for (let i = 0; i < 3; i++) g = boxV(boxH(g, radius), radius);

const glow = [255, 222, 120];
const o = Buffer.alloc(W * H * 4);
for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
  const i = y * W + x;
  const ga = Math.min(1, g[i] * 2.2) * strength;          // the glow
  let r = glow[0], gg = glow[1], b = glow[2], al = ga;
  const sx = x - pad, sy = y - pad;
  if (sx >= 0 && sy >= 0 && sx < w && sy < h) {
    const k = (sy * w + sx) * 4, sa = data[k + 3] / 255;    // the sprite over it
    const outA = sa + al * (1 - sa);
    if (outA > 0) {
      r = (data[k] * sa + r * al * (1 - sa)) / outA;
      gg = (data[k + 1] * sa + gg * al * (1 - sa)) / outA;
      b = (data[k + 2] * sa + b * al * (1 - sa)) / outA;
    }
    al = outA;
  }
  o[i * 4] = Math.round(r); o[i * 4 + 1] = Math.round(gg); o[i * 4 + 2] = Math.round(b); o[i * 4 + 3] = Math.round(al * 255);
}
fs.writeFileSync(out, encode(W, H, o));
console.log("glow", W + "x" + H, "radius", radius);
