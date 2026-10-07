#!/usr/bin/env bash
# "The Big Chill" cast: the creatures and props whose roles were renamed, redrawn to match their new
# names. Each keeps its old file name, so the game picks the new art up with no code changes.
#
#   batch_bigchill.sh          — generate every master into ArtSource/, then ship
#   batch_bigchill.sh ship     — only downscale the masters into Assets/Art
#   batch_bigchill.sh <Name>   — one master again, e.g. Animal_Deer
set -u
cd "$(dirname "$0")/../.." || exit 1

MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline around the whole subject, exactly like the reference. Chunky rounded generous silhouette that reads clearly at 64px. Muted natural palette. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no ground shadow, no glow beneath the subject, no pale oval or ellipse under the feet, the feet stand on nothing at all, no border, no text."
CAPY="ArtSource/Capy_Idle.png"
VIEW="seen from the same 3/4 top-down oblique angle as the reference, standing, facing the viewer's left"

gen () {
  local name="$1"; shift
  local ref="$1"; shift
  local prompt="$1"; shift
  local out="ArtSource/$name.png"
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$MATCH Draw instead: $prompt $TAIL" \
      --out "$out" --transparent --quality high --ref "$ref"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

masters () {
  gen Animal_Monster "$CAPY" "a zombie capybara: the SAME capybara body shape and proportions as the reference, $VIEW, but with muddy grey-green fur with patchy darker spots, droopy half-closed sleepy eyes, a little drip of mud on its chin, one ear slightly bent, and a few frosty blue specks of ice on its back. Grumpy and shivering but cute, never scary or gory."
  gen Animal_Fox "$CAPY" "a burrow capybara: a smaller, rounder, younger version of the reference capybara, $VIEW, with slightly redder russet fur, a pale cream belly and muzzle, and a smudge of brown soil on its nose and front paws from digging. Quick and curious."
  gen Animal_Panda "$CAPY" "a woolly capybara: the SAME capybara shape as the reference, $VIEW, but covered in a thick fluffy cream-and-tan winter coat like a sheep's fleece, with a fluffy tuft on top of its head and only its face and little feet poking out. Extremely soft and huggable."
  gen Animal_Deer "$CAPY" "a guinea pig, $VIEW: small round chubby body, tortoiseshell patches of ginger, white and dark brown fur, tiny round ears, bright beady eyes and cheeks stuffed full with a flower petal sticking out of its mouth. Cheeky and mischievous. Its four little feet rest on empty transparent space with nothing drawn under them."
  gen Item_Bomb "ArtSource/Item_Bomb.png" "a round fizz bomb: a pale blue glass bath-bomb sphere fizzing with tiny bubbles, with a short twisted fuse on top that has a little spark at its tip."
  gen Item_DeerBomb "ArtSource/Item_DeerBomb.png" "a gentle shoo bomb: a small round pink bath bomb wrapped in a paper band printed with a little guinea pig silhouette, with a short fuse on top sparking softly."
  gen Build_Tower "ArtSource/Build_Tower.png" "the Steam Tower: a tall round stone tower seen from a 3/4 top-down oblique angle, built over a small steaming hot spring pool at its base, with a warm wooden roof, a large round window glowing with soft warm light, little heart-shaped vents near the top puffing gentle white steam. Cosy, magical, storybook."
}

ship () {
  for n in Animal_Monster Animal_Fox Animal_Panda Animal_Deer; do
    [ -f "ArtSource/$n.png" ] && node Tools/ChatGPT/downscale.mjs "ArtSource/$n.png" "Assets/Art/Capybaras/$n.png" 256
  done
  for n in Item_Bomb Item_DeerBomb; do
    [ -f "ArtSource/$n.png" ] && node Tools/ChatGPT/downscale.mjs "ArtSource/$n.png" "Assets/Art/Props/$n.png" 128
  done
  [ -f ArtSource/Build_Tower.png ] && node Tools/ChatGPT/downscale.mjs ArtSource/Build_Tower.png Assets/Art/Buildings/Build_Tower.png 256
}

case "${1:-}" in
  "") masters; ship ;;
  ship) ship ;;
  *) line=$(grep -E "^  gen $1 " "$0") && eval "${line#  }" || echo "no such master: $1" ;;
esac
echo "=== BIG CHILL DONE ==="
