# Bunni: How We First Met - mechanics spec

Extracted from the original game's own data files rather than from play observation, so these
are the shipped values. Sources: `game.xml` (balance), `store_items.xml` (83 buildable items),
`save_data.xml` (story/quest data), from <https://archive.org/download/bunnibunni>.
Raw copies are in `docs/reference/`.

## Starting state

| Value | Amount |
|---|---|
| Starting wood | 1000 |
| Starting gold (XML: `startrock`) | 1500 |
| Starting gems | 0 (no startgem key; gems are earned) |
| Starting bunnies | 0 (confirmed by play-test) |
| World size | 4500 square units |
| Minimap | false |

The world is one square map of 4500 units holding 7 islands, each defined as normalized rects
(fractions of world size). The player avatar starts on island 1 at (0.5, 0.3) within that
island. Islands connect via **bridges** that must be unlocked - `save_data.xml` lists 6
(first_bridge, second_bridge, pete_isle, pirate_cove, wedding_isle, monster_isle), each with
explicit world coordinates.

## Resources

Three spendable currencies: **wood**, **gold**, **gem**. They come from harvesting *source
items* placed on the map. Each source has a yield value and a regrowth (seed) rate.

> **Naming.** The XML calls the second currency `rock` (`startrock`, `costrock`), but the game's
> HUD shows it as **gold** with a coin icon. Field names below keep the XML spelling; the player
> sees gold. Bronze/silver/gold hills are what you mine it from.

| Source | Category | Yield | Seed rate |
|---|---|---|---|
| Pine | wood | 3 | 100 |
| Maple | wood | 7 | 50 |
| Ironwood | wood | 15 | 25 |
| Bronze | rock | 2 | 100 |
| Silver | rock | 7 | 50 |
| Gold | rock | 20 | 25 |
| Posie | flower / food | 500 | 300 |
| Tulip | flower / food | 750 | 200 |
| Rose | flower / food | 1600 | 100 |

**Lower seed rate = rarer and slower to regrow.** The designers left intent comments in
`game.xml` that are worth carrying over:

- Pine - "staple that is moderately valuable, and not easy to overharvest"
- Ironwood - "very valuable, but very easy to overharvest"
- Bronze/Silver/Gold - "stone value is less important than finding the right treasure hill"
- Posie - "basic flower, inexpensive, easy to overharvest"
- Tulip - "staple flower, hard to overharvest"
- Rose - "non-renewable, easy to overharvest, but it takes you a while to realize that you are"

Flowers are valued far above wood and stone (500-1600) because they are food, not currency.

## Animals and the job system

> **Corrected by play-testing.** Reading the XML alone suggested there was no job assignment.
> There is: workers are dragged onto a **workplace building** (Sawmill for wood, Mine for gold).
> Without a mill in range, a forest yields nothing. See the play-test findings at the end.

Each animal has a base **speed** (harvest rate) plus per-item **modifiers** that override it for
specific resources. Species specialization sits *on top of* the workplace assignment: you choose
which animal to station at which mill, and the right species there works several times faster.

| Animal | Base speed | Food/turn | Specializations |
|---|---|---|---|
| Bunni | 3 | - | none (generalist) |
| Pirate | 4 | - | Posie x8 |
| Panda | 2 | 2 | Maple x8, Tulip x8 |
| Fox | 6 | - | Tulip x12, Ironwood x12 |
| Monster | 6 | - | Bronze x12, Silver x12, Gold x12 |
| Deer | 5 | 0.005 | **cannot** harvest any wood or ore |

**Deer are pests, not workers.** `can_find: false` on all six wood and ore types means they only
consume gardens. The store sells a "Deer Bomb" and a "Bomb" ("a great way to eliminate unwanted
deer") to deal with them.

Pandas cost 2 food per turn against a base speed of 2 - expensive and slow, unless placed on
maple or tulip where they become the best worker in the game.

## Placement

Every store item carries `inner` and `outer` radii, so placement is **free-position, not grid**
(confirmed by play-test: drag-and-drop, no snapping, blocked only by another item's footprint or
by water, where the item slides along the shoreline).
`inner` is the footprint (collision - nothing else may overlap) and `outer` is the effect or
attraction radius. Gardens keep inner at 22 while outer scales 100-200 with size, so a bigger
garden feeds animals from further away without occupying more ground.

Items also carry `frame` (which sprite variant to draw), `persistent` (survives save/load) and
`max` (how many may exist; 0 means unlimited).

## Store progression

Stores unlock in a chain, each tier selling the next tier's store as an item:

```
General -> Bronze -> Silver -> Steel -> Gold -> Gem
                       |
                       +-> Bank (buy gems / gold / wood)
```

**general store** - Small Posie Garden, Medium Posie Garden, Medium Pine Forest, Medium Gray Hill, Mine, Bronze Store

**bronze store** - Deer Bomb, Medium Tulip Garden, Medium Maple Forest, Fox House, Sawmill, Silver Store

**silver store** - Legendary Sawmill, Medium Black Hill, Wedding Dress, Small Rose Garden, Bomb, Small Gold Hill, Steel Store

**steel store** - Legendary Gem Store, Legendary Mine, String of Pearls, Bank, Medium Ironwood Forest, Gold Store

**bank** - Buy Gems, Buy Gold, Buy Wood

**gold store** - Large Gold Hill, Large Ironwood Forest, Medium Rose Garden, Tiara, Church

**gem store** - Large Rose Garden, Legendary Plum Tree, White Flower Tree, Pink Flower Tree, Holy Statue, Gem Statue

**fancy store** - Bushy Tree, Fall Color, Tiny Bush, Little Greeny, Splash Fountain, Monster Den

## Full item catalogue (83 items)

### Statues (10)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| White Flower Tree | 1500 | 1000 | 0 | 41 | 0 | unlimited |
| Pink Flower Tree | 1000 | 2000 | 0 | 41 | 0 | unlimited |
| Holy Statue | 5000 | 10000 | 0 | 41 | 0 | unlimited |
| Gem Statue | 0 | 0 | 50 | 41 | 0 | unlimited |
| Bushy Tree | 500 | 200 | 0 | 50 | 0 | unlimited |
| Fall Color | 0 | 0 | 70 | 50 | 0 | unlimited |
| Tiny Bush | 0 | 0 | 2 | 31 | 0 | unlimited |
| Little Greeny | 0 | 0 | 120 | 40 | 0 | unlimited |
| Splash Fountain | 0 | 0 | 300 | 65 | 0 | unlimited |
| Monster Den | 400 | 1000 | 0 | 53 | 0 | unlimited |

### Terrain.GardenItems (9)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Small Posie Garden | 1 | 3 | 0 | 22 | 100 | unlimited |
| Medium Posie Garden | 5 | 1 | 0 | 22 | 150 | unlimited |
| Large Posie Garden | 25 | 10 | 0 | 22 | 200 | unlimited |
| Small Tulip Garden | 5 | 3 | 0 | 22 | 100 | unlimited |
| Medium Tulip Garden | 20 | 5 | 0 | 22 | 150 | unlimited |
| Large Tulip Garden | 30 | 15 | 0 | 22 | 200 | unlimited |
| Small Rose Garden | 0 | 0 | 5 | 22 | 100 | unlimited |
| Medium Rose Garden | 0 | 0 | 10 | 22 | 150 | unlimited |
| Large Rose Garden | 0 | 0 | 20 | 22 | 200 | unlimited |

### Terrain.ForestItems (9)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Small Pine Forest | 0 | 2 | 0 | 36 | 150 | unlimited |
| Medium Pine Forest | 0 | 5 | 0 | 36 | 200 | unlimited |
| Large Pine Forest | 25 | 12 | 0 | 36 | 300 | unlimited |
| Small Maple Forest | 8 | 3 | 0 | 36 | 150 | unlimited |
| Medium Maple Forest | 10 | 8 | 0 | 36 | 200 | unlimited |
| Large Maple Forest | 20 | 10 | 0 | 36 | 300 | unlimited |
| Small Ironwood Forest | 0 | 0 | 5 | 36 | 150 | unlimited |
| Medium Ironwood Forest | 0 | 0 | 10 | 36 | 200 | unlimited |
| Large Ironwood Forest | 0 | 0 | 20 | 36 | 300 | unlimited |

### Terrain.HillItems (9)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Small Gray Hill | 2 | 0 | 0 | 36 | 150 | unlimited |
| Medium Gray Hill | 5 | 0 | 0 | 36 | 200 | unlimited |
| Large Gray Hill | 12 | 25 | 0 | 36 | 300 | unlimited |
| Small Black Hill | 5 | 8 | 0 | 36 | 150 | unlimited |
| Medium Black Hill | 8 | 20 | 0 | 36 | 200 | unlimited |
| Large Black Hill | 10 | 30 | 0 | 36 | 300 | unlimited |
| Small Gold Hill | 0 | 0 | 5 | 36 | 150 | unlimited |
| Medium Gold Hill | 0 | 0 | 10 | 36 | 200 | unlimited |
| Large Gold Hill | 0 | 0 | 20 | 36 | 300 | unlimited |

### Building.Stores (8)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| General Store | 25 | 25 | 0 | 41 | 0 | 1 |
| Bronze Store | 200 | 900 | 0 | 41 | 0 | 1 |
| Steel Store | 12250 | 125 | 0 | 41 | 0 | 1 |
| Silver Store | 1750 | 1150 | 0 | 41 | 0 | 1 |
| Gold Store | 12000 | 11500 | 0 | 41 | 0 | 1 |
| Legendary Gem Store | 0 | 0 | 100 | 41 | 0 | 1 |
| Bank | 0 | 0 | 5 | 41 | 0 | 1 |
| Fancy Store | 100 | 100 | 0 | 41 | 0 | 1 |

### StoreItems (8)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Verdant Bay | 100 | 100 | 0 | 0 | 0 | 1 |
| Ginger Cove | 100 | 100 | 0 | 0 | 0 | 1 |
| Wedding Isle | 100 | 100 | 0 | 0 | 0 | 1 |
| Pirate Cove | 0 | 0 | 100 | 0 | 0 | 1 |
| Monster Isle | 100 | 100 | 0 | 0 | 0 | 1 |
| Buy Gems | 0 | 0 | 0 | 10 | 0 | 1 |
| Buy Wood | 0 | 0 | 0 | 10 | 0 | 1 |
| Buy Gold | 0 | 0 | 0 | 10 | 0 | 1 |

### Building.Houses (6)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Bunni House | 500 | 500 | 0 | 41 | 0 | 1 |
| Pirate House | 15000 | 0 | 20 | 41 | 0 | 1 |
| Panda House | 1000 | 300 | 0 | 41 | 0 | 1 |
| Fox House | 400 | 25 | 0 | 41 | 0 | 1 |
| Monster House | 500 | 400 | 0 | 41 | 0 | 1 |
| Deer House | 50 | 0 | 0 | 41 | 0 | 1 |

### Animals (6)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Bunni | 10 | 10 | 0 | 0 | 0 | unlimited |
| Pirate | 0 | 0 | 10 | 0 | 0 | unlimited |
| Panda | 5 | 3 | 0 | 0 | 0 | unlimited |
| Fox | 30 | 15 | 0 | 0 | 0 | unlimited |
| Monster | 1500 | 0 | 0 | 0 | 0 | unlimited |
| Deer | 10 | 0 | 0 | 0 | 0 | unlimited |

### Building (5)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Sawmill | 0 | 250 | 0 | 41 | 0 | unlimited |
| Mine | 100 | 50 | 0 | 41 | 0 | unlimited |
| Church | 2500 | 3000 | 0 | 92 | 0 | 1 |
| Legendary Sawmill | 0 | 0 | 10 | 41 | 0 | unlimited |
| Legendary Mine | 0 | 0 | 20 | 41 | 0 | unlimited |

### FruitTrees (3)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Apple Tree | 500 | 250 | 0 | 50 | 0 | unlimited |
| Pear Tree | 1000 | 250 | 0 | 50 | 0 | unlimited |
| Legendary Plum Tree | 0 | 0 | 100 | 50 | 0 | unlimited |

### Decorations (3)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Wedding Dress | 100 | 100 | 0 | 0 | 0 | 1 |
| String of Pearls | 100 | 100 | 0 | 0 | 0 | 1 |
| Tiara | 100 | 100 | 0 | 0 | 0 | 1 |

### Terrain (2)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Bomb | 10 | 10 | 0 | 20 | 300 | unlimited |
| Deer Bomb | 10 | 10 | 0 | 20 | 300 | unlimited |

### Tower (1)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Tower Of Love | 0 | 0 | 200 | 83 | 0 | 1 |

### Ghosts (1)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Ghost | 100 | 100 | 0 | 30 | 0 | 1 |

### PiratePete (1)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Pirate Pete | 100 | 100 | 0 | 0 | 0 | 1 |

### Peach (1)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Pirate Peach | 100 | 100 | 0 | 30 | 0 | 1 |

### Coriander (1)

| Item | Wood | Rock | Gem | Inner | Outer | Max |
|---|---|---|---|---|---|---|
| Coriander | 100 | 100 | 0 | 30 | 0 | 1 |

## Story layer

`save_data.xml` holds narrative scaffolding rather than tuning: 19 **motes** (story beats, each
with intro / during / complete text, a `reward_item` and a `complete_mote` link), 6 bridges,
3 ghosts, and dialogue for the named characters Coriander, Peach and Pirate Pete. Motes are the
quest chain that gates progression.

## Open questions (not answered by the data files)

- **Harvest timing.** `speed` is a relative rate, not seconds. The conversion to real time lives
  in the SWF, not the XML. Needs observation or a decompile.
- **Input.** Whether placement is drag-and-drop or click-then-place is presentation, not data.
- **`speedup`** ships as 1, with a comment calling it "NOT ACCURATE SPEED UP... not suitable for
  final balancing".
- **Food economy.** Pandas cost 2/turn and deer 0.005/turn, but what a "turn" is, and how food is
  drawn from gardens, is not in the XML.

### Play-test findings (2026-09-23)

Observed by playing a fresh game. The archive.org emulator boots the wrong SWF (`vector_art.swf`,
an art library) and hangs on "Loading Art". The findings below come from the real entry point,
`LandOfBunnies.swf`, served locally with its sibling files in Ruffle 0.6.0. Timestamps come from
reading the HUD's wood-counter pixels every 250 ms.

#### 1. A single harvest, timed

**Setup:** 1 Bunni assigned to 1 Sawmill, next to the tutorial's free Medium Pine Forest (5 trees).
A Medium Posie Garden sits within range so the bunni stays fed.

**Yield: +3 wood per trip**, every time (19 trips across the session, 990 → 1047 wood). This matches
the tutorial card "Pine + Sawmill + Bunni = 3 wood" and `price/pine = 3` in `game.xml`. So the
`price` block is the yield of one trip, not a sale value.

**Cycle: ~45 s per trip (median 45.7 s, mean 48.6 s).** Six consecutive trips, timed from one
deposit to the next:

| Deposit at (s) | Wood | Interval (s) |
|---|---|---|
| 539.2 | 1029 | - |
| 582.5 | 1032 | 43.3 |
| 627.5 | 1035 | 45.0 |
| 677.7 | 1038 | 50.2 |
| 750.2 | 1041 | 72.5 (includes a detour to eat) |
| 784.2 | 1044 | 34.0 |
| 830.5 | 1047 | 46.3 |

- **Throughput:** 18 wood in 291.3 s, about 3.7 wood/min for one pine bunni.
- **Loop:** the bunni walks to a tree with an axe out, chops, carries a log back to the mill, and
  the wood is credited on arrival. Trip length depends on how far the chosen tree is. The first
  trip after assignment finished in under 12 s because the trees were right next to the mill.
- **Hunger stops work.** With no food placed, the first bunni stopped after 2 deliveries. It fell
  asleep ("z") beside the mill with a posie in a thought bubble. After a posie garden went down
  in range, it walked over, ate, and was back to chopping in about 9 s. Fed bunnies still detour
  to eat now and then; the 72.5 s interval above is one of those detours.
- **Caveat:** the SWF runs at 30 fps and uses both `ENTER_FRAME` and `getTimer`. If the game logic
  is frame-driven, these times are only as accurate as Ruffle's frame pacing.

#### 2. How placing an item works

- **Getting an item:** new items appear as floating bubbles on the map with an arrow above them.
  Clicking one shows a "You found a new item!" card, and the item then sits on a small pedestal
  at the avatar's feet. Items bought from a shop land in the same spot (see Bunni House below).
- **Input is drag-and-drop, not click-then-place.** You press on the held item and drag it into
  place ("Planting time! Click and drag the tree to plant it"; hover hint: "Click and hold to
  place **X**"). Clicking empty ground does nothing. The avatar is draggable too, which is how
  you move it, and the camera follows the avatar.
- **Preview while dragging:**
  - The real sprite follows the pointer at full opacity.
  - The rest of the screen dims.
  - A dark flattened ellipse (2:1 isometric) shows the item's `outer` radius. A Medium Pine
    Forest's ellipse measured about 400 px wide, so `outer: 200` is a radius.
  - Every other item shows a dark footprint shadow, which is its `inner` collision circle.
  - A status line reads "You can place your **X** here".
  - Dragging near the view's edge scrolls the camera.
- **Snapping: no grid.** Position is free. The one constraint is land: drag the pointer out
  over water and the item stops at the nearest point on the shoreline. It never shows an invalid
  state for water; it just slides along the coast.
- **Invalid spot (overlapping another item's footprint):**
  - The item turns into a faded, translucent ghost.
  - The status reads "Placement is blocked".
  - The avatar lights up with a yellow ring, marking it as a place to put the item back.
  - Releasing there cancels the drop with "You dropped a **X** item back into your inventory".
    The item goes back to the avatar's feet, and nothing is placed or charged.
- **Valid release:** you see "You created a **Medium Pine Forest**!"
  - One forest item scatters **5 separate trees** around the drop point. They grow from saplings
    to full size in about 4 s.
  - A posie garden likewise drops several separate flower clumps.
  - Forests spread over time; a new sapling appeared nearby within about a minute.
- **Assigning jobs uses the same drag.**
  - The Bunni House is a shop. Clicking the bunni on its pedestal opens a
    Cancel/Purchase dialog (Bunni: 10 wood, 10 gold, 0 gems), and a bought bunni lands at the
    avatar's feet.
  - Dragging a bunni makes each valid workplace glow with a yellow ring under it. The status
    reads "Place your **Bunni** on **Sawmill**", or "…on **You**" over the avatar.
  - Dropping it on the Sawmill gives "You set a **Bunni** to work on a **Sawmill**", and the HUD
    bunni counter goes up by 1.

#### Corrections to the data-derived sections above

- **The second currency is gold, not rock.** The HUD shows 1000 wood / 1500 gold (coin icon) /
  0 gems. `startrock` / `costrock` are gold.
- **There is a job system.** Workers are assigned by dragging them onto a Sawmill (wood) or a
  Mine (gold). Without a mill, a forest yields nothing. The Bunni card says: "Add a Bunni to a
  Sawmill to harvest wood. Add a Bunni to a Mine to harvest gold."
- **New-game opening:** 1000 wood, 1500 gold, 0 gems, 0 bunnies, on island 1 with an empty map.
  The tutorial then hands out free items in this order: Medium Pine Forest → Sawmill →
  Bunni House → Medium Posie Garden. A bunni costs 10 wood + 10 gold.
