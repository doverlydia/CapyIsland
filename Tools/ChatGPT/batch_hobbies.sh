#!/usr/bin/env bash
# Art for the hobbies (gardening, cooking, bug catching, fishing, birdwatching, cat care) and the airport.
# Masters go to ArtSource/Hobby_*.png; `ship` trims and downscales them into
# Assets/Resources/Hobbies/, where the game loads them by name.
#
#   batch_hobbies.sh props|crops|dishes|bugs|fish|birds|baits|cats   — one group
#   batch_hobbies.sh all                            — everything, then ship
#   batch_hobbies.sh ship                           — only trim and downscale
#   batch_hobbies.sh <Name>                         — one master again, e.g. Crop_Carrot
set -u
cd "$(dirname "$0")/../.." || exit 1

MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline, exactly like the reference. Chunky rounded generous shapes that read clearly at 64px. Muted natural palette. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no glow beneath the subject, no pale oval or ellipse or smudge under the subject, nothing at all beneath it, no border, no text."
PROP="ArtSource/Hill_Gray.png"
ANIMAL="ArtSource/Capy_Idle.png"
CRITTER="ArtSource/Hobby_Bug_Bee.png"   # creatures match the bug set: outlined, cute, chunky
FISHVIEW="seen from the side, facing left, as if just caught, cute and chunky with a simple round dot eye"
BIRDVIEW="standing, seen from the side facing left, cute plump storybook bird with a simple round dot eye"
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

# Dishes made with Chef Tofu's pantry (eggs, milk, flour, rice, sugar).
dishes2 () {
  gen Dish_Omelette "$PROP" "a fluffy golden rolled omelette with a few shreds of green cabbage on a small round plate, $VIEW."
  gen Dish_Milkshake "$PROP" "a tall glass of pink strawberry milkshake with a swirl of whipped cream, a strawberry on top and a striped straw, $VIEW."
  gen Dish_Pancakes "$PROP" "a small stack of three fluffy golden pancakes with a melting pat of butter and a drizzle of syrup on a round plate, $VIEW."
  gen Dish_Tempura "$PROP" "a little basket of crispy golden fish tempura pieces in light batter on a folded paper, with a small dipping bowl, $VIEW."
  gen Dish_Onigiri "$PROP" "two triangle rice balls (onigiri) of white rice, each wrapped at the bottom with a strip of dark green seaweed, on a little wooden board, $VIEW."
  gen Dish_CornBread "$PROP" "a square slab of golden yellow corn bread with a few corn kernels baked on top, cut into chunky squares in a small baking tin, $VIEW."
  gen Dish_Pudding "$PROP" "a wobbly round caramel custard pudding with glossy brown caramel on top, on a small plate, $VIEW."
}

# Chef Tofu's pantry: what you can't grow.
pantry () {
  gen Pantry_Egg "$PROP" "three smooth cream-white eggs in a small woven straw basket, $VIEW."
  gen Pantry_Milk "$PROP" "a small old-fashioned glass milk bottle full of white milk with a blue paper cap, $VIEW."
  gen Pantry_Flour "$PROP" "a small cloth sack of white flour tied with string, open at the top with flour peeking out and a little wooden scoop, $VIEW."
  gen Pantry_Rice "$PROP" "a small round wooden bowl heaped with white rice grains next to a little cloth rice sack, $VIEW."
  gen Pantry_Sugar "$PROP" "a small glass jar of white sugar cubes with a cork lid, $VIEW."
}

# Chef Tofu himself, and his kitchen stall on the Heart of the Isles.
chef () {
  gen Chef "ArtSource/Hobby_Sailor.png" "a friendly round capybara chef standing upright like the reference fisherman: a tall puffy white chef's hat, a white chef's jacket and a little red neckerchief, a cream apron, holding a wooden spoon up cheerfully in one paw, a small copper pot at his feet. Warm tan-brown fur, simple dot eyes, a gentle smile."
  gen Chef_Stall "ArtSource/Store_Garden.png" "a small cosy outdoor kitchen stall $VIEW: a wooden counter with a cream and red striped cloth awning on posts, a little stove with a steaming pot on the counter, hanging copper pans and a string of onions, a crate of eggs and a jar of milk beside it. Storybook game building, chunky and simple."
  gen Recipe_Book "$PROP" "a thick old cookbook lying open on a small wooden book stand, cream pages with little drawings of dishes, a red ribbon bookmark and a wooden spoon resting beside it, $VIEW."
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
  gen Bug_RainFrog "$CRITTER" "a single tiny cute bright green tree frog sitting, big round eyes, glossy skin with a few raindrops on it, little round toe pads, seen from the front-side, chunky and simple."
  gen Bug_RainbowMoth "$PROP" "a single rare moth with wings shimmering in soft rainbow pastel colours, wings open, seen from above."
}

fish () {
  gen Fish_sardine "$CRITTER" "a single small slim sardine with a shiny silver-blue body and a darker blue back, $FISHVIEW."
  gen Fish_anchovy "$CRITTER" "a single tiny slender anchovy, pale silver with a soft silver stripe along its side, $FISHVIEW."
  gen Fish_mackerel "$CRITTER" "a single plump mackerel with a blue-green back covered in wavy dark stripes and a silvery white belly, $FISHVIEW."
  gen Fish_bream "$CRITTER" "a single rosy pink sea bream with a tall rounded back, soft red fins and a pale belly, $FISHVIEW."
  gen Fish_clownfish "$CRITTER" "a single bright orange clownfish with three white bands edged in thin black, rounded fins, $FISHVIEW."
  gen Fish_puffer "$CRITTER" "a single round puffed-up pufferfish like a ball, sandy yellow with brown spots, short soft rounded spikes and tiny fins, $FISHVIEW."
  gen Fish_flounder "$CRITTER" "a single flat oval flounder lying flat, seen from above, sandy brown with speckles, both little eyes on the top side, frilly fins all around its edge."
  gen Fish_squid "$CRITTER" "a single cute little squid, pale pink-white with soft lilac spots, a pointed mantle with two small fins, two big round eyes and a bunch of short curly tentacles, seen from the side, pointing left, chunky and simple."
  gen Fish_salmon "$CRITTER" "a single chunky salmon with silver-pink sides, a coral belly and a dark speckled back, $FISHVIEW."
  gen Fish_octopus "$CRITTER" "a single cute round octopus, rosy magenta-red with paler spots, a big round head and eight short curly tentacles curled beneath it, seen from the front-side, friendly."
  gen Fish_tuna "$CRITTER" "a single big chunky bluefin tuna with a dark navy back, a shiny silver belly and little yellow finlets near the tail, $FISHVIEW."
  gen Fish_yuzuray "$CRITTER" "a single ray seen from above, wide rounded diamond-shaped wings, sunny citrus yellow with soft orange spots like a yuzu fruit, a short tail, small eyes on top."
  gen Fish_moonfish "$CRITTER" "a single round flat moonfish (opah), silvery blue body with white spots and bright red fins and lips, $FISHVIEW."
  gen Fish_swordfish "$CRITTER" "a single sleek slate-blue swordfish with a long pointed sword bill, a tall curved dorsal fin and a pale belly, $FISHVIEW, chunky rather than thin."
  gen Fish_snapper "$CRITTER" "a single golden snapper with shiny gold and orange scales and a slight sparkle, a prized rare catch, $FISHVIEW."
  gen Fish_steameel "$CRITTER" "a single friendly mint-teal eel with a soft wavy body curled in an S shape, a little fin along its back, and a few soft wisps of steam rising off it as if it came from a hot spring, $FISHVIEW."
  gen Fish_rainbowtrout "$CRITTER" "a single rainbow trout with a speckled olive-green back, a soft pink and lilac rainbow stripe along its side and a pale belly, glistening with a few raindrops, $FISHVIEW."
  gen Fish_coelacanth "$CRITTER" "a single ancient coelacanth, deep blue body with white speckles, chunky pale blue lobed fins like little legs and a three-lobed tail, a legendary rare catch, $FISHVIEW."
}

birds () {
  gen Bird_sparrow "$CRITTER" "a single sparrow with a streaky brown back, a grey cap and a cream breast, $BIRDVIEW."
  gen Bird_robin "$CRITTER" "a single round European robin with an olive-brown back and a bright orange-red breast and face, $BIRDVIEW."
  gen Bird_bluetit "$CRITTER" "a single blue tit with a bright blue cap and wings, white cheeks, a dark eye stripe and a yellow breast, $BIRDVIEW."
  gen Bird_finch "$CRITTER" "a single goldfinch with a red face, a black and white head, a tan body and black wings with a bright yellow bar, $BIRDVIEW."
  gen Bird_dove "$CRITTER" "a single turtle dove, soft pinkish-grey with scalloped orange-brown wings and a little striped neck patch, $BIRDVIEW."
  gen Bird_heron "$CRITTER" "a single grey heron standing tall on long legs, grey body, white neck folded in an S, a black crest stripe and a long yellow dagger beak, $BIRDVIEW."
  gen Bird_kingfisher "$CRITTER" "a single kingfisher with a brilliant blue back and head, an orange breast, a white throat and a long pointed dark beak, $BIRDVIEW."
  gen Bird_parrot "$CRITTER" "a single green parrot with red and blue wing feathers, a yellow patch and a curved pale beak, perched upright, $BIRDVIEW."
  gen Bird_toucan "$CRITTER" "a single toucan with a black body, a white throat and a huge chunky orange and yellow beak, $BIRDVIEW."
  gen Bird_egret "$CRITTER" "a single snowy egret, pure white with soft wispy plumes, standing on black legs with yellow feet, a slim black beak, $BIRDVIEW."
  gen Bird_hummingbird "$CRITTER" "a single tiny hummingbird hovering, shimmering green body, a bright pink throat, a long thin beak and soft blurred wings, seen from the side facing left."
  gen Bird_crane "$CRITTER" "a single red-crowned crane standing on long legs, white body, black neck and black tail feathers, a small red cap on its head, $BIRDVIEW."
  gen Bird_owl "$CRITTER" "a single round fluffy tawny owl perched upright, warm brown and cream mottled feathers, big round dark eyes in a pale heart-shaped face, tiny ear tufts, $BIRDVIEW but turning its head to look at the viewer."
  gen Bird_pheasant "$CRITTER" "a single golden pheasant with a golden crest, a bright red body, an orange striped cape around its neck and a long striped tail, a legendary rare bird, $BIRDVIEW."
}

baits () {
  gen Bait_Worm "$PROP" "a small round wooden bait tin, open, full of three cute wiggly pink earthworms curling over the rim, $VIEW."
  gen Bait_Shrimp "$PROP" "a little paper bait cone holding three plump pink cooked-looking shrimps with curled tails, $VIEW."
  gen Bait_Lure "$PROP" "a single shiny golden fishing lure shaped like a little fish, with a soft sparkle and a small hook ring, a precious rare item."
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
    case "$name" in Miso_Dress*) continue ;; esac   # untrimmed, framed like Capy_Coriander: batch_miso.sh ships them
    local size=128
    case "$name" in Airport|Chef_Stall) size=320 ;; Plot|Cat_*|Stove|Pot|SewingMachine) size=192 ;; Sailor|Chef|Fish_*|Bird_*|Bug_*) size=256 ;; esac
    node Tools/ChatGPT/trim.mjs "$src" "$tmp/$name.png" --alpha 60 > /dev/null &&
      node Tools/ChatGPT/downscale.mjs "$tmp/$name.png" "$out/$name.png" "$size"
    rm -f "$tmp/$name.png"
  done
}

case "${1:-}" in
  props) props ;;
  crops) crops ;;
  dishes) dishes ;;
  dishes2) dishes2 ;;
  pantry) pantry ;;
  chef) chef ;;
  kitchen) chef; pantry; dishes2 ;;
  bugs) bugs ;;
  fish) fish ;;
  birds) birds ;;
  baits) baits ;;
  cats) cats ;;
  all) props; crops; dishes; bugs; fish; birds; cats; ship ;;
  ship) ship ;;
  "") echo "usage: batch_hobbies.sh props|crops|dishes|bugs|fish|birds|cats|all|ship|<Name>" ;;
  *) line=$(grep -E "^  gen $1 " "$0") && eval "${line#  }" || echo "no such master: $1" ;;
esac
echo "=== HOBBIES DONE ==="
