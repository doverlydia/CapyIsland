#!/usr/bin/env node
// Synthesises the game's small sound effects as 16-bit mono WAVs in Assets/Audio/. Soft, short and
// rounded — wooden taps, bubbly pops, a little chime — so they sit under play rather than on top
// of it. Procedural, like the particle sprites: no recorded or third-party audio.
//
// Usage: node Tools/fx/sfx.mjs

import fs from "node:fs";

const RATE = 22050;
let seed = 12345;
const rnd = () => ((seed = (seed * 1103515245 + 12345) >>> 0) / 4294967296) * 2 - 1;

function write(name, samples, dir = "Assets/Audio") {
  const n = samples.length, data = Buffer.alloc(44 + n * 2);
  data.write("RIFF", 0); data.writeUInt32LE(36 + n * 2, 4); data.write("WAVE", 8);
  data.write("fmt ", 12); data.writeUInt32LE(16, 16); data.writeUInt16LE(1, 20); data.writeUInt16LE(1, 22);
  data.writeUInt32LE(RATE, 24); data.writeUInt32LE(RATE * 2, 28); data.writeUInt16LE(2, 32); data.writeUInt16LE(16, 34);
  data.write("data", 36); data.writeUInt32LE(n * 2, 40);
  let peak = 0; for (const s of samples) peak = Math.max(peak, Math.abs(s));
  const gain = peak > 0 ? 0.9 / peak : 1;
  for (let i = 0; i < n; i++) data.writeInt16LE(Math.round(Math.max(-1, Math.min(1, samples[i] * gain)) * 32767), 44 + i * 2);
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(`${dir}/${name}.wav`, data);
  console.log("wrote", name, (n / RATE).toFixed(2) + "s");
}

const make = (seconds, fn) => {
  const out = new Float32Array(Math.round(seconds * RATE));
  for (let i = 0; i < out.length; i++) out[i] = fn(i / RATE, i);
  return out;
};
const env = (t, attack, decay) => (t < attack ? t / attack : Math.exp(-(t - attack) / decay));
// One-pole low-pass over a signal generator, for soft noise.
function lowpassNoise(cut) {
  let y = 0; const a = Math.exp(-2 * Math.PI * cut / RATE);
  return () => (y = (1 - a) * rnd() + a * y);
}

// Axe on wood: a dull knock plus a short woody noise burst.
{ const noise = lowpassNoise(1800);
  write("Sfx_Chop", make(0.22, (t) => {
    const knock = Math.sin(2 * Math.PI * (180 - 120 * t) * t) * env(t, 0.002, 0.05);
    return knock * 0.9 + noise() * 2.5 * env(t, 0.001, 0.025);
  })); }

// Pickaxe on rock: a brighter clink over a thud.
{ const noise = lowpassNoise(4000);
  write("Sfx_Mine", make(0.3, (t) => {
    const clink = (Math.sin(2 * Math.PI * 1320 * t) + 0.5 * Math.sin(2 * Math.PI * 1980 * t)) * env(t, 0.001, 0.07);
    return clink * 0.55 + noise() * 2.0 * env(t, 0.001, 0.02) + Math.sin(2 * Math.PI * 120 * t) * env(t, 0.002, 0.04) * 0.6;
  })); }

// A nibble: two soft crunches.
{ const noise = lowpassNoise(2500);
  write("Sfx_Nibble", make(0.26, (t) => {
    const a = env(t, 0.002, 0.03), b = t > 0.12 ? env(t - 0.12, 0.002, 0.03) : 0;
    return noise() * 3 * (a + b * 0.8);
  })); }

// Leaves rustling as a tree is shaken.
{ const noise = lowpassNoise(3200);
  write("Sfx_Rustle", make(0.55, (t) => {
    const flutter = 0.6 + 0.4 * Math.sin(2 * Math.PI * 23 * t) * Math.sin(2 * Math.PI * 7 * t);
    return noise() * 2.2 * flutter * env(t, 0.06, 0.18);
  })); }

// A fruit landing: a round, juicy "plop".
write("Sfx_Plop", make(0.2, (t) => {
  const f = 520 * Math.exp(-t * 18) + 140;
  return Math.sin(2 * Math.PI * f * t) * env(t, 0.003, 0.06);
}));

// A bubble popping: a quick upward chirp.
write("Sfx_Pop", make(0.14, (t) => {
  const f = 600 + 1900 * t / 0.14;
  return Math.sin(2 * Math.PI * f * t) * env(t, 0.002, 0.035);
}));

// Treasure chest: a bright rising arpeggio with a shimmer.
write("Sfx_Chest", make(0.75, (t) => {
  const notes = [784, 988, 1175, 1568];
  let s = 0;
  notes.forEach((f, k) => {
    const start = k * 0.08; if (t < start) return;
    const u = t - start;
    s += (Math.sin(2 * Math.PI * f * u) + 0.3 * Math.sin(2 * Math.PI * f * 2 * u)) * env(u, 0.004, 0.22);
  });
  return s * 0.5;
}));

// Coins or wood landing in the counter: a tiny tick.
write("Sfx_Coin", make(0.12, (t) => (Math.sin(2 * Math.PI * 1760 * t) + 0.4 * Math.sin(2 * Math.PI * 2640 * t)) * env(t, 0.001, 0.03)));

// A purchase: two warm notes.
write("Sfx_Buy", make(0.35, (t) => {
  const a = Math.sin(2 * Math.PI * 660 * t) * env(t, 0.004, 0.08);
  const b = t > 0.09 ? Math.sin(2 * Math.PI * 990 * (t - 0.09)) * env(t - 0.09, 0.004, 0.14) : 0;
  return a + b;
}));

// A bit of love: a soft bell chord.
write("Sfx_Heart", make(1.1, (t) => {
  let s = 0;
  for (const f of [523, 659, 784, 1047]) s += Math.sin(2 * Math.PI * f * t) * Math.exp(-t / 0.45);
  return s * env(t, 0.01, 10);
}));

// A bridge rising: a slow shimmering sweep.
{ const noise = lowpassNoise(900);
  write("Sfx_Bridge", make(1.6, (t) => {
    const f = 220 + 330 * t / 1.6;
    const tone = Math.sin(2 * Math.PI * f * t) + 0.5 * Math.sin(2 * Math.PI * f * 1.5 * t);
    return tone * 0.4 * env(t, 0.5, 0.5) + noise() * 1.2 * env(t, 0.3, 0.6);
  })); }

// A bomb: a low boom with a noisy crack.
{ const noise = lowpassNoise(1200);
  write("Sfx_Boom", make(0.9, (t) => {
    const boom = Math.sin(2 * Math.PI * (70 - 30 * t) * t) * env(t, 0.005, 0.25);
    return boom * 1.2 + noise() * 3 * env(t, 0.002, 0.18);
  })); }

// A fuse fizzing.
{ const noise = lowpassNoise(6000);
  write("Sfx_Fuse", make(0.5, (t) => noise() * 1.5 * (0.7 + 0.3 * Math.sin(2 * Math.PI * 31 * t)) * env(t, 0.02, 2))); }

// A tree falling: a creak and a leafy thump.
{ const noise = lowpassNoise(700);
  write("Sfx_Fell", make(0.7, (t) => {
    const creak = Math.sin(2 * Math.PI * (300 - 180 * t) * t) * env(t, 0.02, 0.12) * 0.5;
    const thump = t > 0.35 ? (Math.sin(2 * Math.PI * 90 * (t - 0.35)) * 1.2 + noise() * 3) * env(t - 0.35, 0.004, 0.12) : 0;
    return creak + thump;
  })); }

// UI tap: a soft wooden tick.
write("Sfx_Tap", make(0.07, (t) => Math.sin(2 * Math.PI * 900 * t) * env(t, 0.001, 0.015)));

// A cat asking for fish: "mee-ow". A buzzy voice through two moving vowel resonances, the pitch
// rising then falling, the mouth opening from "ee" to "ow". Loaded at runtime from Resources
// (it arrived after the scene's clip list was baked).
{ const D = 0.62, breath = lowpassNoise(3000);
  const lerp = (a, b, k) => a + (b - a) * Math.max(0, Math.min(1, k));
  const resonator = () => { let y1 = 0, y2 = 0;
    return (x, f, bw) => { const r = Math.exp(-Math.PI * bw / RATE), a1 = -2 * r * Math.cos(2 * Math.PI * f / RATE), a2 = r * r;
      const y = (1 - r) * x - a1 * y1 - a2 * y2; y2 = y1; y1 = y; return y; }; };
  const r1 = resonator(), r2 = resonator(), r3 = resonator();
  let phase = 0;
  write("Sfx_Meow", make(D, (t) => {
    const u = t / D;
    const f0 = (480 + 330 * Math.sin(Math.PI * Math.pow(u, 0.7))) * (1 + 0.02 * Math.sin(2 * Math.PI * 6 * t));
    phase += f0 / RATE;
    let src = 0;
    for (let k = 1; k * f0 < 5000; k++) src += Math.sin(2 * Math.PI * k * phase) / k;
    src += breath() * 0.6;
    const F1 = u < 0.5 ? lerp(380, 950, u / 0.5) : lerp(950, 520, (u - 0.5) / 0.5);
    const F2 = u < 0.4 ? lerp(2300, 1500, u / 0.4) : lerp(1500, 900, (u - 0.4) / 0.6);
    const voice = r1(src, F1, 140) + 0.7 * r2(src, F2, 180) + 0.25 * r3(src, 3200, 300);
    const amp = Math.min(1, t / 0.09) * (u > 0.65 ? Math.pow(1 - (u - 0.65) / 0.35, 1.5) : 1);
    return voice * amp;
  }), "Assets/Resources/Sfx"); }
