#!/usr/bin/env bash
# A standing-on-four-paws pose for every cat, used while it walks (the sitting one is for when
# it stops). Each is drawn from its own sitting picture so the cat stays on-model.
#
#   batch_catwalk.sh            — all six, then ship
#   batch_catwalk.sh <Id>       — one again (Ginger, Black, Calico, Grey, Siamese, White), then ship
set -u
cd "$(dirname "$0")/../.." || exit 1

TAIL="Rendered in clean vector style with soft airbrushed gradients and a dark warm brown medium-weight outline, exactly like the reference. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no ground, no shadow, no pale oval or ellipse or smudge or patch of ground beneath its paws, nothing at all beneath it, no text."

one () {
  local id="$1"
  local ref="ArtSource/Hobby_Cat_$id.png"
  local out="ArtSource/Hobby_Cat_${id}_Walk.png"
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "Draw the EXACT SAME cat as the attached reference — same colours, markings, face, proportions, outline and rendering style — but STANDING on all four paws in a mid-stride walking pose, body side-on and slightly turned to the viewer, facing the SAME direction as the reference (head on the LEFT side of the picture, walking towards the left), tail raised in a happy curve. Same size and chunkiness as the reference. $TAIL" \
      --out "$out" --transparent --quality high --ref "$ref"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

if [ -n "${1:-}" ]; then one "$1"; else for id in Ginger Black Calico Grey Siamese White; do one "$id"; done; fi
bash Tools/ChatGPT/batch_hobbies.sh ship
echo "done"
