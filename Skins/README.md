# Holiday skins

A skin is a look the island wears for a few days: Halloween, Christmas, and any other holiday you add.
**Installed games pick up new skins from GitHub, with no new APK.** Each time the game starts, it reads
`Skins/skins.json` from the `main` branch of this repo. If today's date falls inside a skin's dates,
the game downloads that skin's folder, keeps a copy on the phone and wears it. Every start it checks
the skin's files again, so pictures you add or redraw reach players on their next start (a phone
that's offline uses the copy it has). On the day after the end date the island goes back to normal
by itself, even if the game is open.

Skins only change the island's **fixed scenery**: the land, the bridges, the torii gates, the stone
lanterns, the sea lanterns, wild grass and flowers. They never change things players buy or build.

## The schedule: `skins.json`

```json
{
  "skins": [
    { "id": "halloween", "from": "10-25", "to": "10-31" },
    { "id": "christmas", "from": "12-11", "to": "12-26" }
  ]
}
```

- `id` is the name of the skin's folder.
- `"10-25"` (month-day) repeats every year. `"2027-04-01"` is a single day in a single year.
- Both ends are included, and the date is the player's own phone date.
- A yearly window can cross New Year (`"12-28"` to `"01-03"`). If two skins overlap, the first one
  in the list wins.
- To end a skin early, delete its line (or change its dates) and push.

## A skin's folder: `Skins/<id>/skin.json`

```json
{
  "version": 1,
  "tint": "#EEF3FF",
  "sprites": ["Toro", "Land_00_CapyIsland_Grass"],
  "decor": [{ "file": "decor/XmasTree.png", "count": 5, "height": 2.0 }],
  "falling": { "file": "fx/Snowflake.png", "count": 70, "size": 0.012, "speed": 0.07, "sway": 0.02, "spin": false },
  "music": "music/christmas.ogg",
  "greeting": {
    "title": "Merry Christmas!", "body": "…",
    "titleHe": "…", "bodyHe": "…",
    "titleHeGirl": "", "bodyHeGirl": "",
    "picture": "decor/Snowcapy.png"
  }
}
```

Every part is optional.

| Field | What it does |
|---|---|
| `version` | Raise it to show the greeting card again. Pictures don't need it: on every start the game checks each file against GitHub and downloads only new or changed ones (unchanged files come back "not modified" almost for free). |
| `tint` | Multiplies the island's light. Keep it close to white (e.g. `#EEF3FF`), because the day/night colours still apply on top. |
| `sprites` | Sprite names to replace. Each picture goes at `sprites/<name>.png`. `ART_NAMES.txt` lists every sprite name with its size. Draw the same proportions at any size: the game scales it to the original's size and pivot. |
| `decor` | Pictures scattered on open grass of every island (yours and friends'). `height` is the picture's full height in world units: a capybara is about 1.2, a stone lantern 1.5. Decorations stand on the bottom edge of their picture, never block building, and hide if something gets built over them. |
| `falling` | Something drifting down over the island. `size` is a share of the screen's height, `speed` is screen heights per second, `sway` is sideways drift. |
| `music` | A tune that replaces the morning and afternoon music; the night keeps its lullaby. Use `.ogg` for Android and `.mp3` if WebGL players on Safari matter. Make it in Capy Music Studio. |
| `greeting` | A card shown once per skin version, after the island loads. The English is required. Hebrew is spoken to a boy; fill the `…HeGirl` lines only where a girl's words differ. |

The torii gates (`Torii_Front`, `Torii_Side`) are cut up in code, so their skin pictures must keep
the original layout: same canvas, feet in the same place. `Tools/ChatGPT/batch_skins.sh` makes them
that way. It draws over the game's own picture, then `alignfeet.mjs` fits the result back onto the
original's canvas and feet. The island's land is recoloured in code (only the greens change), so its
outline matches exactly.

## Making one

1. Create `Skins/<id>/` with a `skin.json` and its pictures. For art, add the skin to
   `Tools/ChatGPT/batch_skins.sh` and run it.
2. In Unity, open **CapyIsland ▸ Holiday Skins** and click **Preview** next to the skin. Then press
   Play: the editor wears it from this folder, whatever the date. **Stop previewing** returns to the
   real schedule.
3. Add the dates to `skins.json`.
4. Commit and push to **`main`**. That is the moment it goes live: GitHub's servers can take about 5
   minutes to serve the new files, and every player gets the skin on their next start inside the
   dates.

Keep pictures about the size they show at (256px for props, 64px for falling bits). Every player
downloads the whole folder.
