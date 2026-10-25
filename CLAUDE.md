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
- Harvesting a **parent** node turns it into a **free bubble** of the same item (it does not regrow in place), and workers take children before parents.
- Harvest per hit is `0.1 × speed / 3` — the /3 is easy to miss.
- The SWF runs at **30 fps**; every rate in the config is per frame.

## Licensing boundary

The decompiled ActionScript is the original developers' copyrighted code. Mechanics, formulas and
balance numbers are fine to learn from and reimplement. **Never copy AS3 source, art, or audio into
`Assets/`.** Write original C#.

## Words the player reads

Capy Island has its own story ("The Big Chill") and cast: Old Yuzu, Miso, Captain Wasabi, Salty Kombu,
Elder Matcha. **Every visible name and line lives in `Assets/Scripts/Game/StoryText.cs`.** Item and
achievement *names* in code ("Coriander", "The Girl's New Dress", "Deer"…) are save-file ids only and
must never be shown directly — show `StoryText.Name(id)` / `MoteDef.Title`. The `*.g.cs` data tables
and their generators carry ids and numbers only; never re-introduce prose from `docs/reference/*.xml`.

**Sound and music are ours, and generated.** `Tools/fx/sfx.mjs` makes every effect and the ambience
(waves, wind, rain, birdsong); music is composed in **Capy Music Studio** (`Tools/MusicStudio/`: songs are JSON in
`songs/`, instruments in `app/engine.js`, `render.js` renders them; see its README). The user
composes there too, so write songs as JSON they can open and edit, and only render into
`Assets/Resources/Music` when asked — they disliked the old `Tools/Audio/music.mjs` tunes. Never add recorded or third-party audio. Ambience must stay soft: band-limited noise, mastered
quiet — harsh hissy noise was rejected. Append new sounds at the end of sfx.mjs so its seeded noise, and
therefore every existing file, stays byte-identical.

**Hebrew.** Code keeps writing English; `UI/Lang/LocText` translates every label as it is drawn, from
`Assets/Resources/Lang/he/*.txt` (`@ English` / `= Hebrew`, `{0}` templates; format in `Loc.cs`).
When you add or change player-visible text, add its Hebrew entry too. Playing in Hebrew in the editor
appends anything untranslated to `Logs/loc_missing_he.txt`. **The player picks boy or girl** (opening
screen and Settings, Hebrew only): the `=` line speaks to a boy, and an optional `~` line right under it
speaks to a girl. Add a `~` line whenever the words for a girl differ (הקש/הקישי, אתה/את, קטן/קטנה);
spellings that are the same for both (לך, שלך, אליך) need none. Text shown before the choice
(the opening screen) is written neutral: infinitive or "let's".

## Layout

- `Assets/Scripts/Game/` — gameplay (`Worker`, `Workplace`, `Harvestable`, `Placeable`,
  `ResourceStore`, `Bubble`, `TutorialDirector`), `Wardrobe`, `DailyGifts`, `Objectives`
- `Assets/Scripts/Game/Visits/` — fake multiplayer: bot friends, generated islands, flights (`Visit`)
- `Assets/Scripts/Game/Hobbies/` — fishing, birdwatching, books, sewing (`HobbyLog`)
- `Assets/Scripts/UI/Cutscenes/` — little movies for big moments (`Cutscene` base: paused game, cinema bars, tap to skip; wedding, Captain's dance). Backdrops in `Assets/Resources/Cutscenes/`, masters in `ArtSource/Cutscenes/`
- `Assets/Scripts/UI/` — runtime-built HUD (`MobileHud` bootstraps it), `UiKit`/`IconPainter`, screens
- `Assets/Scripts/CameraRig/`, `Assets/Scripts/UI/`, `Assets/Scripts/Core/`
- `Assets/Editor/IslandBuilder.cs`
- `docs/` — design and reference material
