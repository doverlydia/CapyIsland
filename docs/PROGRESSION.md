# Bunni: progression reference (stores, quests, islands, hazards, tutorial)

This is the spec for everything in *Bunni: How We First Met* that sits **above** the resource loop:
stores, the quest and mote system, islands and bridges, the non-bunni animals, hills and mines, and
the tutorial. The resource loop itself (growth, seeding, harvesting, hunger, the worker FSM) is in
`BUNNI_GROUND_TRUTH.md`. This file follows the same rules: everything here was read from the
decompiled AS3 or the shipped XML, and anything inferred or missing is marked as such.

**Source keys used below**

| Key | Meaning |
|---|---|
| `scripts/…` | `C:\Users\dover\Documents\bunniWiki\decompiled\LandOfBunnies\scripts\…` (decompiled AS3, third-party, not in repo) |
| `game.xml`, `store_items.xml`, `save_data.xml` | `docs/reference/*.xml` (shipped config; `save_data.xml` is the **new-game template** the client loads, `Save/Save.as › XMLloaded`) |
| `game_no_mt.xml` | The Kongregate build's config (`Bunni.as › LoadXML` loads it when `BunniGame.mGifter == "kong"`). Identical to `game.xml` except the Bank has no `Buy Gems`. |
| `A › B` | function `B` in file `A` |

Units: world units on a 4500×4500 square (`game.xml <worldsize>`), drawn 2:1 (screen y = world y / 2).
The SWF runs at 30 fps; "frames" are 1/30 s. Wood and "rock" are the two soft currencies; the HUD
and all player-facing text call rock **gold**. Gems are the hard currency.

---

## 0. The progression graph at a glance

```
NEW GAME: empty map, avatar at (500,500) on island 0, 1000 wood / 1500 gold / 0 gems
 └─ Tutorial (Tasks.as): free Pine Forest → Sawmill → Bunni House → buy Bunni → hunger → Posie Garden
     └─ place Posie Garden → first mote + Tower of Love spawned on island 1
         └─ click mote → first_bridge drawn (0↔1), "Ginger Cove" bridge ghost + Coriander bubble on island 1
             └─ open the Tower → tutorial done (Save enabled)

CORIANDER chain (each step gated on the previous one being finished):
 16 "find the Bronze store"  → free General Store bubble        → done when Bronze Store is PLACED
  1 "talk to a pirate"       → Pirate Pete bubble on Ginger Cove  → done when Pete's dialog is closed   → Small Ironwood Forest
  2 "Silver Store"           →                                   → done when Silver Store PLACED       → 1000 gold
  8 "the dress"              →                                   → done when Wedding Dress BOUGHT      (unlocks Steel Store)
  5 "the deer"               → 6 deer spawned (island 0 + 3)      → done when 0 deer on the map         → 10 gems
  4 "Steel Store"            →                                   → done when Steel Store PLACED        → 1000 wood
  9 "pearls"                 →                                   → done when String of Pearls BOUGHT   (unlocks Gold Store)
  6 "Gold Store"             →                                   → done when Gold Store PLACED         → 1500 wood
 10 "tiara"                  →                                   → done when Tiara BOUGHT              (unlocks Church)
  7 "I love you"             → fires "Madly in Love" immediately →                                     → 15 gems
     └─ buy Church (Gold Store) → Church + Priest appear on Wedding Isle → pay Priest 50 wood + 50 gold → wedding

BRIDGE GHOSTS (paid, one-shot): Ginger Cove (isl 1) → Verdant Bay + Pirate Cove (isl 2) → Wedding Isle (isl 3)
PEACH chain (appears when Verdant Bay is bought): roses → Legendary Gem Store → 5 plums → panda at a mill
                                                  → tame 5 monsters (opens Monster Isle) → 500-gem dance
GHOSTS (spawn on motes): Fredrickson ×2 (foxes), Chappy (Holy Statue)
```

---

## A. Stores and shops

### A1. A store is a building that lays its wares out on the ground

There is **no store window listing the stock**. A store building, when constructed, spawns one
*store display item* (a `StoreItem` with `mFree = false`) per entry in its `game.xml <stores>` list,
arranged on the map around the building.

`scripts/MapItems/Building/StoreBuildingItem.as › StoreLayout`:

- Start vector `(100, 0)` (world units), i.e. 100 units east of the building's centre.
- For each item, place a display at `building + vector`, then rotate the vector by
  `180 / (itemCount − 1)` degrees (Flash `Matrix.rotate`, clockwise on screen, so +y = south/down).
- Result: the items fan in a **half circle of radius 100 on the south (front) side**, from due
  east through due south to due west. With 6 items the step is 36°.

Each store class just names its list (`scripts/MapItems/Building/Stores/*.as`, constructor):
`GeneralStore → general_store`, `BronzeStore → bronze_store`, `SilverStore → silver_store`,
`SteelStore → steel_store`, `GoldStore → gold_store`, `GemStore → gem_store`,
`BankStore → bank` (also calls `SuperReward.initRewards()`), `FancyStore → fancy_store`.

When a store building is destroyed by a Bomb, its displays are removed too
(`StoreBuildingItem › DestroyItems`, called from `Terrain/BombStates/BombFuse.as › Execute`).

Animal houses work the same way with a single display: `Building/HouseItem.as › initStateMachine`
spawns one display 100 units south (`y + 100`) selling that house's animal
(Bunni House→"Bunni", Pirate House→"Pirate", Panda House→"Panda", Fox House→"Fox",
Monster House→"Monster", Deer House→"Deer"; the type number is set in each
`Building/Houses/*HouseItem.as`).

### A2. How a display item looks and reacts

`scripts/MapItems/StoreItems/StoreItem.as › draw` sets the display's alpha every frame:

| Condition | Alpha | Clickable? |
|---|---|---|
| Not yet available (gated, see A7) | 0 | No (`TestCollision › MousePointOnItem` skips alpha 0) |
| Sold out (see A5 `max`) | 0 | No |
| Available but player can't afford it (`HUD.PurchaseFeedback ≠ 0`) | 0.5 | Yes |
| Available and affordable | 1 | Yes |
| Free bubble (`mFree`) | 1 always | Yes |

Hovering swaps to the highlight frame and plays `StoreOver` (`StoreItem › Highlight`).

Clicking a non-free display (`World/World.as › mouseDown` → `StoreItem › Click`) pauses map input
(`World.mMenuOpen`) and opens a **PurchaseMenu** anchored 70 px above the item
(`StoreItem › Update`). The three `Buy …` items in the Bank open a `TradeMenu` instead (A9).

### A3. The PurchaseMenu

`scripts/Menu/PurchaseMenu.as › init` shows: **title** (item name), **description**
(`store_items.xml <description>`), and three cost fields — wood, gold, gems — each with its icon.
Then:

- If `StoreItem.SoldOut()` → feedback frame 5 (sold out), no Purchase button.
- Else `HUD.PurchaseFeedback(wood, gold, gems)` (`scripts/Menu/HUD.as`):

| Return | Meaning (checked in this order) | UI |
|---|---|---|
| 3 | short on **all three** | feedback frame 3 |
| 4 | short on gems | "Get Gems" button instead of Purchase (opens the gem page) |
| 2 | short on gold | feedback frame 2 |
| 1 | short on wood | feedback frame 1 |
| 0 | affordable | **Purchase** and **Cancel** buttons |

- Clicking the dark area around the menu closes it only when the Purchase button is hidden
  (`PurchaseMenu › cancelif`).

The costs shown are the display's own `mCostWood/mCostRock/mCostGem`, which are the XML costs
**except** for the dynamically priced items in A7.

### A4. What a purchase produces

`StoreItem › purchase(wood, gold, gems)` always subtracts the cost (`HUD.SubtractResource`), plays
the cost "ping" text flying from the HUD to the item, closes the menu, then branches on the item's
class:

| Item class | Result of buying |
|---|---|
| Anything placeable (gardens, forests, hills, buildings, stores, statues, bombs, fruit trees…) | A **`PlacementItem`** is created at the avatar and **joins the avatar's trail** (`PlacementItem` constructor → `AvatarItem › AddShopItemToChain`). Nothing is on the map yet. |
| An animal (`GenericAnimal` subclass) | The animal is spawned **at the display's position**; unless it is a Monster or Deer it immediately **follows the avatar** (joins the trail). A bought **Monster or Deer spawns wild** (Monster untamed, see D). |
| `Decorations.Dress / Pearls / Tiara` | Consumed instantly: camera pans to Coriander, the screen flashes white (1 s) and fades back (3 s), `CorianderItem.Decorate()` changes her outfit (`mDecoration += 80`). Nothing is placed. |
| `Building.CastleItem` (Church) | Built instantly at the fixed point **(3937, 1027)** on Wedding Isle; camera pans there, white flash, then `StoreItemsList.MakeItem("Church",…)` and the "Church" card + `built_church` mote (`StoreItem › drawChurch/awardChurch`). |
| `BridgeAngelItem` (bridge ghosts) | Bridge drawn and island contents spawned instantly (C3). |

After every purchase: `Cards.FoundItem(name)` (first-time card, B3), else `Tasks.PickUp(name)`
(tutorial hook), then `Motes.PurchaseMote(name)` (B1).

**The trail ("inventory").** Items and animals following the avatar form one chain
(`AvatarItem.mBunnyChain`); each member is steered to a point **100 units further back** along the
avatar's recorded path (path history is trimmed at 800 units; `AvatarItem › CalculateBunnyDestinations`).
Chain members move at up to 20 units/frame (`StoreItems/States/ItemFollowAvatar.as`). There is
**no inventory cap** in code. Motes waiting to be clicked ride in the same chain.

### A5. Placing, returning and selling carried items

`World/World.as › mouseDown / onEnterFrame / mouseUp`, `StoreItems/PlacementItem.as`:

1. Mouse-down on a trailing `PlacementItem` → it leaves the chain and follows the cursor
   (`PlacementItem › Move`, 50 units/frame max, sliding along blocked edges via `TestCollision.NextStep`).
2. While dragging:
   - within **100 units of the avatar** → shown small, "Return … back to your inventory";
   - within **100 units of Pirate Pete** (if he exists) → "Sell … to Pirate Pete";
   - otherwise shown at full size with its **outer radius drawn as a ring** (`draw_under`,
     ring art scaled by `outer / 100`). `CanPlace()` = `TestCollision.CircleOnMap2(inner circle)`:
     the inner circle must be on land and not overlap any other item's inner circle
     (motes, animals, avatar and other carried items ignored). Valid → alpha 1, "You can place your
     … here". Invalid → alpha 0.2, "Placement is blocked", and the avatar is highlighted.
3. Mouse-up: avatar highlighted → item goes **back into the trail**; Pete highlighted →
   `PiratePete › SellItem`; otherwise `PlacementItem › CreateItem` builds the real object
   (fires `Motes.PlacementMote(name)` and `Tasks.Place(name)`).

Dragged workers follow the same pattern but snap to the **closest Mill / Mine / avatar to the cursor**
(`TestCollision › ClosestAvaliableItem`, screen distance, unbounded) or Pete within 100 units.

### A6. Free bubbles (`mFree = true`)

A free bubble is the same `StoreItem` class with `mFree = true`
(`StoreItem` constructor, `StoreItems/States/GrowBubble.as`):

- Grows from scale 0.2 to 1.0 at +0.01/frame (80 frames), plays `BubbleAppear`.
- Collision radius 21 (a sale display uses 100/21).
- **Clicking picks it up at no cost** (`World › mouseDown` → `StoreItem › pickUp`): placeable →
  `PlacementItem` into the trail; animal → spawned and following (Monster/Deer wild); the bubble pops.
  Same card / tutorial / `PurchaseMote` hooks as a purchase.
- If not picked up within **30 s**, hint 4: "You've discovered a free <name>! Click to pick it up."
- Bubbles never expire, **except** those dropped by fruit trees (B10).

Sources of free bubbles: tutorial items, quest set-ups and rewards, harvesting a parent node
(ground truth), anything hit by a normal Bomb (D6), fruit-tree shakes (B10), and the donation
reward (Fancy Store).

### A7. The `max`, `persistent`, `inner`, `outer`, `frame` fields

`StoreItems/StoreItemsList.as › InitXML` reads them into the item definition.

| Field | What the code does with it |
|---|---|
| `max` | `0` = unlimited. `>0` = **ownership cap**: `StoreItem › SoldOut` returns true when `Items.GetCountByName(name) > max`. The count is every map object with that `mName` — the sale display itself, placed buildings, carried `PlacementItem`s, bubbles. So with `max 1` the display (count 1) disappears as soon as one more exists anywhere (bought and carried, or placed). Exceptions: Wedding Dress / String of Pearls / Tiara are sold out once their mote (`girl_quest_2` / `girl_quest_6` / `girl_crown`) exists. Also: items with `max 0` are the pool for random fruit-tree bubbles (`mFreeItems`), and only `max 0` items can be sold to Pirate Pete (`StoreItemsList › CanSell`). |
| `persistent` | `1` = written to the save (`Save/Save.as › SaveNow` checks `mPersist`). Every shipped item is `1`. Sale displays are not saved (their building recreates them); free bubbles and bridge ghosts are. |
| `inner` | Footprint/collision radius in world units (placement test, `CircleOnMap`). For resource nodes it is the base radius, multiplied by each node's final scale. |
| `outer` | Interaction radius. Gardens/forests/hills: the circle children spawn into (A-table sizes → E2). Bombs: blast radius. Buildings: 0 (a Mill's/Mine's work radius is hard-coded 800, not `outer`). |
| `frame` | Art index into the store-item sheet. The sheet has 73 frames per variant (`StoreItemsList.mNoStoreItemFrames`): `frame` = sale icon, `+73` = highlighted, `+146` = bubble, `+146+73` = bubble highlighted, `+292` = full-size placed art. |
| `id` | Save-file item id. |
| `itemclass` | AS3 class instantiated on placement / spawn. |

### A8. Dynamic prices (the XML cost is ignored for these)

`StoreItem` constructor and `StoreItem › Avaliable`. Two inputs:

- `HUD.mWood`, `HUD.mRock` — the player's **current** wood and gold at the moment of pricing.
- `HUD.mWoodMaxMin`, `HUD.mGoldMaxMin` — the **best wood / gold delivered by workers in any single
  60-second window this session** (`HUD › updatePerMin`, fed by `TakeWoodToMill`). Not saved; resets
  to 0 each session.

Each price is computed **once**, cached in a static, and saved in the save file's `<balance>`
entries (ids 1–9; `Save.as › SaveNow`, `StoreItem › SetUp`). Integer truncation; each capped at 99 999.

| Item | Wood cost | Gold cost | Priced when |
|---|---|---|---|
| Verdant Bay (bridge) | 3·WoodMaxMin + Wood/4 | 3·GoldMaxMin + Gold/4 | ghost spawns |
| Wedding Isle (bridge) | 5·WoodMaxMin + Wood/4 | 5·GoldMaxMin + Gold/4 | ghost spawns |
| Wedding Dress | 1·WoodMaxMin + **Gold**/2 | 1·GoldMaxMin + **Wood**/2 | Silver Store placed |
| String of Pearls | 4·WoodMaxMin + Gold/2 | 4·GoldMaxMin + Wood/2 | Steel Store placed |
| Tiara | 8·WoodMaxMin + Gold/2 | 8·GoldMaxMin + Wood/2 | Gold Store placed |
| Silver Store | 2·WoodMaxMin + **Gold** | 2·GoldMaxMin + **Gold** + 10 | first on-screen frame after `bronze_store` mote |
| Steel Store | 6·WoodMaxMin + Gold/2 | 6·GoldMaxMin + Wood/2 | first on-screen frame after `girl_quest_2` (dress) |
| Gold Store | 7·WoodMaxMin + Gold/2 | 7·GoldMaxMin + Wood/2 | first on-screen frame after `girl_quest_6` (pearls) |
| Church | 10·WoodMaxMin + Gold/2 | 10·GoldMaxMin + Wood/2 | first on-screen frame after `girl_crown` (tiara) |

The wood/gold cross-over (wood price uses your gold and vice versa) and the Silver Store using
gold twice are as written in the code. Gem costs always come from the XML.

### A9. The store chain

| Store | How you get it | Sells (game.xml order) |
|---|---|---|
| **General Store** | Never sold. Coriander's first quest gives a free bubble (`MissionSetUps › create_store`). | Small Posie Garden, Medium Posie Garden, Medium Pine Forest, Medium Gray Hill, Mine, **Bronze Store** |
| **Bronze Store** | Bought in the General Store: **200 wood / 900 gold** (static). No gate. | Deer Bomb, Medium Tulip Garden, Medium Maple Forest, Fox House, Sawmill, **Silver Store** |
| **Silver Store** | Bought in the Bronze Store. Gate `bronze_store` mote (always true by then). Dynamic price. | Legendary Sawmill, Medium Black Hill, Wedding Dress, Small Rose Garden, Bomb, Small Gold Hill, **Steel Store** |
| **Steel Store** | Bought in the Silver Store. **Hidden until the Wedding Dress is bought.** Dynamic price. | Legendary Gem Store, Legendary Mine, String of Pearls, **Bank**, Medium Ironwood Forest, **Gold Store** |
| **Gold Store** | Bought in the Steel Store. **Hidden until the String of Pearls is bought.** Dynamic price. | Large Gold Hill, Large Ironwood Forest, Medium Rose Garden, Tiara, **Church** (hidden until the Tiara is bought) |
| **Legendary Gem Store** | Bought in the Steel Store: **100 gems**. No gate. | Large Rose Garden, Legendary Plum Tree, White Flower Tree, Pink Flower Tree, Holy Statue, Gem Statue |
| **Bank** | Bought in the Steel Store: **5 gems**. | Buy Gems, Buy Gold, Buy Wood (Kongregate build: no Buy Gems) |
| **Fancy Store** | Never sold. A free bubble appears when a real-money donation is confirmed and you don't already have one (`CurrencyPlugin/SuperReward.as › loadPageSuccessful`). | Bushy Tree, Fall Color, Tiny Bush, Little Greeny, Splash Fountain, Monster Den |

No store is gated by a **ghost**; the only gates are the three purchase motes above (dress, pearls,
tiara), all from Coriander's quests. Each store has `max 1`.

### A10. The Bank (`scripts/Menu/TradeMenu.as`)

Four fixed tiers. A tier's button is enabled when current gems ≥ its gem cost (5/10/20/30).
The result is delivered as an auto-opening treasure chest 70 units south of the Bank display.

| Button | Buy Wood | Buy Gold | Buy Gems (real money) |
|---|---|---|---|
| 1 | 5 gems → 250 wood | 5 gems → 250 gold | $5 → 500 gems |
| 2 | 10 gems → 500 wood | 10 gems → 500 gold | $10 → 1000 gems |
| 3 | 20 gems → 1250 wood | 20 gems → 1250 gold | $20 → 3000 gems |
| 4 | 30 gems → 2000 wood | 30 gems → 2000 gold | $30 → 6000 gems |

Buy Gems buttons read "Save First" unless the payment plugin is initialised, saving is enabled and
the player is logged in. (CapyIsland will presumably not ship real-money gems; noted for completeness.)

### A11. Pirate Pete, the buy-back merchant (`scripts/MapItems/PiratePete/PiratePete.as`)

- Clicking him: before `girl_quest_3` exists he "is telling a dirty story"; after, "if you want to
  sell me some spare loot, just drag it onto me booty." **Closing any Pete dialog fires
  `girl_quest_3`** (`PiratePete › Close`).
- **Selling items** (drag a carried item onto him): only `max 0` items (`CanSell`); otherwise "Sorry
  I can't buy this item" and it returns to the trail. Offer = **wood `costRock/2`, gold `costWood/2`,
  gems `costGem/2`** (the wood/gold swap is in the code, `SellItem`). Uses the item's carried cost.
- **Selling animals** (drag a worker onto him), `SellAnimal`:

| Animal | Pete pays |
|---|---|
| bunni | 10 wood + 10 gold |
| pirate | 5 gems |
| panda | 20 wood + 20 gold |
| fox | 15 wood + 30 gold |
| monster | 10 gems |
| deer | nothing (deer can't be dragged anyway) |

- "Deal" pays out as auto-opening chests and deletes the item/animal; "No Deal" returns it to the trail.

### A12. Factory functions (`scripts/MapItems/StoreItems/StoreItemsList.as`)

| Function | Does |
|---|---|
| `MakeStore(name, x, y, free=false, missionId=0)` | Creates a `StoreItem` (or `BridgeAngelItem` if that's the class) at (x,y). `free=true` makes a **free bubble**; `false` a **sale display**. Copies `max` into `mMaxItems`. |
| `MakeStoreRandom(name, rect, free=true)` | Same, at a random valid spot in the island rect (below). Note the **default is free**. |
| `MakeItem(name, x, y)` | Creates the **real object directly** (no bubble, no placement). Animals get `(x,y)`; the Church is forced to (3937, 1027) and sets `MissionStatic.mChurch`. |
| `MakeItemRandom(name, rect)` | `MakeItem` at a random valid spot in the island rect. |
| `MakePlacementItem(name, …)` | Creates a carried `PlacementItem` at the avatar (used when loading the saved inventory). |
| `MakeRandomStore(x, y)` | A free bubble of a **random item with `max 0`** (`mFreeItems`). Used only by fruit-tree shakes (1% per fruit). Returns nothing. |
| `CanSell(name)` | `max == 0`. |

Random spot in an island (`TestCollision › FindRandomCircleOnRect`): the island rect stored in
`Land.mLandRectangles[i][0]` is the XML rect shrunk by 340 on every side; this function grows it
back by 340 and tries up to 200 random points whose `inner`-radius circle is on land and clear. If
all fail it uses a random point in the shrunk rect.

### A13. Every store item (`store_items.xml`)

Costs are the XML values; items marked † are dynamically priced at runtime (A8). "Sold in" is from
`game.xml <stores>`; animals are sold by the matching house's display. All 83 entries have
`persistent 1`.

| Name | id | wood | gold | gems | itemclass (`MapItems.` omitted) | inner | outer | max | pers. | Sold in |
|---|---|---|---|---|---|---|---|---|---|---|
| Small Posie Garden | 1 | 1 | 3 | 0 | `Terrain.GardenItems.PosieItem` | 22 | 100 | 0 | 1 | general_store |
| Bomb | 2 | 10 | 10 | 0 | `Terrain.BombItem` | 20 | 300 | 0 | 1 | silver_store |
| Medium Posie Garden | 3 | 5 | 1 | 0 | `Terrain.GardenItems.PosieItem` | 22 | 150 | 0 | 1 | general_store |
| Large Posie Garden | 4 | 25 | 10 | 0 | `Terrain.GardenItems.PosieItem` | 22 | 200 | 0 | 1 | — (ghost 1 reward) |
| Small Tulip Garden | 5 | 5 | 3 | 0 | `Terrain.GardenItems.TulipItem` | 22 | 100 | 0 | 1 | — |
| Medium Tulip Garden | 6 | 20 | 5 | 0 | `Terrain.GardenItems.TulipItem` | 22 | 150 | 0 | 1 | bronze_store |
| Large Tulip Garden | 7 | 30 | 15 | 0 | `Terrain.GardenItems.TulipItem` | 22 | 200 | 0 | 1 | — |
| Small Rose Garden | 8 | 0 | 0 | 5 | `Terrain.GardenItems.RoseItem` | 22 | 100 | 0 | 1 | silver_store |
| Medium Rose Garden | 9 | 0 | 0 | 10 | `Terrain.GardenItems.RoseItem` | 22 | 150 | 0 | 1 | gold_store |
| Large Rose Garden | 10 | 0 | 0 | 20 | `Terrain.GardenItems.RoseItem` | 22 | 200 | 0 | 1 | gem_store |
| Small Pine Forest | 11 | 0 | 2 | 0 | `Terrain.ForestItems.PineItem` | 36 | 150 | 0 | 1 | — |
| Medium Pine Forest | 12 | 0 | 5 | 0 | `Terrain.ForestItems.PineItem` | 36 | 200 | 0 | 1 | general_store |
| Large Pine Forest | 13 | 25 | 12 | 0 | `Terrain.ForestItems.PineItem` | 36 | 300 | 0 | 1 | — |
| Small Maple Forest | 14 | 8 | 3 | 0 | `Terrain.ForestItems.MapleItem` | 36 | 150 | 0 | 1 | — |
| Medium Maple Forest | 15 | 10 | 8 | 0 | `Terrain.ForestItems.MapleItem` | 36 | 200 | 0 | 1 | bronze_store |
| Large Maple Forest | 16 | 20 | 10 | 0 | `Terrain.ForestItems.MapleItem` | 36 | 300 | 0 | 1 | — (Peach 5 reward) |
| Small Ironwood Forest | 17 | 0 | 0 | 5 | `Terrain.ForestItems.IronwoodItem` | 36 | 150 | 0 | 1 | — (Coriander 1 reward) |
| Medium Ironwood Forest | 18 | 0 | 0 | 10 | `Terrain.ForestItems.IronwoodItem` | 36 | 200 | 0 | 1 | steel_store |
| Large Ironwood Forest | 19 | 0 | 0 | 20 | `Terrain.ForestItems.IronwoodItem` | 36 | 300 | 0 | 1 | gold_store |
| Bunni House | 20 | 500 | 500 | 0 | `Building.Houses.BunnyHouseItem` | 41 | 0 | 1 | 1 | — (tutorial bubble) |
| Pirate House | 21 | 15000 | 0 | 20 | `Building.Houses.PirateHouseItem` | 41 | 0 | 1 | 1 | — (unobtainable, see D1) |
| Panda House | 22 | 1000 | 300 | 0 | `Building.Houses.PandaHouseItem` | 41 | 0 | 1 | 1 | — (unobtainable) |
| Fox House | 23 | 400 | 25 | 0 | `Building.Houses.FoxHouseItem` | 41 | 0 | 1 | 1 | bronze_store |
| Monster House | 24 | 500 | 400 | 0 | `Building.Houses.MonsterHouseItem` | 41 | 0 | 1 | 1 | — (Peach 3 set-up) |
| Deer House | 25 | 50 | 0 | 0 | `Building.Houses.DeerHouseItem` | 41 | 0 | 1 | 1 | — (unobtainable) |
| Bunni | 26 | 10 | 10 | 0 | `Animals.BunnyItem` | 0 | 0 | 0 | 1 | Bunni House |
| Pirate | 27 | 0 | 0 | 10 | `Animals.PirateItem` | 0 | 0 | 0 | 1 | Pirate House |
| Panda | 28 | 5 | 3 | 0 | `Animals.PandaItem` | 0 | 0 | 0 | 1 | Panda House |
| Fox | 29 | 30 | 15 | 0 | `Animals.FoxItem` | 0 | 0 | 0 | 1 | Fox House |
| Monster | 30 | 1500 | 0 | 0 | `Animals.MonsterItem` | 0 | 0 | 0 | 1 | Monster House |
| Deer | 31 | 10 | 0 | 0 | `Animals.DeerItem` | 0 | 0 | 0 | 1 | Deer House |
| Sawmill | 32 | 0 | 250 | 0 | `Building.MillItem` | 41 | 0 | 0 | 1 | bronze_store |
| Mine | 33 | 100 | 50 | 0 | `Building.MineItem` | 41 | 0 | 0 | 1 | general_store |
| General Store | 34 | 25 | 25 | 0 | `Building.Stores.GeneralStore` | 41 | 0 | 1 | 1 | — (Coriander 16 set-up) |
| Bronze Store | 35 | 200 | 900 | 0 | `Building.Stores.BronzeStore` | 41 | 0 | 1 | 1 | general_store |
| Steel Store | 36 | 12250† | 125† | 0 | `Building.Stores.SteelStore` | 41 | 0 | 1 | 1 | silver_store |
| Silver Store | 37 | 1750† | 1150† | 0 | `Building.Stores.SilverStore` | 41 | 0 | 1 | 1 | bronze_store |
| Gold Store | 38 | 12000† | 11500† | 0 | `Building.Stores.GoldStore` | 41 | 0 | 1 | 1 | steel_store |
| Church | 39 | 2500† | 3000† | 0 | `Building.CastleItem` | 92 | 0 | 1 | 1 | gold_store |
| Apple Tree | 40 | 500 | 250 | 0 | `FruitTrees.AppleTree` | 50 | 0 | 0 | 1 | — (see B10) |
| Pear Tree | 41 | 1000 | 250 | 0 | `FruitTrees.PearTree` | 50 | 0 | 0 | 1 | — (see B10) |
| Legendary Plum Tree | 42 | 0 | 0 | 100 | `FruitTrees.PlumTree` | 50 | 0 | 0 | 1 | gem_store |
| Legendary Gem Store | 43 | 0 | 0 | 100 | `Building.Stores.GemStore` | 41 | 0 | 1 | 1 | steel_store |
| Legendary Sawmill | 44 | 0 | 0 | 10 | `Building.LedgendMillItem` | 41 | 0 | 0 | 1 | silver_store |
| Legendary Mine | 45 | 0 | 0 | 20 | `Building.LedgendMineItem` | 41 | 0 | 0 | 1 | steel_store |
| White Flower Tree | 46 | 1500 | 1000 | 0 | `Statues.WhiteTreeStatue` | 41 | 0 | 0 | 1 | gem_store |
| Pink Flower Tree | 47 | 1000 | 2000 | 0 | `Statues.PinkTreeStatue` | 41 | 0 | 0 | 1 | gem_store |
| Holy Statue | 48 | 5000 | 10000 | 0 | `Statues.HolyStatue` | 41 | 0 | 0 | 1 | gem_store |
| Gem Statue | 49 | 0 | 0 | 50 | `Statues.GemStatue` | 41 | 0 | 0 | 1 | gem_store |
| Small Gray Hill | 50 | 2 | 0 | 0 | `Terrain.HillItems.BronzeItem` | 36 | 150 | 0 | 1 | — |
| Medium Gray Hill | 51 | 5 | 0 | 0 | `Terrain.HillItems.BronzeItem` | 36 | 200 | 0 | 1 | general_store |
| Large Gray Hill | 52 | 12 | 25 | 0 | `Terrain.HillItems.BronzeItem` | 36 | 300 | 0 | 1 | — |
| Small Black Hill | 53 | 5 | 8 | 0 | `Terrain.HillItems.SilverItem` | 36 | 150 | 0 | 1 | — |
| Medium Black Hill | 54 | 8 | 20 | 0 | `Terrain.HillItems.SilverItem` | 36 | 200 | 0 | 1 | silver_store |
| Large Black Hill | 55 | 10 | 30 | 0 | `Terrain.HillItems.SilverItem` | 36 | 300 | 0 | 1 | — |
| Small Gold Hill | 56 | 0 | 0 | 5 | `Terrain.HillItems.GoldItem` | 36 | 150 | 0 | 1 | silver_store |
| Medium Gold Hill | 57 | 0 | 0 | 10 | `Terrain.HillItems.GoldItem` | 36 | 200 | 0 | 1 | — |
| Large Gold Hill | 58 | 0 | 0 | 20 | `Terrain.HillItems.GoldItem` | 36 | 300 | 0 | 1 | gold_store |
| Tower Of Love | 59 | 0 | 0 | 200 | `Tower.TowerItem` | 83 | 0 | 1 | 1 | — (tutorial spawn) |
| Ghost | 80 | 100 | 100 | 0 | `Ghosts.GhostItem` | 30 | 0 | 1 | 1 | — (quest spawn) |
| Pirate Pete | 83 | 100 | 100 | 0 | `PiratePete.PiratePete` | 0 | 0 | 1 | 1 | — (Coriander 1 set-up) |
| Pirate Peach | 82 | 100 | 100 | 0 | `Peach.PeachItem` | 30 | 0 | 1 | 1 | — (Verdant Bay) |
| Coriander | 81 | 100 | 100 | 0 | `Coriander.CorianderItem` | 30 | 0 | 1 | 1 | — (tutorial) |
| Wedding Dress | 84 | 100† | 100† | 0 | `Decorations.Dress` | 0 | 0 | 1 | 1 | silver_store |
| String of Pearls | 85 | 100† | 100† | 0 | `Decorations.Pearls` | 0 | 0 | 1 | 1 | steel_store |
| Tiara | 86 | 100† | 100† | 0 | `Decorations.Tiara` | 0 | 0 | 1 | 1 | gold_store |
| Verdant Bay | 87 | 100† | 100† | 0 | `StoreItems.BridgeAngelItem` | 0 | 0 | 1 | 1 | — (bridge ghost) |
| Ginger Cove | 88 | 100 | 100 | 0 | `StoreItems.BridgeAngelItem` | 0 | 0 | 1 | 1 | — (bridge ghost) |
| Wedding Isle | 89 | 100† | 100† | 0 | `StoreItems.BridgeAngelItem` | 0 | 0 | 1 | 1 | — (bridge ghost) |
| Pirate Cove | 90 | 0 | 0 | 100 | `StoreItems.BridgeAngelItem` | 0 | 0 | 1 | 1 | — (bridge ghost) |
| Monster Isle | 91 | 100 | 100 | 0 | `StoreItems.BridgeAngelItem` | 0 | 0 | 1 | 1 | — (never spawned) |
| Bank | 92 | 0 | 0 | 5 | `Building.Stores.BankStore` | 41 | 0 | 1 | 1 | steel_store |
| Buy Gems | 93 | 0 | 0 | 0 | `StoreItems.GemStoreItem` | 10 | 0 | 1 | 1 | bank |
| Buy Wood | 94 | 0 | 0 | 0 | `StoreItems.WoodStoreItem` | 10 | 0 | 1 | 1 | bank |
| Buy Gold | 95 | 0 | 0 | 0 | `StoreItems.GoldStoreItem` | 10 | 0 | 1 | 1 | bank |
| Deer Bomb | 97 | 10 | 10 | 0 | `Terrain.DeerBombItem` | 20 | 300 | 0 | 1 | bronze_store |
| Fancy Store | 98 | 100 | 100 | 0 | `Building.Stores.FancyStore` | 41 | 0 | 1 | 1 | — (donation reward) |
| Bushy Tree | 99 | 500 | 200 | 0 | `Statues.BushyTree` | 50 | 0 | 0 | 1 | fancy_store |
| Fall Color | 100 | 0 | 0 | 70 | `Statues.FallTree` | 50 | 0 | 0 | 1 | fancy_store |
| Tiny Bush | 101 | 0 | 0 | 2 | `Statues.TinyBush` | 31 | 0 | 0 | 1 | fancy_store |
| Little Greeny | 102 | 0 | 0 | 120 | `Statues.LittleGreeny` | 40 | 0 | 0 | 1 | fancy_store |
| Splash Fountain | 103 | 0 | 0 | 300 | `Statues.SplashFountain` | 65 | 0 | 0 | 1 | fancy_store |
| Monster Den | 104 | 400 | 1000 | 0 | `Statues.MonsterDen` | 53 | 0 | 0 | 1 | fancy_store |

Items not in any store with `max 0` (Small/Large variants, Small Tulip, Pirate/Panda/Deer
animals, Apple/Pear trees, …) can still appear as the 1% random fruit-tree bubble (B10). Items not
in any store with `max 1` and no spawning code (Pirate/Panda/Deer House, Monster Isle ghost) are
**unobtainable** in the shipped game.

---

## B. Motes, cards, the Tower of Love, ghosts and quests

### B1. Motes ("bits of love")

A **mote** is a named, one-shot achievement flag that also appears as a physical heart on the map.
There are **27** (`save_data.xml <mote>`: `name`, `id`, `description`, `func`, `collected`).
`scripts/Rewards/Motes.as`:

- `TriggerMote(func)`: if that mote isn't collected yet → `mCollected = true`, spawn a `MoteItem`
  heart at the avatar (+75 y), and broadcast the mote's **name** to all ghosts, Coriander
  missions and Peach missions (`Ghosts.Mote`, `CorianderMissions.Mote`, `PeachMissions.Mote`).
  **Quests key on the mote *name*; triggers are called by *func*.**
- The heart (`MapItems/Mote/MoteItem.as` + `States/`) plays a 32-frame appear, joins the avatar
  trail and beats. **Clicking it** sets `mClicked`, pops a modal card with the mote's name and
  description (pauses the game until clicked), then the heart flies to the Tower of Love at
  15 units/frame; on arrival the Tower gains **+50 score** and redraws (`TowerItem › MoteFound`).
  If no Tower exists the heart just dissolves.
- Quest completion uses `mCollected` (set at trigger), so clicking the heart is **not** required
  for progress; it only matters for the Tower (`mClicked`).

| id | Name (quests reference this) | func (triggers call this) | Fired by |
|---|---|---|---|
| 1 | First Love | `first_mote` | Tutorial: placing the Medium Posie Garden (`Tasks › TaskClickMote`) |
| 2 | Welcome Back | `welcome_back` | Loading as a returning player (`Tasks › GameStarted`) |
| 4 | Fox Importer | `fox1` | Buying/picking up a "Fox" when ≥ 2 objects named "Fox" exist (`Motes › PurchaseMote`) |
| 5 | Swedish Fox Party | `fox5` | Same, when ≥ 6 exist |
| 6 | Everyone loves Pandas | `panda1` | Buying/picking up any "Panda" |
| 7 | Silver Store | `silver_store` | Placing a Silver Store (`Motes › PlacementMote`) |
| 8 | Steel Store | `steel_store` | Placing a Steel Store |
| 9 | The Most Glorious Gold Store | `gold_store` | Placing a Gold Store |
| 10 | Build a church | `built_church` | Buying the Church |
| 12 | Tulips for Coriander | `girl_quest_1` | Placing any item whose name contains "Tulip" (no quest uses it) |
| 13 | The Girl's New Dress | `girl_quest_2` | Buying the Wedding Dress |
| 14 | Priate Tales *(sic)* | `girl_quest_3` | Closing any Pirate Pete dialog |
| 15 | Red roses, Red lips | `girl_quest_5` | Placing any item whose name contains "Rose" |
| 16 | The Girl's New Necklace | `girl_quest_6` | Buying the String of Pearls |
| 17 | Destroyer of Deer | `girl_quest_8` | Timed check: Coriander mission 5 active and 0 deer on the map |
| 18 | Madly in Love | `girl_quest_10` | Set-up `create_priest` (Coriander mission 7) |
| 19 | The wedding bells toll for thee | `wedding_mote` | Paying the Priest (wedding animation loads) |
| 22 | Legendary Store | `gem_store` | Placing a Legendary Gem Store |
| 23 | Holy Statue | `holy_statue` | Placing a Holy Statue |
| 24 | Bronze Store | `bronze_store` | Placing a Bronze Store |
| 26 | Put that panda to work! | `panda_sawmill` | Timed check: Peach mission 1 active and a Panda works at a (Legendary) Mill |
| 27 | Plum orchard | `5_plum` | Timed check: Peach mission 2 active and ≥ 6 objects named "Legendary Plum Tree" |
| 28 | Hired monsters | `monster_tame` | Timed check: Peach mission 3 active and ≥ 5 tame Monsters |
| 29 | Dancing Queen | `peach_dance` | Paying Peach 500 gems (`PeachItem › Dance`) |
| 30 | The Girl's Tiara | `girl_crown` | Buying the Tiara |
| 31 | You Placed a Mine | `place_mine` | Placing a Mine |
| 35 | Visit Forums | `visit_forums` | Clicking the HUD Forums button |

"Buying" in this table means `Motes.PurchaseMote`, which runs both on a paid purchase and on
picking up a free bubble. Name counts use `Items.GetCountByName`, which includes the item's own
sale display beside its house/store and any bubble or carried copy — so `fox1` in practice means
"first fox", `fox5` "fifth fox", and `5_plum` "five plum trees plus the Gem Store's display".
The plum check is **global** — "on Pirate Cove" in Peach's text is not enforced.

### B2. The Tower of Love (`scripts/MapItems/Tower/`)

- Spawned (as a real item, not a bubble) at a random spot on island 1 during the tutorial
  (`Tasks › TaskClickMote`).
- Its art frame is `collected / 27 × 100 + 1` (`Motes › TowerFrame`), i.e. it fills as motes are
  **triggered**.
- Clicking opens the *Dear Diary* panel (`DearDiary.as`): one heart button per mote (hover shows
  name + description), a progress bar of **clicked** motes, "Bits of love left to collect (N)", and
  a mood line by percent clicked: <20 "…% away from certain death", <40 "…% hopeful that life has
  meaning", <60 "…% determined to rebuild what was lost", <80 "…% delighted to be bustling and
  moving forward", else "…% convinced that Love is eternal". Closing it is a tutorial step (F).
- When **all 27 motes have been clicked** (`Motes › Remaining` sets `mIslandSaved`), the Tower
  drops a random chest (wood or gold 50/50, value 1, 5 or 10) near itself **every second**
  (`TowerItem › rewardPlayer`, `TreasureChest › SetRandom(4)`), and the diary shows its "saved" frame.

### B3. Cards

A **card** is a first-discovery popup; 40 exist (`save_data.xml <card>`: `name`, `id`,
`description`, `frame`, `collected`, one or more `func` = store-item names). `scripts/Rewards/Cards.as`:

- `FoundItem(itemName)` is called on every purchase and every bubble pick-up
  (`StoreItem › purchase / pickUp`; the Church via `awardChurch`). The first uncollected card
  listing that item name is marked collected and shown: the game pauses, the card zooms from the
  item to screen centre, and clicking it dismisses it and calls `Tasks.PickUp(cardName)` (this is
  how the tutorial hears "Pine Tree", "Posies" etc.).
- Cards have no other gameplay effect and no album UI; they are saved.
- Placement does **not** trigger cards; items created by `MakeItem` (Tower of Love, set-up spawns)
  never trigger their card.

| id | Card | Items that trigger it |
|---|---|---|
| 1–5 | General / Bronze / Steel / Silver / Gold Store | that store |
| 7, 8, 9 | Gray / Black / Gold Hill | any size of that hill |
| 10, 11, 12 | Pine / Maple / Ironwood Tree | any size of that forest |
| 13, 14 | Sawmill / Legendary Mill | Sawmill / Legendary Sawmill |
| 15, 16 | Mine / Legendary Mine | Mine / Legendary Mine |
| 18, 19, 20, 22 | Bunni / Panda / Fox / Monster | that animal |
| 23, 24, 25 | Posies / Tulips / Roses | any size of that garden |
| 26, 27, 28 | Coriander / Pirate Peach / Pirate Pete | their bubbles |
| 29 | Church | Church |
| 30 | Tower Of Love | (never fires — the Tower is spawned with `MakeItem`) |
| 31–34 | Pink / White Flowering Tree, Holy / Gem Statue | that decoration |
| 35–40 | Fox / Monster / Deer / Pirate / Bunni / Panda House | that house |
| 41, 42, 43 | Apple / Pear / Plum Tree | that tree |

Card descriptions make two claims the code doesn't honour: Legendary Mill/Mine "harvests 3 times"
(code: ×2, see ground truth) and statues "increase your score" (code: no effect, D7).

### B4. The quest engine

Three quest-givers share one engine (`scripts/Quests/Quest.as`, `Ghost.as`, `Mission.as`,
`State/*.as`, `MapItems/Ghosts/QuestItem.as`):

| Giver | Data | Class | Order |
|---|---|---|---|
| Ghosts | `save_data.xml <ghost>` | `Ghost` | Independent; each spawns on its own trigger mote |
| Coriander | `<coriander>` | `Mission` | Strictly sequential, **XML order**, one at a time |
| Pirate Peach | `<piratepeach>` | `Mission` | Strictly sequential, XML order, one at a time |

**Fields.** `intro`, `during`, `complete` (dialogue, split into pages on `[br]`), `trigger_mote`
(ghosts only), `set_up` (action run when the intro is read), `complete_mote` (a mote **name**),
`reward_item` (store-item name), `wood`, `gold`, `gems` (**rewards**, not prices).

**Stages** (`Quest.as`): `0 kNotStarted` → `1 kIntroRead` → `2 kTaskCompete` → `3 kGiftRecived`.
The item's `mExplination` (1 = intro, 2 = during, 3 = complete, 0 = nothing) drives the icon over
its head.

**Talking** (`QuestItem › Click`): shows the current text as a speech bubble (`AngelHelp`) that is
clicked through page by page. Before showing, `TestComplete()` re-checks whether the completion
mote is already collected (`Motes.TestMoteName`) and jumps to the complete text if so.

**Closing the dialog** (`QuestItem › Close` → `AdvanceState`):

1. **Intro read** → if already completed, go to Complete; otherwise go to During, **run `set_up`**
   (`MissionSetUps.CallSetup`), stage 1.
2. **During** → nothing (the objective completes asynchronously, see below).
3. **Complete read** → rewards appear **70 units south of the giver**: `reward_item` as a **free
   bubble**; each non-zero `gold`, `wood`, `gems` as a separate auto-opening treasure chest. Stage 3.
   A ghost then plays its descend animation and is removed. Coriander/Peach wait **15 s**, then load
   the next mission in XML order (`Mission › onNext` → `GetNextMission`).

**Objective completion is always a mote.** When a mote fires, every quest whose `complete_mote`
equals its name is marked complete (`CallMote`), **even if that quest hasn't started yet** — so
Coriander/Peach objectives achieved early are credited the moment the mission comes up (it opens
straight on the complete text). Motes for objectives that aren't naturally events come from
`MissionSetUps › timedChecks`, which runs **every 5 s** and only looks at the *currently active*
(stage 1) Coriander and Peach mission (B8).

Ghost caveat (`Ghost › CallMote`): a ghost only spawns if its completion mote hasn't already fired.
If you get a fox before placing the Bronze Store, Fredrickson never appears and his reward is lost.

**Hints** (`QuestItem` constructor; the "clicked" flag is shared by *all* quest items, so these
only show until the first time you talk to anyone):
- Ghost, after 60 s: hint 70 "A new ghost has appeared and has a quest for you... click him to find out more"
- Peach, after 60 s: hint 71 "Have you chatted with Pirate Peach. She's a saucy lass who knows the secret of how to unlock Pirate Bay."
- Coriander, immediately: hint 72 "Goodness, my dear daughter is still alive! Click on her to see if she needs any help." (the tutorial narrator Susanna is Coriander's dead mother)

**Idle chatter.** With no active mission (or after the last one), Coriander and Peach say a random
line from `<coriander_comment>` / `<peach_comment>`. The `level` field (33/66/80 for Coriander) is
loaded but **ignored**: `RandomComment` picks uniformly. After the dance, Peach has a 30% chance of
"I'll dance for you anytime" with a free replay button.

### B5. Ghosts (in `save_data.xml` order)

| id | Ghost | Appears when | `set_up` | Objective (plain words) | Completion mote | Reward |
|---|---|---|---|---|---|---|
| 1 | Fredrickson, friend of foxes | mote **Bronze Store** fires (Bronze Store placed) | `second_island` (**no-op**) | Get a fox (first fox) | Fox Importer (`fox1`) | Large Posie Garden bubble |
| 3 | Fredrickson again | mote **Fox Importer** fires | — | Have 5 foxes | Swedish Fox Party (`fox5`) | Medium Ironwood Forest bubble |
| 8 | Chappy, the church ghost | mote **Legendary Store** fires (Gem Store placed) | — | Place a Holy Statue | Holy Statue (`holy_statue`) | nothing |

Spawn point (`Ghost › CallMote`): a random point within **800 units of the avatar** where a
radius-30 circle is on clear land (retried until found), created with `MakeItem("Ghost",…)` —
a real item, no bubble. Ghost ids 2 and 4–7 do not exist in the shipped data.

### B6. Coriander's missions (XML order = play order)

Coriander herself arrives as a **free bubble** on island 1 in the tutorial (F); the player picks
her up and places her. Her first mission is the first `<coriander>` entry.

| # | id | Asks | `set_up` (on reading intro) | Done when | Reward |
|---|---|---|---|---|---|
| 1 | 16 | "See if you can buy the Bronze store." | `create_store`: **free General Store bubble** 70 units south of the avatar (focus arrow) | Bronze Store placed (mote Bronze Store) | — |
| 2 | 1 | "Talk to a pirate and tell me a … tale" | `create_pete`: **free Pirate Pete bubble** at a random spot on Ginger Cove (island 2) | any Pete dialog closed (Priate Tales) | Small Ironwood Forest bubble |
| 3 | 2 | "maybe you can … start a new [Silver] store?" | — | Silver Store placed | 1000 gold |
| 4 | 8 | "that beautiful dress at the Silver store" | `dress_store` (**no-op**) | Wedding Dress bought (The Girl's New Dress) | — |
| 5 | 5 | Deer are back — "Take some bombs with you!" | `create_deer`: **3 Deer on island 0 + 3 Deer on island 3** (spawned directly) | 5-s check: **0 deer anywhere** (Destroyer of Deer) | 10 gems |
| 6 | 4 | "a Steel Store … Have you thought of getting one?" | — | Steel Store placed | 1000 wood |
| 7 | 9 | Pearl necklace | `pearl_store` (**no-op**) | String of Pearls bought (The Girl's New Necklace) | — |
| 8 | 6 | The old Gold Store | `create_wedding` (**no-op**) | Gold Store placed | 1500 wood |
| 9 | 10 | Mother's tiara | `crown_store` (**no-op**) | Tiara bought (The Girl's Tiara) | — |
| 10 | 7 | "I love you … What do you say?" | `create_priest`: **fires `girl_quest_10` immediately** | (instant) Madly in Love | 15 gems |

After mission 10 the Church becomes purchasable (it is gated on the tiara, `girl_crown`, not on
`girl_quest_10`), and the Priest (who stands 50 east / 100 south of the Church) offers the
**Wedding Ceremony for 50 wood + 50 gold** once `girl_quest_10` exists (`Priest/PriestItem.as ›
Click`). Before that he says "…first you have to find a girl who is willing!". Paying plays
`wedding.swf` and fires `wedding_mote`; afterwards he offers "Renew Vows" at the same price.

Because the deer check counts **every** deer, deer from Deer Houses, fruit-tree chops or random
bubbles also block mission 5.

### B7. Pirate Peach's missions (XML order = play order)

Peach arrives as a **free bubble** on Verdant Bay (island 3) when the Verdant Bay bridge is bought.

| # | id | Asks | `set_up` | Done when | Reward |
|---|---|---|---|---|---|
| 1 | 5 | "Fetch me a rose" | — | any Rose garden placed (Red roses, Red lips) | Large Maple Forest bubble |
| 2 | 6 | "Fetch me the Legendary Gem Store" | — | Legendary Gem Store placed (Legendary Store) | 2000 gold |
| 3 | 2 | "Five plum trees on Pirate Cove" | — | 5-s check: ≥ 6 objects named "Legendary Plum Tree" anywhere (Plum orchard) | Large Ironwood Forest bubble |
| 4 | 1 | "Could you put this [panda] to work?" | `create_panda`: **free Panda bubble** 70 south of the avatar | 5-s check: a Panda's workplace is a Mill or Legendary Mill (Put that panda to work!) | 100 gold |
| 5 | 3 | "…five wild monsters, could you tame them?" | `create_monsters`: draws the **monster_isle** bridges; spawns a **Monster House + 5 wild Monsters on Pirate Cove** (island 5) and a **Large Gold Hill on Monster Isle** (island 6) | 5-s check: ≥ 5 tame Monsters (Hired monsters) | 2000 wood |
| 6 | 4 | "If only I could [have] 500 gems … a little show" | — | Player gives 500 gems (Dancing Queen) | — |

The dance: when the intro's last page contains "little show for you" or the during text contains
"The offer still stands", and gems ≥ 500, the bubble gets **"Give 500 gems" / "Cancel"** buttons
(`QuestItem › Click`). Accepting runs `PeachItem › Dance`: fires `peach_dance`, subtracts 500 gems,
plays `piratepeach.swf`. (Because the mote completes the mission before `AdvanceState` runs, the
complete text is skipped — as coded.)

### B8. `set_up` actions (`scripts/Quests/MissionSetUps.as › CallSetup`)

| Action | Effect | Used by |
|---|---|---|
| `create_store` | Free **General Store** bubble at avatar (x, y+70); focus arrow on it | Coriander 16 |
| `create_pete` | Free **Pirate Pete** bubble, random spot in island 2 (Ginger Cove); focus arrow | Coriander 1 |
| `create_deer` | `MakeItemRandom("Deer")` ×3 in island 0 and ×3 in island 3 | Coriander 5 |
| `create_priest` | `Motes.TriggerMote("girl_quest_10")` | Coriander 7 |
| `create_panda` | Free **Panda** bubble at avatar (x, y+70); focus arrow | Peach 1 |
| `create_monsters` | `Bridges.DrawBridge("monster_isle")`; Monster House + 5 Monsters in island 5; Large Gold Hill in island 6 (all spawned directly) | Peach 3 |
| `create_peach` | Free Pirate Peach bubble at avatar | (unused by data) |
| `second_island`, `create_wedding`, `dress_store`, `pearl_store`, `crown_store`, `create_coriander` | **Empty functions** | ghost 1, Coriander 8/6/9/10 |

The timed objective checks (`timedChecks`, every 5000 ms from `MissionSetUps.Init`):

| Active mission | Test | Fires |
|---|---|---|
| Coriander id 5 | `Items.CountMapItems("deer") == 0` | `girl_quest_8` |
| Peach id 1 | any item named "Panda" whose `mWorkPlace` is `MillItem`/`LedgendMillItem` | `panda_sawmill` |
| Peach id 3 | count of `MonsterItem` with `mTame` ≥ 5 | `monster_tame` |
| Peach id 2 | `Items.GetCountByName("Legendary Plum Tree") ≥ 6` | `5_plum` |

### B9. Treasure chests (`scripts/MapItems/Treasure/TreasureChest.as`)

Quest rewards, Bank trades and Pete payouts use `SetValue(type, amount)` and open themselves at
once (`OpenChest` adds the resource with a floating number). Random chests use `SetRandom(kind)`
with value table `[1, 5, 10, 20, 50, 100, 500]`:

| Kind (source) | Type roll | Wood/gold value | Gem value |
|---|---|---|---|
| 1 (Apple Tree) | wood 50%, gold 40%, gems 10% | 1, 5, 10 | 1, 5, 10 |
| 2 (Pear Tree) | wood 30%, gold 50%, gems 20% | 1, 5 | 1, 5, 10, 20 |
| 3 (Plum Tree) | wood 33%, gold 33%, gems 34% | 1, 5, 10, 20, 50 | 1, 5, 10, 20, 50 |
| 4 (Tower, island saved) | wood 50%, gold 50% | 1, 5, 10 | — |

(Uniform choice within each value list.)

### B10. Fruit trees, and how Apple and Pear Trees are obtained

**Apple Tree and Pear Tree are in no store list.** They are obtained only by:

1. **Bridge purchases** spawn them directly (`StoreItems/BridgeAngelItem.as › purchase`):
   buying **Ginger Cove** spawns an **Apple Tree** on island 2; buying **Verdant Bay** spawns a
   **Pear Tree** on island 3; buying **Pirate Cove** spawns a **Pear Tree** on island 5.
2. **Shaking** an existing fruit tree can drop a **free bubble of the same tree** (below).
3. The 1% random bubble from a shake (`MakeRandomStore`) can be any `max 0` item, including these.

The Legendary Plum Tree is also sold in the Gem Store (100 gems).

**Fruit tree behaviour** (`scripts/MapItems/FruitTrees/FruitTree.as` + `States/`):

- Grows from scale 0.2 to 1.0 at 0.005/frame (160 frames), then can be shaken.
- Grows fruit (max **5** on the tree): each frame, chance `0.005 / d` to add one, where d = 2
  (apple), 2.5 (pear), 3 (plum). (`GoldTree`/`SilverTree` classes exist with /4 and /2 but are not in
  `store_items.xml`.)
- **Player click = shake** (`FruitTree › Click` → `RemoveFruit`): drops **all** fruit, each at a
  random point near the trunk. For each fruit, in order:
  1. If the global "seed ready" flag is set (armed 60 s after load, re-armed 60 s after each use —
     one per minute across **all** trees) → a **free bubble of this tree type** (Apple Tree / Pear
     Tree / Legendary Plum Tree). These bubbles **pop by themselves** after `(rand×10 + 1) × 200`
     frames (≈ 6.7–73 s) if not collected.
  2. Else 1% → a free bubble of a random `max 0` item (never expires).
  3. Else: Apple 20% chest (kind 1) / 80% a Posie **fruit** on the ground; Pear 30% chest (kind 2) /
     70% a Tulip fruit; Plum 20% chest (kind 3) / 80% a Rose fruit. Fruit on the ground is an edible
     flower node (food for workers and deer).
- **Worker/deer shaking**: hungry workers and deer treat a tree *with fruit* as food; they walk to it
  and shake off **one** fruit (`RemoveOne`) instead of eating it.
- **Chopping**: a lumber worker with no trees left in range will target a fruit tree
  (`AnimalType › AnimalMakePath("wood")` falls back to `FruitTree`); ground truth covers the result
  (500 wood, ⅓ Monster / ⅓ Deer / ⅓ live Bomb spawned, hint 60). The tree regrows from 0.9 at
  0.001/frame.

---

## C. Islands and bridges

### C1. The seven islands

`game.xml <islands>` gives each island one or two rects as fractions of the 4500-unit world.
**They do not define walkable land.** Land is a hard-coded list of **147 overlapping circles**
(`scripts/Land/Land.as › DrawEntireMap`, `mLandArray`; radii 150/198/300 = land-block scales
0.5/0.66/1.0 of 600) that already cover **all seven islands from the start**. The rects are only
used as spawn regions (`Land › DrawLand` → `mLandRectangles`, each shrunk by `WATER_GAP` = 340).
The `<item name="Avatar">` entry in island 0 is **not read**; the avatar is created at (500, 500)
(`World › InitGame`).

| # | Name (from code) | Rect(s), world units (x, y, w, h) | Land circles | What first appears there |
|---|---|---|---|---|
| 0 | Start island (unnamed) | (0, 0, 1125, 1485) | 10 | Tutorial items; 3 deer (Coriander 5) |
| 1 | "The secret heart of the Bunni Islands" (unnamed) | (0, 1485, 1125, 1485) | 10 | Tower of Love, Coriander, Ginger Cove ghost |
| 2 | **Ginger Cove** | (0, 2970, 2250, 1485) | 25 | Mine (focus), Medium Pine Forest, Large + Medium Gray Hill, Apple Tree, Pirate Cove + Verdant Bay ghosts, Pirate Pete (Coriander 1) |
| 3 | **Verdant Bay** | (2250, 2970, 2250, 1485) | 25 | Pirate Peach, Medium Maple Forest, Medium Black Hill, Medium Gray Hill, Pear Tree, Wedding Isle ghost; 3 deer (Coriander 5) |
| 4 | **Wedding Isle** | (3375, 0, 1125, 2970) | 21 | Nothing until the Church is bought (fixed at 3937, 1027) |
| 5 | **Pirate Cove** | (1125, 0, 2250, 1485) ∪ (1125, 0, 1125, 2970) — an L | 46 | Medium Ironwood Forest, Medium Maple Forest, Large Black Hill, Large Gray Hill, Pear Tree; later Monster House + 5 Monsters (Peach 3) |
| 6 | **Monster Isle** | (2250, 1485, 1125, 1485) | 10 | Large Gold Hill (Peach 3) |

Island names come from the bridge-ghost items and what they unlock (C3); the church card says
Wedding island is "in the far north east", which matches #4. Random spawns in island 5 use only its
first rect (the top strip). "Land circles" counts were made by bucketing the hard-coded circle
centres into the rects.

```
x→   0      1125     2250     3375     4500
y 0  ┌───────┬────────────────┬────────┐
     │   0   │   5 (Pirate Cove, top) │ 4      │
1485 ├───────┤  5  ├───────────┤Wedding │
     │   1   │(col)│  6 Monster │  Isle  │
2970 ├───────┴─────┼───────────┴────────┤
     │ 2 Ginger Cove│  3 Verdant Bay     │
4455 └──────────────┴────────────────────┘
```

### C2. Water, walking, and "locked" islands

- The avatar moves up to 20 units/frame (50 in map view) toward the held cursor and **may only
  step onto points inside a land circle** (`AvatarItem › update` → `TestCollision › NextStep` →
  `PointOnLand`), sliding around the coast by trying ±5° rotations.
- Islands are separated by water gaps, so an island without a bridge is **visible and fully drawn
  but unreachable**. There is **no fog, darkening or lock icon** in the code. Workers' paths are also
  land-checked (`AnimalType › FindItem`), so targets on another island are marked inaccessible.
- Placement also requires land (`CircleOnMap2`), so you can't build on an unconnected island
  either (you can't get there to drop anything).

### C3. Bridges (`scripts/Bridge/`, `Land › DrawBridge`)

Six bridges are defined in `save_data.xml <bridge>` (`name`, `id`, `loc` = one or more
`[x, y, horizontal]`, `collected`). Drawing a bridge adds, per `loc` entry, **5 land circles of
radius 150** spaced 150 apart centred on the point (along x if horizontal, along y if vertical),
each jittered ±50 perpendicular (cumulative), marked `hill: true` for shading. The whole background
is then re-rendered: screen tints white over 1 s, the camera jumps to the bridge, then fades back
over 3 s (`Bridge › DrawRedraw`). `collected` is saved, and collected bridges are redrawn on load.

| Bridge | loc | Connects | Opened by |
|---|---|---|---|
| first_bridge | (562, 1485) vertical | 0 ↔ 1 | Tutorial: clicking the first mote (`Tasks › TaskFindTower`) |
| second_bridge | (562, 2970) vertical | 1 ↔ 2 | Buying the **Ginger Cove** ghost |
| pete_isle | (2250, 3712) horizontal | 2 ↔ 3 | Buying the **Verdant Bay** ghost |
| pirate_cove | (1125, 2250) horiz. + (1687, 2970) vert. | 1 ↔ 5 and 2 ↔ 5 | Buying the **Pirate Cove** ghost |
| wedding_isle | (3937, 2970) vertical | 3 ↔ 4 | Buying the **Wedding Isle** ghost |
| monster_isle | (2812,1485) v, (2812,2970) v, (3375,2250) h, (2250,2250) h | 6 ↔ 5, 3, 4, 5 | Peach mission 3 intro (`create_monsters`) |

**Bridge ghosts** (`StoreItems/BridgeAngelItem.as`) are sale displays (not free) drawn with the
ghost art (ascend/hover/descend), showing the ghost's line as the item description. Buying one
subtracts the price, draws its bridge, spawns its contents, and the ghost descends and vanishes.

| Ghost item | Placed | Price | Its line (description) |
|---|---|---|---|
| Ginger Cove | island 1, tutorial | 100 wood + 100 gold | "I can let you through to the next island, Ginger Cove. For a small fee." |
| Verdant Bay | island 2, on buying Ginger Cove | **dynamic** (3·max-per-min + ¼ stock) | "You look lazy. I only let non-lazy bunnies through to Verdant Bay…" |
| Pirate Cove | island 2, on buying Ginger Cove | **100 gems** | "Peach's Pleasure Palace awaits on yonder island. It's a paid trip…" |
| Wedding Isle | island 3, on buying Verdant Bay | **dynamic** (5·max-per-min + ¼ stock) | "Wedding Isle was shut down due to the tragedy at the old church…" |
| Monster Isle | never placed | (100 + 100) | "Only the foolish visit Monster Isle…" |

What each purchase spawns: see the "What first appears" column in C1 (all via `MakeItemRandom`,
i.e. real grown items, except the Mine and the ghosts/Peach, which are `MakeStoreRandom`: the Mine
is a **free bubble**, the next ghosts are sale displays, Peach is a free bubble).

The only gate on the bridge ghosts is being able to reach and afford them. None requires a quest.

---

## D. Other animals and hazards

### D1. Worker types (`scripts/MapItems/Bunny/AnimalType.as`, `game.xml <animals>`)

`AnimalType` builds three speed arrays from the base speed, then overrides entries that have a
`<modifiers><item name="…"><speed>`:

- `mWoodSpeed = [pine, maple, ironwood]`
- `mRockSpeed = [unused, bronze, silver, gold]`
- `mFoodSpeed = [posie, tulip, rose]`

The array value is both the **walk speed to that target** (units/frame) and, **÷3**, the
**harvest speed** (scale removed per hit = 0.1 × value/3) — see ground truth.

| Type (`mAnimalTypeArray` idx) | Base | Pine | Maple | Ironwood | Bronze | Silver | Gold | Posie | Tulip | Rose | Food rate | Can target |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| bunni (0) | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 1 | all |
| pirate (1) | 4 | 4 | 4 | 4 | 4 | 4 | 4 | **8** | 4 | 4 | 1 | all |
| panda (2) | 2 | 2 | **8** | 2 | 2 | 2 | 2 | 2 | **8** | 2 | **2** | all |
| fox (3) | 6 | 6 | 6 | **12** | 6 | 6 | 6 | 6 | **12** | 6 | 1 | all |
| monster (4) | 6 | 6 | 6 | 6 | **12** | **12** | **12** | 6 | 6 | 6 | 1 | all (tame); trees+rocks (wild) |
| deer (5) | 5 | — | — | — | — | — | — | 5 | 5 | 5 | 0.005 | flowers + fruit trees only |

Notes:
- `find_first` is supported by the parser but **no `<find_first>` appears in the shipped
  `game.xml`**, so every "find first" list is empty and modifiers change speed only; target choice is
  "nearest child, else nearest parent" over all allowed types. (Correction to `BUNNI_GROUND_TRUTH.md`.)
- Food rate multiplies what a flower restores (`HarvestWood`: `price × mFoodRate`). A deer never
  restores hunger (see D3), so its 0.005 is unused in practice.
- Fruit trees (chopping/shaking) and "any" targets use the base speed.
- The pirate's description ("bent on discovering glittering treasure") has **no code**: a Pirate is
  an ordinary worker.

### D2. Houses and prices

| Animal | Price | Sold by | House price | How the house is obtained |
|---|---|---|---|---|
| Bunni | 10 wood + 10 gold | Bunni House | 500 + 500 | Tutorial free bubble |
| Fox | 30 wood + 15 gold | Fox House | 400 wood + 25 gold | Bronze Store |
| Monster | 1500 wood (spawns **wild**) | Monster House | 500 + 400 | Peach 3 set-up (spawned on Pirate Cove) |
| Panda | 5 wood + 3 gold | Panda House | 1000 + 300 | **No code path** (not sold anywhere, excluded from random bubbles by `max 1`) |
| Pirate | 10 gems | Pirate House | 15000 wood + 20 gems | **No code path** |
| Deer | 10 wood (spawns wild) | Deer House | 50 wood | **No code path** ("Never hire a deer.") |

Other ways to get animals: Panda — Peach 1 free bubble; any `max 0` animal (Bunni, Pirate, Panda,
Fox, Monster, Deer) — 1% fruit-shake random bubble; Monster/Deer — fruit-tree chop; Deer —
Coriander 5. Every house has `max 1`.

A newly spawned animal grows from 0.2 to 1.0 at +0.01/frame, 70 units off its spawn point
(`Animals/States/Born.as`). Bought workers then follow the avatar; wild ones start hunting.
Nothing starves to death — hunger only changes behaviour (`GenericAnimal › GetHungry`).

### D3. Deer

- Cannot be dragged (`DeerItem`: `mCanDrag = false`) and never works.
- `FindWood` for a deer always searches **food** with **no boundary** (whole map): posie, tulip and
  rose nodes (including dropped fruit), plus **fruit trees that currently have fruit**, which it
  shakes (one fruit falls) rather than eats. Trees and rocks are excluded (`can_find false`).
- When a deer finishes a flower, the flower is **destroyed with no benefit to anyone** — not even
  for the deer (the deer branch in `HarvestWood › HitFrame` comes before the eating branch), and a
  **parent garden eaten by a deer does not become a bubble**: the seeding source is gone for good.
- Walks and eats at speed 5 (removes 0.167 scale per hit); searches every 61 frames; if nothing is
  found it simply stands (it has no workplace to return to).
- 30 s after a deer spawns, hint 62: "A deer is eating all your trees! Find it and put a bomb under
  it's boooty to make it go boom." (The text says trees; the code says flowers.)
- Removed only by a Bomb or Deer Bomb.

### D4. Monsters: wild and tame

- **Wild** (`MonsterItem.mTame = false`): hunts **trees and rocks** (pine, maple, ironwood, bronze,
  silver, gold) anywhere on the map, walking at `int(6 / 5)` = **1 unit/frame**; harvest per hit is
  0.2 on trees and 0.4 on rocks; the node is **destroyed, yields nothing**, and a parent does **not**
  bubble. Wild monsters don't get hungry. Hint 61 after 30 s: "Angry monsters are stealing your
  rocks! Pay it some gems to get it to switch sides."
- **Taming** (`MonsterItem › TameMonster`): mouse-down on a wild monster. With ≥ 20 gems: "I'll work
  for life for 20 Gems and wait for you to login next time." [Pay 20 Gems] [Cancel]; with fewer:
  "I only work for people with gems." Paying sets `mTame`, subtracts 20 gems, and the monster joins
  the trail.
- **Tame**: an ordinary worker (base 6, rocks 12), gets hungry, can be assigned to a Mill or Mine.
- Sources of wild monsters: Monster House purchase (1500 wood — still wild, must also be tamed),
  Peach 3 (5 on Pirate Cove), fruit-tree chop, random bubble.

### D5. Bomb and Deer Bomb (`scripts/MapItems/Terrain/BombItem.as`, `BombStates/BombFuse.as`)

Both are placed like any item (`inner 20`, `outer 300`, 10 wood + 10 gold). On placement the fuse
runs: explosion sound at frame 85, **effect at frame 90 (3 s)**, animation until frame ~102, then
the bomb is removed. The effect hits every item whose **inner circle intersects the bomb's
300-unit outer circle**:

| | Deer / wild Monster | Everything else |
|---|---|---|
| **Deer Bomb** | destroyed (explode-out animation, 11 frames) | untouched |
| **Bomb** | destroyed, no bubble | **turned into a free bubble of itself** (same name, same mission id) and the original removed; a store building or house also deletes its sale displays |

The normal Bomb **never** touches: carried items, Pirate Peach, Pirate Pete, Coriander, the avatar,
sale displays and bubbles, the Church, the Priest, other bombs, or animals following the avatar.
Working animals *are* hit (they become e.g. a "Bunni" bubble). So a Bomb is also a
"pick everything up" tool. A live Bomb is also spawned directly by ⅓ of fruit-tree chops.

### D6. Monster Den and other decorations

`Monster Den` (Fancy Store, 400 wood + 1000 gold, inner 53) is a plain `Statue` subclass — **pure
decoration, no behaviour**; it does not spawn or house monsters. The same is true of every item in
`MapItems/Statues/` (White/Pink Flower Tree, Holy Statue, Gem Statue, Bushy Tree, Fall Color, Tiny
Bush, Little Greeny, Splash Fountain). Holy Statue's only effect is the `holy_statue` mote (Chappy).

### D7. Score

`HUD.mScore` only changes in two places: **+1 per avatar hop cycle** (every 22 animation frames,
`AvatarItem › update`) and **+50 per mote delivered to the Tower** (`TowerItem › MoteFound`). It is
shown in the HUD and can be submitted at the high-score NPC. Statue descriptions ("your score will
go up") and Chappy's line are **not implemented**.

---

## E. Mines, hills and size variants

### E1. Mine and Legendary Mine

| | Mine | Legendary Mine |
|---|---|---|
| Price | 100 wood + 50 gold (General Store) | 20 gems (Steel Store) |
| Class | `Building/MineItem.as` | `Building/LedgendMineItem.as` (subclass) |
| Work radius | fixed circle **800** around it (`mBoundry`) | same |
| Value multiplier | ×1 | **×2** (`HarvestWood`); card text says ×3 |
| Placement mote | `place_mine` (You Placed a Mine) | — (PlacementMote checks the exact name "Mine") |
| Delivery | `MineItem › PingRock`: floating number flies to the HUD over 2 s, then `HUD.AddRock` | same |

Sawmill (0 wood + 250 gold, Bronze Store) and Legendary Sawmill (10 gems, Silver Store) mirror this
for wood. The first Mine you get is the free bubble spawned on Ginger Cove when you buy that bridge.
A worker on a Mine targets only hills (bronze/silver/gold) in its 800 radius; on a Mill only forests
(then fruit trees as a last resort).

### E2. Hills (`scripts/MapItems/Terrain/HillItem.as`, `HillItems/*.as`)

| Hill | Class | Rock | Price per rock (`game.xml <price>`) | Seed countdown |
|---|---|---|---|---|
| Gray Hill | `BronzeItem` | bronze | 2 gold | 100 frames |
| Black Hill | `SilverItem` | silver | 7 gold | 50 frames |
| Gold Hill | `GoldItem` | gold | 20 gold | 25 frames |

(Each ×2 for a parent rock, ×2 at a Legendary Mine.) The Black Hill description's "some rare items
when mined" has **no code**; mining yields only gold.

**Size variants** change only the price and the **outer radius**; inner is 36 for every hill and
forest size (22 for every garden size). The outer radius is the circle children spawn in,
and it sets the number of initial spawn attempts:

| Size | Hill outer | Hill children tried (`outer/5`) | Forest outer | Forest children (`outer/10`) | Garden outer | Garden children (`outer/10`) |
|---|---|---|---|---|---|---|
| Small | 150 | 30 | 150 | 15 | 100 | 10 |
| Medium | 200 | 40 | 200 | 20 | 150 | 15 |
| Large | 300 | 60 | 300 | 30 | 200 | 20 |

**Hills use `outer / 5`, not `outer / 10`** (`HillItem › initStateMachine` vs `ForestItem` /
`GardenItem`). Each attempt only succeeds if the child's inner circle fits on clear land, so the
real count is lower. (Correction to `BUNNI_GROUND_TRUTH.md`, which says /10 for all.)

Hill prices:

| | Small | Medium | Large |
|---|---|---|---|
| Gray | 2 wood | 5 wood (General Store) | 12 wood + 25 gold |
| Black | 5 wood + 8 gold | 8 wood + 20 gold (Silver Store) | 10 wood + 30 gold |
| Gold | 5 gems (Silver Store) | 10 gems | 20 gems (Gold Store) |

Free hills: Large + Medium Gray on Ginger Cove; Medium Black + Medium Gray on Verdant Bay; Large
Black + Large Gray on Pirate Cove; Large Gold on Monster Isle (Peach 3).

---

## F. Tutorial (`scripts/Tutorial/Tasks.as`)

Hints go through `Tasks.HintTextEvent(id, text)`: Susanna the ghost's speech bubble fades in, shows
the text, fades out after 10 s; the same id can't repeat within 20 s. Delayed "nag" hints only
fire if the player still hasn't done the step. Steps are strictly ordered (each listener only
accepts the next expected item name). Tutorial spawns go in island 0 unless stated, as **free
bubbles** with the focus arrow on them.

| # | Trigger | What happens | Hint(s) |
|---|---|---|---|
| 0 | Game load | Login screen | "I'm Susanna the Ghost. Have you been to Bunni island before?" |
| 0a | New player starts | Survey scheduled at a random 5–45 min | 50: "Hi! Welcome to my desolate island. Unless you start building a home, you will soon be dead, just like me." · after 15 s if not moved, 10: "Click and hold to move" |
| 0b | Returning player | Tutorial skipped, autosave on, `welcome_back` mote | 51: "Welcome back <name>... I have restored your island." |
| 1 | Avatar moved (2nd move tick) | **Medium Pine Forest** bubble | after 5 s if not picked up, 11: "If you click on the tree bubble, you'll pick them up." |
| 2 | "Pine Tree" card dismissed | — | after 5 s if no pine exists, 12: "Planting time! Click and drag the tree to plant it." |
| 3 | Medium Pine Forest placed | **Sawmill** bubble | 52: "Still alive? Something horrible happened on these islands. You'll see lots of ghosts like me if you make it very far." · after 10 s if not picked up, 13: "Trees aren't good unless you harvest them and for that you'll need a sawmill" |
| 4 | "Sawmill" card dismissed | — | after 5 s if no *Mine* exists (bug: checks Mine, not Mill, so it usually fires), 14: "Pick up the sawmill and plant it near the trees. Remember: Click and Drag!" |
| 5 | Sawmill placed | **Bunni House** bubble | after 10 s if not picked up, 15: "Look a house! I wonder if you can place that somewhere." |
| 6 | "Bunni House" card dismissed | — | — |
| 7 | Bunni House placed | — | after 10 s if no bunni exists, 16: "Click on the Bunni to hire workers for your sawmill." |
| 8 | "Bunni" card dismissed (first bunni bought) | — | after 15 s if no Bunni works at a Mill, 17: "Click and drag the worker onto the mill. A yellow circle will appear under the mill." |
| 8b | A worker is dropped on a Sawmill (independent listener) | — | 53: "Before the end of everything, Grandpa Angel saved a few survivors in magical bubbles. You may come across a few here and there." |
| 9 | A hungry worker reaches its sleep spot with no food (`FindWood` → `BunniStarve`) | **Medium Posie Garden** bubble | 54: "Oh, look a hungry bunni. That is how most of us died. There just wasn't any food left. \*sigh\*" (plus the regular hint 1: "A Bunni ate the last flower in the garden…") |
| 10 | "Posies" card dismissed | — | — |
| 11 | Medium Posie Garden placed | `first_mote` fires (heart appears); **Tower of Love** spawned on island 1 | 55: "What? Is that a bit of love? All your building has begun to awaken the island spirits. Uh oh." · after 10 s, 19: "Ohh...the heart shaped object following you is a Mote. Click the Mote to collect it." — **never shows** (it tests `mCollected`, already true) |
| 12 | First mote clicked | **first_bridge** drawn (0↔1); **Ginger Cove** bridge ghost (sale display) and **Coriander** bubble on island 1 | 20: "How exciting! A new island is open. Let's go exploring. Click and hold to move your bunni to the next island." · Coriander hint 72 |
| 13 | Avatar y > 1500 (on island 1) | — | 56: "\*Gulp\* I've heard of this place. It is the secret heart of the Bunni Islands." · after 10 s if the Tower was never opened, 21: "Try to find a strange tower. Can you open it?" |
| 14 | Tower diary closed | **Tutorial done**, Save button enabled | 57: "If you get stuck, visit the tower! Bring love back to the island! You can now save your game if you wish." |

After the tutorial, progression is driven entirely by Coriander's chain (B6), which starts when the
player places her bubble and reads her first intro (General Store bubble).

Other hint ids used across the game (for completeness): 1/2/3 overharvest (no food / no trees /
no rock within a workplace), 4 free bubble 30 s, 60 fruit tree chopped, 61 wild monster, 62 deer,
70/71/72 quest-giver hints, 101 "Click Save in the menu to return to this point next time you
login" every 5 min until the first save.

---

## G. Dead or legacy code (do not implement)

These exist in the decompile but are never reached in the shipped game:

| Code | Why it's dead |
|---|---|
| `scripts/Missions/` (`Missions`, `MissionsPeach`, `Mission`) and `MapItems/GirlBunny/*`, `Building/GirlFriendsHouse.as` | Older quest system; `Missions.init` is never called and `GirlFriendsHouse` is not in `store_items.xml`. `Save.as` uses `Quests/` instead. |
| `Switches/Switch.as`, `Land › OpenFirst/OpenSecond/OpenPeteIsle/OpenPirateCove/OpenWeddingIsle/OpenMonsterIsle` | Superseded by `Bridge/`; only reachable from the legacy `Persistance` path. |
| `World › spawnMonster` (20/40/60% chance of a random Monster or Deer on islands 2/3/4) | No timer ever calls it, and its `Switch` flags are never set. |
| `Land.mCoveAngel` / `BeaverItem` bridges | `game.xml` has no `<bridges>` node. |
| `MapItems/Angel/Angels` (random angel jokes) | `Angels.init` is never called. |
| `FruitTrees/GoldTree`, `SilverTree`, `Types/*`, `GuardTowerItem` | Not in `store_items.xml`. |
| Monster Isle bridge ghost (id 91) | No code spawns it; the bridge opens via Peach 3. |

---

## H. Corrections to existing docs

- **`BUNNI_GROUND_TRUTH.md`**
  - Hills spawn `outer / 5` children, not `outer / 10` (E2).
  - "Find first" types are **not** the modifier types — `find_first` is absent from the shipped
    `game.xml`, so all find-first lists are empty (D1).
  - "Quest/ghost gating logic … has not been read yet" — now covered by section B.
- **`BUNNI_MECHANICS.md`** ("Island unlocking (ghosts)")
  - A quest's `wood`/`gold`/`gems` are **rewards** paid out as chests, not prices the ghost asks.
  - `set_up: second_island` is an **empty function**; it unlocks nothing.
  - Islands are unlocked by **paid bridge ghosts** (Ginger Cove, Verdant Bay, Pirate Cove, Wedding
    Isle), the tutorial (first bridge) and Peach quest 3 (Monster Isle) — not by quest ghosts.
  - The wiki's tutorial ghosts (Mr. Summers, Sir Edmunds, Bruce, Mr. Greenspun, Arty, "a mill costs
    50 coins", "three plum trees") **do not exist** in the shipped `save_data.xml` or code; the
    wiki likely documents a different build. The shipped tutorial is section F.
  - The avatar starts at hard-coded (500, 500); `game.xml`'s Avatar location is unused.

---

## I. Not covered by the sources

- **World-unit to pixel scale of the art** for inner/outer radii is not verified (same caveat as the
  ground truth).
- **Store-item art and the PurchaseMenu / TradeMenu layouts** are Flash symbols
  (`symbol177`, `symbol59`); their exact visual layout isn't in the AS3. Only the fields listed
  in A3/A10 are known.
- **Feedback-frame wording** for PurchaseMenu codes 1/2/3/5 lives in the symbol, not in code; the
  meanings above come from the conditions that select them.
- **Coriander's decoration frames** (`mDecoration` 0/80/160/240) are art offsets; which outfit is
  which frame range isn't in code.
- **Wedding and Peach dance animations** are external SWFs (`wedding.swf`, `piratepeach.swf`), not
  decompiled.
- The **high-score NPC** (`MapItems/HiScoreMan/HighScoreItem.as`) exists, but no code path that
  spawns it was found in this pass.
