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

  # The roomy burrow (2026-10-25): one wide room the camera walks along, the same in portrait
  # and landscape, with overlays for the way out and the tunnel on to the next room.
  wide Burrow_Room "a WIDE storybook panorama of the inside of a capybara's underground burrow room, landscape composition, seen from slightly above like the rest of the game, in the same style, colours and earthen materials as the attached picture. The back wall and the floor meet in a long, gently uneven horizontal line 55% of the way up from the bottom of the picture, running the whole width. Above it: a rounded warm packed-brown earth wall with a few curling tree roots, filling the whole top of the picture right up to the edge (more wall above than you need: the top is just more earth and roots). Below it: a broad smooth warm packed-earth floor with a few flat stepping stones, completely empty, running right down to the bottom edge. At the far left and far right ends the wall curves gently forward. A few small softly glowing mushrooms along the foot of the wall. NO doors, NO doorways, NO openings, NO steps, NO furniture, NO characters, NO animals, NO text. Even, seamless, calm."
  prop Burrow_Door "a round wooden front door set into a rounded earthen archway, a sliver of warm daylight around its edges, at the top of three chunky packed-earth steps that come down towards the viewer, seen from the front and slightly above. Only the door, its arch and the steps." ArtSource/Home/Furn_Bookshelf.png
  prop Burrow_Arch "a rounded tunnel opening in a packed-earth wall, a dark cosy passage leading on to another room, framed by a chunky curved wooden beam arch with a little lantern hanging from the top, seen from the front and slightly above. Only the archway and the dark opening." ArtSource/Home/Furn_Bookshelf.png

  FREF=ArtSource/Home/Furn_Armchair.png
  prop Furn_Album "a big cosy photo album lying open on a small wooden reading stand $VIEW: thick cream pages with little photos tucked into the corners, a ribbon bookmark." $FREF
  prop Furn_Sofa "a chunky comfy three-seater sofa $VIEW: rounded dusty-terracotta upholstery, plump seat cushions and two small round pillows, short wooden legs." $FREF
  prop Furn_Bathtub "an old-fashioned white clawfoot bathtub $VIEW, full of fluffy white bubbles, a rubber duck on the rim." $FREF
  prop Furn_Stove "a little cast-iron cooking stove $VIEW: a round black body on short legs, a kettle and a small pot on top, a warm glow in its little door." $FREF
  prop Furn_Icebox "a rounded old-fashioned mint-green icebox fridge $VIEW, one door with a chunky chrome handle." $FREF
  prop Furn_Kotatsu "a low square Japanese kotatsu table $VIEW: a wooden top over a soft quilted patterned blanket that drapes to the floor, a bowl of mandarins on top." $FREF
  prop Furn_Desk "a small wooden writing desk $VIEW: a sheet of paper, an ink pot with a quill, a tiny lamp, and a round little stool tucked under it." $FREF
  prop Furn_Telly "a chunky old rounded television set on short wooden legs $VIEW, its screen softly glowing pale blue, two knobs on the side, a little antenna on top." $FREF
  prop Furn_Hammock "a woven cream rope hammock slung between two short wooden posts $VIEW, a plump pillow in it." $FREF
  prop Furn_BeanBag "a big squashy round bean bag $VIEW in soft mustard-yellow fabric, slightly dented in the middle." $FREF
  prop Furn_Cushion "a plump square floor cushion $VIEW, sage green with a button in the middle and little tassels on the corners. Flat and low." $FREF
  prop Furn_Telescope "a brass telescope on a wooden tripod $VIEW, pointing up and to the side." $FREF
  prop Furn_Taiko "a round Japanese taiko drum on a little wooden stand $VIEW, with two wooden drumsticks resting on top." $FREF
  prop Furn_Mirror "a tall standing oval mirror in a carved wooden frame on little feet $VIEW, its glass softly reflecting pale light." $FREF
  prop Furn_Lantern "a Japanese paper floor lantern $VIEW: a round warm-glowing paper shade on a short wooden stand." $FREF
  prop Furn_Basin "a little washstand $VIEW: a wooden stand with a round ceramic wash basin, a jug beside it and a folded towel." $FREF
  prop Furn_Bonsai "a little bonsai tree in a shallow blue glazed pot $VIEW, with a tiny moss bed." $FREF
  prop Furn_Wardrobe "a tall rounded wooden wardrobe $VIEW: two doors with round knobs, one door slightly open showing a little scarf and a hat inside." $FREF
  prop Furn_SwingChair "a round wicker hanging egg chair on a curved stand $VIEW, with a plump cream cushion inside." $FREF
  prop Furn_Arcade "a small rounded retro arcade cabinet $VIEW in soft teal, its screen glowing, a joystick and two round buttons." $FREF
}

wide () {
  local out="ArtSource/Home/$1.png"; shift
  if [ -f "$out" ]; then echo "skip $out"; return; fi
  echo "=== $out ==="
  node Tools/ChatGPT/chatgpt.mjs image "$SCENE_STYLE Draw instead: $1" --out "$out" --size 1536x1024 --quality high --ref ArtSource/Home/Burrow_Cave.png || echo "FAILED: $out"
}

ship () {
  [ -f ArtSource/Home/Burrow_Cave.png ] && node Tools/ChatGPT/downscale.mjs ArtSource/Home/Burrow_Cave.png Assets/Resources/Home/Burrow_Cave.png 1024 > /dev/null && echo "shipped Burrow_Cave"
  [ -f ArtSource/Home/Burrow_Room.png ] && node Tools/ChatGPT/downscale.mjs ArtSource/Home/Burrow_Room.png Assets/Resources/Home/Burrow_Room.png 1536 > /dev/null && echo "shipped Burrow_Room"
  for f in Burrow_Door Burrow_Arch; do
    [ -f ArtSource/Home/$f.png ] && node Tools/ChatGPT/downscale.mjs ArtSource/Home/$f.png Assets/Resources/Home/$f.png 512 > /dev/null && echo "shipped $f"
  done
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
