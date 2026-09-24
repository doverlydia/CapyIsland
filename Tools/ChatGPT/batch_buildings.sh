#!/usr/bin/env bash
# The two workplace buildings the tutorial loop needs: a sawmill (wood) and a mine (gold),
# plus a sleeping capybara for the "drag the sleeping worker onto the mill" opening beat.
set -u
cd "$(dirname "$0")/../.." || exit 1

REF="ArtSource/Hill_Gray.png"
MATCH="Match the attached reference image's rendering style, outline weight, shading, palette and level of detail EXACTLY. It must look like it came from the same artist and the same game."
TAIL="Rendered in clean vector style with soft airbrushed gradients, with a dark warm brown medium-weight outline around the whole object, exactly like the reference. Chunky rounded generous silhouette that reads clearly at 64px. Muted natural palette. Subject only, centered with a small margin, on a fully transparent background, no backdrop, no scenery, no ground plane, no shadow, no cast shadow, no ground shadow, no glow beneath the subject, no border, no text."

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

gen "ArtSource/Build_Sawmill.png" "$REF" \
  "a small cozy sawmill building seen from a 3/4 top-down oblique angle about 50 degrees above: a compact wooden cabin with a warm brown plank body, a pitched terracotta-red roof, a round waterwheel on one side, and a couple of cut logs stacked by the door. Storybook game building, not realistic."

gen "ArtSource/Build_Mine.png" "$REF" \
  "a small mine entrance building seen from a 3/4 top-down oblique angle about 50 degrees above: a rounded gray rock mound with a dark arched timber-framed tunnel mouth in the front, two crossed wooden support beams, and a small wooden minecart with a gold nugget in it beside the entrance. Storybook game building, not realistic."

gen "ArtSource/Capy_Sleep.png" "ArtSource/Capy_Idle.png" \
  "the SAME capybara character but asleep, curled up lying down on its side with eyes closed as two simple curved lines, seen from the same 3/4 top-down oblique angle. Same warm tan-brown fur, same oversized rounded head, same proportions and same outline. Peaceful and cosy. A completely plain bare capybara with NO clothing, NO accessories and NO props. No letter Z, no zzz, no speech bubble, no thought bubble."

echo "=== BUILDINGS COMPLETE ==="
