# Capy Island trailer

Made in [Remotion](https://www.remotion.dev): the video is React code. The opening (the Big Chill
melting away) is drawn from the game's own sprites; everything after it is real play, recorded in
the Unity editor frame by frame with its sound. Music and sounds are our own.

    npm install
    npm run import        # sprites, music, sounds, fonts from Assets/, and index public/clips
    npm run studio        # live preview with a timeline, in the browser
    npm run render        # the gameplay trailer (about 39 s): out/trailer.mp4
    npm run render:15     # the first 15-second motion-graphics cut: out/trailer_15s.mp4
    npm run render:melt   # the 5-second snow-melt test: out/snow_melt.mp4

- `src/TrailerGameplay.tsx`: the trailer. `TEXT` is every word on screen; `SHOTS` is the cut
  (which recording, from which frame, how many beats, speed, push-in, caption, game sound).
  Cut to "Morning on the Island" at 96 bpm: every shot starts on a beat.
- `public/clips/<name>/`: the recordings (0000.jpg…, `audio.wav`, `taps.json` for the pointing
  hand). Not in git: about 1 GB. `scripts/collect-clips.mjs` writes `src/clips.gen.ts`.
- `src/Trailer15.tsx`, `src/melt.tsx`, `src/World.tsx`, `src/layout.ts`: the snowy opening.
- `src/Hobbies.tsx`, `src/EndCard.tsx`, `src/Caption.tsx`: cards, the last shot, on-screen text.

Clips were filmed in Play with a fixed 1/30 s clock (`Time.captureDeltaTime`), `AudioRenderer`
for the sound, the game's music muted, and most of the HUD hidden. The friend Luna is staged
(a local puppet and a fake live link driving the real chat UI), not a second player.
