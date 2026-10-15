using System;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Feed a cat a fish and, a few minutes later, it comes trotting back with a present — the way
    /// real cats do. Sometimes the present is wonderful (an outfit piece, gems, something growing),
    /// sometimes it is junk only Salty Kombu would pay a coin for, and sometimes it is a disaster:
    /// a lit fizz bomb, a zombie capybara, a whole family of guinea pigs. A cat that trusts you more
    /// tends to bring better things, but never only good ones.
    /// </summary>
    public static class CatGifts
    {
        /// <summary>How long after a fish the present arrives (real time, so it counts while away).</summary>
        static TimeSpan Delay() => TimeSpan.FromSeconds(UnityEngine.Random.Range(120f, 300f));

        /// <summary>The cat-only outfit pieces, found first.</summary>
        static readonly string[] CatPieces = { "Hat_Sock", "Neck_Bell" };

        /// <summary>The junk a cat can drag in: (id, name, line).</summary>
        static readonly (string id, string name, string line)[] Junk =
        {
            ("Gift_Roach", "cockroach", "It looks extremely proud of itself."),
            ("Gift_Boot", "soggy old boot", "Nobody knows where it found it. Nobody wants to."),
            ("Gift_Fishbone", "fish skeleton", "It is your fish. Well. It was your fish."),
        };

        enum Kind { Outfit, Treasure, Junk, Bomb, Zombie, Pests }

        /// <summary>Called when a cat eats a fish: it will be back with something.</summary>
        public static void Fed(HobbyLog.Cat cat)
        {
            if (cat == null || cat.giftDueTicks != 0) return;
            cat.giftDueTicks = (DateTime.Now + Delay()).Ticks;
            HobbyLog.Save();
        }

        public static bool Due(HobbyLog.Cat cat) => cat != null && cat.giftDueTicks != 0 && DateTime.Now.Ticks >= cat.giftDueTicks;

        /// <summary>The cat has arrived at your feet: open the present.</summary>
        public static void Open(CatKind kind, HobbyLog.Cat cat, Vector2 at, Sprite catPicture)
        {
            cat.giftDueTicks = 0;
            cat.gifts++;
            HobbyLog.Save();
            Motes.Trigger("hobby_catgift");

            string who = cat.adopted ? kind.Name : "The stray cat";
            string title = "A present from " + (cat.adopted ? who : "the stray cat") + "!";
            // The very first present is a nice one, so the habit sticks.
            var roll = cat.gifts == 1 && HobbyLog.TotalCatGifts() == 1 ? Kind.Outfit : Roll(cat.bond);

            switch (roll)
            {
                case Kind.Outfit:
                {
                    var piece = NextPiece();
                    if (piece == null) goto case Kind.Treasure;
                    Wardrobe.Give(piece, announce: false);
                    UI.CardPopup.Show(title, $"A {piece.Name}! Where did it even find that?\n<size=85%>It's in your wardrobe now.</size>", Wardrobe.Art(piece));
                    Sfx.Play(SfxKind.Chest, 0.8f);
                    break;
                }
                case Kind.Treasure:
                {
                    var catalog = ItemCatalog.Instance;
                    if (UnityEngine.Random.value < 0.5f || catalog == null)
                    {
                        int gems = UnityEngine.Random.Range(5, 16);
                        UI.CardPopup.Show(title, $"A little pile of {gems} shiny gems. Good cat!", UI.IconPainter.Sparkle,
                            () => TreasureChest.SpawnValue(at, ResourceKind.Gem, gems, catalog != null ? catalog.ItemParent : null));
                    }
                    else
                    {
                        UI.CardPopup.Show(title, "Something it dug up… and it's still growing! Pop the bubble to see.", UI.IconPainter.Gift,
                            () => catalog.MakeRandomStore(at + new Vector2(0.8f, -0.3f)));
                    }
                    break;
                }
                case Kind.Junk:
                {
                    var j = Junk[UnityEngine.Random.Range(0, Junk.Length)];
                    var art = HobbyData.Art(j.id);
                    UI.CardPopup.Show(title, $"A {j.name}. {j.line}\n<size=85%>Salty Kombu might give you a coin for it.</size>", art,
                        () => GiveJunk(j.id, j.name, art, at));
                    break;
                }
                case Kind.Bomb:
                    UI.CardPopup.Show(title, "A lit fizz bomb?! Run!", Icon("Bomb") ?? UI.IconPainter.Star,
                        () => Hazards.Spawn(HazardKind.Bomb, at));
                    break;
                case Kind.Zombie:
                    UI.CardPopup.Show(title, "A zombie capybara. It followed the cat home, and it isn't leaving.", Icon("Monster") ?? catPicture,
                        () => Hazards.Spawn(HazardKind.Monster, at + new Vector2(1.2f, 0.4f)));
                    break;
                case Kind.Pests:
                    UI.CardPopup.Show(title, "A whole family of guinea pigs. They're already eyeing your flowers.", Icon("Deer") ?? catPicture, () =>
                    {
                        for (int i = 0; i < 3; i++)
                            Hazards.Spawn(HazardKind.Deer, at + UnityEngine.Random.insideUnitCircle * 1.6f);
                    });
                    break;
            }
        }

        static Sprite Icon(string name)
        {
            var catalog = ItemCatalog.Instance;
            return catalog != null && catalog.Has(name) ? catalog.Icon(name) : null;
        }

        /// <summary>Good things get likelier as the cat trusts you more; disasters never go away.</summary>
        static Kind Roll(int bond)
        {
            float trust = Mathf.Clamp01(bond / (float)CatCare.MaxBond);
            var weights = new (Kind kind, float weight)[]
            {
                (Kind.Outfit, 8f + 10f * trust),
                (Kind.Treasure, 18f + 10f * trust),
                (Kind.Junk, 34f),
                (Kind.Bomb, 12f),
                (Kind.Zombie, 8f),
                (Kind.Pests, 12f - 4f * trust),
            };
            float total = 0f;
            foreach (var w in weights) total += w.weight;
            float pick = UnityEngine.Random.value * total;
            foreach (var w in weights)
            {
                pick -= w.weight;
                if (pick <= 0f) return w.kind;
            }
            return Kind.Junk;
        }

        /// <summary>The cat pieces first, then any outfit piece you are still missing.</summary>
        static WearItem NextPiece()
        {
            foreach (var id in CatPieces)
            {
                var item = Wardrobe.Find(id);
                if (item != null && !Wardrobe.Owns(item) && Wardrobe.Art(item) != null) return item;
            }
            var daily = Wardrobe.NextDailyPiece();
            return daily;
        }

        /// <summary>Junk rides in your trail like anything else, but only Salty Kombu wants it (cats bring it, and fishing reels it in).</summary>
        public static void GiveJunk(string id, string name, Sprite art, Vector2 at)
        {
            var go = HobbyItems.Make(char.ToUpper(name[0]) + name.Substring(1), art, 0.6f, 0.8f, 10f);
            go.AddComponent<CatJunk>().Name = name;
            var placeable = go.GetComponent<Placeable>();
            placeable.TrailOnly = "Nobody wants a " + name + " lying about. Drop it on Salty Kombu, he might buy it!";
            BubbleSpawner.SpawnAt(go, at + new Vector2(0.6f, -0.2f), null);
        }
    }

    /// <summary>A worthless present from a cat. Salty Kombu will take it for a coin.</summary>
    public class CatJunk : MonoBehaviour
    {
        public string Name;
    }
}
