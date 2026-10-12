# Capy Music Studio

Compose Capy Island's music in its own window, and put it straight into the game.

**Open it:** double-click `Capy Music Studio.bat` (or the desktop shortcut). It starts a small local
server, hidden, and opens the studio as its own window. Close the window and the server stops by
itself a minute later. Needs Node.js.

- 26 synthesised instruments (music box, kalimba, glockenspiel, marimba, steel drum, piano, electric
  piano, harp, guitar, ukulele, pizzicato, strings, flute, ocarina, clarinet, brass, choir, pads,
  leads, chiptune, four basses) and a 15-piece drum kit. Everything is generated, nothing recorded.
- Piano roll per track, scale highlighting and "stay in key", one-click chords, arpeggios, drum
  patterns, loudness lane, bar selection (loop / copy / repeat / clear), undo, autosave.
- **Put in game…** renders the song to `Assets/Resources/Music/music_day.wav` or `music_night.wav`
  (32 kHz mono), which `MusicPlayer.cs` plays. The press of that button is the only thing that
  changes the game.

## Files

- `songs/*.json` — the songs. Plain JSON, one note per line: `t` start step, `d` length in steps
  (a step is a 1/16 note), `p` MIDI pitch (drum tracks: the drum's key from `DRUMS` in
  `app/engine.js`), `v` loudness 0–1.
- `app/engine.js` — every instrument and the mixer. Shared by the window and by Node, so the
  preview and the exported file are the same sound.
- `render.js` — renders without the window:
  `node Tools/MusicStudio/render.js starry_shore --out test.wav` to listen, or
  `node Tools/MusicStudio/render.js starry_shore` to write it into the game under the song's `name`.
- `server.js` — serves the window, saves songs, exports. Deleted songs go to `songs/trash/`.
