# Overnight build notes

What was built on the `overnight-build` branch, how to try it, and what is deliberately different
from the original. The rules come from `docs/BUNNI_GROUND_TRUTH.md` and `docs/PROGRESSION.md`,
which in turn cite the decompiled source and the shipped XML.

## Try it

1. Open `Assets/Scenes/SampleScene.unity` and press Play. A new game starts with the tutorial.
2. To rebuild the scene from code: **CapyIsland → Build → Island** (or **Island (repaint land)**
   after changing island shapes in `Assets/Editor/IslandPainter.cs`).
3. The game saves itself every 30 s once the tutorial is over. **CapyIsland → Delete Save** starts
   fresh.
4. Builds: **CapyIsland → Build → WebGL** (into `Builds/WebGL`) and **Android APK**
   (`Builds/Android/CapyIsland.apk`). Results are logged in `Builds/last-build.txt`. `Builds/` is
   git-ignored.

## What is in

- **Resource loop** exactly as the original: parent groves scatter and seed children, workers take
  children first, 0.1 × speed / 3 per hit on the original swing frames, hunger, sleeping, gardens.
- **Fruit trees** (apple, pear, legendary plum): grow, fruit up to five, **tap to shake**. Fruit
  arcs down, splashes and ripens from green; sometimes a treasure chest, once a minute a free
  sapling, 1% a random item. Hungry workers and deer shake one fruit at a time; sawmills out of
  forest chop fruit trees for 500 wood and release a monster, deer or bomb.
- **Wind**: gusts roll across the island, trees and flowers sway and shed leaves.
- **The archipelago**: seven islands on the original 4500-unit layout. Land bridges rise out of
  the sea when a bridge ghost is paid (Ginger Cove, Verdant Bay, Pirate Cove, Wedding Isle) or a
  quest opens one (the first bridge, Monster Isle). Each crossing brings the island's first
  residents, exactly as listed in the original.
- **Shops**: every store from `game.xml` lays its stock out in a half circle in front of it; tap a
  ware to buy. Prices, ownership caps, gates (Steel/Gold Store, Church) and the dynamically priced
  items follow the original. The Bank trades gems for wood or gold.
- **Quests**: Coriander's ten missions, Pirate Peach's six and the three quest ghosts, all from
  `save_data.xml`, with their set-ups and rewards. Motes appear as hearts that follow you; tap one
  to send it to the Tower of Love. First-discovery cards pause the game like the original.
- **Animals**: capybara (bunni), fox, panda, pirate and tame monster workers with the original
  speeds; wild deer (eat flowers anywhere) and wild monsters (wreck trees and rocks; 20 gems to
  tame). **Bombs** bubble everything nearby; **Deer Bombs** only clear deer and wild monsters.
- **Pirate Pete** buys spare items and workers; the **Priest** marries you once Coriander says yes.
- **Mines and hills** (gray, black, gold) for gold; Legendary mills and mines pay double.

## Different from the original, on purpose

- No real-money gems: the Bank's "Buy Gems" counter is left out, as in the Kongregate build.
- The wedding and Peach's dance were external Flash movies; here they are a celebration of hearts
  and sparkles.
- Coriander's outfits are shown as the gift worn on her rather than whole new costumes.
- A heart not yet tapped is not saved (the mote itself is), nor is fruit lying on the ground.
- Items bought from shops land in your trail like the original, but the card popup and the hint
  line use this game's UI rather than the original's Flash menus.

## Art

All new art came from the ChatGPT image bridge following the art-director skill, referenced against
already-approved assets. Masters are in `ArtSource/`, shipping sprites in `Assets/Art/`. The batch
scripts are `Tools/ChatGPT/batch_fruit.sh` and `batch_world.sh`, and `ship_world.sh` downscales.
Islands are painted procedurally by `Assets/Editor/IslandPainter.cs`; particle sprites by
`Tools/fx/particles.mjs`.

## Data generators

`Tools/data/gen_store_data.mjs` and `gen_quest_data.mjs` turn the shipped XML into
`StoreData.g.cs` and `QuestData.g.cs`. Re-run them after editing the XML; the bunni is renamed to
the capybara in every line the player reads.
