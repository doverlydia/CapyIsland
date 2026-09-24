# CapyIsland

Unity **6000.3.6f1**, URP 17.3.0. A capybara-island colony game, structurally inspired by the 2009
Flash game *Bunni: How We First Met*.

## Read this before answering anything about Bunni's mechanics

This project is a reimplementation, so questions about "how does Bunni do X" come up constantly.
**Do not answer them from memory or from general knowledge of the game — you will be wrong.**
The game has been decompiled and the real answers are in the repo.

Order of precedence:

1. **`docs/BUNNI_GROUND_TRUTH.md`** — mechanics read directly out of the decompiled AS3. Numbers,
   formulas, state machines. This is authoritative. Check here first.
2. **`docs/reference/*.xml`** — the shipped balance config (`game.xml`, `store_items.xml`,
   `save_data.xml`), including the original designer's inline intent comments.
3. **The decompiled source itself** — 365 AS3 classes at
   `C:\Users\dover\Documents\bunniWiki\decompiled\LandOfBunnies\scripts\`.
   Not in this repo (it is third-party code). **Grep it rather than speculating.** Useful entry
   points: `MapItems/Animals/States/` (the worker job FSM), `MapItems/Terrain/` (node growth and
   seeding), `Quests/`, `Rewards/`.
4. **`docs/BUNNI_MECHANICS.md`** — earlier notes from screenshots, the wiki and a playthrough.
   Good for story, art direction and UI layout. **Where it disagrees with (1), it is out of date
   and (1) wins.**

If none of those answer the question, say the source doesn't cover it and offer to grep the
decompiled classes. Do not fill the gap with a plausible guess — the user has had to correct
invented mechanics repeatedly, and that is what these files exist to prevent.

### Things that are counter-intuitive and get guessed wrong

- Harvesting is **hit-frames removing scale**, not a timer. `speed` = scale removed per swing.
- `<seedrates>` is a **frame countdown — lower is faster**. Ironwood and gold reseed fastest.
- Roses **do** regrow, and faster than posies. "Non-renewable" is designer intent, not code.
- Harvesting a **parent** node regenerates its entire cluster.
- The SWF runs at **30 fps**; every rate in the config is per frame.

## Licensing boundary

The decompiled ActionScript is the original developers' copyrighted code. Mechanics, formulas and
balance numbers are fine to learn from and reimplement. **Never copy AS3 source, art, or audio into
`Assets/`.** Write original C#.

## Layout

- `Assets/Scripts/Game/` — gameplay (`Worker`, `Workplace`, `Harvestable`, `Placeable`,
  `ResourceStore`, `Bubble`, `TutorialDirector`)
- `Assets/Scripts/CameraRig/`, `Assets/Scripts/UI/`, `Assets/Scripts/Core/`
- `Assets/Editor/IslandBuilder.cs`
- `docs/` — design and reference material
