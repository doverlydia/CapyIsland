#!/usr/bin/env bash
# Banners for the Events screen: one cosy festival scene per contest, in the cutscene backdrops'
# storybook style. Masters go to ArtSource/Events/; `ship` downscales them into
# Assets/Resources/Events/, where EventsScreen loads them by name.
#
#   batch_events.sh            — every banner, then ship
#   batch_events.sh <Name>     — one banner again (Fish, Bugs, Birds), then ship
#   batch_events.sh ship       — only downscale
set -u
cd "$(dirname "$0")/../.." || exit 1
mkdir -p ArtSource/Events

STYLE="Match the attached image's rendering style, outline weight, shading and palette EXACTLY (clean storybook vector fills, soft airbrushed gradients, warm dark-brown outlines, muted warm palette)."
TAIL="Wide landscape banner composition, cheerful festival mood, colourful triangle bunting strung across the top. Cute chunky capybaras with simple dot eyes, calm and friendly. NO text, NO letters, NO numbers anywhere."
REF="ArtSource/Cutscenes/Cutscene_Wedding.png"

banner () {
  local name="$1"; shift
  local prompt="$1"; shift
  local out="ArtSource/Events/Banner_$name.png"
  echo "=== $out ==="
  node Tools/ChatGPT/chatgpt.mjs image "$STYLE Draw instead: $prompt $TAIL" --out "$out" --size 1536x1024 --quality high --ref "$REF" || echo "FAILED: $out"
}

one () {
  case "$1" in
    Fish) banner Fish "a little fishing contest on a sunny island pier: three capybaras sitting on a wooden dock with fishing rods, one proudly holding up a big fish, buckets of fish beside them, gentle blue sea with a few fish shadows, a small trophy on a barrel." ;;
    Bugs) banner Bugs "a little bug-catching contest in a flowery island meadow: three capybaras with butterfly nets chasing colourful butterflies and a dragonfly, one holding a glass jar with a ladybug inside, tall flowers and a small trophy on a tree stump." ;;
    Birds) banner Birds "a little birdwatching contest at the edge of an island wood: three capybaras with binoculars looking up at cheerful songbirds on branches, one writing in a little notebook, a heron by a pond, a small trophy on a rock." ;;
  esac
}

ship () {
  local out="Assets/Resources/Events"
  mkdir -p "$out"
  for src in ArtSource/Events/Banner_*.png; do
    [ -f "$src" ] || continue
    node Tools/ChatGPT/downscale.mjs "$src" "$out/$(basename "$src")" 900
  done
}

case "${1:-}" in
  ship) ;;
  "") for n in Fish Bugs Birds; do one "$n"; done ;;
  *) one "$1" ;;
esac
ship
echo "=== EVENTS DONE ==="
