using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Horaira, Old Yuzu's cat. He passed away long before the Big Chill and all the cold years kept
    /// watch beside her ice where nobody could see him. Once she is warm again he shows himself, a
    /// little white cat with a ginger cap and a halo, and from then on he is a cat like the others
    /// (<see cref="CatCare"/>): you can pet him, share a fish, win his trust, adopt him and dress
    /// him. Only he never strays far from her side, and he turns up by himself rather than
    /// wandering in. The first present he brings you is a halo of your own, which no other cat
    /// can give.
    ///
    /// His halo is a piece he wears (<see cref="HaloId"/>), not part of his picture, so a hat you
    /// put on him replaces it instead of sitting on top of it; take the hat off and the halo is back.
    ///
    /// Made in memory of a real cat called Horaira, from photos his family shared.
    /// </summary>
    public static class Horaira
    {
        public const string Id = "Horaira";
        public const string HaloId = "Hat_Halo";
        const string MetKey = "CapyIsland.HorairaMet";

        public static bool Is(CatKind kind) => kind != null && kind.Id == Id;

        /// <summary>
        /// A cat's picture for windows and journals. Horaira's has his halo on, since out in the
        /// world he wears it as a piece (CatWear) and a bare picture would look wrong.
        /// </summary>
        public static Sprite Picture(CatKind kind) =>
            kind == null ? null
            : Is(kind) ? HobbyData.Art("Cat_Horaira_Portrait") ?? HobbyData.Art("Cat_" + kind.Id)
            : HobbyData.Art("Cat_" + kind.Id);
        public static bool Is(HobbyLog.Cat cat) => cat != null && cat.id == Id;

        /// <summary>Old Yuzu is warm again and here on your island: he shows himself.</summary>
        public static bool Here => YuzuHeart.Alive && !Visit.Active && FrozenYuzu.Instance != null;

        /// <summary>His favourite spot: on the grass at Old Yuzu's side, away from the pool.</summary>
        public static Vector2? Spot
        {
            get
            {
                var yuzu = FrozenYuzu.Instance;
                if (yuzu == null) return null;
                Vector2 at = yuzu.transform.position;
                foreach (var side in new[] { new Vector2(-0.95f, -0.25f), new Vector2(-0.7f, -0.6f), new Vector2(0f, -0.7f) })
                    if (BunniSpace.PointOnLand(at + side)) return at + side;
                return at;
            }
        }

        // ---------------------------------------------------------------- his halo

        /// <summary>
        /// The halo he wears whenever nothing else is on his head (unless you took it off, once it's
        /// yours too): his own, so it needs no owning.
        /// </summary>
        public static WearItem WornHalo(HobbyLog.Cat cat)
        {
            if (!Is(cat) || cat.bareHead) return null;
            foreach (var id in cat.outfit)
                if (Wardrobe.Find(id) is { Slot: WearSlot.Hat } hat && Wardrobe.Owns(hat)) return null;   // a hat of yours instead
            return Wardrobe.Find(HaloId);
        }

        /// <summary>Halo on or off (from his wardrobe): off leaves him bare-headed; on takes any hat off.</summary>
        public static void ToggleHalo(HobbyLog.Cat cat)
        {
            bool on = WornHalo(cat) != null;
            cat.outfit.RemoveAll(id => Wardrobe.Find(id) is { Slot: WearSlot.Hat });
            cat.bareHead = on;
        }

        // ---------------------------------------------------------------- Old Yuzu tells you about him

        static bool Met => PlayerPrefs.GetInt(MetKey, 0) == 1;

        /// <summary>The first time you come near him, Old Yuzu tells you who he is (CatCare, twice a second).</summary>
        public static void MaybeMeet(Transform him, Vector2 king)
        {
            if (Met || him == null || Attention.Waiting("horaira")) return;
            if (Vector2.Distance(king, him.position) > 3.5f) return;
            // Still beside him when the moment comes: she can't say "you can see him too?" from across the island.
            Attention.Ask("horaira", () => Meet(him), () => Here && !Met && him != null && Near(him), calm: 1f);
        }

        static bool Near(Transform him)
        {
            var king = Object.FindFirstObjectByType<PlayerAvatar>();
            return king != null && Vector2.Distance(king.transform.position, him.position) <= 4.5f;
        }

        static void Meet(Transform him)
        {
            PlayerPrefs.SetInt(MetKey, 1);
            PlayerPrefs.Save();
            if (him != null) Sfx.PlayAt(SfxKind.Meow, him.position, 0.5f);
            UI.SpeechDialog.Show(YuzuFace, StoryText.Guide, StoryText.Yuzu.HorairaMeet);
        }

        static int _said;

        /// <summary>Old Yuzu remembers him (the "Ask Old Yuzu" choice when you tap him).</summary>
        public static void Remember()
        {
            if (!Met) { Meet(null); return; }
            var lines = StoryText.Yuzu.HorairaMemories;
            UI.SpeechDialog.Show(YuzuFace, StoryText.Guide, new[] { lines[_said++ % lines.Length] });
        }

        static Sprite YuzuFace => Resources.Load<Sprite>("Cutscenes/Cutscene_IntroYuzu");
    }
}
