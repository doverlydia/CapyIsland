#!/usr/bin/env bash
# A sleeping pose for every kind of worker (the plain capybara already has Capy_Sleep): burrow,
# woolly, pirate and cured zombie capybaras. Each is drawn from its own picture, posed like
# Capy_Sleep. Ships to Assets/Resources/Hobbies/Sleep_<Animal>.png (pivoted at the feet).
#
#   batch_sleep.sh            — all four, then ship
#   batch_sleep.sh <Animal>   — one again (Fox, Panda, Pirate, Monster), then ship
set -u
cd "$(dirname "$0")/../.." || exit 1

TAIL="Rendered in clean vector style with soft airbrushed gradients and a dark warm brown medium-weight outline, exactly like the references. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no ground, no shadow, no pale oval or ellipse or cream patch or puddle of fluff beneath it, the body simply ends at its outline, nothing beneath it, no text, no letters, no Z letters."

one () {
  local animal="$1" ref
  case "$animal" in
    Fox) ref="ArtSource/Animal_Fox.png" ;;
    Panda) ref="ArtSource/Animal_Panda.png" ;;
    Pirate) ref="ArtSource/Capy_Pirate.png" ;;
    Monster) ref="ArtSource/Animal_Monster_Happy.png" ;;
  esac
  local out="ArtSource/Sleep_$animal.png"
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "Draw the EXACT SAME character as the FIRST attached image (same colours, markings, clothes and accessories, face style, outline and rendering), but ASLEEP in the same lying-down pose as the SECOND attached image: lying on its tummy, legs tucked under, eyes peacefully closed, a contented little smile, head resting low. Same size and proportions as the first image. $TAIL" \
      --out "$out" --transparent --quality high --ref "$ref,ArtSource/Capy_Sleep.png"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

ship () {
  for a in Fox Panda Pirate Monster; do
    [ -f "ArtSource/Sleep_$a.png" ] && node Tools/ChatGPT/downscale.mjs "ArtSource/Sleep_$a.png" "Assets/Resources/Hobbies/Sleep_$a.png" 256
  done
}

if [ -n "${1:-}" ]; then one "$1"; else for a in Fox Panda Pirate Monster; do one "$a"; done; fi
ship
echo "done"
