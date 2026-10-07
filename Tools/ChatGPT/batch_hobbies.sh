#!/usr/bin/env bash
# Art for the hobbies (gardening, cooking, bug catching, cat care) and the airport.
# Masters go to ArtSource/Hobby_*.png; `ship` trims and downscales them into
# Assets/Resources/Hobbies/, where the game loads them by name.
#
#   batch_hobbies.sh props|crops|dishes|bugs|cats   — one group
#   batch_hobbies.sh all                            — everything, then ship
#   batch_hobbies.sh ship                           — only trim and downscale
#   batch_hobbies.sh <Name>                         — one master again, e.g. Crop_Carrot
set -u
cd "$(dirname "$0")/../.." || exit 1

MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline, exactly like the reference. Chunky rounded generous shapes that read clearly at 64px. Muted natural palette. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no glow beneath the subject, no pale oval or ellipse or smudge under the subject, nothing at all beneath it, no border, no text."
PROP="ArtSource/Hill_Gray.png"
ANIMAL="ArtSource/Capy_Idle.png"
VIEW="seen from a 3/4 top-down oblique angle about 50 degrees above"

gen () {
  local name="$1"; shift
  local ref="$1"; shift
  local prompt="$1"; shift
  local out="ArtSource/Hobby_$name.png"
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$MATCH Draw instead: $prompt $TAIL" \
      --out "$out" --transparent --quality high --ref "$ref"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

props () {
  gen Plot "$PROP" "a small square vegetable garden plot $VIEW: dark rich tilled brown soil in neat furrows, edged with a low frame of wooden planks. Empty, nothing planted."
  gen Sprout "$PROP" "a single tiny green seedling sprout with two round leaves poking out of a small mound of brown soil, $VIEW."
  gen Airport "$PROP" "a tiny cosy island airstrip $VIEW: a short paved runway strip with white dashes, a small wooden control hut with a red-and-white striped windsock on a pole, and a little red propeller plane parked beside it. Storybook game building, not realistic."
}

crops () {
  gen Crop_Carrot "$PROP" "three ripe orange carrots with leafy green tops, pulled from the ground and lying together."
  gen Crop_Tomato "$PROP" "a small bunch of three shiny ripe red tomatoes on a green vine stem."
  gen Crop_Pumpkin "$PROP" "one round chunky orange pumpkin with a curly green stem and a leaf."
  gen Crop_Strawberry "$PROP" "three plump red strawberries with green leafy caps and little yellow seeds."
  gen Crop_Corn "$PROP" "two ears of golden yellow corn with their green husks peeled back."
  gen Crop_Cabbage "$PROP" "one round fresh green cabbage with crinkly layered leaves."
}

dishes () {
  gen Dish_CarrotSoup "$PROP" "a round ceramic bowl of smooth orange carrot soup with a swirl of cream and a sprig of herb, $VIEW."
  gen Dish_GrilledFish "$PROP" "a whole grilled fish with golden crispy skin and grill marks on a small oval plate with a lemon wedge, $VIEW."
  gen Dish_VeggieStew "$PROP" "a small clay pot of hearty vegetable stew with chunks of carrot, tomato and cabbage, steaming gently, $VIEW."
  gen Dish_PumpkinPie "$PROP" "a whole round pumpkin pie with a golden crimped crust and a dollop of cream on top, $VIEW."
  gen Dish_StrawberryMochi "$PROP" "three soft round pink mochi sweets with a strawberry peeking out of each, on a little wooden board, $VIEW."
  gen Dish_CornChowder "$PROP" "a round bowl of creamy pale yellow corn chowder with corn kernels and a little fish on top, $VIEW."
  gen Dish_HotPot "$PROP" "a small bubbling hot pot in a round iron pan with fish slices, cabbage and a slice of yellow yuzu floating in the broth, $VIEW."
  gen Dish_TomatoSalad "$PROP" "a small bowl of fresh salad with red tomato wedges, green leaves and sweet corn, $VIEW."
}

bugs () {
  gen Bug_Butterfly "$PROP" "a single pretty butterfly with wide pale-yellow wings with orange spots, wings open, seen from above."
  gen Bug_BlueButterfly "$PROP" "a single butterfly with shimmering sky-blue wings edged in black, wings open, seen from above."
  gen Bug_Ladybug "$PROP" "a single round red ladybug with black spots, seen from above."
  gen Bug_Bee "$PROP" "a single chubby fuzzy bumblebee with yellow and black stripes and small translucent wings, seen from the side."
  gen Bug_Dragonfly "$PROP" "a single teal-green dragonfly with four long translucent wings spread wide, seen from above."
  gen Bug_Beetle "$PROP" "a single shiny round green jewel beetle with a metallic shell, seen from above."
  gen Bug_Firefly "$PROP" "a single little firefly with a softly glowing yellow-green tail and small wings, seen from the side."
  gen Bug_Snail "$PROP" "a single cute garden snail with a swirly brown shell and little eye stalks, seen from the side."
  gen Bug_StagBeetle "$PROP" "a single dark brown stag beetle with big curved antler jaws, seen from above."
  gen Bug_RainbowMoth "$PROP" "a single rare moth with wings shimmering in soft rainbow pastel colours, wings open, seen from above."
}

cats () {
  gen Cat_Ginger "$ANIMAL" "a cute chubby ginger tabby cat sitting upright, $VIEW, round head, small triangle ears, curled tail, simple dot eyes, friendly."
  gen Cat_Black "$ANIMAL" "a cute chubby black cat with bright yellow-green eyes sitting upright, $VIEW, round head, small triangle ears, curled tail, friendly."
  gen Cat_Calico "$ANIMAL" "a cute chubby calico cat with white, orange and black patches sitting upright, $VIEW, round head, small triangle ears, friendly."
  gen Cat_Grey "$ANIMAL" "a cute chubby fluffy grey cat sitting upright, $VIEW, round head, small triangle ears, curled tail, simple dot eyes, friendly."
  gen Cat_Siamese "$ANIMAL" "a cute chubby siamese cat, cream body with dark brown face, ears, paws and tail, sitting upright, $VIEW, blue dot eyes, friendly."
  gen Cat_White "$ANIMAL" "a cute chubby pure white fluffy cat with a pink nose sitting upright, $VIEW, round head, small triangle ears, friendly."
}

ship () {
  local out="Assets/Resources/Hobbies"
  mkdir -p "$out"
  local tmp; tmp=$(mktemp -d)
  for src in ArtSource/Hobby_*.png; do
    [ -f "$src" ] || continue
    local name; name=$(basename "$src" .png); name=${name#Hobby_}
    local size=128
    case "$name" in Airport) size=320 ;; Plot|Cat_*) size=192 ;; esac
    node Tools/ChatGPT/trim.mjs "$src" "$tmp/$name.png" --alpha 60 > /dev/null &&
      node Tools/ChatGPT/downscale.mjs "$tmp/$name.png" "$out/$name.png" "$size"
    rm -f "$tmp/$name.png"
  done
}

case "${1:-}" in
  props) props ;;
  crops) crops ;;
  dishes) dishes ;;
  bugs) bugs ;;
  cats) cats ;;
  all) props; crops; dishes; bugs; cats; ship ;;
  ship) ship ;;
  "") echo "usage: batch_hobbies.sh props|crops|dishes|bugs|cats|all|ship|<Name>" ;;
  *) line=$(grep -E "^  gen $1 " "$0") && eval "${line#  }" || echo "no such master: $1" ;;
esac
echo "=== HOBBIES DONE ==="
