#!/usr/bin/env node
// Downscales a PNG to the size it actually renders at, so shipping art is not 1024x1024.
// Box-filters in PREMULTIPLIED alpha then un-premultiplies, which is what stops the dark fringe
// you get from averaging colour across transparent pixels.
//
// Usage: node Tools/ChatGPT/downscale.mjs <in.png> <out.png> <size>

import fs from "node:fs";
import path from "node:path";
import zlib from "node:zlib";

function decode(buf) {
  let p = 8, w, h, ct; const idat = [];
  while (p < buf.length) {
    const len = buf.readUInt32BE(p);
    const type = buf.toString("ascii", p + 4, p + 8);
    const d = buf.subarray(p + 8, p + 8 + len);
    if (type === "IHDR") {
      w = d.readUInt32BE(0); h = d.readUInt32BE(4); ct = d[9];
      if (d[8] !== 8 || d[12] !== 0) throw new Error("only 8-bit non-interlaced PNG");
    } else if (type === "IDAT") idat.push(d);
    else if (type === "IEND") break;
    p += 12 + len;
  }
  const bpp = ct === 6 ? 4 : ct === 2 ? 3 : null;
  if (!bpp) throw new Error("color type " + ct + " not supported");
  const raw = zlib.inflateSync(Buffer.concat(idat));
  const stride = w * bpp, out = Buffer.alloc(w * h * 4);
  let prev = Buffer.alloc(stride), q = 0;
  for (let y = 0; y < h; y++) {
    const f = raw[q++];
    const line = Buffer.from(raw.subarray(q, q + stride)); q += stride;
    for (let i = 0; i < stride; i++) {
      const a = i >= bpp ? line[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
      let v = line[i];
      if (f === 1) v += a; else if (f === 2) v += b; else if (f === 3) v += (a + b) >> 1;
      else if (f === 4) {
        const pp = a + b - c, pa = Math.abs(pp - a), pb = Math.abs(pp - b), pc = Math.abs(pp - c);
        v += (pa <= pb && pa <= pc) ? a : (pb <= pc ? b : c);
      }
      line[i] = v & 255;
    }
    for (let x = 0; x < w; x++) {
      const o = (y * w + x) * 4, s = x * bpp;
      out[o] = line[s]; out[o + 1] = line[s + 1]; out[o + 2] = line[s + 2];
      out[o + 3] = bpp === 4 ? line[s + 3] : 255;
    }
    prev = line;
  }
  return { w, h, data: out };
}

const CRC = new Int32Array(256).map((_, n) => {
  let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c;
});
const crc32 = b => { let c = -1; for (const x of b) c = CRC[(c ^ x) & 255] ^ (c >>> 8); return (c ^ -1) >>> 0; };
function chunk(t, d) {
  const l = Buffer.alloc(4); l.writeUInt32BE(d.length);
  const td = Buffer.concat([Buffer.from(t, "ascii"), d]);
  const cr = Buffer.alloc(4); cr.writeUInt32BE(crc32(td));
  return Buffer.concat([l, td, cr]);
}
function encode(w, h, rgba) {
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) {
    raw[y * (w * 4 + 1)] = 0;
    rgba.copy(raw, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4);
  }
  const ih = Buffer.alloc(13);
  ih.writeUInt32BE(w, 0); ih.writeUInt32BE(h, 4); ih[8] = 8; ih[9] = 6;
  return Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    chunk("IHDR", ih),
    chunk("IDAT", zlib.deflateSync(raw, { level: 9 })),
    chunk("IEND", Buffer.alloc(0)),
  ]);
}

const [, , src, dst, sizeArg] = process.argv;
if (!src || !dst || !sizeArg) {
  console.error("Usage: downscale.mjs <in.png> <out.png> <size>");
  process.exit(1);
}
const target = parseInt(sizeArg, 10);
const { w, h, data } = decode(fs.readFileSync(src));
const ow = target, oh = Math.round(h * target / w);
const sx = w / ow, sy = h / oh;
const out = Buffer.alloc(ow * oh * 4);

for (let y = 0; y < oh; y++) {
  const y0 = Math.floor(y * sy), y1 = Math.min(h, Math.ceil((y + 1) * sy));
  for (let x = 0; x < ow; x++) {
    const x0 = Math.floor(x * sx), x1 = Math.min(w, Math.ceil((x + 1) * sx));
    let r = 0, g = 0, b = 0, a = 0, n = 0;
    for (let yy = y0; yy < y1; yy++) {
      for (let xx = x0; xx < x1; xx++) {
        const i = (yy * w + xx) * 4;
        const A = data[i + 3] / 255;
        r += data[i] * A; g += data[i + 1] * A; b += data[i + 2] * A; a += A;
        n++;
      }
    }
    const o = (y * ow + x) * 4;
    if (n === 0 || a === 0) { out[o] = out[o + 1] = out[o + 2] = out[o + 3] = 0; continue; }
    // un-premultiply: divide accumulated colour by accumulated alpha, not by sample count
    out[o] = Math.min(255, Math.round(r / a));
    out[o + 1] = Math.min(255, Math.round(g / a));
    out[o + 2] = Math.min(255, Math.round(b / a));
    out[o + 3] = Math.round(255 * a / n);
  }
}

fs.mkdirSync(path.dirname(dst), { recursive: true });
fs.writeFileSync(dst, encode(ow, oh, out));
const inKB = (fs.statSync(src).size / 1024).toFixed(0), outKB = (fs.statSync(dst).size / 1024).toFixed(0);
console.log(`${path.basename(src)} ${w}x${h} (${inKB}KB) -> ${path.basename(dst)} ${ow}x${oh} (${outKB}KB)`);
