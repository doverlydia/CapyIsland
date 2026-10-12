#!/usr/bin/env bash
# Your burrow (Game/Home): the cave you go down into through the den door, the Furniture Shop
# building on the island, and the furniture it sells. Masters go to ArtSource/Home/;
# everything ships to Assets/Resources/Home. Skips anything already made, so it can be re-run.
#
#   batch_home.sh            — everything not made yet, then ship
#   batch_home.sh ship       — only downscale
set -u
cd "$(dirname "$0")/../.." || exit 1
mkdir -p ArtSource/Home Assets/Resources/Home

SCENE_REF="ArtSource/Cutscenes/Cutscene_Wedding.png"
SCENE_STYLE="Match the attached image's rendering style, outline weight, shading and palette EXACTLY (clean storybook vector fills, soft airbrushed gradients, warm dark-brown outlines on built props, muted warm palette)."

PROP_REF="ArtSource/Hobby_Stove.png"
MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline around the whole object, exactly like the reference. Chunky rounded generous silhouette that reads clearly at 64px. Muted natural palette. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no floor, no ground plane, no shadow, no cast shadow, no ground shadow, no pale oval or ellipse under it, no border, no text."
VIEW="seen from a 3/4 top-down oblique angle about 50 degrees above"

scene () {
  local out="ArtSource/Home/$1.png"; shift
  if [ -f "$out" ]; then echo "skip $out"; return; fi
  echo "=== $out ==="
  node Tools/ChatGPT/chatgpt.mjs image "$SCENE_STYLE Draw instead: $1" --out "$out" --size 1024x1536 --quality high --ref "$SCENE_REF" || echo "FAILED: $out"
}

prop () {
  local out="ArtSource/Home/$1.png"; shift
  local ref="${2:-$PROP_REF}"
  if [ -f "$out" ]; then echo "skip $out"; return; fi
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$MATCH Draw instead: $1 $TAIL" --out "$out" --transparent --quality high --ref "$ref"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

make () {
  scene Burrow_Cave "a tall, cosy storybook backdrop of the inside of a capybara's underground burrow home, portrait composition, seen from slightly above like the rest of the game. The back wall and the floor meet in a straight horizontal line exactly halfway down the picture. Upper half: a rounded earthen back wall of warm packed brown earth with a few curling tree roots, little softly glowing mushrooms in the corners, a round wooden front door at the top of a few earthen steps on the left with a sliver of daylight around it. Lower half: a broad, smooth, warm packed-earth floor with a few flat stepping stones, running towards the viewer in gentle perspective, completely empty and clear for furniture. Cosy, warm, safe. NO furniture, NO characters, NO people, NO animals, NO text."

  prop Store_Furniture "a cosy little furniture shop building $VIEW: a round-roofed wooden cottage with a big front window showing a tiny armchair and a lamp inside, a striped plum-and-cream awning, a little wooden chair and a potted plant outside the door, a hanging sign with a painted chair on it (no letters). Storybook island game building." ArtSource/Build_Sawmill.png

  prop Furn_Rug "a round woven rug $VIEW, lying flat: soft cream and terracotta rings with a little fringe. Flat on the ground, nothing on it."
  prop Furn_Bed "a cosy low capybara bed $VIEW: a round wooden frame filled with soft golden hay and a plump cream cushion, a little knitted blanket over one side."
  prop Furn_Armchair "a chunky comfy armchair $VIEW: rounded mossy-green upholstery, a plump seat cushion, short wooden legs."
  prop Furn_Table "a small round wooden table $VIEW: one thick central leg, a little vase with a single flower on top."
  prop Furn_Stool "a mushroom-shaped stool $VIEW: a red cap with white spots as the seat, a cream stalk as the leg."
  prop Furn_Bookshelf "a short wooden bookshelf $VIEW: three shelves of chunky colourful books, a small potted plant on top."
  prop Furn_Lamp "a standing floor lamp $VIEW: a slim wooden pole and a round pleated cream shade glowing warmly."
  prop Furn_Plant "a big leafy fern in a round terracotta pot $VIEW."
  prop Furn_Dresser "a small chest of drawers $VIEW: honey-coloured wood, three drawers with round knobs, a little mirror and a seashell on top."
  prop Furn_Painting "a framed painting $VIEW, standing upright on a small wooden easel: a calm sea with a little island on it, in a chunky wooden frame."
  prop Furn_ToyChest "a wooden toy chest $VIEW: the lid propped open, a ball, a toy boat and a little plush capybara peeking out."
  prop Furn_Radio "an old wooden tabletop radio $VIEW: rounded top, a cloth speaker grille, two round knobs, sitting on a little side table."
  prop Furn_Aquarium "a rounded glass fish tank on a small wooden stand $VIEW: blue water, green waterweeds, two tiny orange fish and bubbles."
  prop Furn_HotTub "a little round wooden hot tub $VIEW: steaming turquoise water with a floating yuzu fruit and a wooden ladle on the rim."
  prop Furn_Piano "a tiny upright piano $VIEW: polished warm brown wood, a little stool in front, a candle on top."
  prop Furn_Fireplace "a rounded stone fireplace $VIEW: grey cobblestones, a cosy crackling fire inside, a wooden mantel with a little clock on it."
}

ship () {
  [ -f ArtSource/Home/Burrow_Cave.png ] && node Tools/ChatGPT/downscale.mjs ArtSource/Home/Burrow_Cave.png Assets/Resources/Home/Burrow_Cave.png 1024 > /dev/null && echo "shipped Burrow_Cave"
  [ -f ArtSource/Home/Store_Furniture.png ] && node Tools/ChatGPT/downscale.mjs ArtSource/Home/Store_Furniture.png Assets/Resources/Home/Store_Furniture.png 384 > /dev/null && echo "shipped Store_Furniture"
  for f in ArtSource/Home/Furn_*.png; do
    [ -f "$f" ] && node Tools/ChatGPT/downscale.mjs "$f" "Assets/Resources/Home/$(basename "$f")" 256 > /dev/null && echo "shipped $(basename "$f")"
  done
}

case "${1:-}" in
  ship) ship ;;
  *) make; ship ;;
esac
echo "=== HOME ART DONE ==="
