# ChatGPT bridge

Lets Claude Code consult ChatGPT and generate 2D assets for CapyIsland.

## Backends
- **codex (default)**: drives the CLI that ships with the Codex desktop app, which is signed in
  with the ChatGPT account. Questions and images come out of the ChatGPT plan's allowance, not API
  credits. The CLI is found automatically under `%LOCALAPPDATA%\OpenAI\Codex\bin\<hash>\codex.exe`
  (newest wins, so app updates are picked up). Set `CODEX_CLI` to override.
  If it stops working, open the Codex app and check you are still signed in.
- **api**: the OpenAI API with `OPENAI_API_KEY` (paid credits). Pick it with `--via api` or
  `CHATGPT_VIA=api`. It is also used automatically if the Codex app isn't installed.

## Commands
node Tools/ChatGPT/chatgpt.mjs ask "How should the shop UI read at 1080p?" [--ref sprite.png]
node Tools/ChatGPT/chatgpt.mjs image "<prompt>" --out Assets/Art/Generated/coin.png --size 1024x1024 --transparent

Sizes: 1024x1024, 1536x1024, 1024x1536. Quality: low | medium (default) | high (API only).

## Codex differences
- Codex's image tool has no size, quality or path arguments. `--size` only picks the shape;
  square output comes back at about 1254x1254. The downscale step resizes it anyway.
- An image takes about 50 s; a question takes about 7 s.
- `ask` accepts `--ref` too, so ChatGPT can look at a sprite before giving art direction.

## API key setup (only for --via api)
1. Create a key at https://platform.openai.com/api-keys and add billing.
2. PowerShell (never paste it in chat or commit it):
   [Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "sk-...", "User")
3. Fully restart VS Code so the new variable is picked up.

## Notes
- `--transparent` asks for real alpha, but the model still paints a background unless the
  prompt explicitly asks for one ("on a fully transparent background, no backdrop").
  Expect a soft halo on transparent output; nothing here strips it yet.
- `--ref a.png,b.png` passes existing PNGs as references to keep new sprites on-model.
  Say "FIRST"/"SECOND" in the prompt when passing more than one.
- Generate sequentially, not in parallel.
- Generated art lands in Assets/Art/Generated/ — run an asset refresh so Unity imports it,
  then set the texture importer (Sprite 2D, PPU, filter mode) to match the rest of Assets/Art.
