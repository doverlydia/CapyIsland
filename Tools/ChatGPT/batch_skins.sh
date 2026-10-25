#!/usr/bin/env bash
# Holiday skins' art (Skins/README.md): the island's fixed scenery dressed for the day — stone
# lanterns, sea lanterns, torii gates — each drawn over the game's own picture and fitted back onto
# its exact canvas and feet (alignfeet.mjs), plus the skin's scattered decorations and the thing
# that falls. Masters stay in ArtSource/Skins/ (never shipped); what players download goes to
# Skins/<id>/. Sequential (~5 images/minute); skips any master already made, so it can be re-run.
#
# Usage: bash Tools/ChatGPT/batch_skins.sh [halloween|christmas|all]
set -u
cd "$(dirname "$0")/../.." || exit 1
WHICH="${1:-all}"

STYLE="Rendered in clean vector style with soft airbrushed gradients, smooth and slightly glossy, almost claymation-soft. Chunky rounded generous silhouette that reads clearly at 64px. Muted natural palette, moderate saturation. Soft top-down lighting with one gentle light. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no border, no text. No shadow, no cast shadow, no ground shadow, no glow beneath the subject."
MATCH="Match the attached image's rendering style, outline weight, shading and palette EXACTLY."
SAME="Keep the object EXACTLY the same shape, pose, angle, proportions and colours as the attached image, standing on the same feet; only add the decorations described:"
VIEW="seen from a 3/4 top-down oblique angle about 50 degrees above so both the top and the front are visible, for a cozy island-builder game."

TORO=Assets/Resources/World/Toro.png
FLOAT=Assets/Resources/World/Lantern_Float.png
FRONT=Assets/Resources/World/Torii_Front.png
SIDE=Assets/Resources/World/Torii_Side.png

master () {  # master <out> <ref> <prompt>
  local out="$1" ref="$2" prompt="$3"
  if [ -f "$out" ]; then echo "skip $out"; return 0; fi
  mkdir -p "$(dirname "$out")"
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$prompt $STYLE" --out "$out" --transparent --quality high --ref "$ref"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"; return 1
  fi
}

dress () {  # dress <skin> <sprite name> <game picture> <what to add>
  local m="ArtSource/Skins/$1/$2.png"
  master "$m" "$3" "$MATCH $SAME $4" || return
  mkdir -p "Skins/$1/sprites"
  node Tools/ChatGPT/alignfeet.mjs "$3" "$m" "Skins/$1/sprites/$2.png"
}

decor () {  # decor <skin> <file> <ref> <prompt> [width]
  local m="ArtSource/Skins/$1/$2.png"
  master "$m" "$3" "$MATCH Draw instead: $4" || return
  mkdir -p "Skins/$1/decor"
  local t="ArtSource/Skins/$1/_trim.png"
  node Tools/ChatGPT/trim.mjs "$m" "$t" --pad 2 > /dev/null
  node Tools/ChatGPT/downscale.mjs "$t" "Skins/$1/decor/$2.png" "${5:-256}" > /dev/null
  rm -f "$t"
  echo "decor -> Skins/$1/decor/$2.png"
}

if [ "$WHICH" = halloween ] || [ "$WHICH" = all ]; then
  S=halloween
  dress $S Toro $TORO "a few soft wisps of white cobweb on the roof corners and a small carved jack-o'-lantern pumpkin with a friendly smile sitting on the roof beside the round top knob."
  dress $S Lantern_Float $FLOAT "the lantern's glowing paper panels are now painted as a friendly carved jack-o'-lantern face glowing warm orange, and a tiny curly pumpkin stem with a leaf sits on the top rim."
  dress $S Torii_Front $FRONT "a string of small round glowing orange paper lanterns painted with friendly pumpkin faces hangs along the lower crossbeam, and soft white cobweb wisps sit in the two upper corners."
  dress $S Torii_Side $SIDE "a string of small round glowing orange paper lanterns painted with friendly pumpkin faces hangs along the lower crossbeam, and soft white cobweb wisps sit where the top beam meets the pillars."
  decor $S Pumpkins ArtSource/Hobby_Crop_Pumpkin.png "a cozy little cluster of three carved Halloween jack-o'-lantern pumpkins of different sizes sitting together on the ground, friendly smiling faces glowing softly orange from inside, curly green stems, $VIEW"
  decor $S Haystack ArtSource/Hobby_Crop_Pumpkin.png "a small round golden haystack with one plump orange pumpkin leaning against it and a few fallen orange and red autumn leaves, $VIEW"
  decor $S Scarecrow ArtSource/Capy_Idle.png "a cute small friendly scarecrow on a wooden post: a stuffed burlap head shaped like a smiling capybara, a floppy straw hat, a patched orange shirt with straw poking out of the sleeves, $VIEW"
  decor $S Leaf ArtSource/Tree_Maple.png "a single autumn maple leaf, warm orange fading to red at the tips, seen flat from above." 64
fi

if [ "$WHICH" = christmas ] || [ "$WHICH" = all ]; then
  S=christmas
  dress $S Toro $TORO "soft fluffy white snow piled thickly on the roof and on the ledge above the base, and a little sprig of green holly with red berries on the roof."
  dress $S Lantern_Float $FLOAT "soft white snow piled along the top rim, a red satin ribbon tied around the middle in a bow on the front, and a sprig of holly with red berries."
  dress $S Torii_Front $FRONT "soft fluffy white snow piled along the top of the upper beam and the crossbeam, a green pine garland with small red and gold baubles draped along the lower crossbeam, and a red bow on the centre plaque."
  dress $S Torii_Side $SIDE "soft fluffy white snow piled along the top of the upper beam and the crossbeam, and a green pine garland with small red and gold baubles draped along the lower crossbeam."
  decor $S XmasTree ArtSource/Tree_Pine.png "a single small cute Christmas pine tree $VIEW Decorated with round red and gold baubles, a soft gold tinsel garland, a shining yellow star on top and a little snow on the branches."
  decor $S Snowcapy ArtSource/Capy_Idle.png "a cute snowman shaped like a round chubby capybara, built from packed white snow, with a red knitted scarf, two coal dot eyes and a little carrot nose, $VIEW"
  decor $S Presents ArtSource/Hobby_Crop_Pumpkin.png "a small cozy pile of three wrapped Christmas presents of different sizes, red, green and cream paper with gold ribbons and bows, a dusting of snow on top, $VIEW"
fi
echo "done"
