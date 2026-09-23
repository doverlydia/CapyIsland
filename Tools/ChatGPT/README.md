# ChatGPT bridge

Lets Claude Code consult ChatGPT and generate 2D assets for CapyIsland via the OpenAI API.

## Setup
The key is already set as a **user** environment variable on this machine (shared with the
OverTheHedgehog2D project). If it ever goes missing:

1. Create a key at https://platform.openai.com/api-keys and add billing.
2. PowerShell (never paste it in chat or commit it):
   [Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "sk-...", "User")
3. Fully restart VS Code so the new variable is picked up.

In a fresh shell you can pull it in with:
  $env:OPENAI_API_KEY = [Environment]::GetEnvironmentVariable("OPENAI_API_KEY","User")

## Commands
node Tools/ChatGPT/chatgpt.mjs ask "How should the shop UI read at 1080p?"
node Tools/ChatGPT/chatgpt.mjs image "<prompt>" --out Assets/Art/Generated/coin.png --size 1024x1024 --transparent

Sizes: 1024x1024, 1536x1024, 1024x1536. Quality: low | medium (default) | high.

## Notes
- `--transparent` sets the API flag, but the model still paints a background unless the
  prompt explicitly asks for one ("on a fully transparent background, no backdrop").
  Expect a soft halo on transparent output; nothing here strips it yet.
- `--ref a.png,b.png` passes existing PNGs as references (images/edits endpoint) to keep new
  sprites on-model. Say "FIRST"/"SECOND" in the prompt when passing more than one.
- Rate limit is ~5 images/minute: generate sequentially.
- Generated art lands in Assets/Art/Generated/ — run an asset refresh so Unity imports it,
  then set the texture importer (Sprite 2D, PPU, filter mode) to match the rest of Assets/Art.
