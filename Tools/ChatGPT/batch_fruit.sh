#!/usr/bin/env bash
# Fruit tree set: three trees drawn bare of fruit (fruit are separate sprites that grow on the
# canopy and drop), the three fruits, and the treasure chest shaking can reveal.
# Sequential: the API allows ~5 images/minute. Each master is de-haloed with cleanalpha.
set -u
cd "$(dirname "$0")/../.." || exit 1

STYLE="Rendered in clean vector style with soft airbrushed gradients, smooth and slightly glossy, almost claymation-soft. Chunky rounded generous silhouette that reads clearly at 64px. Muted natural palette, moderate saturation. Soft top-down lighting with one gentle light. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no border, no text. No shadow, no cast shadow, no ground shadow, no glow beneath the subject."
MATCH="Match the attached image's rendering style, outline weight, shading and palette EXACTLY. Draw instead:"

gen () {
  local out="$1" ref="$2" prompt="$3"
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$MATCH $prompt $STYLE" --out "$out" --transparent --quality high --ref "$ref"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

TREE=ArtSource/Tree_Pine.png
gen ArtSource/Tree_Apple.png $TREE "a single cute round apple tree for a cozy island-builder game, seen from a 3/4 top-down oblique angle about 50 degrees above. A big fluffy rounded canopy made of three or four soft leafy puffs in fresh leafy green, lighter on top where the light hits, short chunky warm-brown trunk with a slight flare at the base. The canopy is full and bare, with no fruit on it."
gen ArtSource/Tree_Pear.png $TREE "a single cute pear tree for a cozy island-builder game, seen from a 3/4 top-down oblique angle about 50 degrees above. A tall rounded oval canopy made of soft leafy puffs in a slightly yellow-leaning sage green, lighter on top, short chunky warm-brown trunk. The canopy is full and bare, with no fruit on it."
gen ArtSource/Tree_Plum.png $TREE "a single cute legendary plum tree for a cozy island-builder game, seen from a 3/4 top-down oblique angle about 50 degrees above. A wide canopy of soft leafy puffs with gently sweeping drooping branches, deep green leaves with a subtle cool purple tint in the shadows and a few tiny pale blossoms, chunky curved warm-brown trunk. It looks special and magical. The canopy is full and bare, with no fruit on it."

FRUIT=ArtSource/Flower_Posie.png
gen ArtSource/Fruit_Apple.png $FRUIT "one single shiny red apple, cute and juicy, round and plump, with a short brown stem and one small green leaf, a bright glossy highlight on the upper left. Seen from slightly above. Dark warm brown medium-weight outline."
gen ArtSource/Fruit_Pear.png $FRUIT "one single shiny golden-green pear, cute and juicy, plump teardrop shape, with a short brown stem and one small green leaf, a bright glossy highlight on the upper left. Seen from slightly above. Dark warm brown medium-weight outline."
gen ArtSource/Fruit_Plum.png $FRUIT "one single shiny deep purple plum, cute and juicy, round and plump with a soft dusty bloom and a faint crease, short brown stem and one small green leaf, a bright glossy highlight on the upper left. Seen from slightly above. Dark warm brown medium-weight outline."

BLD=ArtSource/Build_Sawmill.png
gen ArtSource/Chest_Closed.png $BLD "a small cute closed wooden treasure chest for a cozy island-builder game, seen from a 3/4 top-down oblique angle about 50 degrees above. Rounded domed lid, warm wood planks with dusty gold metal bands and a little gold latch."
gen ArtSource/Chest_Open.png ArtSource/Chest_Closed.png "the same small cute wooden treasure chest, now with its domed lid swung open backwards, a warm golden glow of coins and a sparkle peeking out from inside. Same angle, same proportions, same colours."

echo "=== BATCH COMPLETE ==="
