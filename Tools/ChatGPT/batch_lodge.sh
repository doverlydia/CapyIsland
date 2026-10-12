#!/usr/bin/env bash
# The Woolly Lodge ("Panda House"): the woolly capybaras' home, sold at the Gold Store. Anchored
# to the Burrow Hole so it sits with the other houses. Ship it with:
#   node Tools/ChatGPT/downscale.mjs ArtSource/Build_PandaHouse.png Assets/Art/Buildings/Build_PandaHouse.png 256
set -u
cd "$(dirname "$0")/../.." || exit 1

REF="ArtSource/Build_FoxHouse.png"
MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline around the whole object, exactly like the reference. Chunky rounded generous silhouette that reads clearly at 64px. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no ground shadow, no dark ellipse under the subject, no light ellipse under the subject, no glow, no border, no text."

gen () {
  local out="$1"; shift
  local prompt="$1"; shift
  if [ -f "$out" ]; then echo "skip $out"; return; fi
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$MATCH Draw instead: $prompt $TAIL" \
      --out "$out" --transparent --quality high --ref "$REF"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

gen "ArtSource/Build_PandaHouse.png" \
  "a small cute warm cosy log lodge, seen from the same 3/4 top-down oblique angle as the reference. Round honey-brown log walls, a thick rounded roof thatched with soft fluffy cream wool like a sheep's fleece, a stubby stone chimney with a little curl of smoke, a round wooden door and one small warmly lit window. Snug and wintery, built for fluffy creatures that love the cold."
