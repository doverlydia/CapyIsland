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

function write(name, samples, dir = "Assets/Audio", peakTo = 0.9) {
  const n = samples.length, data = Buffer.alloc(44 + n * 2);
  data.write("RIFF", 0); data.writeUInt32LE(36 + n * 2, 4); data.write("WAVE", 8);
  data.write("fmt ", 12); data.writeUInt32LE(16, 16); data.writeUInt16LE(1, 20); data.writeUInt16LE(1, 22);
  data.writeUInt32LE(RATE, 24); data.writeUInt32LE(RATE * 2, 28); data.writeUInt16LE(2, 32); data.writeUInt16LE(16, 34);
  data.write("data", 36); data.writeUInt32LE(n * 2, 40);
  let peak = 0; for (const s of samples) peak = Math.max(peak, Math.abs(s));
  const gain = peak > 0 ? peakTo / peak : 1;
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

// ---------------------------------------------------------------- ambience
// Loaded at runtime from Resources by Ambience.cs. Appended after everything above so the seeded
// noise those sounds use is unchanged — regenerating keeps every earlier file byte-identical.

// Soft noise: white noise through several low-passes in a row (a steep roll-off, so no hiss up
// top) and a gentle high-pass (so no rumble a phone speaker cannot play): a band of soft air in
// the middle. The top of the band may move, which is how a wave "opens" at its crest.
function softNoise(stages, low = 180) {
  const y = new Array(stages).fill(0); let hp = 0;
  const b = Math.exp(-2 * Math.PI * low / RATE);
  return (cut) => {
    const a = Math.exp(-2 * Math.PI * cut / RATE);
    let x = rnd();
    for (let i = 0; i < stages; i++) x = y[i] = (1 - a) * x + a * y[i];
    hp = (1 - b) * x + b * hp;
    return x - hp;
  };
}

// A seamless loop: render a little extra and cross-fade the tail into the head.
function loop(seconds, xfade, fn) {
  const n = Math.round(seconds * RATE), x = Math.round(xfade * RATE);
  const raw = make(seconds + xfade, fn);
  const out = new Float32Array(n);
  for (let i = 0; i < n; i++) out[i] = raw[i];
  for (let i = 0; i < x; i++) {
    const k = i / x;
    out[i] = raw[i] * Math.sqrt(k) + raw[n + i] * Math.sqrt(1 - k);
  }
  return out;
}

// Gentle waves: a low, soft wash that swells and draws back three times a loop. The filter opens
// only a little at each crest, so it reads as water on sand rather than static.
{ const body = softNoise(3, 160), foam = softNoise(4, 300);
  const swells = [0.0, 4.6, 9.0], L = 13.5;
  const swell = (t) => {
    let a = 0;
    for (const s of swells) for (const off of [0, L]) {
      const u = t - s - off;
      if (u < 0 || u > 5.5) continue;
      a += u < 1.8 ? Math.pow(Math.sin(Math.PI / 2 * u / 1.8), 2) : Math.exp(-(u - 1.8) / 1.3);
    }
    return Math.min(1.2, a);
  };
  write("Amb_Waves", loop(L, 2, (t) => {
    const s = swell(t);
    return body(520 + 260 * s) * (0.5 + 0.6 * s) + foam(900 + 700 * s) * 0.45 * s * s;
  }), "Assets/Resources/Sfx", 0.45); }

// A soft breeze: low air moving, a slow whoosh rising and falling; no whistle, no hiss.
{ const air = softNoise(4, 200);
  write("Amb_Wind", loop(12, 2, (t) => {
    const breath = 0.45 + 0.35 * Math.pow(Math.sin(Math.PI * t / 6), 2) + 0.2 * Math.sin(2 * Math.PI * t / 4.1 + 1);
    return air(480 + 380 * breath) * breath;
  }), "Assets/Resources/Sfx", 0.45); }

// Birdsong: four little calls, sine voices sliding in pitch with a quick flutter.
function tweet(t, start, len, f0, f1, flutter = 0) {
  const u = t - start; if (u < 0 || u > len) return 0;
  const k = u / len, f = f0 + (f1 - f0) * k;
  const shape = Math.sin(Math.PI * k);
  return Math.sin(2 * Math.PI * f * u + flutter * Math.sin(2 * Math.PI * 38 * u)) * shape;
}
write("Amb_Chirp1", make(0.5, (t) =>   // three quick rising tweets
  tweet(t, 0.0, 0.09, 3200, 4600) + tweet(t, 0.15, 0.09, 3300, 4800) + tweet(t, 0.3, 0.11, 3400, 5000)), "Assets/Resources/Sfx");
write("Amb_Chirp2", make(0.8, (t) => {  // a falling trill
  let s = 0;
  for (let k = 0; k < 8; k++) s += tweet(t, k * 0.085, 0.07, 4800 - k * 230, 4300 - k * 230) * (1 - k * 0.07);
  return s;
}), "Assets/Resources/Sfx");
write("Amb_Chirp3", make(0.7, (t) =>   // "tee-oo"
  tweet(t, 0.0, 0.22, 3900, 4200, 1.2) + tweet(t, 0.3, 0.32, 3300, 2500, 1.5)), "Assets/Resources/Sfx");
write("Amb_Chirp4", make(0.9, (t) => {  // a little warble
  const u = t / 0.9; if (u >= 1) return 0;
  const f = 3600 + 600 * Math.sin(2 * Math.PI * 9 * t);
  return Math.sin(2 * Math.PI * f * t) * Math.pow(Math.sin(Math.PI * u), 1.5) * (0.6 + 0.4 * Math.sin(2 * Math.PI * 4 * t));
}), "Assets/Resources/Sfx");

// Soft rain: lots of tiny droplet ticks (each a little decaying tone, high but gentle) over a
// quiet airy bed. Mastered low, like the waves.
{ const bed = softNoise(4, 300);
  const L = 8, drops = [];
  for (let t = 0; t < L + 1; t += -Math.log(1 - (rnd() * 0.5 + 0.5)) / 38) {
    drops.push({ t, f: 1300 + (rnd() * 0.5 + 0.5) * 1800, a: 0.25 + (rnd() * 0.5 + 0.5) * 0.75, d: 0.004 + (rnd() * 0.5 + 0.5) * 0.008 });
  }
  write("Amb_Rain", loop(L, 1, (t) => {
    let s = bed(1400) * 0.9;
    for (const dr of drops) {
      const u = t - dr.t;
      if (u < 0 || u > 0.05) continue;
      s += Math.sin(2 * Math.PI * dr.f * u) * Math.exp(-u / dr.d) * dr.a * 0.06;
    }
    return s;
  }), "Assets/Resources/Sfx", 0.4); }


// ---------------------------------------------------------------- the opening movie
// Played by UI/Cutscenes/IntroAudio.cs. Appended last, like the ambience, so every earlier file
// stays byte-identical. Soft and round: band-limited air, sine bells, nothing hissy.

// a little bell: a sine with a faint fifth above, struck softly and ringing out
const bell = (t, start, f, ring = 0.6, a = 1) => {
  const u = t - start; if (u < 0) return 0;
  const att = Math.min(1, u / 0.006);
  return a * att * Math.exp(-u / ring) * (Math.sin(2 * Math.PI * f * u) + 0.25 * Math.sin(2 * Math.PI * f * 3 * u) * Math.exp(-u / (ring * 0.3)));
};

// A cold gust sweeping through: soft air rising and falling, a little brighter at its peak.
{ const air = softNoise(4, 220);
  write("Intro_Wind", make(3.2, (t) => {
    const k = t / 3.2, swell = Math.pow(Math.sin(Math.PI * k), 1.6);
    return air(380 + 700 * swell) * swell;
  }), "Assets/Resources/Sfx", 0.5); }

// Freezing over: tiny icy pings scattering and thinning out, over a soft settling hush.
{ const hush = softNoise(3, 260);
  const pings = [];
  for (let i = 0; i < 26; i++) pings.push({ t: Math.pow((rnd() * 0.5 + 0.5), 1.8) * 1.6, f: 1700 + (rnd() * 0.5 + 0.5) * 1500, a: 0.3 + (rnd() * 0.5 + 0.5) * 0.5 });
  write("Intro_Freeze", make(2.2, (t) => {
    let s = hush(700) * Math.exp(-t / 0.8) * 0.5;
    for (const p of pings) s += bell(t, p.t, p.f, 0.12, p.a * 0.35);
    return s;
  }), "Assets/Resources/Sfx", 0.45); }

// Old Yuzu's magic: a warm shimmer rising up a pentatonic run of soft bells.
{ const run = [587, 659, 740, 880, 988, 1175, 1319, 1480];
  write("Intro_Magic", make(2.8, (t) => {
    let s = 0;
    run.forEach((f, i) => { s += bell(t, i * 0.12, f, 0.9, 0.55) * (1 + 0.15 * Math.sin(2 * Math.PI * 6 * t)); });
    const pad = Math.sin(2 * Math.PI * 294 * t) + 0.6 * Math.sin(2 * Math.PI * 441 * t);
    return s + pad * 0.12 * Math.sin(Math.PI * Math.min(1, t / 2.8));
  }), "Assets/Resources/Sfx", 0.5); }

// A bubble of warm air floating up: a round little bloop.
write("Intro_Bubble", make(0.4, (t) => {
  const f = 320 + 520 * Math.min(1, t / 0.12);
  return Math.sin(2 * Math.PI * f * t) * Math.min(1, t / 0.01) * Math.exp(-t / 0.09) + 0.2 * bell(t, 0.05, 1400, 0.08);
}), "Assets/Resources/Sfx", 0.45);

// Fading into a ghost: a soft, slightly wavering chord sinking away.
write("Intro_Fade", make(3, (t) => {
  const glide = 1 - 0.18 * (t / 3);
  const wob = 1 + 0.004 * Math.sin(2 * Math.PI * 5 * t);
  const v = [440, 554, 659].reduce((s, f, i) => s + Math.sin(2 * Math.PI * f * glide * wob * t + i), 0);
  return v * Math.sin(Math.PI * Math.min(1, t / 3)) * 0.5;
}), "Assets/Resources/Sfx", 0.35);

// One gentle wave washing in and drawing back.
{ const body = softNoise(3, 160), foam = softNoise(4, 300);
  write("Intro_Waves", make(4, (t) => {
    const s = t < 1.6 ? Math.pow(Math.sin(Math.PI / 2 * t / 1.6), 2) : Math.exp(-(t - 1.6) / 1.1);
    return body(480 + 260 * s) * (0.4 + 0.6 * s) + foam(800 + 600 * s) * 0.35 * s * s;
  }), "Assets/Resources/Sfx", 0.45); }

// Something glinting in the waves: three quick high twinkles.
write("Intro_Sparkle", make(1.1, (t) =>
  bell(t, 0, 1760, 0.25, 0.6) + bell(t, 0.11, 2349, 0.22, 0.5) + bell(t, 0.22, 2637, 0.3, 0.55)), "Assets/Resources/Sfx", 0.45);

// The crown lands: a bright, happy chime up a major chord.
write("Intro_Crown", make(1.6, (t) =>
  bell(t, 0, 784, 0.5) + bell(t, 0.08, 988, 0.5) + bell(t, 0.16, 1175, 0.6) + bell(t, 0.26, 1568, 0.8, 0.8)), "Assets/Resources/Sfx", 0.55);

// A little campfire: a soft low breath of flame and a scatter of gentle crackles. Band-limited
// like the wind, so the crackles are woody ticks, not hiss.
{ const roar = softNoise(4, 120), snap = softNoise(3, 400);
  const L = 8, pops = [];
  for (let t = 0.05; t < L; t += 0.06 + 0.35 * Math.abs(rnd())) pops.push([t, 0.4 + 0.6 * Math.abs(rnd()), 0.008 + 0.018 * Math.abs(rnd())]);
  write("Amb_Fire", loop(L, 1, (t) => {
    const breath = 0.6 + 0.25 * Math.sin(2 * Math.PI * t / 2.7) + 0.15 * Math.sin(2 * Math.PI * t / 1.3 + 2);
    let c = 0;
    for (const [at, a, d] of pops) { const u = t - at; if (u >= 0 && u < 0.12) c += a * Math.exp(-u / d); }
    return roar(300 + 160 * breath) * breath * 0.6 + snap(1700) * c * 0.9;
  }), "Assets/Resources/Sfx", 0.4); }

// ---------------------------------------------------------------- the kitchen and the burrow
// Appended last, so every earlier file stays byte-identical. Soft and round, mastered quiet.

// A pot simmering: little round bloops (low sine glides) rising out of a quiet warm bed.
// Looped by UI/Hobbies/CookingScreen.cs, louder as the heat climbs.
{ const bed = softNoise(3, 140);
  const L = 6, bloops = [];
  for (let t = 0.05; t < L; t += 0.07 + 0.3 * Math.abs(rnd())) bloops.push([t, 160 + 140 * Math.abs(rnd()), 0.35 + 0.65 * Math.abs(rnd())]);
  write("Amb_Simmer", loop(L, 1, (t) => {
    let s = bed(260) * 0.35;
    for (const [at, f, a] of bloops) {
      const u = t - at; if (u < 0 || u > 0.14) continue;
      const g = f * (1 + 1.4 * u / 0.14);
      s += Math.sin(2 * Math.PI * g * u) * Math.sin(Math.PI * u / 0.14) * a * 0.5;
    }
    return s;
  }), "Assets/Resources/Sfx", 0.4); }

// A stir with a wooden spoon: a soft swish round the pot and a gentle knock on its side.
{ const swish = softNoise(3, 300);
  write("Sfx_Stir", make(0.45, (t) => {
    const k = t / 0.45, sweep = Math.pow(Math.sin(Math.PI * Math.min(1, k * 1.2)), 2);
    const knock = Math.sin(2 * Math.PI * (210 - 80 * t) * t) * env(Math.max(0, t - 0.3), 0.002, 0.04) * (t > 0.3 ? 1 : 0);
    return swish(500 + 900 * sweep) * sweep * 0.8 + knock * 0.5;
  }), "Assets/Resources/Sfx", 0.5); }

// Sliding down the tunnel into your burrow: a long soft whoosh, quickening, with a low earthy rumble.
{ const air = softNoise(4, 200), rumble = softNoise(3, 90);
  write("Sfx_Slide", make(2.8, (t) => {
    const k = t / 2.8, rise = Math.pow(k, 1.3) * Math.pow(Math.sin(Math.PI * Math.min(1, k * 1.05)), 0.6);
    return air(420 + 900 * rise) * rise + rumble(140) * (0.3 + 0.4 * rise) * 0.6;
  }), "Assets/Resources/Sfx", 0.45); }
