#!/usr/bin/env bash
# Art for the presents cats bring you: junk (Hobby_Gift_*) and two cat-themed outfit pieces
# (Wear_*). Masters go to ArtSource/; then the hobby and wardrobe batches' `ship` steps put the
# shipping copies in Assets/Resources/.
#
#   batch_catgifts.sh          — everything, then ship
#   batch_catgifts.sh <Name>   — one master again, e.g. Gift_Roach or Hat_Sock
set -u
cd "$(dirname "$0")/../.." || exit 1

MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline, exactly like the reference. Chunky rounded generous shapes that read clearly at 64px. Muted natural palette. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no ground shadow, no glow beneath the subject, no pale oval or ellipse or smudge under the subject, nothing at all beneath it, no border, no text."
PROP="ArtSource/Hill_Gray.png"
BASE="ArtSource/Capy_Player.png"
VIEW="seen from a 3/4 top-down oblique angle about 50 degrees above"
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

one () {
  case "$1" in
    Gift_Roach) gen ArtSource/Hobby_Gift_Roach.png "$PROP" "a single comical cartoon cockroach, $VIEW, glossy dark reddish-brown shell, six little legs, two long curly antennae and a cheeky expression with big round eyes. Cute and funny rather than gross." ;;
    Gift_Boot) gen ArtSource/Hobby_Gift_Boot.png "$PROP" "a single soggy old brown leather boot, $VIEW, scuffed and floppy, a loose untied lace, a little strand of green seaweed hanging off it and a couple of water drops. Comical junk." ;;
    Gift_Fishbone) gen ArtSource/Hobby_Gift_Fishbone.png "$PROP" "a cartoon fish skeleton, $VIEW: a fish head with a big round eye, a clean white spine with ribs, and a tail fin, like a cat's leftovers. Comical." ;;
    # Junk reeled in while fishing (Fishing.Junk); the boot above turns up there too.
    Gift_Can) gen ArtSource/Hobby_Gift_Can.png "$PROP" "a single rusty old tin can, $VIEW, dented, its jagged lid bent open, a faded peeling label and a trickle of water and a strand of seaweed spilling out. Comical junk." ;;
    Gift_Seaweed) gen ArtSource/Hobby_Gift_Seaweed.png "$PROP" "a single dripping clump of tangled green seaweed, $VIEW, glossy wet ribbons of kelp in a soggy heap with a couple of water drops and a tiny shell caught in it. Comical junk." ;;
    Hat_Sock) gen ArtSource/Wear_Hat_Sock.png "$BASE" "$ITEM A long fluffy striped knitted sock worn as a floppy hat, red and cream stripes with a mustard-yellow heel and toe, the toe flopping over to one side like a nightcap. Silly and cosy." ;;
    Neck_Bell) gen ArtSource/Wear_Neck_Bell.png "$BASE" "$ITEM A soft red cat collar shaped as a loop to go around a capybara's neck, seen from the front, with a round shiny golden jingle bell hanging at the front centre." ;;
  esac
}

if [ -n "${1:-}" ]; then one "$1"; exit 0; fi
for n in Gift_Roach Gift_Boot Gift_Fishbone Hat_Sock Neck_Bell; do one "$n"; done
bash Tools/ChatGPT/batch_hobbies.sh ship
bash Tools/ChatGPT/batch_wardrobe.sh ship
echo "done"
