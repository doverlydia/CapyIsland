#!/usr/bin/env bash
# The Hobbies carousel (UI/Hobbies/HobbyScreens): one little picture per hobby of the capybara
# doing it. Anchored to the plain capybara so it stays on-model. Masters go to ArtSource/Hobbies/;
# shipped to Assets/Resources/Hobbies/Vignette_<Hobby>.png. Skips what is already made.
set -u
cd "$(dirname "$0")/../.." || exit 1
mkdir -p ArtSource/Hobbies

REF="ArtSource/Capy_Idle.png"
MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY, and draw the SAME capybara character (same face, proportions and warm tan-brown fur). It must look like it came from the same artist and the same game."
TAIL="A small cosy vignette: the capybara with just the few props the activity needs, seen from a 3/4 view, centred with a small margin, on a fully transparent background, no backdrop, no sky, no ground plane, no frame, no border, no shadow, no text. The capybara wears no clothes or accessories unless the activity needs them."

gen () {
  local name="$1"; shift
  local out="ArtSource/Hobbies/Vignette_$name.png"
  if [ -f "$out" ]; then echo "skip $out"; return; fi
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$MATCH Draw instead: $1 $TAIL" --out "$out" --transparent --quality high --ref "$REF"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
  else
    echo "FAILED: $out"
  fi
}

gen Fishing   "the capybara sitting happily on the end of a little wooden jetty, holding a bamboo fishing rod, the line dropping to a red-and-white bobber on a small patch of blue water with a fish jumping beside it."
gen Birding   "the capybara peeking through a pair of binoculars, crouched beside a little leafy bush, while a small bluebird perches on a twig nearby."
gen Bugs      "the capybara mid-hop, swinging a little butterfly net at a bright orange butterfly fluttering just ahead, a few flowers at its feet."
gen Gardening "the capybara kneeling by a small square of dark soil with sprouting carrots and a pumpkin, holding a little watering can that sprinkles water."
gen Cooking   "the capybara stirring a round black pot of soup on a small stone stove with a wooden spoon, a puff of steam rising, a carrot and a fish beside it."
gen Cats      "the capybara sitting contentedly while a small ginger cat curls up on its back and a grey kitten nuzzles its side."
gen Writing   "the capybara sitting on the sand writing on a little scroll of paper with a quill, a glass message bottle with a cork lying beside it."
gen Sewing    "the capybara sitting at a small wooden sewing machine, stitching a cosy knitted beanie with a pompom, a pincushion and a spool of thread beside it."

for f in ArtSource/Hobbies/Vignette_*.png; do
  [ -f "$f" ] && node Tools/ChatGPT/downscale.mjs "$f" "Assets/Resources/Hobbies/$(basename "$f")" 512 > /dev/null && echo "shipped $(basename "$f")"
done
echo "=== HOBBY CARDS DONE ==="
