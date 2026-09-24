#!/usr/bin/env node
// Generates the small particle sprites HarvestEffects emits: leaf, wood chip, petal, grit, sparkle
// and juice droplet. They are drawn white-ish so the particle system can tint them per burst.
// Procedural rather than AI-generated: at 32-64px they are simple shapes, and generating them keeps
// them pixel-consistent with each other.
//
// Usage: node Tools/fx/particles.mjs   (writes Assets/Art/FX/*.png)

import fs from "node:fs";
import zlib from "node:zlib";

const CRC = new Int32Array(256).map((_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c; });
const crc32 = b => { let c = -1; for (const x of b) c = CRC[(c ^ x) & 255] ^ (c >>> 8); return (c ^ -1) >>> 0; };
const chunk = (t, d) => { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t, "ascii"), d]); const c = Buffer.alloc(4); c.writeUInt32BE(crc32(td)); return Buffer.concat([l, td, c]); };
const sm = t => { t = Math.max(0, Math.min(1, t)); return t * t * (3 - 2 * t); };

function png(name, S, shade) {
  const px = Buffer.alloc(S * S * 4);
  for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) {
    const u = (x + 0.5) / S * 2 - 1, v = (y + 0.5) / S * 2 - 1;   // -1..1, v down
    const [lum, a] = shade(u, v);
    const o = (y * S + x) * 4, L = Math.round(255 * lum);
    px[o] = L; px[o + 1] = L; px[o + 2] = L; px[o + 3] = Math.round(255 * Math.max(0, Math.min(1, a)));
  }
  const raw = Buffer.alloc((S * 4 + 1) * S);
  for (let y = 0; y < S; y++) { raw[y * (S * 4 + 1)] = 0; px.copy(raw, y * (S * 4 + 1) + 1, y * S * 4, (y + 1) * S * 4); }
  const ih = Buffer.alloc(13); ih.writeUInt32BE(S, 0); ih.writeUInt32BE(S, 4); ih[8] = 8; ih[9] = 6;
  fs.mkdirSync("Assets/Art/FX", { recursive: true });
  fs.writeFileSync(`Assets/Art/FX/${name}.png`, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk("IHDR", ih), chunk("IDAT", zlib.deflateSync(raw, { level: 9 })), chunk("IEND", Buffer.alloc(0))]));
  console.log("wrote", name, S + "px");
}

// Leaf: a pointed almond with a pale midrib, lit from the top-left.
png("FX_Leaf", 64, (u, v) => {
  const w = 0.55 * (1 - v * v);                   // width tapers to both tips
  const d = Math.abs(u) - w;
  const a = sm(-d / 0.08) * sm((1 - Math.abs(v)) / 0.06);
  const rib = Math.abs(u) < 0.05 ? 0.12 : 0;
  const lit = 0.82 + 0.18 * (-u - v) * 0.5;
  return [Math.min(1, lit + rib), a];
});

// Wood chip: a small rounded sliver with a lighter cut face.
png("FX_Chip", 32, (u, v) => {
  const d = Math.max(Math.abs(u) / 0.8, Math.abs(v) / 0.42);
  const a = sm((1 - d) / 0.12);
  return [0.78 + (u > 0.35 ? 0.2 : 0), a];
});

// Petal: a soft teardrop, rounder at the top.
png("FX_Petal", 48, (u, v) => {
  const w = 0.62 * Math.sqrt(Math.max(0, (1 - v) / 2)) * (1 + v * 0.15);
  const d = Math.abs(u) - w;
  const a = sm(-d / 0.1) * sm((1 - Math.abs(v)) / 0.08);
  return [0.9 + 0.1 * (1 - Math.abs(u)), a];
});

// Grit: a lumpy little pebble.
png("FX_Grit", 32, (u, v) => {
  const ang = Math.atan2(v, u), r = Math.hypot(u, v);
  const edge = 0.72 + 0.12 * Math.sin(ang * 3) + 0.06 * Math.cos(ang * 5);
  return [0.75 + 0.2 * (-u - v) * 0.5, sm((edge - r) / 0.12)];
});

// Sparkle: a four-point star with a bright core.
png("FX_Sparkle", 48, (u, v) => {
  const star = Math.max(sm((0.14 - Math.abs(u) * (1 + Math.abs(v) * 6)) / 0.06),
                        sm((0.14 - Math.abs(v) * (1 + Math.abs(u) * 6)) / 0.06));
  const core = sm((0.35 - Math.hypot(u, v)) / 0.3);
  return [1, Math.max(star, core * 0.9)];
});

// Juice droplet: a round bead with a highlight, tinted with the fruit's colour.
png("FX_Juice", 32, (u, v) => {
  const r = Math.hypot(u, v * 0.9);
  const hl = sm((0.28 - Math.hypot(u + 0.3, v + 0.3)) / 0.18);
  return [Math.min(1, 0.85 + hl * 0.4), sm((0.85 - r) / 0.12)];
});
