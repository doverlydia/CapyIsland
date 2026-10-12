#!/usr/bin/env bash
# The sewing room (UI/Hobbies/HobbyScreens: SewingScreen): a tall painted backdrop with a floor,
# and a capybara mannequin the hat you are painting sits on. Masters in ArtSource/Hobbies/,
# shipped to Assets/Resources/Hobbies/. Skips what is already made.
set -u
cd "$(dirname "$0")/../.." || exit 1
mkdir -p ArtSource/Hobbies

SCENE_REF="ArtSource/Cutscenes/Cutscene_Wedding.png"
SCENE_STYLE="Match the attached image's rendering style, outline weight, shading and palette EXACTLY (clean storybook vector fills, soft airbrushed gradients, warm dark-brown outlines on built props, muted warm palette)."
PROP_REF="ArtSource/Capy_Idle.png"
MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."

if [ ! -f ArtSource/Hobbies/Sew_Room.png ]; then
  echo "=== Sew_Room ==="
  node Tools/ChatGPT/chatgpt.mjs image "$SCENE_STYLE Draw instead: a tall, cosy storybook backdrop of a little tailor's corner inside an island cabin, portrait composition, seen from slightly above like the rest of the game. The back wall and the floor meet in a straight horizontal line exactly halfway down the picture. Upper half: soft pink-and-cream striped wallpaper, wooden shelves at both sides stacked with folded bolts of colourful fabric and jars of buttons and spools of thread, a little cork pinboard with paper patterns and swatches, a string of tiny fabric bunting across the top, a warm window glow. Lower half: a warm wooden plank floor with a round braided rug in the middle, completely empty and clear. NO furniture in the middle, NO mannequin, NO people, NO characters, NO animals, NO text." \
    --out ArtSource/Hobbies/Sew_Room.png --size 1024x1536 --quality high --ref "$SCENE_REF" || echo "FAILED: Sew_Room"
fi

if [ ! -f ArtSource/Hobbies/Sew_Mannequin.png ]; then
  echo "=== Sew_Mannequin ==="
  if node Tools/ChatGPT/chatgpt.mjs image "$MATCH Draw instead: a tailor's dress-form mannequin shaped like the capybara's head and shoulders, seen straight from the front, on a slim turned wooden pole with a little three-footed wooden base. The mannequin head is smooth pale cream linen with simple stitched seams, round little ears, and NO face at all (no eyes, no nose, no mouth). Nothing on its head. Subject only, centred with a small margin, on a fully transparent background, no backdrop, no floor, no shadow, no text." \
      --out ArtSource/Hobbies/Sew_Mannequin.png --transparent --quality high --ref "$PROP_REF"; then
    node Tools/ChatGPT/cleanalpha.mjs ArtSource/Hobbies/Sew_Mannequin.png
  else
    echo "FAILED: Sew_Mannequin"
  fi
fi

[ -f ArtSource/Hobbies/Sew_Room.png ] && node Tools/ChatGPT/downscale.mjs ArtSource/Hobbies/Sew_Room.png Assets/Resources/Hobbies/Sew_Room.png 1024 > /dev/null && echo "shipped Sew_Room"
[ -f ArtSource/Hobbies/Sew_Mannequin.png ] && node Tools/ChatGPT/downscale.mjs ArtSource/Hobbies/Sew_Mannequin.png Assets/Resources/Hobbies/Sew_Mannequin.png 512 > /dev/null && echo "shipped Sew_Mannequin"
echo "=== SEW ROOM DONE ==="
