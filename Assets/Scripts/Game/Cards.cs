using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// First-discovery cards (Rewards/Cards.as). The first time you buy or collect an item, the
    /// first not-yet-found card that lists it pops up: the game pauses on a card with the item's
    /// picture and a line about it, until you tap it away.
    /// </summary>
    public static class Cards
    {
        /// <summary>Raised when a card is found, with the card and the item that found it.</summary>
        public static event Action<CardDef, string> Found;

        static readonly HashSet<int> _collected = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            _collected.Clear();
            Found = null;
        }

        public static IEnumerable<int> Collected => _collected;
        public static bool IsCollected(int id) => _collected.Contains(id);

        public static void Restore(IEnumerable<int> ids)
        {
            _collected.Clear();
            foreach (var id in ids) _collected.Add(id);
        }

        /// <summary>An item was bought or collected (Cards.FoundItem).</summary>
        public static void FoundItem(string itemName)
        {
            foreach (var card in QuestData.Cards)
            {
                if (_collected.Contains(card.Id)) continue;
                if (Array.IndexOf(card.Items, itemName) < 0) continue;
                _collected.Add(card.Id);
                NewStuff.Add("collection/" + card.Id);
                Found?.Invoke(card, itemName);
                return;
            }
        }
    }
}
