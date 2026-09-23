#!/usr/bin/env bash
# Reroll the two assets that still carry a baked cream shadow ellipse, so the whole set is
# shadow-free and shadows can be drawn in-engine instead. Anchored to Tree_Pine.png, which is
# now correctly outlined AND shadow-free.
set -u
cd "$(dirname "$0")/../.." || exit 1

REF="ArtSource/Tree_Pine.png"
MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline around the whole object, exactly like the reference. Chunky rounded generous silhouette that reads clearly at 64px. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no ground shadow, no dark ellipse under the subject, no light ellipse under the subject, no glow, no border, no text."

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

gen "ArtSource/Hill_Gray.png" \
  "a small rocky gray hill seen from a 3/4 top-down oblique angle about 50 degrees above: a cluster of rounded boulders in cool gray with subtle warm undertones, the largest at the back, a few facets catching light on top."

gen "ArtSource/Capy_Idle.png" \
  "a capybara character standing idle, seen from a 3/4 top-down oblique angle about 50 degrees above so both the top of the head and the front of the face are visible. Oversized rounded head, small rounded body, short stubby legs, tiny rounded ears, two simple black dot eyes, a small darker muzzle, calm friendly expression. Warm tan-brown fur in the #B5813F family with a slightly darker muzzle and darker shading under the chin and belly. A completely plain bare capybara with NO clothing, NO cape, NO scarf, NO collar, NO necklace, NO medallion, NO hat, NO accessories and NO props of any kind."

echo "=== NO-SHADOW REROLL COMPLETE ==="
