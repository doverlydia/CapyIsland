#!/usr/bin/env bash
# Art for the island's wildlife (Game/Wildlife): a two-frame flap for every bird and every flying bug,
# drawn facing the same way as the creature's standing picture (the game mirrors both together),
# and the seagull, new to the bird journal.
#
# Each flap is drawn as ONE wide sheet — the same creature twice, side by side, wings up and wings
# down — so both frames come out the same size and style; splitpair.mjs cuts them apart onto one
# shared box, and alignflap.mjs slides them so the bodies sit on each other (else the creature
# shakes). Sheets go to ArtSource/Wild_<Name>_Sheet.png, frames to ArtSource/Wild_<Name>_Fly1/2.png, and `ship` downscales the frames (no trim: that would undo the
# shared box) into Assets/Resources/Hobbies/<Name>_Fly1/2.png, where the game loads them.
#
#   batch_wildlife.sh gull | birds | bugs   — one group
#   batch_wildlife.sh all                   — everything, then ship
#   batch_wildlife.sh ship                  — only downscale the frames into the game
#   batch_wildlife.sh prompts               — print every sheet's prompt, file to attach and file to save (to draw them by hand)
#   batch_wildlife.sh cut                   — split sheets saved by hand into frames, then ship
#   batch_wildlife.sh <Name>                — one sheet again, e.g. Bird_robin
set -u
# gpt-image-1 is gone from the edits endpoint (Oct 2026); 1.5 matches the existing art closely.
export OPENAI_IMAGE_MODEL="${OPENAI_IMAGE_MODEL:-gpt-image-1.5}"
cd "$(dirname "$0")/../.." || exit 1

MATCH="Match the attached reference image EXACTLY: the same creature, the same colours and markings, the same rendering style, outline weight, shading, palette and level of detail. It must look like it came from the same artist and the same game."
SHEET="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline, exactly like the reference. Chunky rounded generous shapes that read clearly at 64px. Muted natural palette. The two drawings are the same size, side by side, clearly apart with empty space between them, each centred in its half. On a fully transparent background, no backdrop, no scenery, no sky, no ground plane, no shadow, no cast shadow, no glow beneath the subject, no motion lines, no border, no text."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline, exactly like the reference. Chunky rounded generous shapes that read clearly at 64px. Muted natural palette. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no glow beneath the subject, no pale oval or ellipse or smudge under the subject, nothing at all beneath it, no border, no text."
CRITTER="ArtSource/Hobby_Bug_Bee.png"
BIRDVIEW="standing, seen from the side facing left, cute plump storybook bird with a simple round dot eye"
FLAP="a two-frame flying animation sheet: the SAME creature drawn twice side by side at the SAME size, both flying the way the reference faces (head and beak toward the same side as in the reference), seen from the side, legs tucked up under the body, body level. Only the wings differ between the two drawings"

# How alignflap.mjs should line up a flap: bugs drawn from above are lined up on their middle line.
align () {
  case "$1" in
    Wild_Bug_Butterfly|Wild_Bug_BlueButterfly|Wild_Bug_RainbowMoth|Wild_Bug_Dragonfly) node Tools/ChatGPT/alignflap.mjs "ArtSource/$1_Fly1.png" "ArtSource/$1_Fly2.png" --symmetric ;;
    *) node Tools/ChatGPT/alignflap.mjs "ArtSource/$1_Fly1.png" "ArtSource/$1_Fly2.png" ;;
  esac
}

# A flap sheet for a creature whose standing picture is ArtSource/Hobby_<name>.png.
sheet () {
  local name="$1"; shift
  local prompt="$1"; shift
  local out="ArtSource/Wild_${name}_Sheet.png"
  if [ -f "$out" ] && [ -z "${FORCE:-}" ]; then [ -z "${PROMPTS:-}" ] && echo "have $out (FORCE=1 to redo)"; return; fi
  if [ -n "${PROMPTS:-}" ]; then
    # To draw it by hand in a ChatGPT project whose sources hold the standing pictures (and the
    # finished sparrow sheet, as a layout example): the prompt names its reference files.
    local ref="Hobby_$name.png"
    local manual="Make one image, landscape 1536x1024, with a transparent background. Use the project source file $ref as the reference: match it EXACTLY, the same creature, the same colours and markings, the same rendering style, outline weight, shading, palette and level of detail. It must look like it came from the same artist and the same game. Lay the image out like the example source file Wild_Bird_sparrow_Sheet.png (two drawings side by side), but draw the creature from $ref. Draw: $prompt $SHEET"
    printf '\n%s\n%s\n\nReference: %s   |   Save as: %s\n\n%s\n' "------------------------------------------------------------" "$name" "$ref" "Wild_${name}_Sheet.png" "$manual"
    return
  fi
  echo "=== $out ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$MATCH Draw instead: $prompt $SHEET" \
      --out "$out" --transparent --quality high --size 1536x1024 --ref "ArtSource/Hobby_$name.png"; then
    node Tools/ChatGPT/cleanalpha.mjs "$out"
    node Tools/ChatGPT/splitpair.mjs "$out" "ArtSource/Wild_${name}_Fly1.png" "ArtSource/Wild_${name}_Fly2.png"
    align "Wild_${name}"
  else
    echo "FAILED: $out"
  fi
}

bird () {
  sheet "Bird_$1" "$FLAP. LEFT drawing: wings raised high above its back, feathers spread. RIGHT drawing: wings swept down below its body. $2"
}

gull () {
  echo "=== ArtSource/Hobby_Bird_gull.png ==="
  node Tools/ChatGPT/chatgpt.mjs image "Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. Draw instead: a single cute plump seagull with a white head and body, soft grey wings with black wingtips and little white spots, a yellow beak with a small red dot near the tip, and pink-orange webbed feet, $BIRDVIEW. $TAIL" \
    --out ArtSource/Hobby_Bird_gull.png --transparent --quality high --ref "$CRITTER" &&
    node Tools/ChatGPT/cleanalpha.mjs ArtSource/Hobby_Bird_gull.png
  bird gull "Long narrow wings like a gliding gull."
}

birds () {
  bird sparrow "A small round sparrow with short rounded wings."
  bird robin "A small round robin with short rounded wings."
  bird bluetit "A small round blue tit with short rounded wings."
  bird finch "A small goldfinch, the yellow wing bar showing on the open wings."
  bird dove "A plump turtle dove with broad rounded wings."
  bird heron "A grey heron with its long neck folded back in flight and its long legs trailing straight out behind, very broad wings."
  bird kingfisher "A kingfisher with short fast wings and its long beak pointing forward."
  bird parrot "A green parrot with its red and blue wing feathers spread, long tail streaming behind."
  bird toucan "A toucan with its huge beak pointing forward and short broad wings."
  bird egret "A snowy egret with its neck folded back and black legs trailing straight out behind, broad white wings."
  bird hummingbird "A tiny hummingbird hovering, wings a soft fan above (LEFT) and below (RIGHT) its body."
  bird crane "A red-crowned crane with its long neck stretched forward and long legs trailing straight out behind, very broad white wings with black trailing feathers."
  bird owl "A round tawny owl with broad soft rounded wings, its big round face toward the viewer."
  bird pheasant "A golden pheasant with short rounded wings and its long striped tail streaming behind."
}

bugs () {
  sheet Bug_Butterfly "a two-frame flying animation sheet of this butterfly, seen from above: the SAME butterfly drawn twice side by side. LEFT drawing: wings fully open and flat. RIGHT drawing: wings lifted and folded up together so they look narrow."
  sheet Bug_BlueButterfly "a two-frame flying animation sheet of this butterfly, seen from above: the SAME butterfly drawn twice side by side. LEFT drawing: wings fully open and flat. RIGHT drawing: wings lifted and folded up together so they look narrow."
  sheet Bug_RainbowMoth "a two-frame flying animation sheet of this moth, seen from above: the SAME moth drawn twice side by side. LEFT drawing: wings fully open and flat. RIGHT drawing: wings lifted and folded up together so they look narrow."
  sheet Bug_Bee "$FLAP. A chubby bumblebee. LEFT drawing: its small translucent wings raised up. RIGHT drawing: its wings pushed down."
  sheet Bug_Firefly "$FLAP. A little firefly with its softly glowing yellow tail. LEFT drawing: wings raised up. RIGHT drawing: wings pushed down."
  sheet Bug_Dragonfly "a two-frame flying animation sheet of this dragonfly, seen from above: the SAME dragonfly drawn twice side by side, long thin body pointing the way the reference points. LEFT drawing: its four clear wings swept forward. RIGHT drawing: its four clear wings swept back."
}

ship () {
  local out="Assets/Resources/Hobbies"
  mkdir -p "$out"
  [ -f ArtSource/Hobby_Bird_gull.png ] && {
    local tmp; tmp=$(mktemp -d)
    node Tools/ChatGPT/trim.mjs ArtSource/Hobby_Bird_gull.png "$tmp/g.png" --alpha 60 > /dev/null &&
      node Tools/ChatGPT/downscale.mjs "$tmp/g.png" "$out/Bird_gull.png" 256
    rm -rf "$tmp"
  }
  for src in ArtSource/Wild_*_Fly[12].png; do
    [ -f "$src" ] || continue
    local name; name=$(basename "$src" .png); name=${name#Wild_}
    node Tools/ChatGPT/downscale.mjs "$src" "$out/$name.png" 256
  done
}

case "${1:-}" in
  gull) gull ;;
  birds) birds ;;
  bugs) bugs ;;
  all) gull; birds; bugs; ship ;;
  ship) ship ;;
  prompts) PROMPTS=1 birds; PROMPTS=1 bugs ;;
  cut) for s in ArtSource/Wild_*_Sheet.png; do n=$(basename "$s" _Sheet.png); [ -f "ArtSource/${n}_Fly1.png" ] && [ -z "${FORCE:-}" ] && continue; node Tools/ChatGPT/cleanalpha.mjs "$s"; node Tools/ChatGPT/splitpair.mjs "$s" "ArtSource/${n}_Fly1.png" "ArtSource/${n}_Fly2.png"; align "$n"; done; ship ;;
  "") echo "usage: batch_wildlife.sh gull|birds|bugs|all|ship|<Name>" ;;
  Bird_gull) gull ;;
  Bird_*) line=$(grep -E "^  bird ${1#Bird_} " "$0") && eval "${line#  }" || echo "no such bird: $1" ;;
  *) line=$(grep -E "^  sheet $1 " "$0") && eval "${line#  }" || echo "no such sheet: $1" ;;
esac
echo "=== WILDLIFE DONE ==="
