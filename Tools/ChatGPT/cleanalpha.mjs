#!/usr/bin/env node
// Cleans the soft semi-transparent halo gpt-image-1 leaves around sprites on a "transparent" background.
// Alpha below `low` becomes fully transparent, alpha above `high` fully opaque, in between is stretched
// linearly, so anti-aliased edges survive but the glow doesn't. Rewrites the file in place.
//
// Usage: node Tools/ArtDirector/cleanalpha.mjs <file.png> [...] [--low 90] [--high 200]

import fs from "node:fs";
import zlib from "node:zlib";

function decode(buf) {
  let p = 8, w, h, ct; const idat = [];
  while (p < buf.length) {
    const len = buf.readUInt32BE(p); const type = buf.toString("ascii", p + 4, p + 8);
    const data = buf.subarray(p + 8, p + 8 + len);
    if (type === "IHDR") { w = data.readUInt32BE(0); h = data.readUInt32BE(4); ct = data[9]; if (data[8] !== 8 || data[12] !== 0) throw new Error("only 8-bit non-interlaced PNG"); }
    else if (type === "IDAT") idat.push(data);
    else if (type === "IEND") break;
    p += 12 + len;
  }
  const bpp = ct === 6 ? 4 : ct === 2 ? 3 : null;
  if (!bpp) throw new Error("color type " + ct + " not supported");
  const raw = zlib.inflateSync(Buffer.concat(idat));
  const stride = w * bpp, out = Buffer.alloc(w * h * 4);
  let prev = Buffer.alloc(stride), q = 0;
  for (let y = 0; y < h; y++) {
    const f = raw[q++]; const line = Buffer.from(raw.subarray(q, q + stride)); q += stride;
    for (let i = 0; i < stride; i++) {
      const a = i >= bpp ? line[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
      let v = line[i];
      if (f === 1) v += a; else if (f === 2) v += b; else if (f === 3) v += (a + b) >> 1;
      else if (f === 4) { const pp = a + b - c, pa = Math.abs(pp - a), pb = Math.abs(pp - b), pc = Math.abs(pp - c); v += (pa <= pb && pa <= pc) ? a : (pb <= pc ? b : c); }
      line[i] = v & 255;
    }
    for (let x = 0; x < w; x++) { const o = (y * w + x) * 4, s = x * bpp; out[o] = line[s]; out[o + 1] = line[s + 1]; out[o + 2] = line[s + 2]; out[o + 3] = bpp === 4 ? line[s + 3] : 255; }
    prev = line;
  }
  return { w, h, data: out };
}

const CRC = new Int32Array(256).map((_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c; });
function crc32(buf) { let c = -1; for (const b of buf) c = CRC[(c ^ b) & 255] ^ (c >>> 8); return (c ^ -1) >>> 0; }
function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type, "ascii"), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
}
function encode(w, h, rgba) {
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) { raw[y * (w * 4 + 1)] = 0; rgba.copy(raw, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4); }
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk("IHDR", ihdr), chunk("IDAT", zlib.deflateSync(raw, { level: 9 })), chunk("IEND", Buffer.alloc(0))]);
}

const argv = process.argv.slice(2);
const files = [], opt = { low: 90, high: 200 };
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === "--low") opt.low = +argv[++i];
  else if (argv[i] === "--high") opt.high = +argv[++i];
  else files.push(argv[i]);
}
for (const file of files) {
  const { w, h, data } = decode(fs.readFileSync(file));
  let changed = 0;
  for (let i = 3; i < data.length; i += 4) {
    const a = data[i];
    const na = a <= opt.low ? 0 : a >= opt.high ? 255 : Math.round((a - opt.low) * 255 / (opt.high - opt.low));
    if (na !== a) { data[i] = na; changed++; }
  }
  fs.writeFileSync(file, encode(w, h, data));
  console.log(`${file}: ${w}x${h}, ${changed} alpha values adjusted`);
}
