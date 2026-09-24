#!/usr/bin/env bash
# Props a working capybara holds: an axe for the sawmill, a pickaxe for the mine, and a carried log
# so a return trip reads as "bringing something back".
set -u
cd "$(dirname "$0")/../.." || exit 1

REF="ArtSource/Build_Sawmill.png"
MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline around the whole object, exactly like the reference. Chunky rounded generous silhouette that reads clearly at 48px. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no glow, no border, no text, no hands, no characters."

gen () {
  local out="$1"; shift
  local prompt="$1"; shift
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$MATCH Draw instead: $prompt $TAIL" \
      --out "$out" --transparent --quality high --ref "$REF"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

gen "ArtSource/Tool_Axe.png" \
  "a small woodcutting axe seen from the side, lying at a slight angle with the handle pointing down-left and the head up-right: a short warm-brown wooden handle and a chunky grey steel head with a lighter bevelled edge."

gen "ArtSource/Tool_Pickaxe.png" \
  "a small mining pickaxe seen from the side, lying at a slight angle with the handle pointing down-left: a short warm-brown wooden handle and a chunky grey steel double-pointed head with lighter bevelled tips."

gen "ArtSource/Prop_Log.png" \
  "a single short cut log seen from the side, lying horizontally: warm brown bark along its length with a lighter cream-tan end grain circle showing simple concentric rings at the near end."

echo "=== TOOLS COMPLETE ==="
