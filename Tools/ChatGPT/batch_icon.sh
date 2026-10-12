#!/usr/bin/env bash
# The app icon: a full square icon (iOS, WebGL, older Android) and Android's adaptive pair — a
# transparent foreground and a background. Masters go to ArtSource/Icon/; shipped copies to
# Assets/Art/Icon/, which the Capy Island > App Icon menu item wires into Player Settings.
#
#   batch_icon.sh          — all three
#   batch_icon.sh <Name>   — one again (Full, Foreground, Background)
set -u
cd "$(dirname "$0")/../.." || exit 1
mkdir -p ArtSource/Icon Assets/Art/Icon

STYLE="Match the attached character's rendering style, outline weight, shading and palette EXACTLY (clean storybook vector fills, soft airbrushed gradients, warm dark-brown outline)."
HERO="the SAME cute capybara character as the reference, wearing its little golden crown, relaxing up to its chest in a round steaming hot spring, eyes happily closed, with one small yellow yuzu fruit floating beside it and soft curls of steam rising"
NOTEXT="NO text, NO letters, NO numbers, NO border, NO frame."
REF="ArtSource/Capy_King.png"

gen () {
  local name="$1"; shift
  local prompt="$1"; shift
  local flags="$1"; shift
  local out="ArtSource/Icon/Icon_$name.png"
  echo "=== $out ==="
  node Tools/ChatGPT/chatgpt.mjs image "$prompt" --out "$out" --size 1024x1024 --quality high --ref "$REF" $flags || echo "FAILED: $out"
}

one () {
  case "$1" in
    Full) gen Full "$STYLE Draw instead: a mobile app icon: $HERO. Big, bold and centred, filling most of the square, readable at small sizes. The whole square is a warm painted background: soft teal hot-spring water and a pale peach sky. $NOTEXT" "" ;;
    Foreground) gen Foreground "$STYLE Draw instead: $HERO. Just the capybara, the round hot-spring pool rim, the yuzu and the steam, centred and kept well inside the middle 60% of the square with generous empty margin all round, on a fully transparent background, no backdrop. $NOTEXT" "--transparent" ;;
    Background) gen Background "$STYLE Draw instead: a soft simple background only: gentle teal hot-spring water at the bottom fading up into a pale warm peach sky, a few soft round steam clouds. No characters, no objects, no detail in the centre. $NOTEXT" "" ;;
  esac
}

if [ -n "${1:-}" ]; then one "$1"; else for n in Full Foreground Background; do one "$n"; done; fi
[ -f ArtSource/Icon/Icon_Foreground.png ] && node Tools/ChatGPT/cleanalpha.mjs ArtSource/Icon/Icon_Foreground.png
for f in ArtSource/Icon/Icon_*.png; do [ -f "$f" ] && node Tools/ChatGPT/downscale.mjs "$f" "Assets/Art/Icon/$(basename "$f")" 1024; done
echo "=== ICON DONE ==="
