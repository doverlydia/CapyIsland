#!/usr/bin/env bash
# The rest of the island's cast: forests, gardens and hills for every resource, the store chain,
# houses, animals, quest givers, bombs and decorations. Every prompt is referenced against an
# approved asset so the set stays on-model. Sequential: the API allows ~5 images/minute.
# Skips anything already generated, so it can be re-run after a failure.
set -u
cd "$(dirname "$0")/../.." || exit 1

STYLE="Rendered in clean vector style with soft airbrushed gradients, smooth and slightly glossy, almost claymation-soft. Chunky rounded generous silhouette that reads clearly at 64px. Muted natural palette, moderate saturation. Soft top-down lighting with one gentle light. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no border, no text. No shadow, no cast shadow, no ground shadow, no glow beneath the subject."
MATCH="Match the attached image's rendering style, outline weight, shading and palette EXACTLY. Draw instead:"
VIEW="seen from a 3/4 top-down oblique angle about 50 degrees above so both the top and the front are visible, for a cozy island-builder game."

gen () {
  local out="$1" ref="$2" prompt="$3"
  if [ -f "$out" ]; then echo "skip $out"; return; fi
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$MATCH $prompt $STYLE" --out "$out" --transparent --quality high --ref "$ref"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

PINE=ArtSource/Tree_Pine.png
POSIE=ArtSource/Garden_Posie.png
FLOWER=ArtSource/Flower_Posie.png
HILL=ArtSource/Hill_Gray.png
MILL=ArtSource/Build_Sawmill.png
HOUSE=ArtSource/Build_CapyHouse.png
CAPY=ArtSource/Capy_Idle.png
GHOST=ArtSource/Capy_Ghost.png

# ---- forests, gardens, hills
gen ArtSource/Tree_Maple.png $PINE "a single cute round maple tree $VIEW A full rounded canopy of warm autumn orange and soft red leaves in puffy clusters, short chunky warm-brown trunk."
gen ArtSource/Tree_Ironwood.png $PINE "a single cute tall ironwood tree $VIEW A tall narrow sturdy canopy in deep blue-green, with a thick dark grey-brown trunk that looks strong as iron."
gen ArtSource/Flower_Tulip.png $FLOWER "a single cute tulip plant: one plump yellow-orange tulip bloom on a short green stem with two broad leaves."
gen ArtSource/Flower_Rose.png $FLOWER "a single cute rose bush plant: one plump deep red rose bloom on a short green stem with a few leaves."
gen ArtSource/Garden_Tulip.png $POSIE "a small round flower garden patch $VIEW A low circular bed of soil with several plump yellow and orange tulips on short green stems."
gen ArtSource/Garden_Rose.png $POSIE "a small round flower garden patch $VIEW A low circular bed of soil with several plump deep red roses on short leafy stems."
gen ArtSource/Hill_Black.png $HILL "a small rocky dark hill outcrop $VIEW Rounded boulders of charcoal black-grey stone with glints of silver ore veins on top."
gen ArtSource/Hill_Gold.png $HILL "a small rocky hill outcrop $VIEW Rounded warm grey boulders studded with shiny gold ore nuggets that catch the light."

# ---- store chain and buildings
gen ArtSource/Store_General.png $MILL "a small cute general store shop building $VIEW Wooden walls, a striped green and cream awning over the front, crates and a barrel by the door."
gen ArtSource/Store_Bronze.png $MILL "a small cute shop building $VIEW Warm wooden walls with bronze-coloured metal trim and a bronze bell sign hanging over the door, a striped orange awning."
gen ArtSource/Store_Silver.png $MILL "a small cute shop building $VIEW Pale stone walls with shiny silver trim, a silver-grey roof and a blue striped awning."
gen ArtSource/Store_Steel.png $MILL "a small cute sturdy shop building $VIEW Grey stone walls with riveted steel plates and a dark slate roof with a little chimney, a teal awning."
gen ArtSource/Store_Gold.png $MILL "a small cute fancy shop building $VIEW Cream stone walls, a gleaming gold roof and gold trim, a red velvet awning."
gen ArtSource/Store_Gem.png $MILL "a small cute magical shop building $VIEW Lavender walls with a roof made of glittering teal and purple crystals, a gem sign over the door."
gen ArtSource/Store_Bank.png $MILL "a small cute bank building $VIEW White stone walls with little columns at the front, a dusty blue roof and a round gold coin emblem over the door."
gen ArtSource/Store_Fancy.png $MILL "a small cute boutique shop building $VIEW Pink walls, a scalloped white awning, flower boxes under the windows."
gen ArtSource/Build_LegendMill.png $MILL "the same kind of sawmill, but legendary: warm wood walls with shining gold trim, a golden water wheel and a little glowing star on the roof."
gen ArtSource/Build_LegendMine.png ArtSource/Build_Mine.png "the same kind of mine entrance, but legendary: shining gold beams and a golden minecart with a little glowing star above."
gen ArtSource/Build_FoxHouse.png $HOUSE "a small cute cottage $VIEW Neat dark wood walls, a steep red roof, a little lantern and a fox-tail weather vane on top."
gen ArtSource/Build_MonsterHouse.png $HOUSE "a small cute but spooky hut $VIEW Crooked purple-grey stone walls, a mossy lopsided roof, a round door with big friendly claw marks."
gen ArtSource/Build_Church.png $HOUSE "a small cute white wedding chapel $VIEW White walls, a soft blue roof with a small bell tower, a round pink window and a heart above the door."
gen ArtSource/Build_Tower.png $HOUSE "a small cute magical tower of love $VIEW A slim round stone tower with a pink conical roof, heart-shaped windows and a glowing heart at the very top."

# ---- animals and characters
gen ArtSource/Animal_Fox.png $CAPY "a cute little fox character in the same chibi style, standing, facing three-quarters to the left, orange fur with a cream chest and a fluffy white-tipped tail."
gen ArtSource/Animal_Panda.png $CAPY "a cute little panda character in the same chibi style, standing, facing three-quarters to the left, round and chubby."
gen ArtSource/Animal_Monster.png $CAPY "a cute little fuzzy monster character in the same chibi style, standing, facing three-quarters to the left, purple fur, two small horns, big friendly underbite with two tiny teeth."
gen ArtSource/Animal_Deer.png $CAPY "a cute little deer character in the same chibi style, standing on four legs, facing three-quarters to the left, soft brown with pale spots and small antlers, looking mischievous."
gen ArtSource/Capy_Pirate.png $CAPY "the same capybara as a pirate worker, wearing a red bandana and a tiny eyepatch, same pose and proportions."
gen ArtSource/Capy_Coriander.png $CAPY "a sweet girl capybara character, same pose and proportions, with long eyelashes, a pink flower tucked behind one ear and a little pink scarf."
gen ArtSource/Capy_Peach.png $CAPY "a sassy girl pirate capybara character, same pose and proportions, wearing a big tricorn pirate hat with a pink feather and gold hoop earrings."
gen ArtSource/Capy_Pete.png $CAPY "an old pirate capybara character, same pose and proportions, with a black eyepatch, a scruffy grey beard and a small black pirate hat."
gen ArtSource/Capy_Priest.png $CAPY "a kind old capybara priest character, same pose and proportions, wearing a simple white robe with a gold collar and round spectacles."

# ---- items and decorations
gen ArtSource/Item_Bomb.png ArtSource/Chest_Closed.png "a cute round black cartoon bomb with a short lit fuse sparkling at the top and a little shine on its surface."
gen ArtSource/Item_DeerBomb.png ArtSource/Chest_Closed.png "a cute round cartoon bomb painted green with a white deer antler symbol on the front and a short lit fuse."
gen ArtSource/Item_Dress.png ArtSource/Chest_Closed.png "a cute white wedding dress on a little wooden dress stand, with lace and a pink ribbon at the waist."
gen ArtSource/Item_Pearls.png ArtSource/Chest_Closed.png "a cute string of shiny white pearls laid in a heart shape on a small pink velvet cushion."
gen ArtSource/Item_Tiara.png ArtSource/Chest_Closed.png "a cute small golden tiara set with pink and blue gems, resting on a small purple velvet cushion."
gen ArtSource/Deco_WhiteFlowerTree.png $PINE "a single cute blossoming tree $VIEW A round fluffy canopy covered in white blossoms, short chunky trunk."
gen ArtSource/Deco_PinkFlowerTree.png $PINE "a single cute blossoming cherry tree $VIEW A round fluffy canopy covered in soft pink blossoms, short chunky trunk."
gen ArtSource/Deco_HolyStatue.png $HILL "a small cute stone statue of a capybara angel with little wings and a halo, on a round stone plinth."
gen ArtSource/Deco_GemStatue.png $HILL "a small cute statue of a capybara carved from glittering teal crystal, on a round stone plinth."
gen ArtSource/Deco_BushyTree.png $PINE "a single cute very bushy round topiary tree $VIEW Dense rich green leaves clipped into a big ball on a short trunk."
gen ArtSource/Deco_FallTree.png $PINE "a single cute tree in autumn colours $VIEW A round canopy of golden yellow and russet leaves, short chunky trunk."
gen ArtSource/Deco_TinyBush.png $PINE "a single cute tiny round green bush $VIEW Small and plump."
gen ArtSource/Deco_LittleGreeny.png $CAPY "a cute little green leafy sprite creature, round like a bush with two dot eyes and a sprout on its head, same chibi style."
gen ArtSource/Deco_Fountain.png $HILL "a small cute round stone fountain $VIEW Water splashing up from the middle into a round basin of light blue water."
gen ArtSource/Deco_MonsterDen.png $HILL "a small cute monster den $VIEW A mossy rock cave with a round dark entrance and two glowing friendly eyes peeking out."
gen ArtSource/Prop_Mote.png ArtSource/Chest_Closed.png "a cute glowing pink heart gem, soft and plump, with a bright sparkle highlight and a faint pink glow."

# ---- island dressing
gen ArtSource/Deco_GrassTuft.png $FLOWER "a small tuft of bright green grass blades, low and rounded."
gen ArtSource/Deco_Wildflowers.png $FLOWER "a tiny low cluster of little white and yellow wildflowers among a few leaves."
gen ArtSource/Deco_Pebbles.png $HILL "a tiny group of three small smooth grey pebbles."

echo "=== BATCH COMPLETE ==="
