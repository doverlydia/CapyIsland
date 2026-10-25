// Usage: node Tools/ChatGPT/alignbounds.mjs <reference.png> <in.png> <out.png>
// Moves and scales <in> so its opaque bounds match <reference>'s exactly (a variant drawn over a master
// lands a little off; the game swaps them in place, so they must line up).
import fs from "node:fs";
import { decode, encode } from "./pngio.mjs";
const [, , refPath, srcPath, outPath] = process.argv;
const bbox = ({ w, h, data }) => {
  let x0 = w, y0 = h, x1 = -1, y1 = -1;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) if (data[(y * w + x) * 4 + 3] > 40) {
    if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
  return { x0, y0, x1, y1 };
};
const ref = decode(fs.readFileSync(refPath)), src = decode(fs.readFileSync(srcPath));
const a = bbox(ref), b = bbox(src);
console.log("warm", a, "cold", b);
const out = Buffer.alloc(ref.w * ref.h * 4);
const sx = (b.x1 - b.x0) / (a.x1 - a.x0), sy = (b.y1 - b.y0) / (a.y1 - a.y0);
const sample = (x, y, c) => {
  const x0 = Math.floor(x), y0 = Math.floor(y), fx = x - x0, fy = y - y0;
  const px = (xx, yy) => (xx < 0 || yy < 0 || xx >= src.w || yy >= src.h) ? 0 : src.data[(yy * src.w + xx) * 4 + c];
  return px(x0, y0) * (1 - fx) * (1 - fy) + px(x0 + 1, y0) * fx * (1 - fy) + px(x0, y0 + 1) * (1 - fx) * fy + px(x0 + 1, y0 + 1) * fx * fy;
};
for (let y = 0; y < ref.h; y++) for (let x = 0; x < ref.w; x++) {
  const u = b.x0 + (x - a.x0) * sx, v = b.y0 + (y - a.y0) * sy;
  const o = (y * ref.w + x) * 4;
  const al = sample(u, v, 3);
  if (al <= 0) continue;
  // un-premultiplied colour from alpha-weighted samples
  for (let c = 0; c < 3; c++) {
    const x0 = Math.floor(u), y0 = Math.floor(v), fx = u - x0, fy = v - y0;
    let num = 0, den = 0;
    for (const [xx, yy, wgt] of [[x0, y0, (1 - fx) * (1 - fy)], [x0 + 1, y0, fx * (1 - fy)], [x0, y0 + 1, (1 - fx) * fy], [x0 + 1, y0 + 1, fx * fy]]) {
      if (xx < 0 || yy < 0 || xx >= src.w || yy >= src.h) continue;
      const i = (yy * src.w + xx) * 4, aa = src.data[i + 3] * wgt; num += src.data[i + c] * aa; den += aa;
    }
    out[o + c] = den > 0 ? Math.round(num / den) : 0;
  }
  out[o + 3] = Math.round(al);
}
fs.writeFileSync(outPath, encode(ref.w, ref.h, out));
console.log("aligned ->", outPath);
