#!/usr/bin/env bash
# The wardrobe: a bare version of the player's capybara (the crown becomes one hat among many)
# and every outfit piece on its own, to be layered over the body in game.
#
#   batch_wardrobe.sh base     — the bare capybara only (approve it before the rest)
#   batch_wardrobe.sh items    — every outfit piece, styled against the approved base
#   batch_wardrobe.sh <Name>   — one piece again, e.g. Hat_Yuzu
#   batch_wardrobe.sh ship     — trim and downscale everything into Assets/Resources/Wardrobe
#   batch_wardrobe.sh all      — base, items, then ship
#
# The game finds the art by file name, so shipping is all it takes: the Wardrobe menu tile, the
# Dressed Up achievement and the outfit rewards switch on by themselves once Capy_Player exists.
set -u
cd "$(dirname "$0")/../.." || exit 1

MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline, exactly like the reference. Chunky rounded generous shapes that read clearly at 64px. Muted natural palette. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no ground shadow, no glow beneath the subject, no pale oval or ellipse or smudge under the subject, nothing at all beneath it, no border, no text."
ITEM="A single clothing accessory for a cute cartoon capybara, drawn on its own with nobody wearing it, seen from the front and slightly above, slightly turned to the viewer's left like the character in the reference. Sized and shaped to fit the reference capybara's head."

gen () {
  local out="$1"; shift
  local ref="$1"; shift
  local prompt="$1"; shift
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$MATCH Draw instead: $prompt $TAIL" \
      --out "$out" --transparent --quality high --ref "$ref"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

BASE="ArtSource/Capy_Player.png"

base () {
  gen "$BASE" "ArtSource/Capy_King.png" \
    "the SAME capybara character in the SAME pose, angle, proportions, colours and outline, but with nothing on its head: remove the crown completely and draw the top of its round head and both small ears where the crown was. A completely plain bare capybara with no clothing, no accessories and no props."
}

item () {  # item <Name> <description>
  gen "ArtSource/Wear_$1.png" "$BASE" "$ITEM $2"
}

items () {
  item Hat_Crown "a small golden king's crown with five rounded points, each tipped with a little gold ball, and one red gem on the front band."
  item Hat_Yuzu "a single round yellow-orange yuzu citrus fruit with one small green leaf on its stem, sized to balance on top of a capybara's head like a hat."
  item Hat_Straw "a wide-brimmed woven straw sun hat with a red ribbon band, seen from the front and slightly above."
  item Hat_Flowers "a ring-shaped flower crown woven from small pink, white and yellow blossoms with little green leaves."
  item Hat_Beanie "a soft knitted beanie hat in mustard yellow with a folded cuff and a fluffy pom-pom on top."
  item Hat_Party "a cone-shaped party hat in teal with cream polka dots and a small fluffy pom-pom at the tip."
  item Hat_Towel "a small white bath towel folded into a neat square, the kind worn on the head at a Japanese hot spring, with a thin pale blue stripe."
  item Neck_Scarf "a cosy knitted red scarf wrapped once around a neck, with its two fringed ends hanging down at the front, seen from the front."
  item Neck_Bandana "a teal neckerchief bandana tied around a neck with the triangle pointing down at the front, cream dot pattern."
  item Neck_BowTie "a dapper bow tie in deep plum purple with a tiny knot in the middle."
  item Face_Sunglasses "a pair of round dark sunglasses with a thin black frame, seen from the front, lenses with a small white shine."
  item Face_Glasses "a pair of round wire reading glasses with thin gold frames and clear lenses, seen from the front."
  item Back_Backpack "a tiny rounded hiking backpack in forest green with a brown flap and two shoulder straps, seen from the side."
  item Neck_MedalFish "a shiny round gold winner's medal stamped with a little fish, hanging on a short teal ribbon loop worn around a neck, seen from the front."
  item Neck_MedalBug "a shiny round gold winner's medal stamped with a little butterfly, hanging on a short leaf-green ribbon loop worn around a neck, seen from the front."
  item Neck_MedalBird "a shiny round gold winner's medal stamped with a little songbird, hanging on a short sky-blue ribbon loop worn around a neck, seen from the front."
  item Hat_Angler "a soft olive-green fisherman's bucket hat with a little colourful fishing fly hooked onto its band."
  item Hat_Feather "a small jaunty red felt cap with one long colourful bird feather tucked into its band."
  item Hat_Antennae "a thin headband with two springy curled bug antennae, each topped with a fluffy little yellow pom-pom ball."
  item Back_Cape "a short flowing royal cape in deep red with a gold trim edge, drawn as it hangs from the shoulders seen from behind and slightly to the side."
}

ship () {
  local out="Assets/Resources/Wardrobe"
  mkdir -p "$out"
  local tmp; tmp=$(mktemp -d)
  # The bare capybara keeps its full frame, like every other character sprite.
  [ -f "$BASE" ] && node Tools/ChatGPT/downscale.mjs "$BASE" "$out/Capy_Player.png" 256
  for src in ArtSource/Wear_*.png; do
    [ -f "$src" ] || continue
    local name; name=$(basename "$src" .png); name=${name#Wear_}
    node Tools/ChatGPT/trim.mjs "$src" "$tmp/$name.png" > /dev/null &&
      node Tools/ChatGPT/downscale.mjs "$tmp/$name.png" "$out/$name.png" 192
    rm -f "$tmp/$name.png"
  done
}

case "${1:-}" in
  ship) ship ;;
  all) base; items; ship ;;
  base) base ;;
  items) items ;;
  "") echo "usage: batch_wardrobe.sh base|items|ship|all|<Name>" ;;
  *) # one named piece again: find its line in items() and run it
     line=$(grep -E "^  item $1 " "$0") && eval "${line#  }" || echo "no such item: $1" ;;
esac
echo "=== WARDROBE DONE ==="
