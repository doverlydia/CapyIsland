#!/usr/bin/env bash
# Art for the in-world hobby kit: the Garden Shop building, the stove you cook on, the pot and
# the burnt "bizarre" dish, and the fishing rod the capybara holds. Masters go to ArtSource/;
# everything ships to Assets/Resources/Hobbies.
#
#   batch_kitchen.sh            — everything, then ship
#   batch_kitchen.sh <Name>     — one master again (Shop, Stove, Pot, Bizarre, Rod), then ship
set -u
cd "$(dirname "$0")/../.." || exit 1

REF="ArtSource/Hill_Gray.png"
MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline around the whole object, exactly like the reference. Chunky rounded generous silhouette that reads clearly at 64px. Muted natural palette. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no ground shadow, no glow beneath the subject, no pale oval or ellipse or smudge under it, no border, no text."
VIEW="seen from a 3/4 top-down oblique angle about 50 degrees above"

gen () {
  local out="$1"; shift
  local prompt="$1"; shift
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$MATCH Draw instead: $prompt $TAIL" --out "$out" --transparent --quality high --ref "$REF"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

one () {
  case "$1" in
    Shop) gen ArtSource/Store_Garden.png "a cosy little garden shop building $VIEW: a small wooden greenhouse-cottage with a sloped glass-panelled roof, a green-and-white striped awning over the front, wooden crates of seed packets, potted seedlings and a watering can out front, a little sign board with a painted carrot (no letters). Storybook island game building." ;;
    Stove) gen ArtSource/Hobby_Stove.png "a small outdoor cooking stove $VIEW: a chunky rounded brick-and-stone hearth with a wooden counter top on one side, a black iron cooking plate on top with a little glowing fire visible through the front opening, a stack of firewood at its side. Empty, nothing cooking on it. Cosy storybook game prop." ;;
    Pot) gen ArtSource/Hobby_Pot.png "a single round black iron cooking pot with two small handles, seen from the front and slightly above so you can see into it, empty with a warm dark interior. Chunky and cute." ;;
    Bizarre) gen ArtSource/Hobby_Dish_Bizarre.png "a comical burnt dish on a small plate $VIEW: a lumpy charred black-and-dark-brown blob with a little curl of grey smoke above it and two cartoon swirl eyes in the blob. Funny, not gross." ;;
    Rod) gen ArtSource/Hobby_Rod.png "a single simple wooden fishing rod lying diagonally from bottom-left to top-right, a light bamboo-coloured pole with a small round reel near the bottom handle and a little red-and-white bobber hanging from the tip by a short loop of line. Side view, flat, no hands." ;;
  esac
}

ship () {
  [ -f ArtSource/Store_Garden.png ] && node Tools/ChatGPT/trim.mjs ArtSource/Store_Garden.png "$TMPDIR/sg.png" --alpha 60 > /dev/null && node Tools/ChatGPT/downscale.mjs "$TMPDIR/sg.png" Assets/Resources/Hobbies/Store_Garden.png 384
  bash Tools/ChatGPT/batch_hobbies.sh ship
}

TMPDIR=$(mktemp -d)
if [ -n "${1:-}" ]; then one "$1"; else for n in Shop Stove Pot Bizarre Rod; do one "$n"; done; fi
ship
echo "done"
