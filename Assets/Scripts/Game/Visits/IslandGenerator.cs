using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Builds a believable island for a pretend player: what someone at their level would have
    /// built, laid out on the same archipelago, with groves grown in, workers at their mills and a
    /// pest or two. Their gardens have all been eaten bare, so their workers soon go hungry: the
    /// flowers come from you — carried from home, popped from a bubble there, or bought in their shop.
    ///
    /// Everything is placed into a <see cref="SaveData"/>, so arriving is just loading a save. The
    /// same seed always gives the same island; a friend's island is kept and grows from there.
    /// </summary>
    public static class IslandGenerator
    {
        public const int MaxLevel = 5;

        // Which bridges are open at each level, and which islands they reach.
        static readonly string[] Bridges = { "first_bridge", "second_bridge", "pete_isle", "pirate_cove" };
        static readonly int[] IslandsByLevel = { 0, 1, 2, 3, 5 };

        // What each level adds: (item, island slot within the level's new island(s), worker species or null).
        static readonly string[][] Additions =
        {
            new[] { "Capy House", "Sawmill", "Medium Pine Forest", "Medium Pine Forest", "Little Greeny", "General Store", "Apple Tree", "Small Gray Hill" },
            new[] { "Mine", "Medium Gray Hill", "Large Gray Hill", "Bronze Store", "Tiny Bush", "Fox House", "Small Maple Forest", "Bushy Tree" },
            new[] { "Medium Maple Forest", "Sawmill", "Silver Store", "Medium Black Hill", "White Flower Tree", "Pear Tree", "Bushy Tree", "Tiny Bush" },
            new[] { "Steel Store", "Large Ironwood Forest", "Legendary Sawmill", "Fall Color", "Splash Fountain", "Pink Flower Tree", "Mine", "Large Black Hill" },
            new[] { "Gold Store", "Large Gold Hill", "Legendary Mine", "Pink Flower Tree", "Holy Statue", "Gem Statue", "Legendary Plum Tree", "Fall Color" },
        };

        /// <summary>Things to grow by between visits, at the friend's current level or below.</summary>
        static readonly string[] Growth =
        {
            "Medium Pine Forest", "Fall Color", "Bushy Tree", "Tiny Bush", "Medium Maple Forest", "White Flower Tree",
            "Little Greeny", "Medium Gray Hill", "Apple Tree", "Gem Statue", "Large Ironwood Forest", "Splash Fountain",
        };

        class Spot { public Vector2 at; public float radius; }

        public static SaveData Generate(FriendProfile who)
        {
            var land = LandMap.Instance;
            var d = new SaveData();
            if (land == null) return d;

            var saved = Random.state;
            Random.InitState(who.seed);
            var taken = new List<Spot>();
            int level = Mathf.Clamp(who.level, 1, MaxLevel);

            for (int i = 0; i < level - 1 && i < Bridges.Length; i++) d.bridges.Add(Bridges[i]);
            d.king = Find(land, 0, 30f, taken, reserve: false);

            var workplaces = new List<int>();
            for (int l = 0; l < level; l++)
            {
                int island = IslandsByLevel[l];
                foreach (var name in Additions[l]) Place(d, land, name, island, taken, workplaces);
            }

            // Workers: two at every mill and mine, a mix of species as the level allows.
            string[] species = level >= 4 ? new[] { "Capybara", "Capybara", "Fox", "Panda", "Monster" }
                             : level >= 2 ? new[] { "Capybara", "Capybara", "Fox" }
                             : new[] { "Capybara" };
            int hungry = 0;
            foreach (int index in workplaces)
            {
                var wp = d.placed[index];
                for (int k = 0; k < 2; k++)
                {
                    string kind = species[Random.Range(0, species.Length)];
                    // Some workers have run out of food: those are the ones you can help.
                    bool starving = hungry < 2 && Random.value < 0.45f;
                    if (starving) hungry++;
                    d.animals.Add(new AnimalSave
                    {
                        name = kind, tame = true, workplace = index,
                        pos = wp.pos + Random.insideUnitCircle * 1.5f,
                        hunger = starving ? 0 : Random.Range(1100, 1500),
                    });
                }
            }

            // Pests to chase off, and a little something lying in a bubble.
            int pests = Random.Range(1, 3) + (level >= 3 ? 1 : 0);
            for (int i = 0; i < pests; i++)
            {
                int island = IslandsByLevel[Random.Range(0, level)];
                string pest = level >= 3 && i == pests - 1 ? "Monster" : "Deer";
                d.animals.Add(new AnimalSave { name = pest, pos = Find(land, island, 20f, taken, reserve: false), tame = false });
            }
            d.bubbles.Add(new BubbleSave { name = Random.value < 0.5f ? "Small Posie Garden" : "Small Tulip Garden", pos = Find(land, 0, 30f, taken) });
            if (level >= 2) d.bubbles.Add(new BubbleSave { name = "Small Posie Garden", pos = Find(land, IslandsByLevel[1], 30f, taken) });

            Random.state = saved;
            return d;
        }

        /// <summary>
        /// A friend kept playing while you were away: a few new things on their island for every
        /// few hours, a level up now and then, and fresh trouble to help with.
        /// </summary>
        public static void Progress(FriendProfile who, SaveData d, double hoursAway)
        {
            var land = LandMap.Instance;
            if (land == null || hoursAway < 0.25) return;

            var saved = Random.state;
            Random.InitState(who.seed + who.visits * 7919);
            var taken = new List<Spot>();
            foreach (var p in d.placed) taken.Add(new Spot { at = p.pos, radius = Radius(p.name) });

            // A level every day or so, until they have built everything.
            int levels = Mathf.Min((int)(hoursAway / 20.0), MaxLevel - who.level);
            for (int i = 0; i < levels; i++)
            {
                who.level++;
                string bridge = Bridges[who.level - 2];
                if (!d.bridges.Contains(bridge)) d.bridges.Add(bridge);
                var workplaces = new List<int>();
                foreach (var name in Additions[who.level - 1]) Place(d, land, name, IslandsByLevel[who.level - 1], taken, workplaces);
                foreach (int index in workplaces)
                    d.animals.Add(new AnimalSave { name = "Capybara", tame = true, workplace = index, pos = d.placed[index].pos, hunger = 1400 });
            }

            // Smaller touches: roughly one new thing every three hours.
            int extras = Mathf.Clamp((int)(hoursAway / 3.0), 1, 6);
            for (int i = 0; i < extras; i++)
            {
                string name = Growth[Random.Range(0, Mathf.Min(Growth.Length, 4 + who.level * 2))];
                Place(d, land, name, IslandsByLevel[Random.Range(0, who.level)], taken, null);
            }

            // Life goes on: someone gets hungry, a pest wanders in, a bubble turns up.
            foreach (var a in d.animals)
                if (a.tame && a.workplace >= 0 && Random.value < 0.25f) a.hunger = 0;
            if (Random.value < 0.7f)
                d.animals.Add(new AnimalSave { name = "Deer", pos = Find(land, IslandsByLevel[Random.Range(0, who.level)], 20f, taken, reserve: false) });
            if (Random.value < 0.6f)
                d.bubbles.Add(new BubbleSave { name = "Small Posie Garden", pos = Find(land, 0, 30f, taken) });

            Random.state = saved;
        }

        // ---------------------------------------------------------------- placing

        static void Place(SaveData d, LandMap land, string name, int island, List<Spot> taken, List<int> workplaces)
        {
            if (!StoreData.TryGet(name, out var def)) return;
            float radius = Radius(name);
            var at = Find(land, island, radius, taken);
            int index = d.placed.Count;
            d.placed.Add(new ItemSave { name = name, pos = at, scale = 1f, finalScale = Random.Range(0.85f, 1f) });
            if (def.Kind == StoreItemKind.Workplace) workplaces?.Add(index);

            // A grove arrives grown in: a ring of children around its parent.
            if (def.Kind == StoreItemKind.Forest || def.Kind == StoreItemKind.Hill || def.Kind == StoreItemKind.Garden)
            {
                int children = def.Kind == StoreItemKind.Forest ? Random.Range(5, 9) : def.Kind == StoreItemKind.Hill ? Random.Range(2, 4) : Random.Range(3, 6);
                for (int i = 0; i < children; i++)
                {
                    var spot = BunniSpace.RandomPointInCircle(at, def.Outer * 0.9f);
                    if (!land.CircleOnLand(spot, def.Inner) || Overlaps(spot, def.Inner * 0.8f, taken)) continue;
                    taken.Add(new Spot { at = spot, radius = def.Inner * 0.8f });
                    float final = Random.Range(0.6f, 1f);
                    d.children.Add(new NodeSave { origin = name, pos = spot, inner = def.Inner, scale = final, finalScale = final });
                }
            }
        }

        static float Radius(string name) =>
            StoreData.TryGet(name, out var def) ? Mathf.Max(30f, def.Inner + 15f) : 40f;

        static Vector2 Find(LandMap land, int island, float radius, List<Spot> taken, bool reserve = true)
        {
            var piece = land.Island(island);
            if (piece == null || piece.rects.Count == 0) return Vector2.zero;
            var rect = piece.rects[0];
            for (int i = 0; i < 300; i++)
            {
                var w = new Vector2(Random.Range(rect.xMin, rect.xMax), Random.Range(rect.yMin, rect.yMax));
                var u = LandMap.WorldToUnity(w);
                if (land.IslandAt(u) != island || !land.CircleOnLand(u, radius)) continue;
                if (Overlaps(u, radius, taken)) continue;
                if (reserve) taken.Add(new Spot { at = u, radius = radius });
                return u;
            }
            return LandMap.WorldToUnity(rect.center);
        }

        static bool Overlaps(Vector2 at, float radius, List<Spot> taken)
        {
            foreach (var s in taken)
                if (BunniSpace.WorldDistance(at, s.at) < radius + s.radius) return true;
            return false;
        }
    }
}
