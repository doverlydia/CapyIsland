---
name: art-director
description: Generate CapyIsland's 2D art (sprites, icons, tiles, UI, backgrounds) via ChatGPT/OpenAI, or ask ChatGPT for art direction. Use whenever the project needs a new visual asset or a call on palette, shape language, or composition. All art for this project is AI-generated through this bridge.
---

# Art Director (ChatGPT via OpenAI)

Bridge: `Tools/ChatGPT/chatgpt.mjs` (Node). By default it runs through the **Codex app's CLI**, signed
in with the user's ChatGPT account, so it costs no API credits. `--via api` uses `OPENAI_API_KEY`
instead (paid credits; only when asked). Via Codex there is no size/quality control: `--size` only
sets the shape (output is ~1254px square, 1536x1024 landscape), and `--quality` is ignored.
If Codex reports a usage limit, tell the user rather than switching to the API.

CapyIsland is a capybara-themed recreation of **Bunni: How We First Met**. The art must read as that
game's cozy island-builder look. Every asset goes through this bridge — no hand-drawn or stock art.

## The look

Match these, and state them in every image prompt:

- **Camera:** 3/4 top-down oblique, as if viewed ~50° above. Objects show their top *and* front face.
  Never straight side-on, never true isometric, never front-facing.
- **Rendering:** clean vector-style fills with soft airbrushed gradients. Smooth, slightly glossy,
  almost claymation-soft. No pixel art, no canvas/paper texture, no visible brush strokes, no noise.
- **Silhouette:** chunky, rounded, generous. No thin spindly parts. Reads clearly at 64px.
- **Outlines:** dark warm brown (not pure black), medium weight, on characters and built props only.
  Terrain, foliage and ground tiles have **no** outline — they separate by value instead.
- **Characters:** oversized head, small rounded body, tiny limbs, simple dot eyes, minimal or no mouth.
  Calm and friendly. Capybaras are warm tan-brown (#B5813F family) with a darker muzzle.
- **Palette:** muted and natural, nothing neon. Grass mid-green (#7FAF4F), darker tree canopy
  (#4F7F3A), sand (#E0C99A), shallow water (#8FCBE0), wood (#8A5A32), roofs and accents in dusty
  terracotta and slate. Keep saturation moderate.
- **Lighting:** soft, top-down, one gentle light. A soft elliptical contact shadow sits under
  characters and props — flat on the ground, not a baked drop shadow around the sprite.

## Workflow for a new asset

1. **Get the prompt from the Art Director first** — it keeps assets consistent:
   `node Tools/ChatGPT/chatgpt.mjs ask "Write an image prompt for: <asset>. Follow this direction: <paste the look section>"`
2. **Generate:**
   `node Tools/ChatGPT/chatgpt.mjs image "<prompt>" --out Assets/Art/Generated/<name>.png --transparent`
   Sizes: 1024x1024 (default), 1536x1024 (wide), 1024x1536 (tall). Quality defaults to medium; use
   `--quality high` for hero art and UI that scales large.
3. **Look at the result** (Read the PNG) before wiring it into anything. Regenerate if it drifted.
4. Move it out of `Generated/` into a real folder (`Assets/Art/Capybaras/`, `Terrain/`, `UI/`, `Props/`)
   and set the texture importer: Sprite (2D and UI), PPU consistent with its neighbours, Point filter
   off (this art is smooth, use Bilinear), compression high quality.

## Keeping assets on-model

Plain text-to-image drifts toward plush/3D-render/clay looks between calls. Pass an approved asset as
a reference to hold the line:

    node Tools/ChatGPT/chatgpt.mjs image "<prompt: 'Match the attached sprite's style, shading and
    proportions EXACTLY. Draw instead: ...'>" --out Assets/Art/Props/barrel.png --transparent \
      --ref Assets/Art/Capybaras/Capy_Idle.png

Several refs are allowed (`--ref a.png,b.png`) — say "FIRST"/"SECOND" in the prompt to distinguish them.
Once a few assets are approved, always `--ref` against one of them.

## Transparency

`--transparent` sets the API flag but is **not** sufficient on its own — the model will paint a
background anyway. The prompt must also say:

> on a fully transparent background, no backdrop, no scenery, no ground plane, subject only, centered
> with a small margin

Expect a faint halo at the edges regardless; there is no cleanup script in this project yet. If halos
start showing against game backgrounds, port `cleanalpha.mjs` / `defringe.mjs` from OverTheHedgehog2D.

## Mobile constraints

The game must work in portrait and landscape, so:
- Generate UI icons square and centred with even margin — they get anchored to different edges per
  orientation and must not look cropped.
- Don't bake text into art; text is TMP so it can reflow.
- Avoid art that assumes a fixed aspect ratio. Backgrounds should over-extend past the safe area.

## Build targets: WebGL and Android

Both targets are memory- and download-constrained, and the bridge returns 1024px PNGs. **Source art is
not shipping art.** For every generated asset:

- Keep the 1024px original in `Assets/Art/Source/` (excluded from builds) and downscale a shipping
  copy to the size it actually renders at — most props and characters need 128-256px, UI icons 64-128px.
- Put shipping sprites in a **Sprite Atlas** per category. Dozens of loose sprites is the single
  biggest cause of a bloated WebGL build and slow first load.
- Set Max Size on the importer (don't rely on the file being small) and enable Crunch compression for
  WebGL; ASTC on Android.
- Prefer one tiling/repeating terrain sprite over many unique large ones.

Watch total uncompressed texture memory — WebGL builds get unstable well before desktop would.

## Shadows: never bake them

The model renders "contact shadow" as a **cream/white ellipse**, which reads as a bright patch on
grass, not a shadow. Do not ask for one. Every prompt should say:

> no shadow, no cast shadow, no ground shadow, no glow beneath the subject

Shadows belong in the engine: a separate soft dark ellipse sprite parented under each prop, tinted
and scaled per item. That also lets shadows sit correctly on slopes, water edges and varied terrain,
which a baked ellipse never can.

## Keeping the set consistent

Generated blind, assets drift badly between calls: the first pine tree came back as a soft 3D
claymation render with no outline at all, while the rock hill next to it came back correctly as
outlined vector. They do not belong in the same game.

So, in order:

1. Generate one asset and approve it by eye. `Assets/Art/Source/Hill_Gray.png` is the current
   reference for outlined props.
2. Generate everything else with `--ref` against an approved asset, and open the prompt with:
   "Match the attached image's rendering style, outline weight, shading and palette EXACTLY.
   Draw instead: ..."
3. Never state a rule as a negative the model can over-apply. "The canopy has no outline" produced
   a tree with no outline anywhere. Say what you want, not what you don't.

Always composite a new asset over the grass colour (#7FAF4F) before judging it. A sprite viewed on
a dark background hides both halos and wrong-coloured shadows.

## Rate limit

~5 images/minute. Generate sequentially, never in parallel.
