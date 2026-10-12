// Renders songs off the main thread, so the window never freezes while a song is mixed.
import { createEngine, DEFAULT_SR } from "./engine.js";

const engine = createEngine(DEFAULT_SR);

onmessage = e => {
  const { id, song } = e.data;
  try {
    const samples = engine.renderSong(song);
    postMessage({ id, samples, sr: DEFAULT_SR }, [samples.buffer]);
  } catch (err) {
    postMessage({ id, error: String(err?.stack ?? err) });
  }
};
