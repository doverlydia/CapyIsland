#!/usr/bin/env bash
# Downscales the batch_world.sh masters into shipping sprites under Assets/Art, at the size each
# one actually renders at. Masters stay in ArtSource/ (outside Assets, never shipped).
set -u
cd "$(dirname "$0")/../.." || exit 1
D=Tools/ChatGPT/downscale.mjs

ship () {  # ship <master name> <folder> <size>
  local src="ArtSource/$1.png"
  if [ ! -f "$src" ]; then echo "missing $src"; return; fi
  node $D "$src" "Assets/Art/$2/$1.png" "$3" > /dev/null && echo "shipped $2/$1 @$3"
}

for n in Tree_Maple Tree_Ironwood Garden_Tulip Garden_Rose Hill_Black Hill_Gold; do ship $n Terrain 256; done
for n in Flower_Tulip Flower_Rose; do ship $n Terrain 128; done
for n in Store_General Store_Bronze Store_Silver Store_Steel Store_Gold Store_Gem Store_Bank Store_Fancy \
         Build_LegendMill Build_LegendMine Build_FoxHouse Build_MonsterHouse Build_Church Build_Tower; do ship $n Buildings 256; done
for n in Animal_Fox Animal_Panda Animal_Monster Animal_Deer Capy_Pirate Capy_Coriander Capy_Peach Capy_Pete Capy_Priest; do ship $n Capybaras 256; done
for n in Item_Bomb Item_DeerBomb Item_Dress Item_Pearls Item_Tiara Prop_Mote Deco_TinyBush Deco_LittleGreeny; do ship $n Props 128; done
for n in Deco_WhiteFlowerTree Deco_PinkFlowerTree Deco_HolyStatue Deco_GemStatue Deco_BushyTree Deco_FallTree Deco_Fountain Deco_MonsterDen; do ship $n Props 256; done
for n in Deco_GrassTuft Deco_Wildflowers Deco_Pebbles; do ship $n Terrain 96; done
