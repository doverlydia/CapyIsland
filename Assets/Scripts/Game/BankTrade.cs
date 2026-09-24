using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The Bank's trades (Menu/TradeMenu): spend gems on wood or gold in four fixed tiers. The goods
    /// arrive as a chest that opens itself 70 units in front of the counter. (The original's third
    /// counter sold gems for real money; CapyIsland leaves it out, like the Kongregate build.)
    /// </summary>
    public static class BankTrade
    {
        static readonly (int gems, int amount)[] Tiers = { (5, 250), (10, 500), (20, 1250), (30, 2000) };

        public static void Open(string counter, Vector2 at)
        {
            var kind = counter == "Buy Gold" ? ResourceKind.Gold : ResourceKind.Wood;
            var store = ResourceStore.Instance;
            int gems = store != null ? store.Get(ResourceKind.Gem) : 0;
            string noun = kind == ResourceKind.Gold ? "gold" : "wood";

            var choices = new List<UI.SpeechDialog.Choice>();
            foreach (var (cost, amount) in Tiers)
            {
                int c = cost, a = amount;
                choices.Add(new UI.SpeechDialog.Choice($"{c} gems → {a} {noun}", () => Trade(kind, c, a, at), gems >= c));
            }
            choices.Add(new UI.SpeechDialog.Choice("Cancel", null));

            var catalog = ItemCatalog.Instance;
            var face = catalog != null ? catalog.Icon(counter) : null;
            UI.SpeechDialog.ShowChoices(face, "Bank", new[] { $"Trade your gems for {noun}. You have {gems} gems." }, null, choices);
        }

        static void Trade(ResourceKind kind, int gems, int amount, Vector2 at)
        {
            var store = ResourceStore.Instance;
            if (store == null || !store.TrySpend(0, 0, gems)) return;
            var catalog = ItemCatalog.Instance;
            TreasureChest.SpawnValue(at + BunniSpace.ToUnity(new Vector2(0f, -70f)), kind, amount,
                catalog != null ? catalog.ItemParent : null);
        }
    }
}
