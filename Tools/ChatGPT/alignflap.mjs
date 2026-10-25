#!/usr/bin/env node
// Lines up a two-frame flap on the part of the creature that doesn't move. splitpair.mjs crops both
// halves to one box, but the model never draws the two creatures at quite the same spot in their
// halves, so the body hops each time the game swaps frames — it reads as shaking.
//
// Matching whole silhouettes fails (a big wing outweighs a small body), so this matches the picture
// itself — colours — over one END of the creature: its head and beak, or its tail, or a bug's head or
// abdomen. Only the wings change between the two drawings, and they're in the middle. Each end is
// tried and the one that matches best wins; then frame two is slid onto frame one and both are
// cropped to one shared box again. Rewrites both files in place.
//
// A bug drawn from above (butterfly, moth, dragonfly) is matched another way, with --symmetric: its
// wings fold over its body, so an end can match a wing; instead the two are centred on their middle
// line (the wings are mirror images, so the middle of the picture is the body) and matched up and down
// on a thin strip along it: the body itself.
//
// Usage: node Tools/ChatGPT/alignflap.mjs <fly1.png> <fly2.png> [--symmetric] [--pad 6] [--alpha 40]

import fs from "node:fs";
import { decode, encode } from "./pngio.mjs";

const args = process.argv.slice(2);
const flags = ["--symmetric"];
const files = args.filter((a, i) => !a.startsWith("--") && !(i > 0 && args[i - 1].startsWith("--") && !flags.includes(args[i - 1])));
const symmetric = args.includes("--symmetric");
const opt = (name, fallback) => {
  const i = args.indexOf("--" + name);
  return i >= 0 ? parseInt(args[i + 1], 10) : fallback;
};
const [aPath, bPath] = files;
if (!aPath || !bPath) {
  console.error("Usage: alignflap.mjs <fly1.png> <fly2.png> [--pad 6] [--alpha 40]");
  process.exit(1);
}
const pad = opt("pad", 6), threshold = opt("alpha", 40);
const A = decode(fs.readFileSync(aPath)), B = decode(fs.readFileSync(bPath));

// Premultiplied colour, shrunk by `s` (box average) for the coarse search.
const shrink = (img, s) => {
  const w = Math.ceil(img.w / s), h = Math.ceil(img.h / s), px = new Float32Array(w * h * 4), n = new Float32Array(w * h);
  for (let y = 0; y < img.h; y++) for (let x = 0; x < img.w; x++) {
    const i = (y * img.w + x) * 4, o = Math.floor(y / s) * w + Math.floor(x / s), a = img.data[i + 3] / 255;
    px[o * 4] += img.data[i] * a; px[o * 4 + 1] += img.data[i + 1] * a; px[o * 4 + 2] += img.data[i + 2] * a; px[o * 4 + 3] += 255 * a;
    n[o]++;
  }
  for (let o = 0; o < w * h; o++) for (let c = 0; c < 4; c++) px[o * 4 + c] /= Math.max(1, n[o]);
  return { w, h, px };
};
const bbox = (m) => {
  let x0 = m.w, y0 = m.h, x1 = -1, y1 = -1;
  for (let y = 0; y < m.h; y++) for (let x = 0; x < m.w; x++) if (m.px[(y * m.w + x) * 4 + 3] > threshold) {
    if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y;
  }
  return { x0, y0, x1, y1 };
};
// The middle line of a picture: where its painted pixels balance left to right.
const middle = (m) => {
  let sum = 0, n = 0;
  for (let y = 0; y < m.h; y++) for (let x = 0; x < m.w; x++) { const a = m.px[(y * m.w + x) * 4 + 3]; sum += x * a; n += a; }
  return sum / Math.max(1, n);
};
// Where to match in frame one: its four ends, a quarter of its box deep — or, for a bug seen from
// above, the strip down its middle.
const windows = (m) => {
  const b = bbox(m), qw = Math.round((b.x1 - b.x0) * 0.25), qh = Math.round((b.y1 - b.y0) * 0.25);
  if (symmetric) {
    const mid = Math.round(middle(m)), half = Math.max(1, Math.round((b.x1 - b.x0) * 0.05));
    return { middle: { x0: mid - half, x1: mid + half, y0: b.y0, y1: b.y1 } };
  }
  return {
    left: { x0: b.x0, x1: b.x0 + qw, y0: b.y0, y1: b.y1 },
    right: { x0: b.x1 - qw, x1: b.x1, y0: b.y0, y1: b.y1 },
    top: { x0: b.x0, x1: b.x1, y0: b.y0, y1: b.y0 + qh },
    bottom: { x0: b.x0, x1: b.x1, y0: b.y1 - qh, y1: b.y1 },
  };
};
// How different frame one's window is from frame two slid by (dx, dy): mean squared colour
// difference over the window's pixels that either frame paints.
const differ = (a, b, win, dx, dy) => {
  let sum = 0, n = 0;
  for (let y = win.y0; y <= win.y1; y++) for (let x = win.x0; x <= win.x1; x++) {
    const ia = (y * a.w + x) * 4, bx = x - dx, by = y - dy;
    const inB = bx >= 0 && by >= 0 && bx < b.w && by < b.h, ib = (by * b.w + bx) * 4;
    const aa = a.px[ia + 3], ba = inB ? b.px[ib + 3] : 0;
    if (aa <= threshold && ba <= threshold) continue;
    for (let c = 0; c < 4; c++) { const d = a.px[ia + c] - (inB ? b.px[ib + c] : 0); sum += d * d; }
    n++;
  }
  return n > 0 ? sum / n : Infinity;
};
const search = (a, b, win, cx, cy, reach) => {
  let low = Infinity, bx = cx, by = cy;
  const sideways = symmetric ? 0 : reach;
  for (let dy = cy - reach; dy <= cy + reach; dy++) for (let dx = cx - sideways; dx <= cx + sideways; dx++) {
    const d = differ(a, b, win, dx, dy);
    if (d < low) { low = d; bx = dx; by = dy; }
  }
  return { dx: bx, dy: by, low };
};

const s = 4, smallA = shrink(A, s), smallB = shrink(B, s), fullA = shrink(A, 1), fullB = shrink(B, 1);
const reach = Math.ceil(Math.max(A.w, A.h) / s / 5);
const across = symmetric ? Math.round(middle(fullA) - middle(fullB)) : 0;
let best = null;
for (const [name, win] of Object.entries(windows(smallA))) {
  const coarse = search(smallA, smallB, win, Math.round(across / s), 0, reach);
  if (!best || coarse.low < best.low) best = { ...coarse, name };
}
const fullWin = windows(fullA)[best.name];
const { dx, dy } = search(fullA, fullB, fullWin, symmetric ? across : best.dx * s, best.dy * s, s);

// One canvas holding A at the origin and B slid by (dx, dy); then crop both to their union.
const x0c = Math.min(0, dx), y0c = Math.min(0, dy);
const W = Math.max(A.w, B.w + dx) - x0c, H = Math.max(A.h, B.h + dy) - y0c;
const place = (img, ox, oy) => {
  const out = Buffer.alloc(W * H * 4);
  for (let y = 0; y < img.h; y++) img.data.copy(out, ((y + oy) * W + ox) * 4, y * img.w * 4, (y + 1) * img.w * 4);
  return out;
};
const pa = place(A, -x0c, -y0c), pb = place(B, dx - x0c, dy - y0c);
let bx0 = W, by0 = H, bx1 = -1, by1 = -1;
for (const p of [pa, pb])
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) if (p[(y * W + x) * 4 + 3] > threshold) {
    if (x < bx0) bx0 = x; if (x > bx1) bx1 = x; if (y < by0) by0 = y; if (y > by1) by1 = y;
  }
bx0 = Math.max(0, bx0 - pad); by0 = Math.max(0, by0 - pad);
bx1 = Math.min(W - 1, bx1 + pad); by1 = Math.min(H - 1, by1 + pad);
const cw = bx1 - bx0 + 1, ch = by1 - by0 + 1;
const crop = (p) => {
  const out = Buffer.alloc(cw * ch * 4);
  for (let y = 0; y < ch; y++) p.copy(out, y * cw * 4, ((y + by0) * W + bx0) * 4, ((y + by0) * W + bx0 + cw) * 4);
  return out;
};
fs.writeFileSync(aPath, encode(cw, ch, crop(pa)));
fs.writeFileSync(bPath, encode(cw, ch, crop(pb)));
console.log(`${aPath}: matched on the ${best.name} end, second frame moved ${dx},${dy} -> ${cw}x${ch}`);
