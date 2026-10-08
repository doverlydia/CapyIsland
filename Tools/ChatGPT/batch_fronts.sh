#!/usr/bin/env bash
# Outfit pieces that wrap round a neck can't be drawn as whole loops in a 2D side-on game: you
# would see the back of the loop. These redraw them as only the FRONT part that shows below the
# chin (like the bow tie), plus a few small pieces: the bare fishing rod and Miso's tiara.
#
#   batch_fronts.sh            — everything, then ship
#   batch_fronts.sh <Name>     — one again (Bandana, Scarf, Bell, Rod, MisoTiara, MisoPearls), then ship
set -u
cd "$(dirname "$0")/../.." || exit 1

MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline, exactly like the reference. Chunky rounded generous shapes that read clearly at 64px. Muted natural palette. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no glow beneath the subject, no pale oval or ellipse or smudge under the subject, nothing at all beneath it, no border, no text."
BASE="ArtSource/Capy_Player.png"
PROP="ArtSource/Hill_Gray.png"
FRONT="Draw ONLY the front part of it that would be visible on the chest just under a capybara's chin, as a flat front-facing sticker-like piece: NO loop, NO strap going around, NO back part, NO hole in the middle, nothing behind. Nobody is wearing it."

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
    Bandana) gen ArtSource/Wear_Neck_Bandana.png "$BASE" "A teal polka-dot bandana neckerchief, as seen hanging on the chest of someone wearing it. $FRONT The knot and the tied ends are behind the neck and are NOT visible: draw NO knot, NO bow, NO tie ends, NO strings, NO loops. It is ONLY a single soft downward-pointing triangle of teal cloth with cream polka dots, slightly wider than tall, its top edge a gentle smile-shaped curve with a soft rolled hem (where it tucks under the chin), its sides falling to a rounded point at the bottom, with a couple of soft fabric folds." ;;
    Scarf) gen ArtSource/Wear_Neck_Scarf.png "$BASE" "A cosy red knitted winter scarf. $FRONT It is a short soft curved band of red knit (a gentle smile-shaped arc, solid, no gap) with two fringed ends hanging down from one side." ;;
    Bell) gen ArtSource/Wear_Neck_Bell.png "$BASE" "A red cat collar with a bell. $FRONT It is a short soft red strap shaped as a gentle smile-shaped arc (solid, no gap) with a round shiny golden jingle bell hanging from its middle." ;;
    Rod) gen ArtSource/Hobby_Rod.png "$PROP" "a single simple wooden fishing rod lying diagonally from bottom-left to top-right, a light bamboo-coloured pole with a small round reel near the bottom handle. NO fishing line, NO bobber, NO hook: just the bare rod. Side view, flat, no hands." ;;
    MisoPearls) gen ArtSource/Hobby_Miso_Pearls.png "$BASE" "A string of round creamy-white pearls worn as a necklace. $FRONT It is a single gentle smile-shaped arc of about nine round glossy pearls, solid, with a tiny pink heart-shaped clasp charm hanging from the middle pearl." ;;
    MisoTiara) gen ArtSource/Hobby_Miso_Tiara.png "$PROP" "a small dainty golden tiara on its own, seen from the front: a delicate gold band with three little points and a pink gem in the centre. NO cushion, NO pillow, NO stand, NO box, nothing under it." ;;
  esac
}

if [ -n "${1:-}" ]; then one "$1"; else for n in Bandana Scarf Bell Rod MisoTiara; do one "$n"; done; fi
bash Tools/ChatGPT/batch_wardrobe.sh ship
bash Tools/ChatGPT/batch_hobbies.sh ship
echo "done"
