#!/usr/bin/env bash
# Miso in the dress you give her: a whole new sprite rather than a dress pinned on top, in the
# same pose and framing as Capy_Coriander so her tiara and pearls still sit in the same places.
# She stays a four-legged capybara: the dress is fitted like a pet's coat, not a flared skirt.
#
#   batch_miso.sh            — generate, then ship
#   batch_miso.sh try <n>    — generate a candidate to ArtSource/Miso_Dress_try<n>.png (no ship)
#   batch_miso.sh pearls     — the dress plus the pearl necklace, then ship
#   batch_miso.sh tiara      — that plus the tiara, then ship
#   batch_miso.sh ship       — ship only
set -u
cd "$(dirname "$0")/../.." || exit 1

PROMPT="The FIRST attached image is a character: a capybara on four legs. Redraw that SAME character with EXACTLY the same body: copy her outline, body shape, pose, size, position, framing and camera angle exactly, and keep EXACTLY her four short capybara legs and feet as they are in the FIRST image, standing on all fours. Same face, same long eyelashes, same pink flower tucked behind her ear, same rendering style, outline weight, shading and palette. The only change: she wears the dress from the SECOND attached image as a fitted PET OUTFIT, the way a little dog wears a dress: white fabric hugging her chest, shoulders, back and sides and following the outline of her body exactly, with little puffed sleeve frills at the tops of her two front legs, the big pink bow sash at her chest just under her chin, and a short white scalloped frill trimming the lower edge of the fabric along her belly line. The fabric ends at her belly line: NO long skirt, NO skirt flaring out, NO hem hanging below her belly, nothing covering her legs. She is an animal, not a person: NOT standing upright, NOT humanoid, NO human legs. The dress replaces her pink neck scarf. No mannequin, no stand. Clean vector style with soft airbrushed gradients and a dark warm brown medium-weight outline, exactly like the FIRST image. Subject only, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no glow, no pale oval or ellipse under her, no border, no text."

gen () {
  echo "=== $1 ==="
  node Tools/ChatGPT/chatgpt.mjs image "$PROMPT" --out "$1" --transparent --quality high \
    --ref ArtSource/Capy_Coriander.png,ArtSource/Item_Dress.png && node Tools/ChatGPT/cleanalpha.mjs "$1"
}

# Her later gifts are drawn on too (pinned-on pieces never sat right), each from the stage before
# so she stays identical: the pearls only ever come after the dress, and the tiara after the pearls.
SAME="The FIRST attached image is a character. Redraw it IDENTICALLY: exactly the same character, body, four legs, dress, bow, flower, pose, size, position, framing, camera angle, rendering style, outline weight, shading and palette, changing nothing at all, except for ONE addition:"
TAIL2="Clean vector style with soft airbrushed gradients and a dark warm brown medium-weight outline, exactly like the FIRST image. Subject only, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no glow, no pale oval under her, no border, no text."

add () {  # add <out> <from> <ref> <what>
  echo "=== $1 ==="
  node Tools/ChatGPT/chatgpt.mjs image "$SAME $4 $TAIL2" --out "$1" --transparent --quality high --ref "$2,$3" && node Tools/ChatGPT/cleanalpha.mjs "$1"
}

pearls () { add "${1:-ArtSource/Hobby_Miso_DressPearls.png}" ArtSource/Hobby_Miso_Dress.png ArtSource/Hobby_Miso_Pearls.png "she now also wears the pearl necklace from the SECOND attached image, resting snugly around her neck just under her chin and just above the pink bow, following the curve of her neck, a little smaller than in the SECOND image so it fits her."; }
tiara () { add "${1:-ArtSource/Hobby_Miso_DressTiara.png}" ArtSource/Hobby_Miso_DressPearls.png ArtSource/Hobby_Miso_Tiara.png "she now also wears the small golden tiara from the SECOND attached image, sitting on the top of her head between her two ears, slightly tilted with the curve of her head, behind her flower, small and dainty."; }

ship () {
  for n in Miso_Dress Miso_DressPearls Miso_DressTiara; do
    [ -f "ArtSource/Hobby_$n.png" ] || continue
    node Tools/ChatGPT/downscale.mjs "ArtSource/Hobby_$n.png" "Assets/Resources/Hobbies/$n.png" 256 > /dev/null && echo "shipped Hobbies/$n @256"
  done
}

case "${1:-}" in
  try) gen "ArtSource/Miso_Dress_try${2:-1}.png" ;;   # not Hobby_*: batch_hobbies.sh ship would ship it
  pearls) pearls; ship ;;
  tiara) tiara; ship ;;
  ship) ship ;;
  *) gen ArtSource/Hobby_Miso_Dress.png && ship ;;
esac
