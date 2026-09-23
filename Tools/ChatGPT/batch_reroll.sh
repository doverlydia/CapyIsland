#!/usr/bin/env bash
# Reroll the two world props that came back off-model (soft 3D, no outline), this time anchored
# with --ref against Hill_Gray.png, which is the approved reference for outlined props.
set -u
cd "$(dirname "$0")/../.." || exit 1

REF="ArtSource/Hill_Gray.png"
MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline around the whole object, exactly like the reference. Chunky rounded generous silhouette that reads clearly at 64px. Muted natural palette. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no ground shadow, no glow beneath the subject, no border, no text."

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

gen "ArtSource/Tree_Pine.png" \
  "a single stylized pine tree seen from a 3/4 top-down oblique angle about 50 degrees above, with a conical layered canopy in mid and dark green and a short sturdy warm-brown trunk."

gen "ArtSource/Garden_Posie.png" \
  "a small round flower garden patch seen from a 3/4 top-down oblique angle about 50 degrees above: a low circular bed of dark soil with several simple rounded pink and white blossoms on short green stems."

echo "=== REROLL COMPLETE ==="
