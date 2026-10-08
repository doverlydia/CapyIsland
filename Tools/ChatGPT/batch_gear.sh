#!/usr/bin/env bash
# Hobby gear and the sea library: binoculars (birdwatching), the sewing machine (a placed
# station), the bottle-selling sailor capybara and a message bottle. Masters go to ArtSource/,
# shipped copies to Assets/Resources/Hobbies/ (the sailor is pivoted at its feet).
#
#   batch_gear.sh            — everything, then ship
#   batch_gear.sh <Name>     — one again (Binoculars, Sewing, Sailor, Bottle), then ship
set -u
cd "$(dirname "$0")/../.." || exit 1

MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline, exactly like the reference. Chunky rounded generous shapes that read clearly at 64px. Muted natural palette. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground, no shadow, no pale oval or ellipse or smudge beneath it, nothing at all beneath it, no border, no text."
PROP="ArtSource/Hill_Gray.png"
VIEW="seen from a 3/4 top-down oblique angle about 50 degrees above"

gen () {
  local out="$1"; shift
  local ref="$1"; shift
  local prompt="$1"; shift
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$MATCH Draw instead: $prompt $TAIL" --out "$out" --transparent --quality high --ref "$ref"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

one () {
  case "$1" in
    Binoculars) gen ArtSource/Hobby_Binoculars.png "$PROP" "a pair of chunky cute brass-and-green binoculars with a little leather neck strap, seen from the front and slightly above." ;;
    Sewing) gen ArtSource/Hobby_SewingMachine.png "$PROP" "a cosy old-fashioned sewing machine on a small wooden sewing table $VIEW, a cream-and-teal machine with a spool of red thread on top, a little pincushion and a folded piece of patterned fabric beside it. Storybook island game prop." ;;
    Sailor) gen ArtSource/Hobby_Sailor.png "ArtSource/Capy_Pete.png" "a DIFFERENT friendly old sailor capybara character in the same style as the reference character: a kind, round, grandfatherly capybara wearing a navy blue fisherman's knit sweater and a little yellow rain hat, holding a glass message bottle with a rolled-up scroll inside, standing, facing the viewer's left like the reference." ;;
    Bottle) gen ArtSource/Hobby_Bottle.png "$PROP" "a single small glass message bottle lying on its side, pale sea-green glass with a cork stopper and a rolled-up paper scroll tied with a red ribbon visible inside, a tiny bit of seaweed on it." ;;
  esac
}

ship () {
  bash Tools/ChatGPT/batch_hobbies.sh ship
}

if [ -n "${1:-}" ]; then one "$1"; else for n in Binoculars Sewing Sailor Bottle; do one "$n"; done; fi
ship
echo "done"
