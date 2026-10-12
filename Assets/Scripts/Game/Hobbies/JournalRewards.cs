using System;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Finding every fish, every bird or every bug fills a journal page — and a full page earns a
    /// reward, once: gems and a hat for the wardrobe (an angler's bucket hat, a feathered cap, bug
    /// antennae). Something for a long-time player to chase, with the times of day and the rain
    /// making the last few entries a proper hunt.
    /// </summary>
    public static class JournalRewards
    {
        const string Key = "CapyIsland.JournalDone.";
        public const int Gems = 40;

        /// <summary>Raised when a page is completed, so the HUD can show the card.</summary>
        public static event Action<Hobby, WearItem> Completed;

        public static int Total(Hobby h) => h switch
        {
            Hobby.Fishing => HobbyData.Fish.Length,
            Hobby.Birding => HobbyData.Birds.Length,
            Hobby.Bugs => HobbyData.Bugs.Length,
            Hobby.Gardening => HobbyData.Crops.Length,
            _ => 0,
        };

        public static int Found(Hobby h) => h switch
        {
            Hobby.Fishing => HobbyLog.Fish.Count,
            Hobby.Birding => HobbyLog.Birds.Count,
            Hobby.Bugs => HobbyLog.Bugs.Count,
            Hobby.Gardening => HobbyLog.Crops.Count,
            _ => 0,
        };

        public static WearItem Hat(Hobby h) => Array.Find(Wardrobe.Items, w => w.Unlock == "journal_" + h);

        public static bool Rewarded(Hobby h) => PlayerPrefs.GetInt(Key + h, 0) == 1;

        /// <summary>Called after a new kind is logged: pays out once when the page is full.</summary>
        public static void Check(Hobby h)
        {
            int total = Total(h);
            if (total == 0 || Found(h) < total || Rewarded(h)) return;
            PlayerPrefs.SetInt(Key + h, 1);
            PlayerPrefs.Save();
            var store = ResourceStore.Instance;
            if (store != null) store.Add(ResourceKind.Gem, Gems);
            var hat = Hat(h);
            if (hat != null) Wardrobe.Give(hat, announce: false);
            Sfx.Play(SfxKind.Chest, 0.8f);
            Completed?.Invoke(h, hat);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => Completed = null;
    }
}
