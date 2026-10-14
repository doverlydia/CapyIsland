#!/usr/bin/env bash
# Backdrops (and the odd prop) for the little movies in Assets/Scripts/UI/Cutscenes/.
# Masters go to ArtSource/Cutscenes/; `ship` downscales them into Assets/Resources/Cutscenes/.
#
#   batch_cutscenes.sh <Name>   — one master, e.g. Bath, then ship
#   batch_cutscenes.sh new      — the ones not made yet, then ship
#   batch_cutscenes.sh ship     — only downscale
set -u
cd "$(dirname "$0")/../.." || exit 1
mkdir -p ArtSource/Cutscenes

STYLE="Match the attached image's rendering style, outline weight, shading and palette EXACTLY (clean storybook vector fills, soft airbrushed gradients, warm dark-brown outlines on built props, muted warm palette)."
STAGE="NO characters, NO people, NO animals, NO text. The lower 40% of the image is open flat ground, clear and empty for characters to stand on."
REF="ArtSource/Cutscenes/Cutscene_Wedding.png"

backdrop () {
  local name="$1"; shift
  local prompt="$1"; shift
  local out="ArtSource/Cutscenes/Cutscene_$name.png"
  echo "=== $out ==="
  node Tools/ChatGPT/chatgpt.mjs image "$STYLE Draw instead: $prompt $STAGE" --out "$out" --size 1536x1024 --quality high --ref "$REF" || echo "FAILED: $out"
}

prop () {
  local name="$1"; shift
  local prompt="$1"; shift
  local out="ArtSource/Cutscenes/Cutscene_$name.png"
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$STYLE Draw instead: $prompt Subject only, on a fully transparent background, no backdrop, no shadow, nothing beneath it, no text." \
      --out "$out" --size 1536x1024 --transparent --quality high --ref "$REF"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

one () {
  case "$1" in
    Wedding) backdrop Wedding "a wide, cosy storybook backdrop for a wedding scene, landscape composition, gentle front 3/4 view: a small white wooden chapel with a terracotta roof and a little bell tower on a grassy island hill, framed by two blooming pink cherry-blossom trees, a soft flower arch of pink and white blossoms in the centre middle ground with an open grassy clearing in front of it, paper lanterns strung between the trees, pastel evening sky, calm sea glimpsed at the far sides." ;;
    Dance) backdrop Dance "a wide cosy storybook backdrop for a little dance performance at a harbour: a small wooden pirate ship stuck in pale blue sea ice in the background, a wooden dock across the picture with a small raised plank stage, colourful bunting and lanterns overhead, barrels and rope at the sides, the sea behind, late-afternoon gold and blue sky." ;;
    Bath) backdrop Bath "a wide cosy storybook backdrop of a little outdoor hot spring on a snowy island: in the centre middle ground a round natural rock pool of steaming turquoise water with soft white steam curling up, a small wooden bucket and a stack of folded towels on a rock beside it, a couple of glowing paper lanterns on posts, a few yuzu fruits floating in the water, snow on the rocks and on dark green pine trees behind, soft blue-violet evening sky. The water surface of the pool is wide and flat in the middle of the image." ;;
    Shore) backdrop Shore "a wide cosy storybook backdrop looking out to sea from a grassy island shore: in the foreground a sandy grassy shore with a few tufts and pebbles, in the middle a wide band of calm blue-green sea with gentle wave lines, and on the far side across the water a second small green island with a few round trees and a tiny wooden hut. Soft clear morning sky with fluffy clouds. The sea between the two shores is empty and open." ;;
    Bridge) prop Bridge "a long low land bridge, a gently arched natural causeway of golden sand topped with green grass and a few small rocks and tufts, running horizontally left to right across the whole width of the image, seen from the front at a gentle 3/4 angle, with wet dark sand along its lower edge and a few drips of sea water falling from it as if it has just risen out of the sea. Wide and flat, filling the width of the picture." ;;
    # ---- the intro movie: the warm isles, the Big Chill, Old Yuzu's bubbles, the drift to shore
    IntroWarm) backdrop IntroWarm "a wide cosy storybook backdrop of a happy, warm island village long ago: in the centre middle ground a big round natural rock hot spring of steaming turquoise water with soft white steam curling up and a few yuzu fruits floating, beside it a small traditional wooden bathhouse with a curved terracotta roof and a red noren curtain, a tall round stone Steam Tower behind it puffing soft white steam, paper lanterns strung on posts, colourful flower beds and round green trees, a calm sea glimpsed at the sides, bright warm late-morning sunshine and a clear blue sky with fluffy clouds. The water surface of the pool is wide and flat in the middle of the image." ;;
    IntroFrozen) REF=ArtSource/Cutscenes/Cutscene_IntroWarm.png backdrop IntroFrozen "the SAME scene with the SAME composition and the same buildings in the same places, but frozen by a sudden bitter winter: the hot spring is frozen solid into pale blue ice with no steam, the Steam Tower is cold with no steam and icicles hanging from it, the bathhouse roof and lanterns are heavy with snow, flowers withered under frost, trees bare or snow-laden, snow drifts on the ground, a heavy grey-blue stormy sky with swirling snow." ;;
    IntroSea) backdrop IntroSea "a wide calm storybook seascape at night: a dark blue-violet starry sky with a big soft glowing moon, gentle wave lines on a wide calm dark-teal sea filling the lower two thirds, its surface catching a soft silver moon path, a few tiny frozen snowy islands far away on the horizon. The sea in the lower half is open and empty." ;;
    IntroTub) prop IntroTub "one small round wooden bath tub, the kind used at a hot spring bathhouse, made of wooden staves held by two copper bands, empty, seen from the front at a gentle 3/4 angle so a little of its inside shows, a little wet and weathered from the sea." ;;
    IntroYuzu)
      echo "=== ArtSource/Cutscenes/Cutscene_IntroYuzu.png ==="
      node Tools/ChatGPT/chatgpt.mjs image "Match the attached capybara sprites' rendering style, outline weight, shading and proportions EXACTLY (FIRST: a capybara sprite, SECOND: a ghostly version of the same old lady). Draw instead: Old Yuzu, a kind elderly grandmother capybara, ALIVE and warm-coloured (the same tan-brown capybara fur as the FIRST image), standing facing left like the FIRST image, a little hunched with age, wearing a cosy knitted shawl in soft orange and a small yuzu fruit with a leaf tucked behind one ear, round spectacles, a gentle smile, holding a short wooden walking stick. Oversized head, small rounded body, tiny limbs, dot eyes, warm dark-brown outline. Subject only, on a fully transparent background, no backdrop, no shadow, nothing beneath it, no text."         --out ArtSource/Cutscenes/Cutscene_IntroYuzu.png --size 1024x1024 --transparent --quality high --ref Assets/Art/Capybaras/Capy_Idle.png,Assets/Art/Capybaras/Capy_Ghost.png         && node Tools/ChatGPT/cleanalpha.mjs ArtSource/Cutscenes/Cutscene_IntroYuzu.png || echo FAILED ;;
    Mill) backdrop Mill "a wide cosy storybook backdrop of a woodcutting clearing on an island: a small wooden sawmill with a water wheel on the left in the middle ground, a stack of cut logs and a big tree stump with an axe resting against it in the centre, round green trees and pines behind, wood chips scattered on the grass, warm midday sunlight and a soft blue sky." ;;
    # The hobby movies' own places (HobbyCutscene).
    Kitchen) backdrop Kitchen "a wide cosy storybook backdrop of an outdoor island kitchen: a wooden kitchen stall with a cream and red striped awning in the middle ground on the right, a stone stove with a steaming pot on its counter, hanging copper pans and a string of onions, baskets of eggs and vegetables beside it; on the left far behind, a steaming hot spring ringed with river stones at the foot of a little round stone tower; round green trees, warm afternoon light and a soft blue sky." ;;
    Meadow) backdrop Meadow "a wide cosy storybook backdrop of a sunny island flower meadow: patches of wildflowers in pink, yellow and white among soft green grass, a few round flowering bushes, a big leafy tree with a hollow in its trunk on the left, a low wooden fence and a few stepping stones in the middle ground, rolling green hills behind, a soft summer sky with fluffy clouds." ;;
    Shrine) backdrop Shrine "a wide cosy storybook backdrop of a little hilltop shrine on an island: a small wooden shrine with a curved roof and a red torii gate in the middle ground, a wooden rack of small hanging wooden prayer plaques beside it, two stone lanterns, a cherry tree with pale pink blossom, the calm sea far below behind, soft golden evening light." ;;
    CatHome) backdrop CatHome "a wide cosy storybook backdrop of an island cottage garden: the wooden porch of a small cosy cottage on the right with a soft cushion and a little saucer on its steps and a basket of yarn balls beside the door, flower pots along the porch, a low wooden fence, a big shady round tree on the left, warm afternoon light and a soft blue sky." ;;
  esac
}

ship () {
  mkdir -p Assets/Resources/Cutscenes
  for src in ArtSource/Cutscenes/Cutscene_*.png; do
    [ -f "$src" ] || continue
    node Tools/ChatGPT/downscale.mjs "$src" "Assets/Resources/Cutscenes/$(basename "$src")" 1024
  done
}

case "${1:-}" in
  ship) ship ;;
  new) for n in Bath Shore Bridge Mill; do one "$n"; done; ship ;;
  intro) for n in IntroWarm IntroFrozen IntroSea IntroTub IntroYuzu; do one "$n"; done; ship ;;
  "") echo "usage: batch_cutscenes.sh <Name>|new|ship" ;;
  *) one "$1"; ship ;;
esac
echo "done"
