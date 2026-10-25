// Usage: node Tools/ChatGPT/alignfeet.mjs <reference.png> <in.png> <out.png>
// Fits a variant drawn over a master (a holiday skin's lantern, its torii gate) onto the master's
// canvas: one even scale so its opaque width matches the master's, centred the same and standing on
// the same feet. Unlike alignbounds.mjs it never stretches, so snow piled on top only adds height.
// Box-filtered in premultiplied alpha, so a 1254px picture comes down to 256px cleanly.
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
const k = (b.x1 - b.x0 + 1) / (a.x1 - a.x0 + 1);   // source pixels per output pixel
const cxA = (a.x0 + a.x1 + 1) / 2, cxB = (b.x0 + b.x1 + 1) / 2;
const out = Buffer.alloc(ref.w * ref.h * 4);
for (let y = 0; y < ref.h; y++) for (let x = 0; x < ref.w; x++) {
  // this output pixel's footprint in the source
  const u0 = cxB + (x - cxA) * k, u1 = u0 + k;
  const v1 = b.y1 + 1 - (a.y1 + 1 - y) * k, v0 = v1 - k;
  let r = 0, g = 0, bl = 0, al = 0, area = 0;
  for (let yy = Math.floor(v0); yy < Math.ceil(v1); yy++) {
    const wy = Math.min(v1, yy + 1) - Math.max(v0, yy);
    if (wy <= 0) continue;
    for (let xx = Math.floor(u0); xx < Math.ceil(u1); xx++) {
      const wx = Math.min(u1, xx + 1) - Math.max(u0, xx);
      if (wx <= 0) continue;
      const wgt = wx * wy;
      area += wgt;
      if (xx < 0 || yy < 0 || xx >= src.w || yy >= src.h) continue;
      const i = (yy * src.w + xx) * 4, aa = src.data[i + 3] / 255 * wgt;
      r += src.data[i] * aa; g += src.data[i + 1] * aa; bl += src.data[i + 2] * aa; al += aa;
    }
  }
  if (al <= 0 || area <= 0) continue;
  const o = (y * ref.w + x) * 4;
  out[o] = Math.round(r / al); out[o + 1] = Math.round(g / al); out[o + 2] = Math.round(bl / al);
  out[o + 3] = Math.round(Math.min(1, al / area) * 255);
}
fs.writeFileSync(outPath, encode(ref.w, ref.h, out));
console.log(`fitted ${srcPath} -> ${outPath} (x${(1 / k).toFixed(3)})`);
