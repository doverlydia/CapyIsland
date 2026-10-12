#!/usr/bin/env node
// Music for Capy Island, composed note by note and rendered to Assets/Resources/Music/<name>.wav
// (32 kHz, mono, 16-bit) for MusicPlayer.cs. Everything is original. The toolkit (oscillators,
// filters, instruments, song/melody helpers, seamless loops) comes from the Over The Hedgehog
// project's Tools/Audio/music.mjs; the kalimba, the flute and the tracks are Capy Island's:
//   music_day    warm and easy: music box over a soft pad, plucked bass, soft kick and shaker;
//                the flute answers in the second half, kalimba underneath
//   music_night  a lullaby: rolling kalimba arpeggios and a sparse music box, no drums
//
//   node Tools/Audio/music.mjs              # all
//   node Tools/Audio/music.mjs music_night  # one

import fs from "node:fs";
import path from "node:path";

const SR = 32000;
const OUT = path.resolve("Assets/Resources/Music");

// ---------------------------------------------------------------- primitives

const len = s => Math.round(s * SR);
const zeros = s => new Float32Array(len(s));
const clamp = (v, a, b) => Math.min(b, Math.max(a, v));
const at = (v, t) => (typeof v === "function" ? v(t) : v);

function osc(shape, freq, dur, phase = 0) {
  const out = zeros(dur); let ph = phase;
  for (let i = 0; i < out.length; i++) {
    ph += at(freq, i / SR) / SR; const p = ph - Math.floor(ph);
    switch (shape) {
      case "sine": out[i] = Math.sin(2 * Math.PI * p); break;
      case "tri": out[i] = 1 - 4 * Math.abs(p - 0.5); break;
      case "saw": out[i] = 2 * p - 1; break;
      case "square": out[i] = p < 0.5 ? 1 : -1; break;
      case "pulse": out[i] = p < 0.3 ? 1 : -1; break;
    }
  }
  return out;
}
let seed = 12345;
function rnd() { seed ^= seed << 13; seed ^= seed >>> 17; seed ^= seed << 5; return ((seed >>> 0) / 4294967296); }
function noise(dur) { const out = zeros(dur); for (let i = 0; i < out.length; i++) out[i] = rnd() * 2 - 1; return out; }
function env(points, dur) {
  const out = zeros(dur ?? points[points.length - 1][0]); let k = 0;
  for (let i = 0; i < out.length; i++) {
    const t = i / SR;
    while (k < points.length - 2 && t >= points[k + 1][0]) k++;
    const [t0, v0] = points[k], [t1, v1] = points[Math.min(k + 1, points.length - 1)];
    out[i] = t1 > t0 ? v0 + (v1 - v0) * clamp((t - t0) / (t1 - t0), 0, 1) : v1;
  }
  return out;
}
function adsr(dur, a, d, s, r) { return env([[0, 0], [a, 1], [a + d, s], [dur, s], [dur + r, 0]], dur + r); }
const mul = (a, b) => a.map((v, i) => v * (b[i] ?? 0));
const gain = (a, g) => a.map(v => v * g);
function lowpass(a, cutoff) {
  const out = new Float32Array(a.length); let y = 0;
  for (let i = 0; i < a.length; i++) { const k = 1 - Math.exp(-2 * Math.PI * at(cutoff, i / SR) / SR); y += k * (a[i] - y); out[i] = y; }
  return out;
}
function highpass(a, cutoff) { const lp = lowpass(a, cutoff); return a.map((v, i) => v - lp[i]); }
function bandpass(a, centre, q = 4) {
  const out = new Float32Array(a.length);
  let x1 = 0, x2 = 0, y1 = 0, y2 = 0, b0 = 0, b2 = 0, a1 = 0, a2 = 0;
  for (let i = 0; i < a.length; i++) {
    if (i % 32 === 0) { const w = 2 * Math.PI * clamp(at(centre, i / SR), 20, SR * 0.45) / SR; const al = Math.sin(w) / (2 * q), a0 = 1 + al; b0 = al / a0; b2 = -al / a0; a1 = -2 * Math.cos(w) / a0; a2 = (1 - al) / a0; }
    const y = b0 * a[i] + b2 * x2 - a1 * y1 - a2 * y2; x2 = x1; x1 = a[i]; y2 = y1; y1 = y; out[i] = y;
  }
  return out;
}
const soft = a => a.map(v => Math.tanh(v * 1.1) / Math.tanh(1.1));
function normalize(a, peak = 0.85) { let m = 0; for (const v of a) m = Math.max(m, Math.abs(v)); return m > 0 ? gain(a, peak / m) : a; }
//master: block DC (pulse waves lean to one side), leave headroom, then a gentle tanh as a safety limiter
function master(a, peak) { return normalize(soft(normalize(highpass(a, 25), 0.9)), peak); }
//a simple feedback delay for room
function delay(a, seconds, feedback, wet) {
  const out = Float32Array.from(a), d = len(seconds);
  for (let i = d; i < out.length; i++) out[i] += out[i - d] * feedback * wet;
  return out;
}

// ---------------------------------------------------------------- notes

const NOTE = { C: 0, "C#": 1, Db: 1, D: 2, "D#": 3, Eb: 3, E: 4, F: 5, "F#": 6, Gb: 6, G: 7, "G#": 8, Ab: 8, A: 9, "A#": 10, Bb: 10, B: 11 };
//"F4" -> Hz
function hz(name) {
  const m = /^([A-G][#b]?)(-?\d)$/.exec(name); if (!m) throw new Error("bad note " + name);
  const midi = 12 * (parseInt(m[2]) + 1) + NOTE[m[1]];
  return 440 * Math.pow(2, (midi - 69) / 12);
}

//a song is rendered onto one master buffer. add(sample array, beat) places a voice at a beat
function song(bpm, beats) {
  const spb = 60 / bpm, master = zeros(beats * spb + 3);
  return {
    spb, beats, master,
    add(arr, beat, g = 1) { const off = Math.round(beat * spb * SR); for (let i = 0; i < arr.length && off + i < master.length; i++) master[off + i] += arr[i] * g; },
    //fold the tail (release/reverb past the loop end) back onto the start so the loop is seamless
    finish() { const n = len(beats * spb); const out = new Float32Array(n); for (let i = 0; i < master.length; i++) out[i % n] += master[i]; return out; },
  };
}

// ---------------------------------------------------------------- instruments (all take hz, duration s)

//music box: bright sine with a decaying 3rd partial, plucky
function musicBox(f, dur) {
  const total = dur + 0.6;
  const v = mul(osc("sine", f, total), adsr(0.02, 0.002, 0.15, 0.35, 0.55));
  const ping = mul(osc("sine", f * 3, total), env([[0, 0.5], [0.12, 0]], total));
  const body = mul(osc("tri", f, total), env([[0, 0.35], [dur * 0.9, 0.12], [total, 0]], total));
  return gain(lowpass(mix(v, gain(ping, 0.4), body), 6000), 0.6);
}
//warm pad: three detuned triangles, slow attack, low-passed
function pad(f, dur) {
  const total = dur + 0.5;
  const v = mix(osc("tri", f, total), osc("tri", f * 1.004, total, 0.3), osc("tri", f * 0.996, total, 0.6), gain(osc("sine", f / 2, total), 0.6));
  return gain(lowpass(mul(v, adsr(dur, 0.35, 0.3, 0.8, 0.5)), 900), 0.22);
}
//plucked bass: sine + a bit of tri, quick decay
function pluckBass(f, dur) {
  const total = Math.min(dur, 0.6) + 0.1;
  const v = mix(osc("sine", f, total), gain(osc("tri", f, total), 0.4), gain(osc("sine", f * 2, total), 0.15));
  return gain(mul(lowpass(v, t => 900 * Math.exp(-t * 6) + 180), env([[0, 0], [0.008, 1], [total * 0.8, 0.25], [total, 0]], total)), 0.75);
}
//upright-ish walking bass: rounder, longer
function walkBass(f, dur) {
  const total = dur + 0.05;
  const v = mix(osc("tri", f, total), gain(osc("sine", f, total), 0.8), gain(osc("saw", f, total), 0.12));
  return gain(mul(lowpass(v, t => 700 * Math.exp(-t * 8) + 220), env([[0, 0], [0.006, 1], [dur * 0.9, 0.5], [total, 0]], total)), 0.8);
}
//brass stab: saw stack through a moving low-pass, punchy
function stab(f, dur) {
  const total = dur + 0.08;
  const v = mix(osc("saw", f, total), osc("saw", f * 1.007, total, 0.4), gain(osc("square", f / 2, total), 0.35));
  return gain(mul(lowpass(v, t => 2600 * Math.exp(-t * 9) + 500), env([[0, 0], [0.01, 1], [0.06, 0.7], [dur, 0.55], [total, 0]], total)), 0.35);
}
//sneaky lead: pulse with a touch of vibrato and a soft low-pass
function sneak(f, dur) {
  const total = dur + 0.12;
  const v = osc("pulse", t => f * (1 + 0.006 * Math.sin(2 * Math.PI * 5.5 * t)), total);
  return gain(mul(lowpass(v, 2200), env([[0, 0], [0.015, 1], [dur * 0.85, 0.75], [total, 0]], total)), 0.3);
}
//electric-piano-ish comp for the boss: sine + 2nd partial, quick
function comp(f, dur) {
  const total = dur + 0.15;
  const v = mix(osc("sine", f, total), gain(osc("sine", f * 2, total), 0.35), gain(osc("tri", f, total), 0.3));
  return gain(mul(lowpass(v, 3000), env([[0, 0], [0.006, 1], [dur * 0.7, 0.35], [total, 0]], total)), 0.35);
}

// drums
const kick = () => gain(mul(osc("sine", t => 110 * Math.exp(-t * 22) + 42, 0.3), env([[0, 0], [0.004, 1], [0.3, 0]], 0.3)), 0.9);
const softKick = () => gain(lowpass(kick(), 300), 0.8);
const shaker = () => gain(mul(highpass(noise(0.08), 4500), env([[0, 0], [0.005, 1], [0.08, 0]], 0.08)), 0.22);
const hat = (open = false) => gain(mul(highpass(noise(open ? 0.22 : 0.06), 6000), env([[0, 0], [0.003, 1], [open ? 0.22 : 0.06, 0]], open ? 0.22 : 0.06)), open ? 0.18 : 0.25);
const snare = () => gain(mix(mul(bandpass(noise(0.16), 1800, 1.2), env([[0, 0], [0.004, 1], [0.16, 0]], 0.16)), gain(mul(osc("sine", t => 220 * Math.exp(-t * 30) + 150, 0.1), env([[0, 1], [0.1, 0]], 0.1)), 0.5)), 0.5);
const rim = () => gain(mul(bandpass(noise(0.05), 3200, 6), env([[0, 0], [0.002, 1], [0.05, 0]], 0.05)), 0.35);

// ---------------------------------------------------------------- helpers

//chord voicings by name -> note names
const CH = {
  G: ["G3", "B3", "D4"], "D/F#": ["F#3", "A3", "D4"], Em: ["E3", "G3", "B3"], Bm: ["F#3", "B3", "D4"], D: ["F#3", "A3", "D4"],
  F: ["F3", "A3", "C4"], "C/E": ["E3", "G3", "C4"], Dm: ["D3", "F3", "A3"], Bb: ["F3", "Bb3", "D4"], Am: ["E3", "A3", "C4"], C: ["E3", "G3", "C4"], Gm: ["G3", "Bb3", "D4"],
  Dm7: ["D3", "F3", "A3", "C4"], Gm7: ["G3", "Bb3", "D4", "F4"], A7: ["A3", "C#4", "E4", "G4"], Bb7: ["Bb3", "D4", "F4", "Ab4"], Em7b5: ["E3", "G3", "Bb3", "D4"],
};

//place a melody: [[note|null, beats], ...] starting at a beat with an instrument
function melody(s, inst, start, notes, g = 1, legato = 0.9) {
  let b = start;
  for (const [n, d] of notes) { if (n) s.add(inst(hz(n), d * s.spb * legato), b, g); b += d; }
  return b;
}

// ---------------------------------------------------------------- island instruments

//kalimba (thumb piano): a round sine that rings a while, with a bright tine "tick" on the attack
function kalimba(f, dur) {
  const total = dur + 1.2;
  const body = mul(osc("sine", f, total), env([[0, 0], [0.003, 1], [0.35, 0.45], [total, 0]], total));
  const tine = mul(osc("sine", f * 5.4, total), env([[0, 0.35], [0.06, 0]], total));
  return gain(lowpass(mix(body, tine), 5000), 0.5);
}
//soft flute: a sine with a slow vibrato and a whisper of breath, gentle in and out
function flute(f, dur) {
  const total = dur + 0.25;
  const tone = osc("sine", t => f * (1 + 0.004 * Math.sin(2 * Math.PI * 5 * t)), total);
  const breath = gain(lowpass(highpass(noise(total), 1200), 3000), 0.06);
  return gain(mul(mix(tone, gain(osc("sine", f * 2, total), 0.12), breath), env([[0, 0], [0.08, 1], [dur, 0.85], [total, 0]], total)), 0.32);
}

// ---------------------------------------------------------------- day (G major, 88 bpm, 16 bars)
// Warm and easy, a little sunny: a music-box tune over a soft pad, plucked bass, a soft kick and
// a shaker. In the second half the flute answers it, with kalimba underneath.

function day() {
  const bpm = 88, bars = 16, s = song(bpm, bars * 4);
  const prog = ["G", "D/F#", "Em", "C", "G", "Bm", "C", "D", "Em", "C", "G", "D", "C", "D", "G", "G"];
  prog.forEach((ch, bar) => {
    const b = bar * 4;
    for (const n of CH[ch]) s.add(pad(hz(n), 4 * s.spb), b, 0.8);
    const root = CH[ch][0].replace(/\d/, m => String(parseInt(m) - 1));
    const fifth = CH[ch][2].replace(/\d/, m => String(parseInt(m) - 1));
    s.add(pluckBass(hz(root), s.spb), b); s.add(pluckBass(hz(fifth), s.spb * 0.5), b + 2); s.add(pluckBass(hz(root), s.spb), b + 3);
    s.add(softKick(), b, 0.8); s.add(softKick(), b + 2, 0.55);
    for (let e = 0; e < 8; e++) s.add(shaker(), b + e * 0.5, e % 2 === 0 ? 0.7 : 0.4);
  });
  const A = [
    ["B4", 1], ["A4", 0.5], ["B4", 0.5], ["D5", 1], ["B4", 1],
    ["A4", 1], ["F#4", 1], ["A4", 1.5], [null, 0.5],
    ["G4", 1], ["B4", 0.5], ["G4", 0.5], ["E4", 1], ["G4", 1],
    ["E4", 1.5], ["G4", 0.5], ["A4", 2],
    ["B4", 1], ["D5", 0.5], ["E5", 0.5], ["D5", 1], ["B4", 1],
    ["D5", 1], ["F#5", 1], ["D5", 1.5], ["B4", 0.5],
    ["C5", 1], ["E5", 0.5], ["C5", 0.5], ["B4", 1], ["A4", 1],
    ["F#4", 1], ["A4", 1], ["D5", 2],
  ];
  melody(s, musicBox, 0, A, 1);
  const B = [
    ["G5", 1.5], ["E5", 0.5], ["D5", 1], ["B4", 1],
    ["C5", 1], ["E5", 1], ["G5", 2],
    ["D5", 1], ["B4", 0.5], ["D5", 0.5], ["G5", 1], ["D5", 1],
    ["F#5", 1], ["E5", 1], ["D5", 1.5], [null, 0.5],
    ["E5", 1], ["G5", 0.5], ["E5", 0.5], ["C5", 1], ["E5", 1],
    ["D5", 1], ["A4", 1], ["F#5", 1.5], ["E5", 0.5],
    ["D5", 1], ["B4", 1], ["G4", 1], ["B4", 1],
    ["G4", 3], [null, 1],
  ];
  melody(s, flute, 32, B, 1, 0.95);
  const under = [["E4", 2], ["G4", 2], ["C4", 2], ["E4", 2], ["B3", 2], ["D4", 2], ["A3", 2], ["D4", 2],
                 ["C4", 2], ["E4", 2], ["D4", 2], ["F#4", 2], ["B3", 2], ["D4", 2], ["G4", 4]];
  melody(s, kalimba, 32, under, 0.45, 0.8);
  return master(delay(s.finish(), 0.34, 0.3, 0.32), 0.75);
}

// ---------------------------------------------------------------- night (A minor, 66 bpm, 16 bars)
// A lullaby: rolling kalimba arpeggios and a sparse music box over the pad. No drums.

function night() {
  const bpm = 66, bars = 16, s = song(bpm, bars * 4);
  const prog = ["Am", "F", "C", "G", "Am", "Em", "F", "G", "F", "C", "Dm", "Am", "F", "G", "Am", "Am"];
  prog.forEach((ch, bar) => {
    const b = bar * 4;
    for (const n of CH[ch]) s.add(pad(hz(n), 4 * s.spb), b, 0.7);
    // An arpeggio rolling up and back down the chord, in eighths.
    const [x, y, z] = CH[ch];
    const up = z.replace(/\d/, m => String(parseInt(m) + 1));
    [x, y, z, up, z, y, x, y].forEach((n, i) => s.add(kalimba(hz(n), s.spb * 0.5), b + i * 0.5, i === 0 ? 0.55 : 0.4));
    const root = CH[ch][0].replace(/\d/, m => String(parseInt(m) - 1));
    s.add(pluckBass(hz(root), s.spb * 2), b, 0.55);
  });
  const tune = [
    ["E5", 2], ["C5", 1], ["D5", 1],  ["C5", 2], ["A4", 2],  ["G4", 2], ["C5", 1], ["E5", 1],  ["D5", 4],
    ["E5", 2], ["G5", 1], ["E5", 1],  ["B4", 2], ["G4", 2],  ["A4", 2], ["C5", 1], ["F5", 1],  ["D5", 3], [null, 1],
    ["C5", 2], ["A4", 1], ["C5", 1],  ["E5", 2], ["G5", 2],  ["F5", 2], ["D5", 1], ["A4", 1],  ["C5", 2], ["E5", 2],
    ["A5", 2], ["F5", 1], ["C5", 1],  ["B4", 2], ["D5", 2],  ["C5", 1], ["B4", 1], ["A4", 2],  ["A4", 4],
  ];
  melody(s, musicBox, 0, tune, 0.8, 0.95);
  return master(delay(s.finish(), 0.42, 0.35, 0.38), 0.7);
}

// ---------------------------------------------------------------- wav

function writeWav(file, samples) {
  const n = samples.length, buf = Buffer.alloc(44 + n * 2);
  buf.write("RIFF", 0); buf.writeUInt32LE(36 + n * 2, 4); buf.write("WAVE", 8);
  buf.write("fmt ", 12); buf.writeUInt32LE(16, 16); buf.writeUInt16LE(1, 20); buf.writeUInt16LE(1, 22);
  buf.writeUInt32LE(SR, 24); buf.writeUInt32LE(SR * 2, 28); buf.writeUInt16LE(2, 32); buf.writeUInt16LE(16, 34);
  buf.write("data", 36); buf.writeUInt32LE(n * 2, 40);
  for (let i = 0; i < n; i++) buf.writeInt16LE(Math.round(clamp(samples[i], -1, 1) * 32767), 44 + i * 2);
  fs.writeFileSync(file, buf);
}
function mix(...parts) {
  const n = Math.max(...parts.map(p => p.length)), out = new Float32Array(n);
  for (const p of parts) for (let i = 0; i < p.length; i++) out[i] += p[i];
  return out;
}

const TRACKS = { music_day: day, music_night: night };
const only = process.argv.slice(2);
fs.mkdirSync(OUT, { recursive: true });
for (const [name, make] of Object.entries(TRACKS)) {
  if (only.length && !only.includes(name)) continue;
  seed = 12345;
  const samples = make();
  writeWav(path.join(OUT, name + ".wav"), samples);
  console.log(`${name}.wav  ${(samples.length / SR).toFixed(1)}s`);
}
