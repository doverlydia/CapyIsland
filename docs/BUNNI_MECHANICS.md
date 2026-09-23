# Bunni: How We First Met - mechanics spec

Extracted from the original game's own data files rather than from play observation, so these
are the shipped values. Sources: `game.xml` (balance), `store_items.xml` (83 buildable items),
`save_data.xml` (story/quest data), from <https://archive.org/download/bunnibunni>.
Raw copies are in `docs/reference/`.

## Starting state

| Value | Amount |
|---|---|
| Starting wood | 1000 |
| Starting rock | 1500 |
| Starting gems | 0 (no startgem key; gems are earned) |
| World size | 4500 square units |
| Minimap | false |

The world is one square map of 4500 units holding 7 islands, each defined as normalized rects
(fractions of world size). The player avatar starts on island 1 at (0.5, 0.3) within that
island. Islands connect via **bridges** that must be unlocked - `save_data.xml` lists 6
(first_bridge, second_bridge, pete_isle, pirate_cove, wedding_isle, monster_isle), each with
explicit world coordinates.

## Resources

Three spendable currencies: **wood**, **rock**, **gem**. They come from harvesting *source
items* placed on the map. Each source has a yield value and a regrowth (seed) rate.

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

There is no job-assignment table. Each animal has a base **speed** (harvest rate) plus per-item
**modifiers** that override it for specific resources. An animal near a resource it specializes
in works several times faster. That is the entire job system: specialization by species, not
assignment by the player.

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

Every store item carries `inner` and `outer` radii, so placement is **free-position, not grid**.
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
