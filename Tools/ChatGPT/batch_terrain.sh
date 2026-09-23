#!/usr/bin/env bash
# First art batch for the place-and-gather loop. Sequential: the API allows ~5 images/minute.
# Each asset is generated then immediately de-haloed with cleanalpha.
set -u
cd "$(dirname "$0")/../.." || exit 1

STYLE="Rendered in clean vector style with soft airbrushed gradients, smooth and slightly glossy, almost claymation-soft. No pixel art, no canvas texture, no visible brush strokes, no noise. Chunky rounded generous silhouette that reads clearly at 64px. Muted natural palette, moderate saturation, nothing neon. Soft top-down lighting with one gentle light. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no border, no text."

gen () {
  local out="$1"; shift
  local prompt="$1"; shift
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$prompt $STYLE" --out "$out" --transparent --quality high; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

gen "ArtSource/Tree_Pine.png" \
  "A single stylized pine tree for a cozy island-builder game, seen from a 3/4 top-down oblique angle about 50 degrees above so both the top and the front are visible. Conical layered canopy in mid to dark green, short sturdy warm-brown trunk. Dark warm brown medium-weight outline on the trunk; the canopy has no outline and separates by value instead. A soft elliptical contact shadow sits flat beneath it."

gen "ArtSource/Hill_Gray.png" \
  "A small rocky gray hill or stone outcrop for a cozy island-builder game, seen from a 3/4 top-down oblique angle about 50 degrees above. Rounded boulder forms in cool gray with subtle warm undertones, a few visible facets catching light on top. Dark warm brown medium-weight outline. A soft elliptical contact shadow sits flat beneath it."

gen "ArtSource/Garden_Posie.png" \
  "A small round flower garden patch for a cozy island-builder game, seen from a 3/4 top-down oblique angle about 50 degrees above. A low circular bed of soil with several simple rounded pink and white blossoms on short green stems. Dark warm brown medium-weight outline on the bed; the flowers separate by value."

gen "ArtSource/Icon_Wood.png" \
  "A game UI resource icon representing wood: two or three short stacked cut logs seen end-on, warm brown bark with lighter cream-tan end grain and simple concentric rings. Bold simple shape, heavy dark warm brown outline, readable at 48px. Front-facing icon, not perspective."

gen "ArtSource/Icon_Rock.png" \
  "A game UI resource icon representing stone: a cluster of three rounded gray rocks, the largest at the back. Bold simple shape, heavy dark warm brown outline, subtle top light, readable at 48px. Front-facing icon, not perspective."

gen "ArtSource/Icon_Gem.png" \
  "A game UI resource icon representing a gem: a single faceted teal-blue jewel in a classic cut with a bright highlight facet. Bold simple shape, heavy dark warm brown outline, readable at 48px. Front-facing icon, not perspective."

echo "=== BATCH COMPLETE ==="
ls -la ArtSource/
