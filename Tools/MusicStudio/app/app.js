// Capy Music Studio — the window: tracks, a piano roll, playback, saving and export.
import { INSTRUMENTS, DRUMS, SCALES, KEYS, noteName, instrument, newSong, createEngine, DEFAULT_SR } from "./engine.js";

const $ = s => document.querySelector(s);
const clamp = (v, a, b) => Math.min(b, Math.max(a, v));
const KEYW = 76, RULER = 30;
const LOW = 24, HIGH = 108;   // C1 .. C8

// ---------------------------------------------------------------- state

const S = {
  song: null, file: null,
  track: 0, sel: new Set(),
  noteLen: 4, chord: "off", scaleSnap: true, ghosts: true, velocity: 0.8,
  stepW: 22, rowH: 16,
  marker: 0, range: null,   // range: { a, b } in steps
  loop: true,
};

const track = () => S.song?.tracks[S.track];
const isDrums = (t = track()) => t?.instrument === "drums";
const steps = () => S.song.bars * S.song.beatsPerBar * S.song.stepsPerBeat;
const barSteps = () => S.song.beatsPerBar * S.song.stepsPerBeat;
const beatSteps = () => S.song.stepsPerBeat;
const stepSec = () => 60 / S.song.bpm / S.song.stepsPerBeat;
const unit = () => Math.min(S.noteLen, beatSteps());   // the grid notes snap to

const rowCount = () => (isDrums() ? DRUMS.length : HIGH - LOW + 1);
const pitchOfRow = r => (isDrums() ? DRUMS[r]?.key : r >= 0 && r <= HIGH - LOW ? HIGH - r : undefined);
const rowOfPitch = (p, t = track()) => (isDrums(t) ? DRUMS.findIndex(d => d.key === p) : HIGH - p);
const drumName = p => DRUMS.find(d => d.key === p)?.name ?? "?";

// ---------------------------------------------------------------- scale and chords

const scaleOf = () => SCALES[S.song.scale] ?? SCALES.major;
const keyIndex = () => Math.max(0, KEYS.indexOf(S.song.key));
const pc = p => ((p % 12) + 12) % 12;
const inScale = p => scaleOf().includes(pc(p - keyIndex()));

function snapToScale(p, sc = scaleOf()) {
  const k = keyIndex(), ok = q => sc.includes(pc(q - k));
  if (ok(p)) return p;
  for (let d = 1; d < 12; d++) { if (ok(p - d)) return p - d; if (ok(p + d)) return p + d; }
  return p;
}

// a chord stacked in thirds on the clicked note, from the song's scale
function chordFrom(p, kind) {
  let sc = scaleOf();
  if (sc.length !== 7) sc = sc.includes(3) ? SCALES.minor : SCALES.major;
  p = snapToScale(p, sc);
  const deg = sc.indexOf(pc(p - keyIndex())), base = p - sc[deg];
  const out = [];
  for (let i = 0; i < (kind === "seventh" ? 4 : 3); i++) { const d = deg + 2 * i; out.push(base + sc[d % 7] + 12 * Math.floor(d / 7)); }
  return out;
}

// ---------------------------------------------------------------- undo

const history = [], future = [];
function checkpoint() {
  history.push(JSON.stringify(S.song));
  if (history.length > 300) history.shift();
  future.length = 0;
}
function undo() {
  if (!history.length) return;
  future.push(JSON.stringify(S.song));
  S.song = JSON.parse(history.pop());
  afterRestore();
}
function redo() {
  if (!future.length) return;
  history.push(JSON.stringify(S.song));
  S.song = JSON.parse(future.pop());
  afterRestore();
}
function afterRestore() {
  S.sel.clear();
  S.track = clamp(S.track, 0, S.song.tracks.length - 1);
  fillSongBar(); renderTracks(); changed();
}

// ---------------------------------------------------------------- saving

let saveTimer = 0;
function changed() {
  setSaveState("Saving…");
  clearTimeout(saveTimer);
  saveTimer = setTimeout(save, 800);
  scheduleRerender();
  layout(); draw(); drawVel();
  $("#undo").disabled = !history.length;
  $("#redo").disabled = !future.length;
}
async function save() {
  clearTimeout(saveTimer); saveTimer = 0;
  if (!S.file) return;
  try {
    const r = await fetch(`/api/songs/${S.file}`, { method: "PUT", body: JSON.stringify(S.song) });
    if (!r.ok) throw new Error((await r.json()).error);
    setSaveState("Saved ✓");
  } catch (e) { setSaveState("Not saved — is the studio server running?"); }
}
const setSaveState = t => { $("#saveState").textContent = t; };

async function api(path, opts) {
  const r = await fetch(path, opts);
  const body = await r.json().catch(() => ({}));
  if (!r.ok) throw new Error(body.error ?? r.statusText);
  return body;
}

// ---------------------------------------------------------------- audio

let actx = null, out = null;
function audio() {
  if (!actx) {
    actx = new AudioContext();
    out = actx.createGain(); out.gain.value = 0.9; out.connect(actx.destination);
  }
  if (actx.state === "suspended") actx.resume();
  return actx;
}

const previewEngine = createEngine(DEFAULT_SR);
function audition(inst, pitch, lenSteps = 4, vel = 0.9) {
  if (pitch == null) return;
  const a = audio();
  const secs = clamp(lenSteps * stepSec(), 0.15, 1.5);
  const s = previewEngine.previewNote(inst, pitch, secs);
  const buf = a.createBuffer(1, s.length, DEFAULT_SR); buf.copyToChannel(s, 0);
  const src = a.createBufferSource(); src.buffer = buf;
  const g = a.createGain(); g.gain.value = 0.7 * vel * (track()?.volume ?? 1);
  src.connect(g).connect(out); src.start();
}

// whole-song mixes come from a worker so the window never stalls
const worker = new Worker("worker.js", { type: "module" });
let reqId = 0; const pending = new Map();
worker.onmessage = e => { const p = pending.get(e.data.id); pending.delete(e.data.id); e.data.error ? p.reject(new Error(e.data.error)) : p.resolve(e.data); };
const renderInWorker = song => new Promise((resolve, reject) => { const id = ++reqId; pending.set(id, { resolve, reject }); worker.postMessage({ id, song }); });

const P = { buffer: null, key: null, src: null, startedAt: 0, playing: false, stepSec: 0, loopStart: 0, loopEnd: 0 };

async function getBuffer() {
  const key = JSON.stringify(S.song);
  if (P.key === key && P.buffer) return P.buffer;
  $("#rendering").textContent = "mixing…";
  const tempo = stepSec();
  try {
    const res = await renderInWorker(JSON.parse(key));
    const a = audio();
    const buf = a.createBuffer(1, res.samples.length, res.sr); buf.copyToChannel(res.samples, 0);
    P.buffer = buf; P.key = key; P.bufferStepSec = tempo;
    return buf;
  } finally { $("#rendering").textContent = ""; }
}

function loopBounds(buf) {
  const total = Math.min(steps() * stepSec(), buf.duration);
  if (S.range) return { start: Math.min(S.range.a * stepSec(), total), end: Math.min(S.range.b * stepSec(), total) };
  return { start: 0, end: total };
}

function startAt(buf, sec) {
  stopSource();
  const a = audio();
  const L = loopBounds(buf);
  if (!(sec >= 0 && sec < L.end)) sec = L.start;
  const src = a.createBufferSource(); src.buffer = buf;
  src.loop = S.loop;
  if (S.loop) { src.loopStart = L.start; src.loopEnd = L.end; }
  src.connect(out); src.start(0, sec);
  Object.assign(P, { src, startedAt: a.currentTime - sec, playing: true, stepSec: P.bufferStepSec ?? stepSec(), loopStart: L.start, loopEnd: L.end });
  src.onended = () => { if (P.src === src) { P.playing = false; P.src = null; S.marker = 0; updatePlay(); draw(); } };
  updatePlay();
}

function stopSource() {
  if (P.src) { const s = P.src; P.src = null; try { s.stop(); } catch { } }
  P.playing = false;
}

// seconds into the song
function position() {
  if (!P.playing || !actx) return S.marker * stepSec();
  let t = actx.currentTime - P.startedAt;
  if (S.loop && t >= P.loopEnd && P.loopEnd > P.loopStart) t = P.loopStart + ((t - P.loopStart) % (P.loopEnd - P.loopStart));
  return t;
}
const positionStep = () => position() / (P.playing ? P.stepSec : stepSec());

async function play() {
  audio();
  if (P.playing) { S.marker = Math.floor(positionStep()); stopSource(); updatePlay(); draw(); return; }
  const buf = await getBuffer();
  let at = S.marker;
  if (S.range && (at < S.range.a || at >= S.range.b)) at = S.range.a;
  startAt(buf, at * stepSec());
}

let rerenderTimer = 0;
function scheduleRerender() {
  clearTimeout(rerenderTimer);
  rerenderTimer = setTimeout(async () => {
    if (!P.playing) return;
    const step = positionStep();
    const buf = await getBuffer();
    if (!P.playing) return;
    // carry on from the same musical spot, even if the tempo changed
    const now = positionStep();
    startAt(buf, (Number.isFinite(now) ? now : step) * stepSec());
  }, 350);
}

function updatePlay() {
  $("#play").textContent = P.playing ? "■ Stop" : "▶ Play";
  $("#play").classList.toggle("accent", P.playing);
  $("#play").classList.toggle("primary", !P.playing);
}

// ---------------------------------------------------------------- the piano roll

const scroller = $("#scroller"), spacer = $("#spacer"), roll = $("#roll"), g = roll.getContext("2d");
const velLane = $("#velLane"), vg = velLane.getContext("2d");
let vw = 0, vh = 0, dpr = 1;

function layout() {
  spacer.style.width = KEYW + steps() * S.stepW + 160 + "px";
  spacer.style.height = RULER + rowCount() * S.rowH + 8 + "px";
}

function resize() {
  dpr = window.devicePixelRatio || 1;
  vw = scroller.clientWidth; vh = scroller.clientHeight;
  roll.width = vw * dpr; roll.height = vh * dpr;
  roll.style.width = vw + "px"; roll.style.height = vh + "px";
  const lw = velLane.clientWidth, lh = velLane.clientHeight;
  velLane.width = lw * dpr; velLane.height = lh * dpr;
  draw(); drawVel();
}

const sx = () => scroller.scrollLeft, sy = () => scroller.scrollTop;
const xOfStep = s => KEYW + s * S.stepW - sx();
const yOfRow = r => RULER + r * S.rowH - sy();
const stepAtX = x => Math.floor((x - KEYW + sx()) / S.stepW);
const rowAtY = y => Math.floor((y - RULER + sy()) / S.rowH);

function shade(hex, f) {
  const n = parseInt(hex.slice(1), 16);
  const ch = i => clamp(Math.round(((n >> i) & 255) * f), 0, 255);
  return `rgb(${ch(16)},${ch(8)},${ch(0)})`;
}

function noteRect(n, t = track()) {
  const r = rowOfPitch(n.p, t);
  return { x: xOfStep(n.t) + 1, y: yOfRow(r) + 1, w: Math.max(4, n.d * S.stepW - 2), h: S.rowH - 2, r };
}

function noteAt(x, y) {
  const list = track().notes;
  for (let i = list.length - 1; i >= 0; i--) {
    const q = noteRect(list[i]);
    if (x >= q.x - 1 && x <= q.x + q.w + 1 && y >= q.y - 1 && y <= q.y + q.h + 1) return list[i];
  }
  return null;
}

let box = null;   // rubber-band selection, in canvas pixels

function draw() {
  if (!S.song || !vw) return;
  g.setTransform(dpr, 0, 0, dpr, 0, 0);
  const n = steps(), sw = S.stepW, rh = S.rowH, rows = rowCount(), drums = isDrums();
  g.fillStyle = "#fff"; g.fillRect(0, 0, vw, vh);

  // ---- grid
  g.save();
  g.beginPath(); g.rect(KEYW, RULER, vw - KEYW, vh - RULER); g.clip();
  const r0 = Math.max(0, Math.floor(sy() / rh)), r1 = Math.min(rows - 1, Math.ceil((sy() + vh) / rh));
  for (let r = r0; r <= r1; r++) {
    const y = yOfRow(r), p = pitchOfRow(r);
    if (drums) g.fillStyle = r % 2 ? "#fbf6ec" : "#fffdf8";
    else {
      const black = [1, 3, 6, 8, 10].includes(pc(p)), tonic = pc(p - keyIndex()) === 0;
      g.fillStyle = tonic ? "#dff1e6" : inScale(p) ? (black ? "#eaf5ee" : "#f3faf5") : (black ? "#ece4d6" : "#faf6ef");
    }
    g.fillRect(KEYW, y, vw - KEYW, rh);
    g.fillStyle = !drums && pc(p) === 0 ? "#d9c6a2" : "#efe6d6";
    g.fillRect(KEYW, y + rh - 1, vw - KEYW, 1);
  }
  const s0 = Math.max(0, Math.floor(sx() / sw)), s1 = Math.min(n, Math.ceil((sx() + vw) / sw));
  for (let s = s0; s <= s1; s++) {
    const x = xOfStep(s), bar = s % barSteps() === 0, beat = s % beatSteps() === 0;
    if (!bar && !beat && sw < 9) continue;
    g.fillStyle = bar ? "#c9b088" : beat ? "#e2d2b5" : "#f2ebde";
    g.fillRect(Math.round(x), RULER, bar ? 2 : 1, vh - RULER);
  }
  const xEnd = xOfStep(n);
  if (xEnd < vw) { g.fillStyle = "rgba(120,100,70,.13)"; g.fillRect(xEnd, RULER, vw - xEnd, vh - RULER); }
  if (S.range) { g.fillStyle = "rgba(240,138,60,.10)"; g.fillRect(xOfStep(S.range.a), RULER, (S.range.b - S.range.a) * sw, vh - RULER); }

  // ---- the other tracks, faintly
  if (S.ghosts) {
    S.song.tracks.forEach((t, i) => {
      if (i === S.track || isDrums(t) !== drums || t.mute) return;
      g.fillStyle = instrument(t.instrument).color; g.globalAlpha = 0.22;
      for (const q of t.notes) {
        const rc = noteRect(q, t);
        if (rc.x > vw || rc.x + rc.w < KEYW || rc.y > vh || rc.y + rc.h < RULER) continue;
        g.beginPath(); g.roundRect(rc.x, rc.y, rc.w, rc.h, 3); g.fill();
      }
      g.globalAlpha = 1;
    });
  }

  // ---- this track's notes
  const col = instrument(track().instrument).color;
  g.font = "600 10px Nunito, Segoe UI, sans-serif"; g.textBaseline = "middle";
  for (const q of track().notes) {
    const rc = noteRect(q);
    if (rc.x > vw || rc.x + rc.w < KEYW || rc.y > vh || rc.y + rc.h < RULER) continue;
    const sel = S.sel.has(q);
    g.globalAlpha = 0.45 + 0.55 * (q.v ?? 1);
    g.fillStyle = sel ? shade(col, 1.12) : col;
    g.beginPath(); g.roundRect(rc.x, rc.y, rc.w, rc.h, 4); g.fill();
    g.globalAlpha = 1;
    g.lineWidth = sel ? 2 : 1; g.strokeStyle = sel ? "#3a2a1a" : shade(col, 0.7);
    g.stroke();
    if (rc.w > 26 && rh >= 12) {
      g.fillStyle = "rgba(40,25,10,.7)";
      g.fillText(drums ? drumName(q.p) : noteName(q.p), rc.x + 4, rc.y + rc.h / 2 + 0.5, rc.w - 6);
    }
  }
  if (box) {
    g.fillStyle = "rgba(63,181,163,.12)"; g.strokeStyle = "#3fb5a3"; g.lineWidth = 1;
    const x = Math.min(box.x0, box.x1), y = Math.min(box.y0, box.y1);
    g.fillRect(x, y, Math.abs(box.x1 - box.x0), Math.abs(box.y1 - box.y0));
    g.strokeRect(x + 0.5, y + 0.5, Math.abs(box.x1 - box.x0), Math.abs(box.y1 - box.y0));
  }
  g.restore();

  // ---- ruler
  g.fillStyle = "#f4e9d4"; g.fillRect(0, 0, vw, RULER);
  g.save(); g.beginPath(); g.rect(KEYW, 0, vw - KEYW, RULER); g.clip();
  if (S.range) { g.fillStyle = "#f6c48f"; g.fillRect(xOfStep(S.range.a), 0, (S.range.b - S.range.a) * sw, RULER); }
  g.font = "700 12px Nunito, Segoe UI, sans-serif"; g.textBaseline = "middle";
  for (let s = s0 - (s0 % beatSteps()); s <= s1; s += beatSteps()) {
    const x = xOfStep(s);
    if (s % barSteps() === 0) {
      g.fillStyle = "#8a6a40"; g.fillRect(x, 8, 2, RULER - 8);
      g.fillText(String(s / barSteps() + 1), x + 5, 13);
    } else { g.fillStyle = "#c9b088"; g.fillRect(x, RULER - 8, 1, 8); }
  }
  // the start marker and the playhead
  const mx = xOfStep(S.marker);
  g.fillStyle = "#3fb5a3"; g.beginPath(); g.moveTo(mx - 6, RULER - 10); g.lineTo(mx + 6, RULER - 10); g.lineTo(mx, RULER); g.fill();
  g.restore();
  g.fillStyle = "#e2d3b6"; g.fillRect(0, RULER - 1, vw, 1);
  if (P.playing) {
    const px = xOfStep(positionStep());
    if (px >= KEYW) { g.fillStyle = "#e86a6a"; g.fillRect(px - 1, 0, 2, vh); }
  }

  // ---- keyboard
  g.save(); g.beginPath(); g.rect(0, RULER, KEYW, vh - RULER); g.clip();
  g.font = "600 11px Nunito, Segoe UI, sans-serif";
  for (let r = r0; r <= r1; r++) {
    const y = yOfRow(r), p = pitchOfRow(r);
    if (drums) {
      g.fillStyle = r % 2 ? "#f6eedf" : "#fbf5ea"; g.fillRect(0, y, KEYW, rh);
      g.fillStyle = "#5a4630"; g.fillText(DRUMS[r].name, 6, y + rh / 2 + 0.5, KEYW - 8);
    } else {
      const black = [1, 3, 6, 8, 10].includes(pc(p));
      g.fillStyle = black ? "#4a3a2a" : "#fffdf8"; g.fillRect(0, y, KEYW, rh);
      if (black) { g.fillStyle = "#fffdf8"; g.fillRect(KEYW * 0.62, y, KEYW * 0.38, rh); }
      g.fillStyle = "#e2d3b6"; g.fillRect(0, y + rh - 1, KEYW, 1);
      if (pc(p) === 0 || rh >= 15) {
        g.fillStyle = black ? "#e9dcc4" : pc(p) === 0 ? "#b8642a" : "#a8977c";
        if (!black || rh >= 15) g.fillText(noteName(p), 6, y + rh / 2 + 0.5);
      }
    }
  }
  g.restore();
  g.fillStyle = "#e2d3b6"; g.fillRect(KEYW - 1, RULER, 1, vh);
  g.fillStyle = "#fff1d6"; g.fillRect(0, 0, KEYW, RULER);
  g.fillStyle = "#b8642a"; g.font = "700 11px Nunito, Segoe UI, sans-serif";
  g.fillText(instrument(track().instrument).name, 6, RULER / 2, KEYW - 8);
}

function drawVel() {
  if (!S.song) return;
  const w = velLane.clientWidth, h = velLane.clientHeight;
  vg.setTransform(dpr, 0, 0, dpr, 0, 0);
  vg.fillStyle = "#fffaf0"; vg.fillRect(0, 0, w, h);
  vg.fillStyle = "#9a8466"; vg.font = "600 11px Nunito, Segoe UI, sans-serif";
  vg.fillText("Loudness", 8, 16);
  vg.save(); vg.beginPath(); vg.rect(KEYW, 0, w - KEYW, h); vg.clip();
  const col = instrument(track().instrument).color;
  for (const q of track().notes) {
    const x = xOfStep(q.t) + 2;
    if (x < KEYW - 10 || x > w) continue;
    const top = 6 + (1 - (q.v ?? 1)) * (h - 12);
    vg.fillStyle = S.sel.has(q) ? "#3a2a1a" : col;
    vg.fillRect(x, top, 3, h - 6 - top);
    vg.beginPath(); vg.arc(x + 1.5, top, 4, 0, Math.PI * 2); vg.fill();
  }
  vg.restore();
}

// ---------------------------------------------------------------- editing

function removeNotes(list) {
  const kill = new Set(list);
  for (const t of S.song.tracks) t.notes = t.notes.filter(n => !kill.has(n));
  for (const n of kill) S.sel.delete(n);
}

function local(e, el = roll) { const r = el.getBoundingClientRect(); return { x: e.clientX - r.left, y: e.clientY - r.top }; }

let drag = null;

roll.addEventListener("contextmenu", e => e.preventDefault());
roll.addEventListener("pointerdown", e => {
  if (!S.song) return;
  roll.setPointerCapture(e.pointerId);
  const { x, y } = local(e);

  if (y < RULER && x >= KEYW) {
    const s = clamp(Math.floor(stepAtX(x) / beatSteps()) * beatSteps(), 0, steps());
    drag = { kind: "ruler", from: s, moved: false };
    return;
  }
  if (x < KEYW && y >= RULER) {
    const p = pitchOfRow(rowAtY(y));
    audition(track().instrument, p, 4);
    drag = { kind: "keys", last: p };
    return;
  }
  if (x < KEYW || y < RULER) return;

  const step = stepAtX(x), row = rowAtY(y), pitch = pitchOfRow(row);
  if (pitch == null || step < 0) return;
  const hit = noteAt(x, y);

  if (e.button === 2) {
    checkpoint();
    if (hit) removeNotes([hit]);
    drag = { kind: "erase" };
    changed();
    return;
  }

  if (hit) {
    if (e.shiftKey || e.ctrlKey) { S.sel.has(hit) ? S.sel.delete(hit) : S.sel.add(hit); }
    else if (!S.sel.has(hit)) S.sel = new Set([hit]);
    const rc = noteRect(hit);
    const edge = !isDrums() && x > rc.x + rc.w - clamp(rc.w * 0.3, 4, 10);
    let checkpointed = false;
    if (e.altKey && !edge) {
      // alt-drag: drag out a copy
      checkpoint(); checkpointed = true;
      const copies = [...S.sel].map(n => ({ ...n }));
      track().notes.push(...copies);
      S.sel = new Set(copies);
    }
    drag = { kind: edge ? "resize" : "move", startStep: step, startRow: row, anchor: hit, checkpointed, moved: false, toggle: isDrums() && !e.shiftKey && !e.ctrlKey && !e.altKey,
             orig: [...S.sel].map(n => ({ n, t: n.t, p: n.p, d: n.d })), lastPitch: hit.p };
    if (!edge) audition(track().instrument, hit.p, hit.d, hit.v);
    draw(); drawVel();
    return;
  }

  if (e.shiftKey || e.ctrlKey) {
    box = { x0: x, y0: y, x1: x, y1: y, base: e.ctrlKey ? new Set(S.sel) : new Set() };
    drag = { kind: "box" };
    return;
  }

  // a new note (or chord, or a run of drum hits)
  checkpoint();
  // drums land exactly in the clicked cell; notes snap to the grid of the chosen length
  const t = isDrums() ? step : Math.floor(step / unit()) * unit();
  let p = pitch;
  if (!isDrums() && S.scaleSnap) p = snapToScale(p);
  const pitches = !isDrums() && S.chord !== "off" ? chordFrom(p, S.chord) : [p];
  const created = [];
  for (const q of pitches) {
    if (!isDrums() && (q < LOW || q > HIGH)) continue;
    if (track().notes.some(n => n.t === t && n.p === q)) continue;
    created.push({ t, p: q, d: isDrums() ? 1 : S.noteLen, v: S.velocity });
  }
  track().notes.push(...created);
  S.sel = new Set(created);
  for (const q of pitches) audition(track().instrument, q, S.noteLen, S.velocity);
  drag = { kind: isDrums() ? "paint" : "create", startStep: t, notes: created, row, pitch: p };
  changed();
});

roll.addEventListener("pointermove", e => {
  const { x, y } = local(e);
  hover(x, y);
  if (!drag) return;
  const step = stepAtX(x), row = rowAtY(y);

  switch (drag.kind) {
    case "ruler": {
      const s = clamp(Math.floor(step / beatSteps()) * beatSteps(), 0, steps());
      if (s !== drag.from || drag.moved) {
        drag.moved = true;
        S.range = { a: Math.min(drag.from, s), b: Math.max(drag.from, s) + beatSteps() };
        S.range.b = Math.min(S.range.b, steps());
        showRange(); draw();
      }
      break;
    }
    case "keys": {
      const p = pitchOfRow(row);
      if (p != null && p !== drag.last) { drag.last = p; audition(track().instrument, p, 4); }
      break;
    }
    case "erase": {
      const hit = noteAt(x, y);
      if (hit) { removeNotes([hit]); changed(); }
      break;
    }
    case "create": {
      const u = unit();
      const len = Math.max(u, Math.ceil((step + 1 - drag.startStep) / u) * u);
      if (drag.notes[0] && drag.notes[0].d !== len) { for (const n of drag.notes) n.d = len; draw(); drawVel(); }
      break;
    }
    case "paint": {
      // a row of hits, spaced by the chosen note length from where the drag began
      const u = S.noteLen, t = drag.startStep + Math.round((step - drag.startStep) / u) * u;
      if (t >= 0 && t < steps() && !track().notes.some(n => n.t === t && n.p === drag.pitch)) {
        const n = { t, p: drag.pitch, d: 1, v: S.velocity };
        track().notes.push(n); drag.notes.push(n); S.sel.add(n);
        audition("drums", drag.pitch, 1, S.velocity);
        changed();
      }
      break;
    }
    case "move": {
      const u = e.ctrlKey ? 1 : unit();
      const dStep = Math.round((step - drag.startStep) / u) * u;
      const dRow = row - drag.startRow;
      if (!dStep && !dRow && !drag.moved) break;
      if (!drag.checkpointed) { checkpoint(); drag.checkpointed = true; }
      drag.moved = true;
      const minT = Math.min(...drag.orig.map(o => o.t));
      const ds = Math.max(dStep, -minT);
      for (const o of drag.orig) {
        o.n.t = o.t + ds;
        if (isDrums()) { const r = clamp(rowOfPitch(o.p) + dRow, 0, DRUMS.length - 1); o.n.p = DRUMS[r].key; }
        else o.n.p = clamp(o.p - dRow, LOW, HIGH);
      }
      if (drag.anchor.p !== drag.lastPitch) { drag.lastPitch = drag.anchor.p; audition(track().instrument, drag.anchor.p, drag.anchor.d, drag.anchor.v); }
      draw(); drawVel();
      break;
    }
    case "resize": {
      const u = e.ctrlKey ? 1 : unit();
      const dStep = Math.round((step + 1 - (drag.anchor.t + drag.orig.find(o => o.n === drag.anchor).d)) / u) * u;
      if (!dStep && !drag.moved) break;
      if (!drag.checkpointed) { checkpoint(); drag.checkpointed = true; }
      drag.moved = true;
      for (const o of drag.orig) o.n.d = Math.max(1, o.d + dStep);
      draw();
      break;
    }
    case "box": {
      box.x1 = x; box.y1 = y;
      const xa = Math.min(box.x0, box.x1), xb = Math.max(box.x0, box.x1), ya = Math.min(box.y0, box.y1), yb = Math.max(box.y0, box.y1);
      S.sel = new Set(box.base);
      for (const n of track().notes) {
        const rc = noteRect(n);
        if (rc.x < xb && rc.x + rc.w > xa && rc.y < yb && rc.y + rc.h > ya) S.sel.add(n);
      }
      draw(); drawVel();
      break;
    }
  }
});

roll.addEventListener("pointerup", e => {
  if (!drag) return;
  const d = drag; drag = null;
  if (d.kind === "ruler" && !d.moved) {
    S.marker = d.from;
    if (S.range && (d.from < S.range.a || d.from >= S.range.b)) { S.range = null; showRange(); }
    if (P.playing && P.buffer) startAt(P.buffer, S.marker * stepSec());
    draw();
    return;
  }
  if (d.kind === "ruler" && P.src && S.loop) {
    // keep playing, now looping the chosen bars
    const L = loopBounds(P.buffer);
    P.src.loopStart = L.start; P.src.loopEnd = L.end; P.loopStart = L.start; P.loopEnd = L.end;
  }
  if (d.kind === "move" && !d.moved && d.toggle) {
    // a drum grid: clicking a hit takes it away again
    checkpoint(); removeNotes([d.anchor]); changed(); return;
  }
  if (d.kind === "box") { box = null; draw(); return; }
  if (d.moved || d.kind === "create") changed();
});

roll.addEventListener("dblclick", e => {
  const { x, y } = local(e);
  const hit = noteAt(x, y);
  if (hit && !isDrums()) { checkpoint(); removeNotes([hit]); changed(); }
});

roll.addEventListener("pointerleave", () => setStatus());

roll.addEventListener("wheel", e => {
  if (e.ctrlKey) {
    e.preventDefault();
    const { x } = local(e);
    const at = (x - KEYW + sx()) / S.stepW;
    zoom(e.deltaY < 0 ? 1.15 : 1 / 1.15, at, x);
  } else if (e.altKey) {
    e.preventDefault();
    S.rowH = clamp(S.rowH + (e.deltaY < 0 ? 1 : -1), 10, 28);
    layout(); draw();
  }
}, { passive: false });

function zoom(f, atStep, atX) {
  if (atStep == null) { atStep = (vw / 2 - KEYW + sx()) / S.stepW; atX = vw / 2; }
  S.stepW = clamp(S.stepW * f, 5, 60);
  layout();
  scroller.scrollLeft = atStep * S.stepW - (atX - KEYW);
  draw(); drawVel();
}

scroller.addEventListener("scroll", () => { draw(); drawVel(); });

function hover(x, y) {
  if (x < KEYW || y < RULER || !S.song) return setStatus();
  const s = stepAtX(x), p = pitchOfRow(rowAtY(y));
  if (p == null || s < 0 || s >= steps()) return setStatus();
  const bar = Math.floor(s / barSteps()) + 1, beat = Math.floor((s % barSteps()) / beatSteps()) + 1;
  const name = isDrums() ? drumName(p) : noteName(p) + (inScale(p) ? "" : "  (not in " + S.song.key + " " + S.song.scale + ")");
  setStatus(`${name} · bar ${bar}, beat ${beat}`);
}
const HINT = "Click the grid to add a note · drag a note to move it · drag its right edge to lengthen · right-click deletes · Shift-drag selects · Space plays";
const setStatus = t => { $("#status").textContent = t ? t + "     —     " + HINT : HINT; };

// ---- loudness lane

let velDrag = null;
velLane.addEventListener("pointerdown", e => {
  velLane.setPointerCapture(e.pointerId);
  checkpoint();
  velDrag = true; setVel(e);
});
velLane.addEventListener("pointermove", e => { if (velDrag) setVel(e); });
velLane.addEventListener("pointerup", () => { if (velDrag) { velDrag = null; changed(); } });
function setVel(e) {
  const { x, y } = local(e, velLane);
  const h = velLane.clientHeight, v = clamp(1 - (y - 6) / (h - 12), 0.05, 1);
  const s = (x - KEYW + sx()) / S.stepW;
  let hits = track().notes.filter(n => Math.abs(n.t + 0.1 - s) < Math.max(0.6, 6 / S.stepW));
  if (S.sel.size && hits.some(n => S.sel.has(n))) hits = hits.filter(n => S.sel.has(n));
  for (const n of hits) n.v = +v.toFixed(2);
  if (hits.length) { draw(); drawVel(); }
}

// ---------------------------------------------------------------- keys

let noteClip = null;
document.addEventListener("keydown", e => {
  const typing = /INPUT|SELECT|TEXTAREA/.test(document.activeElement?.tagName) && document.activeElement.type !== "range" && document.activeElement.type !== "checkbox";
  if ($("#modal").open) return;
  const ctrl = e.ctrlKey || e.metaKey;
  if (ctrl && e.key.toLowerCase() === "s") { e.preventDefault(); save(); return; }
  if (typing) return;
  if (e.code === "Space") { e.preventDefault(); play(); return; }
  if (ctrl && e.key.toLowerCase() === "z") { e.preventDefault(); e.shiftKey ? redo() : undo(); return; }
  if (ctrl && e.key.toLowerCase() === "y") { e.preventDefault(); redo(); return; }
  if (ctrl && e.key.toLowerCase() === "a") { e.preventDefault(); S.sel = new Set(track().notes); draw(); drawVel(); return; }
  if (ctrl && e.key.toLowerCase() === "c") { copyNotes(); return; }
  if (ctrl && e.key.toLowerCase() === "v") { pasteNotes(); return; }
  if (ctrl && e.key.toLowerCase() === "d") { e.preventDefault(); duplicateNotes(); return; }
  if (e.key === "Delete" || e.key === "Backspace") { if (S.sel.size) { checkpoint(); removeNotes([...S.sel]); changed(); } return; }
  if (e.key === "Escape") { S.sel.clear(); S.range = null; showRange(); draw(); drawVel(); return; }
  if (e.key === "Home") { S.marker = 0; scroller.scrollLeft = 0; if (P.playing && P.buffer) startAt(P.buffer, 0); draw(); return; }
  if (["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"].includes(e.key) && S.sel.size) {
    e.preventDefault(); checkpoint();
    const sel = [...S.sel];
    if (e.key === "ArrowUp" || e.key === "ArrowDown") {
      const dir = e.key === "ArrowUp" ? 1 : -1;
      for (const n of sel) {
        if (isDrums()) { const r = clamp(rowOfPitch(n.p) - dir, 0, DRUMS.length - 1); n.p = DRUMS[r].key; }
        else if (e.shiftKey) n.p = clamp(n.p + 12 * dir, LOW, HIGH);
        else if (S.scaleSnap) { let q = n.p + dir; while (!inScale(q) && q > LOW && q < HIGH) q += dir; n.p = clamp(q, LOW, HIGH); }
        else n.p = clamp(n.p + dir, LOW, HIGH);
      }
      if (sel.length === 1) audition(track().instrument, sel[0].p, sel[0].d, sel[0].v);
    } else {
      const d = (e.key === "ArrowRight" ? 1 : -1) * (e.shiftKey ? barSteps() : unit());
      if (sel.every(n => n.t + d >= 0)) for (const n of sel) n.t += d;
    }
    changed();
    return;
  }
  const lens = { "1": 1, "2": 2, "3": 4, "4": 8, "5": 16 };
  if (lens[e.key] && !ctrl) { S.noteLen = lens[e.key]; syncTools(); }
});

function copyNotes() {
  if (!S.sel.size) return;
  const sel = [...S.sel], t0 = Math.min(...sel.map(n => n.t));
  noteClip = sel.map(n => ({ ...n, t: n.t - t0 }));
  setStatus(`Copied ${sel.length} note${sel.length > 1 ? "s" : ""} — Ctrl+V pastes them at the green marker`);
}
function pasteNotes() {
  if (!noteClip) return;
  checkpoint();
  const copies = noteClip.map(n => ({ ...n, t: n.t + S.marker }));
  if (isDrums()) for (const n of copies) if (!DRUMS.some(d => d.key === n.p)) n.p = 36;
  track().notes.push(...copies);
  S.sel = new Set(copies);
  growTo(Math.max(...copies.map(n => n.t + n.d)));
  changed();
}
function duplicateNotes() {
  if (!S.sel.size) return;
  checkpoint();
  const sel = [...S.sel];
  const t0 = Math.min(...sel.map(n => n.t)), t1 = Math.max(...sel.map(n => n.t + n.d));
  const span = Math.ceil((t1 - t0) / beatSteps()) * beatSteps();
  const copies = sel.map(n => ({ ...n, t: n.t + span }));
  track().notes.push(...copies);
  S.sel = new Set(copies);
  growTo(t1 + span);
  changed();
}
// make the song long enough to hold a step
function growTo(step) {
  const bars = Math.ceil(step / barSteps());
  if (bars > S.song.bars) { S.song.bars = Math.min(128, bars); fillSongBar(); }
}

// ---------------------------------------------------------------- range (bars across all tracks)

let barClip = null;
function showRange() {
  $("#rangeBar").hidden = !S.range;
  if (!S.range) return;
  const b = barSteps(), a = S.range.a / b + 1, z = S.range.b / b;   // z: the last bar touched
  const fmt = v => Number.isInteger(v) ? String(v) : v.toFixed(2).replace(/0+$/, "");
  $("#rangeLabel").textContent = `Bars ${fmt(a)} – ${fmt(z)} selected (playback loops here)`;
}
const inRange = (n, a, b) => n.t >= a && n.t < b;
$("#rangeCopy").onclick = () => {
  const { a, b } = S.range;
  barClip = { len: b - a, tracks: S.song.tracks.map(t => t.notes.filter(n => inRange(n, a, b)).map(n => ({ ...n, t: n.t - a }))) };
  setStatus("Copied — select where it should go, then Paste here");
};
$("#rangePaste").onclick = () => {
  if (!barClip) return setStatus("Copy some bars first");
  checkpoint();
  const a = S.range.a;
  S.song.tracks.forEach((t, i) => {
    if (!barClip.tracks[i]) return;
    t.notes = t.notes.filter(n => !inRange(n, a, a + barClip.len));
    t.notes.push(...barClip.tracks[i].map(n => ({ ...n, t: n.t + a })));
  });
  growTo(a + barClip.len);
  changed();
};
$("#rangeDup").onclick = () => {
  checkpoint();
  const { a, b } = S.range, len = b - a;
  for (const t of S.song.tracks) {
    t.notes = t.notes.filter(n => !inRange(n, b, b + len));
    t.notes.push(...t.notes.filter(n => inRange(n, a, b)).map(n => ({ ...n, t: n.t + len })));
  }
  growTo(b + len);
  S.range = { a: b, b: b + len }; showRange();
  changed();
};
$("#rangeClear").onclick = () => {
  checkpoint();
  const { a, b } = S.range;
  for (const t of S.song.tracks) t.notes = t.notes.filter(n => !inRange(n, a, b));
  S.sel.clear();
  changed();
};
$("#rangeOff").onclick = () => { S.range = null; showRange(); draw(); };

// ---------------------------------------------------------------- tools

function seg(id, key, parse = Number) {
  for (const b of document.querySelectorAll(`#${id} button`)) b.onclick = () => { S[key] = parse(b.dataset.v); syncTools(); };
}
seg("noteLen", "noteLen");
seg("chordMode", "chord", String);
function syncTools() {
  for (const b of document.querySelectorAll("#noteLen button")) b.classList.toggle("on", Number(b.dataset.v) === S.noteLen);
  for (const b of document.querySelectorAll("#chordMode button")) b.classList.toggle("on", b.dataset.v === S.chord);
  $("#scaleSnap").checked = S.scaleSnap;
  $("#ghosts").checked = S.ghosts;
  $("#velocity").value = S.velocity;
  const drums = isDrums();
  $("#drumTools").hidden = !drums;
  $("#chordMode").style.opacity = drums ? 0.4 : 1;
  $("#arp").disabled = drums;
}
$("#scaleSnap").onchange = e => { S.scaleSnap = e.target.checked; };
$("#ghosts").onchange = e => { S.ghosts = e.target.checked; draw(); };
$("#velocity").oninput = e => { S.velocity = Number(e.target.value); };
$("#zoomIn").onclick = () => zoom(1.25);
$("#zoomOut").onclick = () => zoom(1 / 1.25);

// drum patterns, written for a 16-step bar: [drum key, steps, loudness]
const PATTERNS = {
  "Gentle — soft kick and shaker": [[35, [0, 8], 0.8], [70, [0, 2, 4, 6, 8, 10, 12, 14], 0.5]],
  "Pop beat": [[36, [0, 8, 10], 0.9], [38, [4, 12], 0.75], [42, [0, 2, 4, 6, 8, 10, 12, 14], 0.45]],
  "Island — congas": [[35, [0, 8], 0.7], [62, [2, 6, 10, 14], 0.55], [64, [3, 11], 0.6], [70, [0, 4, 8, 12], 0.4]],
  "Bouncy — rim and tambourine": [[35, [0, 6, 8], 0.75], [37, [4, 12], 0.55], [54, [2, 6, 10, 14], 0.4]],
  "Lullaby — triangle": [[81, [0], 0.45], [70, [4, 8, 12], 0.3]],
  "Waltz (3 beats)": [[35, [0], 0.8], [37, [4, 8], 0.5], [70, [0, 2, 4, 6, 8, 10], 0.35]],
  "March": [[36, [0, 4, 8, 12], 0.8], [38, [4, 12, 14], 0.6]],
  "Woodblock clock": [[76, [0, 4, 8, 12], 0.5], [35, [0], 0.6]],
};
for (const name of Object.keys(PATTERNS)) { const o = document.createElement("option"); o.value = o.textContent = name; $("#drumPattern").append(o); }
$("#drumPattern").onchange = e => {
  const pat = PATTERNS[e.target.value]; e.target.value = "";
  if (!pat || !isDrums()) return;
  checkpoint();
  const bs = barSteps(), a = S.range?.a ?? 0, b = S.range?.b ?? steps();
  track().notes = track().notes.filter(n => !inRange(n, a, b));
  for (let bar = Math.floor(a / bs) * bs; bar < b; bar += bs)
    for (const [key, at, v] of pat) at.forEach((s, i) => {
      if (s >= bs || bar + s < a || bar + s >= b) return;
      track().notes.push({ t: bar + s, p: key, d: 1, v: +(v * (i % 2 ? 0.75 : 1)).toFixed(2) });
    });
  setStatus(S.range ? "Filled the selected bars" : "Filled the whole song — select bars on the ruler first to fill just those");
  changed();
};

// selected chords → a rolling arpeggio (up and back down) in the current note length
$("#arp").onclick = () => {
  const sel = [...S.sel];
  if (!sel.length) return setStatus("Select a chord first (Shift-drag around it), then press Arpeggio");
  checkpoint();
  const groups = new Map();
  for (const n of sel) { if (!groups.has(n.t)) groups.set(n.t, []); groups.get(n.t).push(n); }
  const made = [];
  for (const [t, notes] of groups) {
    const ps = notes.map(n => n.p).sort((x, y) => x - y);
    const cycle = ps.length > 2 ? [...ps, ...ps.slice(1, -1).reverse()] : ps;
    const span = Math.max(...notes.map(n => n.d)), v = Math.max(...notes.map(n => n.v ?? 1));
    let i = 0;
    for (let s = t; s < t + span; s += S.noteLen, i++) made.push({ t: s, p: cycle[i % cycle.length], d: Math.min(S.noteLen, t + span - s), v: +(v * (i % cycle.length === 0 ? 1 : 0.8)).toFixed(2) });
  }
  removeNotes(sel);
  track().notes.push(...made);
  S.sel = new Set(made);
  changed();
};

// ---------------------------------------------------------------- tracks

function instrumentSelect(value) {
  const sel = document.createElement("select");
  const groups = {};
  for (const inst of INSTRUMENTS) {
    if (!groups[inst.group]) { groups[inst.group] = document.createElement("optgroup"); groups[inst.group].label = inst.group; sel.append(groups[inst.group]); }
    const o = document.createElement("option"); o.value = inst.id; o.textContent = inst.name;
    groups[inst.group].append(o);
  }
  sel.value = value;
  return sel;
}

function renderTracks() {
  const list = $("#trackList"); list.innerHTML = "";
  const soloing = S.song.tracks.some(t => t.solo);
  S.song.tracks.forEach((t, i) => {
    const inst = instrument(t.instrument);
    const el = document.createElement("div");
    el.className = "track" + (i === S.track ? " selected" : "") + (t.mute || (soloing && !t.solo) ? " silent" : "");
    el.style.setProperty("--c", inst.color);
    el.innerHTML = `<input class="name" spellcheck="false">
      <div class="btns">
        <button class="m${t.mute ? " on" : ""}" title="Mute">M</button>
        <button class="s${t.solo ? " on" : ""}" title="Solo: hear only this track">S</button>
        <button class="dup" title="Duplicate track">⧉</button>
        <button class="del" title="Delete track">✕</button>
      </div>
      <div class="sliders"><span>Vol</span><input type="range" class="vol" min="0" max="1.5" step="0.01"><span>Room</span><input type="range" class="rev" min="0" max="1" step="0.01"></div>`;
    const name = el.querySelector(".name");
    name.value = t.name;
    name.onfocus = () => checkpoint();
    name.oninput = () => { t.name = name.value; scheduleSaveOnly(); };
    const pick = instrumentSelect(t.instrument);
    el.querySelector(".btns").after(pick);
    pick.onchange = () => {
      const was = isDrums(t), now = pick.value === "drums";
      if (was !== now && t.notes.length && !confirm("Switching between drums and an instrument clears this track's notes. Go ahead?")) { pick.value = t.instrument; return; }
      checkpoint();
      if (was !== now) t.notes = [];
      t.instrument = pick.value;
      const ins = instrument(t.instrument);
      audition(t.instrument, now ? 36 : Math.round((ins.low + ins.high) / 2 / 12) * 12, 4);
      renderTracks(); syncTools(); scrollToNotes(); changed();
    };
    el.querySelector(".m").onclick = ev => { ev.stopPropagation(); checkpoint(); t.mute = !t.mute; renderTracks(); changed(); };
    el.querySelector(".s").onclick = ev => { ev.stopPropagation(); checkpoint(); t.solo = !t.solo; renderTracks(); changed(); };
    el.querySelector(".dup").onclick = ev => {
      ev.stopPropagation(); checkpoint();
      S.song.tracks.splice(i + 1, 0, JSON.parse(JSON.stringify({ ...t, name: t.name + " copy" })));
      S.track = i + 1; renderTracks(); changed();
    };
    el.querySelector(".del").onclick = ev => {
      ev.stopPropagation();
      if (S.song.tracks.length === 1) return;
      if (t.notes.length && !confirm(`Delete the track "${t.name}"? (Undo brings it back.)`)) return;
      checkpoint(); S.song.tracks.splice(i, 1);
      S.track = clamp(S.track, 0, S.song.tracks.length - 1); S.sel.clear();
      renderTracks(); syncTools(); changed();
    };
    const vol = el.querySelector(".vol"), rev = el.querySelector(".rev");
    vol.value = t.volume ?? 1; rev.value = t.reverb ?? 0;
    for (const s of [vol, rev]) s.addEventListener("pointerdown", () => checkpoint());
    vol.oninput = () => { t.volume = Number(vol.value); changed(); };
    rev.oninput = () => { t.reverb = Number(rev.value); changed(); };
    el.onclick = ev => {
      if (ev.target.closest("input, select, button")) { if (S.track !== i) selectTrack(i); return; }
      selectTrack(i);
    };
    list.append(el);
  });
}

function selectTrack(i) {
  if (S.track === i) return;
  S.track = i; S.sel.clear();
  for (const [j, el] of [...$("#trackList").children].entries()) el.classList.toggle("selected", j === i);
  syncTools(); layout(); scrollToNotes(); draw(); drawVel();
}

function scheduleSaveOnly() { setSaveState("Saving…"); clearTimeout(saveTimer); saveTimer = setTimeout(save, 800); }

$("#addTrack").onclick = async () => {
  const res = await modal("Add a track", `
    <label class="field">Name <input id="mName" type="text" value="New track"></label>
    <label class="field">Instrument <span id="mInstSlot"></span></label>`, () => ({ name: $("#mName").value, instrument: $("#mInstSlot select").value }),
    () => { const s = instrumentSelect("kalimba"); s.onchange = () => audition(s.value, s.value === "drums" ? 36 : 60, 4); $("#mInstSlot").append(s); });
  if (!res) return;
  checkpoint();
  S.song.tracks.push({ name: res.name || instrument(res.instrument).name, instrument: res.instrument, volume: 0.9, reverb: 0.3, mute: false, solo: false, notes: [] });
  S.track = S.song.tracks.length - 1; S.sel.clear();
  renderTracks(); syncTools(); layout(); scrollToNotes(); changed();
};

function scrollToNotes() {
  const t = track();
  if (isDrums(t)) { scroller.scrollTop = 0; return; }
  const ps = t.notes.map(n => n.p);
  const ins = instrument(t.instrument);
  const mid = ps.length ? ps.sort((a, b) => a - b)[Math.floor(ps.length / 2)] : Math.round((ins.low + ins.high) / 2);
  scroller.scrollTop = Math.max(0, (HIGH - mid) * S.rowH - (vh - RULER) / 2);
}

// ---------------------------------------------------------------- song settings

for (const k of KEYS) { const o = document.createElement("option"); o.value = o.textContent = k; $("#key").append(o); }
for (const s of Object.keys(SCALES)) { const o = document.createElement("option"); o.value = s; o.textContent = s; $("#scale").append(o); }

function fillSongBar() {
  const s = S.song, m = s.master ?? (s.master = {});
  // only touch fields whose value differs, so typing never loses its place
  const set = (id, v) => { const el = $(id); if (el.value !== String(v)) el.value = v; };
  set("#title", s.title ?? "");
  set("#bpm", s.bpm); set("#bars", s.bars); set("#beatsPerBar", s.beatsPerBar);
  set("#key", s.key); set("#scale", s.scale); set("#swing", s.swing ?? 0); set("#humanize", s.humanize ?? 0);
  set("#mVolume", m.volume ?? 0.75); set("#mWarmth", m.warmth ?? 0); set("#mReverb", m.reverb ?? 0.35); set("#mEcho", m.echo ?? 0); set("#mEchoTime", m.echoTime ?? 0.34);
  document.title = (s.title || "Untitled") + " — Capy Music Studio";
}

function bindSong(id, apply, live = false) {
  const el = $(id);
  el.addEventListener("pointerdown", () => checkpoint());
  el.addEventListener("focus", () => { if (el.type !== "range") checkpoint(); });
  el.addEventListener(live ? "input" : "change", () => { apply(el.value); fillSongBar(); layout(); changed(); });
}
bindSong("#title", v => {
  S.song.title = v;
  const o = $("#songList").selectedOptions[0];
  if (o) o.textContent = v + "  (" + S.file + ")";
}, true);
bindSong("#bpm", v => { S.song.bpm = clamp(Number(v) || 90, 40, 220); });
bindSong("#bars", v => { S.song.bars = clamp(Math.round(Number(v)) || 8, 1, 128); });
bindSong("#beatsPerBar", v => { S.song.beatsPerBar = Number(v); });
bindSong("#key", v => { S.song.key = v; });
bindSong("#scale", v => { S.song.scale = v; });
bindSong("#swing", v => { S.song.swing = Number(v); });
bindSong("#humanize", v => { S.song.humanize = Number(v); });
bindSong("#mWarmth", v => { S.song.master.warmth = Number(v); });
bindSong("#mVolume", v => { S.song.master.volume = Number(v); });
bindSong("#mReverb", v => { S.song.master.reverb = Number(v); });
bindSong("#mEcho", v => { S.song.master.echo = Number(v); });
bindSong("#mEchoTime", v => { S.song.master.echoTime = Number(v); });

// ---------------------------------------------------------------- songs

const slug = s => (s || "song").toLowerCase().normalize("NFKD").replace(/[^\w]+/g, "_").replace(/^_+|_+$/g, "").slice(0, 48) || "song";

let songs = [];
async function refreshSongList() {
  try { songs = await api("/api/songs"); } catch { songs = []; }
  const sel = $("#songList"); sel.innerHTML = "";
  for (const s of songs) {
    const o = document.createElement("option"); o.value = s.file;
    o.textContent = (s.file === S.file ? S.song.title : s.title) + "  (" + s.file + ")";
    sel.append(o);
  }
  if (S.file) sel.value = S.file;
}

async function loadSong(file) {
  if (saveTimer) await save();
  stopSource(); updatePlay();
  const song = JSON.parse(await (await fetch(`/api/songs/${file}`)).text());
  S.song = song; S.file = file; S.track = 0; S.sel.clear(); S.marker = 0; S.range = null;
  history.length = 0; future.length = 0;
  P.key = null; P.buffer = null;
  try { localStorage.setItem("capyStudio.last", file); } catch { }
  fillSongBar(); renderTracks(); syncTools(); showRange(); layout();
  scroller.scrollLeft = 0; scrollToNotes();
  setSaveState("Saved ✓");
  $("#undo").disabled = true; $("#redo").disabled = true;
  await refreshSongList();
  draw(); drawVel();
}

async function createSong(title, file, template) {
  if (songs.some(s => s.file === file)) file = file + "_" + Date.now().toString(36).slice(-4);
  const song = template ? { ...JSON.parse(JSON.stringify(template)), title } : { ...newSong(file), title };
  await api(`/api/songs/${file}`, { method: "PUT", body: JSON.stringify(song) });
  await refreshSongList();
  await loadSong(file);
}

$("#songList").onchange = e => loadSong(e.target.value);
$("#newSong").onclick = async () => {
  const res = await modal("A new song", `
    <label class="field">What's it called? <input id="mTitle" type="text" value="My new song"></label>
    <p class="muted">It starts with four empty tracks — melody, chords, bass and drums — in C major at 90 bpm. Change any of it at the top.</p>`,
    () => $("#mTitle").value.trim());
  if (res) await createSong(res, slug(res));
};
$("#copySong").onclick = async () => {
  const res = await modal("Save a copy", `<label class="field">Name of the copy <input id="mTitle" type="text" value="${(S.song.title || "Song").replace(/"/g, "&quot;")} (copy)"></label>`,
    () => $("#mTitle").value.trim());
  if (res) { if (saveTimer) await save(); await createSong(res, slug(res), S.song); }
};
$("#deleteSong").onclick = async () => {
  const ok = await modal("Delete this song?", `<p>“${S.song.title}” moves to <b>songs/trash</b>, so it isn't lost for good.</p>`, () => true);
  if (!ok) return;
  clearTimeout(saveTimer); saveTimer = 0;
  await api(`/api/songs/${S.file}`, { method: "DELETE" });
  S.file = null;
  await refreshSongList();
  if (songs.length) await loadSong(songs[0].file);
  else await createSong("My first song", "my_first_song");
};

// ---------------------------------------------------------------- export into the game

$("#export").onclick = async () => {
  const cur = S.song.name;
  const res = await modal("Put this song in the game", `
    <p>The song is mixed and saved into <b>Assets/Resources/Music</b>. The game plays it in place of:</p>
    <label class="choice"><input type="radio" name="slot" value="music_day" ${cur === "music_day" ? "checked" : ""}><span><b>Daytime music</b><small>music_day — plays from morning to evening</small></span></label>
    <label class="choice"><input type="radio" name="slot" value="music_night" ${cur === "music_night" ? "checked" : ""}><span><b>Night music</b><small>music_night — the island crossfades to it after dark</small></span></label>
    <label class="choice"><input type="radio" name="slot" value="other" ${cur !== "music_day" && cur !== "music_night" ? "checked" : ""}><span><b>A new music file</b><small>for later — saved as <input id="mOther" type="text" value="${cur && cur !== "music_day" && cur !== "music_night" ? cur : slug(S.song.title)}" size="18"></small></span></label>
    <p class="muted">The song loops in the game, so its end runs straight back into its start.</p>`,
    () => { const v = document.querySelector('input[name="slot"]:checked')?.value; return v === "other" ? slug($("#mOther").value) : v; }, null, "Put in game");
  if (!res) return;
  checkpoint();
  S.song.name = res;
  await save();
  try {
    setSaveState("Mixing for the game…");
    const r = await api(`/api/export/${S.file}`, { method: "POST", body: JSON.stringify(S.song) });
    setSaveState("Saved ✓");
    await modal("It's in the game ✓", `<p>Saved <b>${r.path}</b> (${r.seconds.toFixed(1)} seconds).</p><p>Switch to Unity and it picks the new file up by itself; press Play to hear it on the island.</p>`, () => true, null, "Lovely", true);
  } catch (e) {
    setSaveState("");
    await modal("That didn't work", `<p>${e.message}</p>`, () => true, null, "OK", true);
  }
};

// ---------------------------------------------------------------- help

$("#help").onclick = () => modal("How to use the studio", `
  <ul>
    <li><b>Add notes:</b> click the grid. Drag while placing to make the note longer. Pick the length first (1/16 … 1 bar, or keys <kbd>1</kbd>–<kbd>5</kbd>).</li>
    <li><b>Move</b> a note by dragging it; <b>lengthen</b> it by dragging its right edge; <b>delete</b> with right-click or double-click.</li>
    <li><b>Select several:</b> Shift-drag a box. Then drag them together, <kbd>↑</kbd><kbd>↓</kbd> to move up and down the scale (<kbd>Shift</kbd> = a whole octave), <kbd>←</kbd><kbd>→</kbd> to shift in time, <kbd>Ctrl</kbd>+<kbd>C</kbd>/<kbd>V</kbd> to copy and paste at the green marker, <kbd>Ctrl</kbd>+<kbd>D</kbd> to repeat them, <kbd>Del</kbd> to delete. Alt-drag drags out a copy.</li>
    <li><b>Chords:</b> choose Triad or 7th and one click places a whole chord that fits the key.</li>
    <li><b>Stay in key</b> keeps every note on the song's scale (the green rows), so it always sounds right.</li>
    <li><b>Drums:</b> click a lane to add a hit, click it again to take it away, drag along to paint a row of hits. Or pick a ready <b>Drum pattern</b>.</li>
    <li><b>Loudness:</b> drag the dots in the strip under the grid.</li>
    <li><b>The ruler:</b> click to move the green start marker; drag across it to select bars — playback then loops just those, and you can copy, repeat or clear them across every track.</li>
    <li><b>Listen:</b> <kbd>Space</kbd> plays and stops. Edits are heard straight away while it plays. Click a key on the keyboard to hear a note.</li>
    <li><b>Zoom:</b> <kbd>Ctrl</kbd>+scroll; <kbd>Alt</kbd>+scroll makes rows taller.</li>
    <li><b>Undo</b> anything with <kbd>Ctrl</kbd>+<kbd>Z</kbd>. Songs save by themselves.</li>
    <li><b>Put in game…</b> mixes the song into the island's day or night music.</li>
  </ul>
  <p class="muted">Songs live in Tools/MusicStudio/songs as plain files — Claude can read and write them too, so you can ask for a tune and then change it here.</p>`, () => true, null, "Got it", true);

// ---------------------------------------------------------------- dialogs

function modal(title, html, read, init, okText = "OK", alertOnly = false) {
  const dlg = $("#modal");
  $("#modalTitle").textContent = title;
  $("#modalBody").innerHTML = html;
  $("#modalOk").textContent = okText;
  $("#modalCancel").hidden = alertOnly;
  init?.();
  dlg.showModal();
  dlg.querySelector("input[type=text]")?.select();
  return new Promise(resolve => {
    dlg.onclose = () => resolve(dlg.returnValue === "ok" ? read() : null);
  });
}

// a dropdown keeps the keyboard after a pick; hand it back so Space still plays
document.addEventListener("change", e => { if (e.target.tagName === "SELECT" || e.target.type === "checkbox") e.target.blur(); });

// ---------------------------------------------------------------- transport wiring

$("#play").onclick = () => play();
$("#toStart").onclick = () => { S.marker = 0; scroller.scrollLeft = 0; if (P.playing && P.buffer) startAt(P.buffer, 0); draw(); };
$("#loop").onclick = () => {
  S.loop = !S.loop; $("#loop").classList.toggle("on", S.loop);
  if (P.playing && P.buffer) startAt(P.buffer, position());
};
$("#undo").onclick = undo;
$("#redo").onclick = redo;

function tick() {
  if (P.playing && S.song) {
    const step = positionStep();
    const bar = Math.floor(step / barSteps()) + 1, beat = Math.floor((step % barSteps()) / beatSteps()) + 1;
    $("#clock").textContent = `${bar}.${beat}`;
    // keep the playhead in view
    const x = xOfStep(step);
    if (x > vw - 40 || x < KEYW) scroller.scrollLeft = Math.max(0, step * S.stepW - 40);
    draw();
  } else if (S.song) {
    const bar = Math.floor(S.marker / barSteps()) + 1, beat = Math.floor((S.marker % barSteps()) / beatSteps()) + 1;
    $("#clock").textContent = `${bar}.${beat}`;
  }
  requestAnimationFrame(tick);
}

// the server stops by itself once this window has been closed for a while
const ping = () => fetch("/api/ping", { method: "POST" }).catch(() => { });
setInterval(ping, 15000); ping();

window.addEventListener("beforeunload", () => { if (saveTimer) navigator.sendBeacon?.(`/api/songs/${S.file}`, JSON.stringify(S.song)); });

// ---------------------------------------------------------------- start

new ResizeObserver(resize).observe(scroller);
(async function start() {
  syncTools(); setStatus();
  await refreshSongList();
  let last = null;
  try { last = localStorage.getItem("capyStudio.last"); } catch { }
  if (songs.length) await loadSong(songs.some(s => s.file === last) ? last : songs[0].file);
  else await createSong("My first song", "my_first_song");
  resize();
  requestAnimationFrame(tick);
})();

// for poking at the studio from the browser console (and for automated checks)
window.capyStudio = { S, P, play, undo, redo, save, track };
