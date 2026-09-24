# Bunni: ground truth from the decompiled game

Everything here was read out of the actual game code, not inferred from screenshots or the wiki.
Where the original is ambiguous or I am extrapolating, it says so explicitly.

**This supersedes guesses in `BUNNI_MECHANICS.md`.** That file stays useful for the story layer,
visual reference and playthrough notes. For anything numeric or behavioural, this file wins.

## Provenance

| What | Where |
|---|---|
| Game SWF | `~/Downloads/bunni/bunnibunni/LandOfBunnies.swf` |
| Decompiled AS3 (365 classes) | `~/Documents/bunniWiki/decompiled/LandOfBunnies/scripts/` |
| Balance config | `~/Downloads/bunni/bunnibunni/game.xml`, `store_items.xml` |
| Decompiler | FFDec 26.3.0 at `~/Documents/bunniWiki/tools/ffdec/` |

The decompiled source is kept **outside this repo on purpose** — it is someone else's code. Read it
to understand the design; do not copy it into `Assets/`.

To re-run the decompile:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.6f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java.exe" \
  -jar ~/Documents/bunniWiki/tools/ffdec/ffdec-cli.jar \
  -export script ~/Documents/bunniWiki/decompiled/LandOfBunnies \
  ~/Downloads/bunni/bunnibunni/LandOfBunnies.swf
```

## The tick model

The SWF runs at **30 fps, 800×600**. Every rate below is *per frame*, so divide by 30 for seconds.
This answers the old open question: `speed` values in `game.xml` are not seconds and not
multipliers on a timer — they are **scale removed per animation hit-frame**. See Harvesting.

`<speedup>` ships as `1` and the designer's own comment says it is not accurate. Ignore it.

## Resource nodes

Every tree, flower and rock is one node with a `scale` value that doubles as its health.

### Parent vs child — the single most important structural fact

When you place a garden/forest/hill from the store you create **one parent node**. The parent
immediately spawns `outerRadius / 10` children at random points inside its outer circle (each only
if the child's inner circle still fits on the map). Radii come from the `inner`/`outer` fields in
`store_items.xml`.

- **Only parents seed.** Children never reproduce.
- Parents cycle `WaitTillSeed → SetSeed → WaitTillSeed`, adding one child per cycle.
- Harvesting a **child** yields its base value and it is gone.
- Harvesting a **parent** yields **double** value *and* re-creates the entire cluster at that spot.

That last point matters: a grove cannot be permanently destroyed by harvesting the parent — it
regenerates. What actually strips the land is workers eating children faster than the parent seeds.

### Growth

A new node enters at scale `0.2` and gains `0.005`/frame (`0.15`/sec) until it reaches its final
scale, at which point it becomes harvestable. Final scale is randomised per node:

| | Parent | Child |
|---|---|---|
| Trees & flowers | 1.25 – 1.75 | 0.75 – 1.25 |
| Rocks/hills | 1.0 – 1.5 | 0.5 – 1.0 |

So a child tree takes ~3.7–7.0 s to mature, a parent tree ~7.0–10.3 s; child rock ~2.0–5.3 s,
parent rock ~5.3–8.7 s.

### Seed rates

`<seedrates>` is a **frame countdown, so lower = faster**. This reads backwards at first glance:

| Resource | Frames | ≈ seconds |
|---|---|---|
| Pine / Bronze | 100 | 3.3 |
| Maple / Silver | 50 | 1.7 |
| Ironwood / Gold | 25 | 0.8 |
| Posie | 300 | 10.0 |
| Tulip | 200 | 6.7 |
| Rose | 100 | 3.3 |

The valuable resources reseed *fastest*. The scarcity pressure comes from the harvest side, not the
growth side — see below.

## Harvesting

Each hit-frame of the chop animation does:

```
node.scale -= 0.1 * harvestSpeed
```

The node dies at `scale <= 0`. `harvestSpeed` is looked up from the animal-vs-resource `modifiers`
table in `game.xml`. So **hits to fell a node = finalScale / (0.1 × speed)**.

| Worker | Base speed | Boosted against |
|---|---|---|
| Bunni | 3 | — |
| Pirate | 4 | Posie 8 |
| Panda | 2 | Maple 8, Tulip 8 |
| Fox | 6 | Tulip 12, Ironwood 12 |
| Monster | 6 | Bronze/Silver/Gold 12 |
| Deer | 5 | — (destroys, cannot harvest) |

This is where overharvesting actually comes from. A fox on ironwood removes `1.2` scale per hit —
it one-shots any ironwood tree. A bunni on the same tree needs five hits. Ironwood reseeds quickly,
but a fox strips it faster than it can reseed. That is the whole tension the designer's price
comments refer to.

### Value

```
value = base price (game.xml <price>)
      × 2 if the node was a parent
      × 2 if the worker's workplace is a Legendary Mill / Legendary Mine
```

Both stack, so a parent ironwood delivered to a Legendary Mill is worth 4×.

## The worker job loop

A finite state machine, one per animal:

```
FindWood ──► GetWood ──► HarvestWood ──► TakeWoodToMill ──► FindWood
```

- **FindWood** picks a target by priority:
  1. untamed monster → hunts `monster`
  2. deer → seeks `food` (anywhere, ignores boundaries)
  3. **hungry** → seeks `food` *within its workplace boundary*
  4. workplace is a Mill → seeks `wood`; a Mine → seeks `rock`
- If nothing is found it walks back to its workplace and sleeps at a random point ~70 units off,
  playing a dream animation for whatever it lacks, and fires the overharvest hint text.
- **GetWood** walks to the target; **HarvestWood** chops; **TakeWoodToMill** delivers the value.

Note the boundary asymmetry: a hungry worker only looks for food inside its own workplace radius,
but a deer roams freely. Gardens must be placed within a workplace's radius to feed its workers.

## Hunger

| | |
|---|---|
| Starting hunger | 1500 |
| Drain | −1 per frame while active (walking, chopping) |
| Becomes hungry below | 1000 |
| Stops being hungry at/above | 1000 |

Hunger only drains while the worker is doing something — sleeping is free. From full, a worker
works ~500 frames (~17 s) before it first needs to eat.

Eating restores `flowerPrice × foodRate`:

| Flower | Restores (foodRate 1) |
|---|---|
| Posie | 500 |
| Tulip | 750 |
| Rose | 1600 |

`foodRate` is 1 for most animals; panda is 2 (eats double), deer 0.005. There is **no upper cap** on
hunger, so a rose banks a worker far past 1500 — that is why roses feel so strong.

Becoming hungry **interrupts the current job immediately** and restarts the FSM at FindWood.

## Hazards

- **Fruit trees** can be *shaken* (harvest one fruit, tree survives) or *chopped* (destroyed,
  value 500). Chopping rolls evenly: **⅓ monster, ⅓ deer, ⅓ bomb** spawned on the spot.
- **Deer and untamed monsters** destroy nodes outright and yield nothing.

## Corrections to earlier assumptions

- Harvest is **not** time-based. It is hit-frames removing scale. A "faster" worker fells in fewer
  swings, not in less time per swing.
- Seed rate is a **frame countdown**, so a lower number is faster. Ironwood/gold reseed fastest.
- The rose is **not** non-renewable in code. It seeds exactly like posie and tulip, and faster than
  both. The designer's "non-renewable" comment describes intended *economic* feel (huge food value
  invites stripping), not a special code path. Do not implement a rose that fails to regrow.
- Harvesting a parent node **regenerates the whole cluster** rather than removing it.
- Panda's `food: 2` is a multiplier on what it *gains* from eating, not a per-turn cost.

## Still genuinely unknown

- Exact placement radii per store item are in `store_items.xml` (`inner`/`outer`) but their
  world-unit scale relative to the 4500-unit world is not verified against the visuals.
- Quest/ghost gating logic lives in `Quests/` and has not been read yet.
