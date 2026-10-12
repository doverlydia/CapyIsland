// Capy Music Studio — the synth engine.
//
// Every instrument is synthesised from scratch (oscillators, filters, plucked strings), so all
// music is original and nothing recorded ships with the game. The same file runs in the studio
// window (preview while you compose) and in Node (export into the game), so what you hear while
// writing is exactly what the game plays.
//
// A song is plain JSON:
//   { name, title, bpm, bars, beatsPerBar, stepsPerBeat, key, scale, swing, loop,
//     master: { volume, reverb, echo, echoTime, echoFeedback },
//     tracks: [ { name, instrument, volume, reverb, mute, solo,
//                 notes: [ { t, p, d, v } ] } ] }
// t = start step, d = length in steps (a step is one 1/stepsPerBeat of a beat), p = MIDI pitch
// (for a drum track, the drum's key below), v = velocity 0..1.

export const DEFAULT_SR = 32000;

// ---------------------------------------------------------------- catalogue

// group: what kind of sound, for the picker. low/high: the comfortable range, for the piano roll.
export const INSTRUMENTS = [
  { id: "feltpiano", name: "Felt Piano", group: "Bells & keys", color: "#c9a27e", low: 31, high: 91 },
  { id: "musicbox", name: "Music Box", group: "Bells & keys", color: "#f2a65a", low: 60, high: 96 },
  { id: "celesta", name: "Celesta", group: "Bells & keys", color: "#f3c9a0", low: 60, high: 96 },
  { id: "kalimba", name: "Kalimba", group: "Bells & keys", color: "#e8c25a", low: 55, high: 88 },
  { id: "glock", name: "Glockenspiel", group: "Bells & keys", color: "#f5d76e", low: 72, high: 100 },
  { id: "marimba", name: "Marimba", group: "Bells & keys", color: "#c9864a", low: 45, high: 84 },
  { id: "steeldrum", name: "Steel Drum", group: "Bells & keys", color: "#7fc6c0", low: 55, high: 84 },
  { id: "piano", name: "Piano", group: "Bells & keys", color: "#9aa7c7", low: 36, high: 96 },
  { id: "epiano", name: "Electric Piano", group: "Bells & keys", color: "#8fb3e0", low: 40, high: 88 },
  { id: "harp", name: "Harp", group: "Strings", color: "#d9a3c7", low: 40, high: 91 },
  { id: "guitar", name: "Guitar", group: "Strings", color: "#c6905f", low: 40, high: 81 },
  { id: "nylon", name: "Nylon Guitar", group: "Strings", color: "#d8b48a", low: 40, high: 81 },
  { id: "ukulele", name: "Ukulele", group: "Strings", color: "#e0b07c", low: 55, high: 84 },
  { id: "pizz", name: "Pizzicato", group: "Strings", color: "#b98bd1", low: 43, high: 84 },
  { id: "strings", name: "String Section", group: "Strings", color: "#a77fc9", low: 36, high: 88 },
  { id: "cello", name: "Cello", group: "Strings", color: "#9a6f8f", low: 36, high: 69 },
  { id: "flute", name: "Flute", group: "Winds", color: "#7ec8e3", low: 60, high: 96 },
  { id: "ocarina", name: "Ocarina", group: "Winds", color: "#8fd1b5", low: 60, high: 88 },
  { id: "accordion", name: "Accordion", group: "Winds", color: "#d98b8b", low: 48, high: 84 },
  { id: "clarinet", name: "Clarinet", group: "Winds", color: "#6fa8a8", low: 50, high: 84 },
  { id: "brass", name: "Brass", group: "Winds", color: "#e0a03a", low: 40, high: 77 },
  { id: "choir", name: "Choir \"Ooh\"", group: "Pads & voices", color: "#e6a1a1", low: 48, high: 79 },
  { id: "pad", name: "Warm Pad", group: "Pads & voices", color: "#9cc98a", low: 36, high: 84 },
  { id: "glasspad", name: "Glass Pad", group: "Pads & voices", color: "#a9d8e8", low: 48, high: 88 },
  { id: "lead", name: "Soft Lead", group: "Synths", color: "#f08a8a", low: 55, high: 91 },
  { id: "chip", name: "Chiptune", group: "Synths", color: "#ff9fd0", low: 55, high: 96 },
  { id: "pluck", name: "Synth Pluck", group: "Synths", color: "#ffb88c", low: 48, high: 88 },
  { id: "pluckbass", name: "Plucked Bass", group: "Bass", color: "#6f8f5a", low: 28, high: 55 },
  { id: "walkbass", name: "Upright Bass", group: "Bass", color: "#7a6a4f", low: 28, high: 55 },
  { id: "subbass", name: "Round Sub Bass", group: "Bass", color: "#5f6f8f", low: 24, high: 52 },
  { id: "tuba", name: "Tuba", group: "Bass", color: "#b08a3a", low: 28, high: 55 },
  { id: "drums", name: "Drum Kit", group: "Drums", color: "#c99a78", low: 0, high: 0 },
];

// The drum kit's pieces, keyed like General MIDI so songs read sensibly.
export const DRUMS = [
  { key: 36, id: "kick", name: "Kick" },
  { key: 35, id: "softkick", name: "Soft Kick" },
  { key: 38, id: "snare", name: "Snare" },
  { key: 37, id: "rim", name: "Rim Click" },
  { key: 39, id: "clap", name: "Clap" },
  { key: 42, id: "hat", name: "Closed Hat" },
  { key: 46, id: "openhat", name: "Open Hat" },
  { key: 70, id: "shaker", name: "Shaker" },
  { key: 54, id: "tambourine", name: "Tambourine" },
  { key: 76, id: "woodblock", name: "Woodblock" },
  { key: 62, id: "congahi", name: "Conga High" },
  { key: 64, id: "congalo", name: "Conga Low" },
  { key: 81, id: "triangle", name: "Triangle" },
  { key: 56, id: "cowbell", name: "Cowbell" },
  { key: 49, id: "crash", name: "Soft Cymbal" },
  { key: 34, id: "pulse", name: "Soft Pulse" },
  { key: 40, id: "brush", name: "Brush" },
];

export const SCALES = {
  major: [0, 2, 4, 5, 7, 9, 11],
  minor: [0, 2, 3, 5, 7, 8, 10],
  dorian: [0, 2, 3, 5, 7, 9, 10],
  mixolydian: [0, 2, 4, 5, 7, 9, 10],
  "pentatonic major": [0, 2, 4, 7, 9],
  "pentatonic minor": [0, 3, 5, 7, 10],
  "harmonic minor": [0, 2, 3, 5, 7, 8, 11],
  chromatic: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11],
};

export const KEYS = ["C", "C#", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"];

export const noteName = midi => KEYS[((midi % 12) + 12) % 12] + (Math.floor(midi / 12) - 1);
export const instrument = id => INSTRUMENTS.find(i => i.id === id) ?? INSTRUMENTS[0];

/** A fresh, empty song. */
export function newSong(name = "untitled") {
  return {
    name, title: "New song", bpm: 90, bars: 8, beatsPerBar: 4, stepsPerBeat: 4, key: "C", scale: "major", swing: 0, loop: true,
    master: { volume: 0.75, reverb: 0.35, echo: 0.2, echoTime: 0.34, echoFeedback: 0.3 },
    tracks: [
      { name: "Melody", instrument: "musicbox", volume: 1, reverb: 0.3, mute: false, solo: false, notes: [] },
      { name: "Chords", instrument: "pad", volume: 0.7, reverb: 0.4, mute: false, solo: false, notes: [] },
      { name: "Bass", instrument: "pluckbass", volume: 0.8, reverb: 0.1, mute: false, solo: false, notes: [] },
      { name: "Drums", instrument: "drums", volume: 0.7, reverb: 0.15, mute: false, solo: false, notes: [] },
    ],
  };
}

// ---------------------------------------------------------------- the engine

export function createEngine(SR = DEFAULT_SR) {
  const len = s => Math.max(1, Math.round(s * SR));
  const zeros = s => new Float32Array(len(s));
  const clamp = (v, a, b) => Math.min(b, Math.max(a, v));
  const at = (v, t) => (typeof v === "function" ? v(t) : v);

  let seed = 12345;
  const rnd = () => { seed ^= seed << 13; seed ^= seed >>> 17; seed ^= seed << 5; return (seed >>> 0) / 4294967296; };

  function osc(shape, freq, dur, phase = 0) {
    const out = zeros(dur); let ph = phase;
    const fixed = typeof freq !== "function";
    for (let i = 0; i < out.length; i++) {
      ph += (fixed ? freq : freq(i / SR)) / SR; const p = ph - Math.floor(ph);
      switch (shape) {
        case "sine": out[i] = Math.sin(2 * Math.PI * p); break;
        case "tri": out[i] = 1 - 4 * Math.abs(p - 0.5); break;
        case "saw": out[i] = 2 * p - 1; break;
        case "square": out[i] = p < 0.5 ? 1 : -1; break;
        case "pulse": out[i] = p < 0.3 ? 1 : -1; break;
        case "pulse12": out[i] = p < 0.125 ? 1 : -1; break;
      }
    }
    return out;
  }
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
  // exponential decay from 1 with the given time constant (seconds)
  function decay(tau, dur, attack = 0.002) {
    const out = zeros(dur);
    for (let i = 0; i < out.length; i++) { const t = i / SR; out[i] = Math.min(1, t / attack) * Math.exp(-t / tau); }
    return out;
  }
  const adsr = (dur, a, d, s, r) => env([[0, 0], [a, 1], [a + d, s], [Math.max(dur, a + d), s], [Math.max(dur, a + d) + r, 0]], Math.max(dur, a + d) + r);
  const mul = (a, b) => { const out = new Float32Array(a.length); for (let i = 0; i < a.length; i++) out[i] = a[i] * (b[i] ?? 0); return out; };
  const gain = (a, g) => { const out = new Float32Array(a.length); for (let i = 0; i < a.length; i++) out[i] = a[i] * g; return out; };
  function mix(...parts) {
    const n = Math.max(...parts.map(p => p.length)), out = new Float32Array(n);
    for (const p of parts) for (let i = 0; i < p.length; i++) out[i] += p[i];
    return out;
  }
  function lowpass(a, cutoff) {
    const out = new Float32Array(a.length); let y = 0;
    const fixed = typeof cutoff !== "function", k0 = fixed ? 1 - Math.exp(-2 * Math.PI * cutoff / SR) : 0;
    for (let i = 0; i < a.length; i++) { const k = fixed ? k0 : 1 - Math.exp(-2 * Math.PI * cutoff(i / SR) / SR); y += k * (a[i] - y); out[i] = y; }
    return out;
  }
  function highpass(a, cutoff) { const lp = lowpass(a, cutoff); const out = new Float32Array(a.length); for (let i = 0; i < a.length; i++) out[i] = a[i] - lp[i]; return out; }
  function bandpass(a, centre, q = 4) {
    const out = new Float32Array(a.length);
    let x1 = 0, x2 = 0, y1 = 0, y2 = 0, b0 = 0, b2 = 0, a1 = 0, a2 = 0;
    for (let i = 0; i < a.length; i++) {
      if (i % 32 === 0) { const w = 2 * Math.PI * clamp(at(centre, i / SR), 20, SR * 0.45) / SR; const al = Math.sin(w) / (2 * q), a0 = 1 + al; b0 = al / a0; b2 = -al / a0; a1 = -2 * Math.cos(w) / a0; a2 = (1 - al) / a0; }
      const y = b0 * a[i] + b2 * x2 - a1 * y1 - a2 * y2; x2 = x1; x1 = a[i]; y2 = y1; y1 = y; out[i] = y;
    }
    return out;
  }
  // adds one exponentially decaying sine partial into out (a rotating phasor: no Math.sin per sample)
  function addPartial(out, f, amp, tau, phase = 0) {
    if (f >= SR * 0.45 || amp <= 0) return;
    const w = 2 * Math.PI * f / SR, cw = Math.cos(w), sw = Math.sin(w), dec = Math.exp(-1 / (tau * SR));
    let c = Math.cos(phase), s = Math.sin(phase), a = amp;
    for (let i = 0; i < out.length; i++) {
      out[i] += s * a;
      const ns = s * cw + c * sw; c = c * cw - s * sw; s = ns; a *= dec;
      if ((i & 4095) === 4095) { const r = Math.hypot(c, s); c /= r; s /= r; if (a < 1e-5) break; }
    }
  }
  const vib = (f, depth, rate, delay = 0) => t => f * (1 + depth * Math.sin(2 * Math.PI * rate * t) * Math.min(1, Math.max(0, (t - delay) / 0.3)));

  // a plucked string (Karplus–Strong): a burst of filtered noise circulating in a delay line
  function string(f, dur, { bright = 0.5, sustain = 0.996, ring = 1.2 } = {}) {
    const total = dur + ring, out = zeros(total);
    // The loss filter blends each sample with the next. Low strings take an even blend; high ones
    // much less, or their tone would die in a moment (they go round the loop far more often).
    const s = clamp(0.5 * 110 / f, 0.04, 0.5);
    // Tuning: the loop is n whole samples, plus s from the blend, plus a fraction from an all-pass.
    const period = SR / f - s;
    const n = Math.max(2, Math.floor(period - 0.1)), frac = period - n, C = (1 - frac) / (1 + frac);
    const line = new Float32Array(n);
    let lp = 0;
    for (let i = 0; i < n; i++) { lp += (bright + 0.1) * (rnd() * 2 - 1 - lp); line[i] = lp; }
    const stopAt = len(dur);
    // the same ring time at every pitch
    const holdDamp = Math.min(0.99995, Math.pow(sustain, 220 / f)), letGo = Math.pow(0.985, 220 / f);
    let idx = 0, apX = 0, apY = 0;
    for (let i = 0; i < out.length; i++) {
      const next = (idx + 1) % n, v = line[idx];
      const y = (i < stopAt ? holdDamp : letGo) * ((1 - s) * v + s * line[next]);
      const ap = C * y + apX - C * apY; apX = y; apY = ap;
      line[idx] = ap;
      out[i] = v; idx = next;
    }
    return gain(highpass(out, 40), 1.4);
  }

  // ---------------------------------------------------------------- melodic instruments (f Hz, dur s)

  const MELODIC = {
    // a soft, dark upright with felt on the hammers and the sustain pedal down: two slightly
    // detuned strings per note, overtones that fade fast, a muffled thump, a long gentle ring
    feltpiano(f, dur) {
      const release = 1.3, total = dur + release, out = zeros(total);
      const ring = clamp(2.3 - Math.log2(f / 65) * 0.35, 0.8, 2.3);
      const amps = [1, 0.42, 0.2, 0.11, 0.06, 0.035, 0.02, 0.012];
      amps.forEach((a, i) => {
        const n = i + 1, fn = f * n * (1 + 0.00035 * n * n), tau = ring / (1 + i * 0.6);
        addPartial(out, fn * 1.0006, a * 0.5, tau, i * 0.7);
        addPartial(out, fn * 0.9994, a * 0.5, tau * 0.93, i * 0.7 + 1.1);
      });
      const att = len(0.009);
      for (let i = 0; i < att && i < out.length; i++) out[i] *= i / att;
      const thump = mul(lowpass(noise(0.07), 420), env([[0, 1], [0.07, 0]], 0.07));
      for (let i = 0; i < thump.length; i++) out[i] += thump[i] * 0.1;
      // lifting the key: the damper settles over the release
      const r0 = Math.min(out.length, len(dur));
      for (let i = r0; i < out.length; i++) { const t = (i - r0) / (out.length - r0); out[i] *= (1 - t) * (1 - t); }
      return gain(lowpass(lowpass(out, 3800), 5200), 0.34);
    },
    // bowed and warm: two saws, late vibrato, a woody body
    cello(f, dur) {
      const total = dur + 0.4;
      const v = mix(osc("saw", vib(f, 0.005, 5.2, 0.25), total), gain(osc("saw", vib(f * 1.003, 0.005, 4.8, 0.25), total, 0.4), 0.7));
      const body = mix(lowpass(lowpass(v, 1500), 2200), gain(bandpass(v, 420, 2), 0.6));
      return gain(mul(body, adsr(dur, 0.2, 0.3, 0.85, 0.4)), 0.3);
    },
    nylon(f, dur) {
      return gain(lowpass(string(f, dur, { bright: 0.28, sustain: 0.997, ring: 0.9 }), 2400), 0.62);
    },
    // bright sine with a decaying 3rd partial, plucky
    musicbox(f, dur) {
      const total = dur + 0.6;
      const v = mul(osc("sine", f, total), adsr(0.02, 0.002, 0.15, 0.35, 0.55));
      const ping = mul(osc("sine", f * 3, total), env([[0, 0.5], [0.12, 0]], total));
      const body = mul(osc("tri", f, total), env([[0, 0.35], [dur * 0.9, 0.12], [total, 0]], total));
      return gain(lowpass(mix(v, gain(ping, 0.4), body), 6000), 0.6);
    },
    // a round sine that rings a while, with a bright tine tick on the attack
    kalimba(f, dur) {
      const total = dur + 1.2;
      const body = mul(osc("sine", f, total), env([[0, 0], [0.003, 1], [0.35, 0.45], [total, 0]], total));
      const tine = mul(osc("sine", f * 5.4, total), env([[0, 0.35], [0.06, 0]], total));
      return gain(lowpass(mix(body, tine), 5000), 0.5);
    },
    // metal bars: inharmonic partials with long, separate decays
    glock(f, dur) {
      const total = Math.max(dur, 0.2) + 1.6;
      return gain(mix(
        mul(osc("sine", f, total), decay(0.9, total)),
        gain(mul(osc("sine", f * 2.76, total), decay(0.35, total)), 0.35),
        gain(mul(osc("sine", f * 5.4, total), decay(0.12, total)), 0.18),
        gain(mul(osc("sine", f * 8.93, total), decay(0.05, total)), 0.08)), 0.42);
    },
    // wooden bars: a warm fundamental, a quick 4th partial knock
    marimba(f, dur) {
      const total = Math.max(dur, 0.15) + 0.7;
      const tau = clamp(0.5 - (f - 200) / 4000, 0.18, 0.55);
      return gain(mix(
        mul(osc("sine", f, total), decay(tau, total, 0.003)),
        gain(mul(osc("sine", f * 3.93, total), decay(0.035, total)), 0.45),
        gain(mul(osc("sine", f * 9.2, total), decay(0.012, total)), 0.12)), 0.6);
    },
    // a steelpan: strong octave, quick shimmer
    steeldrum(f, dur) {
      const total = Math.max(dur, 0.2) + 0.9;
      const bend = t => f * (1 + 0.012 * Math.exp(-t * 30));
      return gain(lowpass(mix(
        mul(osc("sine", bend, total), decay(0.55, total, 0.004)),
        gain(mul(osc("sine", t => bend(t) * 2, total), decay(0.3, total, 0.004)), 0.6),
        gain(mul(osc("sine", t => bend(t) * 3, total), decay(0.12, total, 0.004)), 0.25)), 5000), 0.45);
    },
    // a soft piano: harmonics that fade faster the higher they are, a little hammer
    piano(f, dur) {
      const total = dur + 0.5;
      const tau = clamp(2.4 - f / 500, 0.5, 2.4);
      const parts = [];
      const amps = [1, 0.5, 0.28, 0.16, 0.09, 0.05];
      amps.forEach((a, i) => {
        const n = i + 1, inh = 1 + 0.0004 * n * n;
        if (f * n * inh > SR * 0.45) return;
        parts.push(gain(mul(osc("sine", f * n * inh, total, i * 0.13), decay(tau / (1 + i * 0.7), total, 0.002)), a));
      });
      const hammer = gain(mul(lowpass(noise(0.03), 2500), env([[0, 1], [0.03, 0]], 0.03)), 0.08);
      const release = env([[0, 1], [dur, 1], [total, 0]], total);
      return gain(mul(lowpass(mix(...parts, hammer), 4200), release), 0.42);
    },
    // a mellow electric piano: sine with a decaying bell tine and gentle tremolo
    epiano(f, dur) {
      const total = dur + 0.4;
      const trem = t => 1 + 0.12 * Math.sin(2 * Math.PI * 4.5 * t);
      const body = mul(osc("sine", f, total), env([[0, 0], [0.004, 1], [dur * 0.8 + 0.1, 0.55], [total, 0]], total));
      const tine = gain(mul(osc("sine", f * 4, total), decay(0.15, total)), 0.22);
      const out = mix(body, tine, gain(mul(osc("sine", f * 2, total), decay(0.5, total)), 0.18));
      for (let i = 0; i < out.length; i++) out[i] *= trem(i / SR);
      return gain(out, 0.42);
    },
    harp(f, dur) {
      // plucked with the soft of the finger: a dark excitation, a gentle filter, a slow fade-in of
      // a few milliseconds so the pluck never clicks
      const s = lowpass(lowpass(string(f, Math.max(dur, 0.6), { bright: 0.18, sustain: 0.9985, ring: 1.4 }), 2400), 3400);
      const a = len(0.006); for (let i = 0; i < a && i < s.length; i++) s[i] *= i / a;
      return gain(s, 0.6);
    },
    guitar(f, dur) {
      return gain(lowpass(string(f, dur, { bright: 0.55, sustain: 0.997, ring: 0.5 }), 4000), 0.55);
    },
    ukulele(f, dur) {
      return gain(lowpass(string(f, Math.min(dur, 0.8), { bright: 0.7, sustain: 0.993, ring: 0.3 }), 5000), 0.5);
    },
    // plucked strings: short, round
    pizz(f, dur) {
      const total = 0.45;
      const v = mix(osc("tri", f, total), gain(osc("sine", f * 2, total), 0.3));
      return gain(mul(lowpass(v, t => 2200 * Math.exp(-t * 10) + 300), decay(0.12, total, 0.003)), 0.55);
    },
    // a string section: detuned saws, slow bow, vibrato that comes in late
    strings(f, dur) {
      const total = dur + 0.45;
      const v = mix(osc("saw", vib(f, 0.004, 5.2, 0.25), total), osc("saw", vib(f * 1.006, 0.004, 4.8, 0.25), total, 0.33), osc("saw", vib(f * 0.995, 0.004, 5.5, 0.25), total, 0.66));
      return gain(lowpass(mul(v, adsr(dur, 0.18, 0.2, 0.85, 0.4)), 1900), 0.16);
    },
    // a sine with a slow vibrato and a whisper of breath, gentle in and out
    flute(f, dur) {
      const total = dur + 0.4;
      const tone = osc("sine", t => f * (1 + 0.004 * Math.sin(2 * Math.PI * 5 * t) * Math.min(1, t / 0.35)), total);
      const breath = gain(lowpass(highpass(noise(total), 1200), 2600), 0.035);
      const shape = env([[0, 0], [0.07, 1], [dur, 0.88], [dur + 0.12, 0.45], [dur + 0.26, 0.15], [total, 0]], total);
      return gain(mul(mix(tone, gain(osc("sine", f * 2, total), 0.12), breath), shape), 0.32);
    },
    // a musette accordion: three reeds a hair apart, so it shimmers; the bellows breathe it in
    accordion(f, dur) {
      const total = dur + 0.12;
      const reed = (ff, ph) => lowpass(osc("pulse", ff, total, ph), 2400);
      const v = mix(reed(f, 0), gain(reed(f * 1.0055, 0.3), 0.8), gain(reed(f * 0.9948, 0.6), 0.8), gain(osc("sine", f / 2, total), 0.25));
      return gain(lowpass(mul(v, env([[0, 0], [0.06, 1], [dur, 0.9], [total, 0]], total)), 3000), 0.1);
    },
    // a celesta: a little hammer on a soft bell — round, short shimmer, never sharp
    celesta(f, dur) {
      const total = Math.max(dur, 0.3) + 1.1, out = zeros(total);
      addPartial(out, f, 0.8, 0.85);
      addPartial(out, f * 2, 0.12, 0.35, 0.5);
      addPartial(out, f * 4, 0.16, 0.12, 1.1);
      const a = len(0.004); for (let i = 0; i < a; i++) out[i] *= i / a;
      return gain(lowpass(out, 4500), 0.42);
    },
    // hollow and round: a sine, a faint third harmonic, breath on the attack
    ocarina(f, dur) {
      const total = dur + 0.15;
      const tone = mix(osc("sine", vib(f, 0.005, 5.5, 0.2), total), gain(osc("sine", f * 3, total), 0.05));
      const breath = gain(mul(lowpass(highpass(noise(total), 1500), 4000), env([[0, 1], [0.12, 0.25], [total, 0.2]], total)), 0.05);
      return gain(mul(mix(tone, breath), env([[0, 0], [0.04, 1], [dur, 0.9], [total, 0]], total)), 0.36);
    },
    // a woody reed: odd harmonics (a soft square), low-passed
    clarinet(f, dur) {
      const total = dur + 0.12;
      const v = lowpass(lowpass(osc("square", vib(f, 0.003, 5, 0.3), total), 1600), 2400);
      return gain(mul(v, env([[0, 0], [0.05, 1], [dur, 0.8], [total, 0]], total)), 0.3);
    },
    // brass: saw stack through a low-pass that opens on the attack
    brass(f, dur) {
      const total = dur + 0.1;
      const v = mix(osc("saw", f, total), osc("saw", f * 1.005, total, 0.4));
      return gain(mul(lowpass(v, t => 600 + 2200 * Math.min(1, t / 0.06) * Math.exp(-t * 3)), env([[0, 0], [0.04, 1], [0.12, 0.8], [dur, 0.7], [total, 0]], total)), 0.25);
    },
    // "ooh": a buzzy voice through two vowel formants, slow in
    choir(f, dur) {
      const total = dur + 0.5;
      const src = mix(osc("saw", vib(f, 0.005, 5, 0.2), total), osc("saw", vib(f * 1.004, 0.006, 4.6, 0.2), total, 0.5));
      const v = mix(bandpass(src, 380, 5), gain(bandpass(src, 820, 6), 0.5), gain(bandpass(src, 2600, 8), 0.08));
      return gain(mul(v, adsr(dur, 0.25, 0.2, 0.85, 0.45)), 0.9);
    },
    // three detuned triangles, slow attack, low-passed
    pad(f, dur) {
      const total = dur + 0.5;
      const v = mix(osc("tri", f, total), osc("tri", f * 1.004, total, 0.3), osc("tri", f * 0.996, total, 0.6), gain(osc("sine", f / 2, total), 0.6));
      return gain(lowpass(mul(v, adsr(dur, 0.35, 0.3, 0.8, 0.5)), 900), 0.22);
    },
    // airy and bright: sines an octave apart with a slow shimmer
    glasspad(f, dur) {
      const total = dur + 0.8;
      const v = mix(osc("sine", f, total), gain(osc("sine", f * 2.001, total, 0.2), 0.45), gain(osc("sine", f * 3.002, total, 0.4), 0.15));
      for (let i = 0; i < v.length; i++) v[i] *= 1 + 0.15 * Math.sin(2 * Math.PI * 0.7 * i / SR);
      return gain(mul(v, adsr(dur, 0.5, 0.4, 0.75, 0.8)), 0.2);
    },
    // a soft pulse with vibrato
    lead(f, dur) {
      const total = dur + 0.12;
      const v = osc("pulse", vib(f, 0.006, 5.5, 0.15), total);
      return gain(mul(lowpass(v, 2200), env([[0, 0], [0.015, 1], [dur * 0.85, 0.75], [total, 0]], total)), 0.25);
    },
    // a little game-console square, softened
    chip(f, dur) {
      const total = dur + 0.03;
      const v = osc("square", f, total);
      return gain(mul(lowpass(v, 3500), env([[0, 0], [0.003, 1], [0.08, 0.7], [dur, 0.6], [total, 0]], total)), 0.16);
    },
    // a short synth pluck with a closing filter
    pluck(f, dur) {
      const total = Math.min(dur, 0.6) + 0.25;
      const v = mix(osc("saw", f, total), gain(osc("square", f * 0.5, total), 0.3));
      return gain(mul(lowpass(v, t => 3200 * Math.exp(-t * 12) + 250), decay(0.22, total, 0.002)), 0.32);
    },
    // sine + a bit of tri, quick decay
    pluckbass(f, dur) {
      const total = Math.min(dur, 0.6) + 0.1;
      const v = mix(osc("sine", f, total), gain(osc("tri", f, total), 0.4), gain(osc("sine", f * 2, total), 0.15));
      return gain(mul(lowpass(v, t => 900 * Math.exp(-t * 6) + 180), env([[0, 0], [0.008, 1], [total * 0.8, 0.25], [total, 0]], total)), 0.75);
    },
    // rounder, longer
    walkbass(f, dur) {
      const total = dur + 0.05;
      const v = mix(osc("tri", f, total), gain(osc("sine", f, total), 0.8), gain(osc("saw", f, total), 0.12));
      return gain(mul(lowpass(v, t => 700 * Math.exp(-t * 8) + 220), env([[0, 0], [0.006, 1], [dur * 0.9, 0.5], [total, 0]], total)), 0.8);
    },
    subbass(f, dur) {
      const total = dur + 0.08;
      const v = mix(osc("sine", f, total), gain(osc("sine", f * 2, total), 0.12));
      return gain(mul(v, env([[0, 0], [0.01, 1], [dur, 0.9], [total, 0]], total)), 0.7);
    },
    tuba(f, dur) {
      const total = dur + 0.1;
      const v = mix(osc("saw", f, total), gain(osc("sine", f, total), 0.8));
      return gain(mul(lowpass(v, t => 350 + 500 * Math.min(1, t / 0.05) * Math.exp(-t * 4)), env([[0, 0], [0.035, 1], [dur, 0.8], [total, 0]], total)), 0.55);
    },
  };

  // ---------------------------------------------------------------- drums

  const kick = () => gain(mul(osc("sine", t => 110 * Math.exp(-t * 22) + 42, 0.3), env([[0, 0], [0.004, 1], [0.3, 0]], 0.3)), 0.9);
  const DRUM = {
    kick,
    softkick: () => gain(lowpass(kick(), 300), 0.8),
    snare: () => gain(mix(mul(bandpass(noise(0.16), 1800, 1.2), env([[0, 0], [0.004, 1], [0.16, 0]], 0.16)), gain(mul(osc("sine", t => 220 * Math.exp(-t * 30) + 150, 0.1), env([[0, 1], [0.1, 0]], 0.1)), 0.5)), 0.5),
    rim: () => gain(mul(bandpass(noise(0.05), 3200, 6), env([[0, 0], [0.002, 1], [0.05, 0]], 0.05)), 0.35),
    clap: () => {
      const n = bandpass(noise(0.22), 1300, 1.5);
      const e = env([[0, 0], [0.002, 1], [0.01, 0.2], [0.012, 1], [0.022, 0.2], [0.024, 1], [0.22, 0]], 0.22);
      return gain(mul(n, e), 0.6);
    },
    hat: () => gain(mul(highpass(noise(0.06), 6000), env([[0, 0], [0.003, 1], [0.06, 0]], 0.06)), 0.25),
    openhat: () => gain(mul(highpass(noise(0.22), 6000), env([[0, 0], [0.003, 1], [0.22, 0]], 0.22)), 0.18),
    shaker: () => gain(mul(highpass(noise(0.08), 4500), env([[0, 0], [0.005, 1], [0.08, 0]], 0.08)), 0.22),
    tambourine: () => {
      const jingle = mul(bandpass(noise(0.25), 7500, 3), env([[0, 0], [0.003, 1], [0.04, 0.4], [0.25, 0]], 0.25));
      return gain(mix(jingle, gain(mul(highpass(noise(0.03), 3000), env([[0, 1], [0.03, 0]], 0.03)), 0.4)), 0.35);
    },
    woodblock: () => gain(mul(bandpass(osc("sine", 1250, 0.09), 1250, 3), env([[0, 0], [0.001, 1], [0.09, 0]], 0.09)), 0.9),
    congahi: () => gain(mul(osc("sine", t => 330 * (1 + 0.15 * Math.exp(-t * 40)), 0.25), decay(0.07, 0.25, 0.002)), 0.6),
    congalo: () => gain(mul(osc("sine", t => 210 * (1 + 0.18 * Math.exp(-t * 40)), 0.3), decay(0.09, 0.3, 0.002)), 0.7),
    triangle: () => gain(mix(mul(osc("sine", 3150, 1.2), decay(0.4, 1.2)), gain(mul(osc("sine", 4730, 1.2), decay(0.25, 1.2)), 0.5)), 0.12),
    cowbell: () => gain(mul(bandpass(mix(osc("square", 540, 0.35), osc("square", 800, 0.35)), 800, 2), decay(0.09, 0.35, 0.001)), 0.25),
    // a muffled low heartbeat, no click
    pulse: () => gain(lowpass(mix(mul(osc("sine", t => 92 + 45 * Math.exp(-t * 30), 0.35), decay(0.12, 0.35, 0.006)), gain(mul(lowpass(noise(0.08), 500), decay(0.03, 0.08, 0.004)), 0.6)), 520), 0.7),
    brush: () => gain(mul(bandpass(noise(0.3), 2500, 0.8), env([[0, 0], [0.03, 1], [0.3, 0]], 0.3)), 0.12),
    crash: () => gain(mul(lowpass(highpass(noise(1.6), 3500), 9000), decay(0.5, 1.6, 0.004)), 0.14),
  };

  // ---------------------------------------------------------------- rendering

  const LEGATO = 0.94;   // a hair of air between touching notes
  const SLUR = new Set(["flute", "ocarina", "clarinet", "accordion", "strings", "cello", "choir", "pad", "glasspad", "brass", "lead", "tuba"]);
  const cache = new Map();

  /** One note, as samples. Cached: a song repeats the same notes a lot. */
  function renderNote(inst, midi, seconds) {
    const key = inst + "|" + midi + "|" + seconds.toFixed(4);
    let s = cache.get(key);
    if (s) return s;
    seed = (12345 + midi * 7919 + Math.round(seconds * 1000) * 104729 + inst.length * 31) >>> 0 || 1;
    if (inst === "drums") {
      const d = DRUMS.find(x => x.key === midi);
      s = d ? DRUM[d.id]() : new Float32Array(1);
    } else {
      const f = 440 * Math.pow(2, (midi - 69) / 12);
      s = (MELODIC[inst] ?? MELODIC.musicbox)(f, Math.max(0.03, seconds * LEGATO));
    }
    if (cache.size > 4000) cache.clear();
    cache.set(key, s);
    return s;
  }

  const stepSeconds = song => 60 / song.bpm / song.stepsPerBeat;
  const songSteps = song => song.bars * song.beatsPerBar * song.stepsPerBeat;

  // when a note starts, with swing pushing every second eighth (or sixteenth) late
  function noteStart(song, t) {
    const step = stepSeconds(song);
    let s = t * step;
    const swing = song.swing ?? 0;
    if (swing > 0) {
      const unit = song.stepsPerBeat >= 4 ? song.stepsPerBeat / 2 : 1;   // swing eighths
      const pos = t / unit;
      if (Math.abs(pos - Math.round(pos)) < 1e-6 && Math.round(pos) % 2 === 1) s += swing * unit * step * 0.33;
    }
    return s;
  }

  // a gentle room: four damped combs into two all-passes (the classic Schroeder/Freeverb shape)
  function reverb(a) {
    const scale = SR / 44100;
    const combs = [1116, 1188, 1277, 1356].map(n => Math.round(n * scale));
    const alls = [556, 441].map(n => Math.round(n * scale));
    const out = new Float32Array(a.length);
    for (const n of combs) {
      const buf = new Float32Array(n); let idx = 0, lp = 0;
      for (let i = 0; i < a.length; i++) {
        const y = buf[idx];
        lp = y * 0.75 + lp * 0.25;
        buf[idx] = a[i] + lp * 0.84;
        out[i] += y * 0.25;
        idx = (idx + 1) % n;
      }
    }
    for (const n of alls) {
      const buf = new Float32Array(n); let idx = 0;
      for (let i = 0; i < out.length; i++) {
        const b = buf[idx], x = out[i];
        out[i] = -x + b;
        buf[idx] = x + b * 0.5;
        idx = (idx + 1) % n;
      }
    }
    return out;
  }

  function echo(a, seconds, feedback, wet) {
    const out = Float32Array.from(a), d = len(seconds);
    for (let i = d; i < out.length; i++) out[i] += out[i - d] * feedback * wet;
    return out;
  }

  const soft = a => { const k = Math.tanh(1.1); const out = new Float32Array(a.length); for (let i = 0; i < a.length; i++) out[i] = Math.tanh(a[i] * 1.1) / k; return out; };
  function normalize(a, peak) { let m = 0; for (const v of a) m = Math.max(m, Math.abs(v)); return m > 0 ? gain(a, peak / m) : a; }

  /**
   * The whole song, mixed and mastered. With loop on, the tail (release, reverb and echo past the
   * last bar) folds back onto the start, so the game can loop it without a seam.
   */
  function renderSong(song) {
    const step = stepSeconds(song), steps = songSteps(song);
    const length = steps * step, tail = 4;
    const dry = zeros(length + tail), wet = zeros(length + tail);
    const soloing = song.tracks.some(t => t.solo);
    // Human feel: notes land a touch early or late, a touch louder or softer, and chords roll from
    // the bottom up like a hand spreading over the keys. Seeded by the note, so it never changes
    // between plays (or between the window and the game).
    const human = clamp(song.humanize ?? 0, 0, 1);
    song.tracks.forEach((track, ti) => {
      if (track.mute || (soloing && !track.solo)) return;
      const vol = track.volume ?? 1, send = track.reverb ?? 0, drums = track.instrument === "drums";
      const chords = new Map();
      if (human > 0 && !drums) for (const n of track.notes) { if (!chords.has(n.t)) chords.set(n.t, []); chords.get(n.t).push(n.p); }
      for (const ps of chords.values()) ps.sort((a, b) => a - b);
      // Sustained instruments slur: a note that runs into the next one keeps sounding until the
      // next has begun, so a phrase is one breath instead of a string of separate puffs.
      const slurs = SLUR.has(track.instrument);
      const starts = slurs ? new Set(track.notes.map(q => q.t)) : null;
      for (const n of track.notes) {
        if (n.t >= steps) continue;
        let secs = n.d * step;
        if (slurs) { const end = n.t + n.d; if (starts.has(end) || starts.has(end + 1)) secs = (n.d + (starts.has(end) ? 0 : 1)) * step / LEGATO + 0.09; }
        const s = renderNote(track.instrument, n.p, secs);
        let at = noteStart(song, n.t), g = vol * (n.v ?? 1);
        if (human > 0) {
          let h = (Math.imul(ti + 1, 73856093) ^ Math.imul(n.t + 7, 19349663) ^ Math.imul(n.p + 3, 83492791)) >>> 0;
          const r = () => { h ^= h << 13; h >>>= 0; h ^= h >>> 17; h ^= h << 5; h >>>= 0; return h / 4294967296; };
          r();
          at += (r() - 0.5) * (drums ? 0.016 : 0.036) * human;
          g *= 1 + (r() - 0.5) * 0.3 * human;
          const chord = chords.get(n.t);
          if (chord && chord.length > 1) at += chord.indexOf(n.p) * 0.024 * human;
          at = Math.max(0, at);
        }
        const off = Math.round(at * SR);
        const end = Math.min(s.length, dry.length - off);
        for (let i = 0; i < end; i++) { const v = s[i] * g; dry[off + i] += v; wet[off + i] += v * send; }
      }
    });
    const m = song.master ?? {};
    const room = reverb(wet);
    const amount = (m.reverb ?? 0.35) * 0.9;
    for (let i = 0; i < dry.length; i++) dry[i] += room[i] * amount;
    let out = (m.echo ?? 0) > 0 ? echo(dry, m.echoTime ?? 0.34, m.echoFeedback ?? 0.3, m.echo) : dry;
    // Warmth: a gentle tilt toward the low end (0 = untouched, 1 = soft and dark, like a felt room)
    const warmth = clamp(m.warmth ?? 0, 0, 1);
    if (warmth > 0.01) { const cut = 14000 * Math.pow(1400 / 14000, warmth); out = lowpass(lowpass(out, cut), cut * 1.4); }
    if (song.loop !== false) {
      const n = len(length), folded = new Float32Array(n);
      for (let i = 0; i < out.length; i++) folded[i % n] += out[i];
      out = folded;
    }
    // block DC, leave headroom, a gentle tanh as a safety limiter, then the song's level
    return normalize(soft(normalize(highpass(out, 25), 0.9)), m.volume ?? 0.75);
  }

  /** A single note to audition (click on the keyboard, or a note being placed). */
  function previewNote(inst, midi, seconds = 0.4) {
    const s = renderNote(inst, midi, seconds);
    return gain(s, 0.8);
  }

  return { SR, renderSong, renderNote, previewNote, stepSeconds, songSteps, noteStart };
}

// ---------------------------------------------------------------- WAV (16-bit mono)

export function encodeWav(samples, SR) {
  const n = samples.length, buf = new ArrayBuffer(44 + n * 2), v = new DataView(buf);
  const str = (o, s) => { for (let i = 0; i < s.length; i++) v.setUint8(o + i, s.charCodeAt(i)); };
  str(0, "RIFF"); v.setUint32(4, 36 + n * 2, true); str(8, "WAVE");
  str(12, "fmt "); v.setUint32(16, 16, true); v.setUint16(20, 1, true); v.setUint16(22, 1, true);
  v.setUint32(24, SR, true); v.setUint32(28, SR * 2, true); v.setUint16(32, 2, true); v.setUint16(34, 16, true);
  str(36, "data"); v.setUint32(40, n * 2, true);
  for (let i = 0; i < n; i++) v.setInt16(44 + i * 2, Math.round(Math.max(-1, Math.min(1, samples[i])) * 32767), true);
  return new Uint8Array(buf);
}

// ---------------------------------------------------------------- saving

/** Song JSON that reads well in a diff: settings pretty-printed, one note per line. */
export function songToJson(song) {
  const notes = new Map();
  const copy = { ...song, tracks: song.tracks.map((t, i) => { const key = `@@notes${i}@@`; notes.set(key, t.notes); return { ...t, notes: key }; }) };
  let text = JSON.stringify(copy, null, 2);
  for (const [key, list] of notes) {
    const body = list.length === 0 ? "[]"
      : "[\n" + [...list].sort((a, b) => a.t - b.t || a.p - b.p).map(n => `        { "t": ${n.t}, "p": ${n.p}, "d": ${n.d}, "v": ${+(n.v ?? 1).toFixed(2)} }`).join(",\n") + "\n      ]";
    text = text.replace(`"${key}"`, body);
  }
  return text + "\n";
}
