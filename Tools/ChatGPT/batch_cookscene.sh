#!/usr/bin/env bash
# The kitchen the cooking minigame is played in (UI/Hobbies/CookingScreen): a tall painted
# backdrop, plus big copies of the stove and pot for the middle of the screen (the in-world
# sprites are 192px). Masters go to ArtSource/; everything ships to Assets/Resources/Hobbies.
#
#   batch_cookscene.sh          — the backdrop (if not made yet), then ship
#   batch_cookscene.sh ship     — only downscale
set -u
cd "$(dirname "$0")/../.." || exit 1

REF="ArtSource/Cutscenes/Cutscene_Wedding.png"
STYLE="Match the attached image's rendering style, outline weight, shading and palette EXACTLY (clean storybook vector fills, soft airbrushed gradients, warm dark-brown outlines on built props, muted warm palette)."

backdrop () {
  local out="ArtSource/Cook_Kitchen.png"
  if [ -f "$out" ]; then echo "skip $out"; return; fi
  echo "=== $out ==="
  node Tools/ChatGPT/chatgpt.mjs image "$STYLE Draw instead: a tall, cosy storybook backdrop of the inside of a little island cabin kitchen, portrait composition, seen from slightly above like the rest of the game. The back wall and the floor meet in a straight horizontal line exactly halfway down the picture. Upper half: warm honey-coloured wooden plank wall with a round window showing a pale blue sky and a glimpse of sea, wooden shelves at both sides with clay jars, bottles and stacked bowls, bunches of dried herbs and a string of garlic hanging from the beams at the very top, a softly glowing lantern; in the middle of the wall, just above the floor line, a plain arched brick hearth nook (an empty alcove, nothing in it). Lower half: a broad warm wooden plank floor running towards the viewer in gentle perspective, with a round woven rug in the centre of the floor, empty and clear, for a stove to stand on. NO stove, NO fire, NO pots, NO furniture on the floor, NO characters, NO people, NO animals, NO text." \
    --out "$out" --size 1024x1536 --quality high --ref "$REF" || echo "FAILED: $out"
}

ship () {
  node Tools/ChatGPT/downscale.mjs ArtSource/Cook_Kitchen.png Assets/Resources/Hobbies/Cook_Kitchen.png 1024 > /dev/null && echo "shipped Cook_Kitchen"
  node Tools/ChatGPT/downscale.mjs ArtSource/Hobby_Stove.png Assets/Resources/Hobbies/Cook_Stove.png 512 > /dev/null && echo "shipped Cook_Stove"
  node Tools/ChatGPT/downscale.mjs ArtSource/Hobby_Pot.png Assets/Resources/Hobbies/Cook_Pot.png 512 > /dev/null && echo "shipped Cook_Pot"
}

case "${1:-}" in
  ship) ship ;;
  *) backdrop; ship ;;
esac
