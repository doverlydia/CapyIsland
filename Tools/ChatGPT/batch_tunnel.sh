#!/usr/bin/env bash
# The tunnel you slide down from the den into your burrow (UI/Cutscenes/TunnelCutscene): one tall
# painting of earthen tunnel walls, scrolled past (mirrored on every other copy, so it never seams).
set -u
cd "$(dirname "$0")/../.." || exit 1
mkdir -p ArtSource/Home Assets/Resources/Home

REF="ArtSource/Cutscenes/Cutscene_Wedding.png"
STYLE="Match the attached image's rendering style, outline weight, shading and palette EXACTLY (clean storybook vector fills, soft airbrushed gradients, warm dark-brown outlines, muted warm palette)."
OUT="ArtSource/Home/Tunnel.png"

if [ ! -f "$OUT" ]; then
  echo "=== $OUT ==="
  node Tools/ChatGPT/chatgpt.mjs image "$STYLE Draw instead: looking straight down a smooth earthen slide tunnel that runs from the top of the picture to the bottom, portrait composition. In the middle, a broad smooth worn groove of packed light-brown earth; on both sides, darker crumbly earthen walls with curling tree roots, a few pebbles and little softly glowing mushrooms. Even, cosy lighting. The top and bottom edges look alike so the picture can repeat. NO characters, NO people, NO animals, NO text." \
    --out "$OUT" --size 1024x1536 --quality high --ref "$REF" || echo "FAILED: $OUT"
fi
[ -f "$OUT" ] && node Tools/ChatGPT/downscale.mjs "$OUT" Assets/Resources/Home/Tunnel.png 768 > /dev/null && echo "shipped Tunnel"
