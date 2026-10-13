using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// What's new and not looked at yet: a bird just spotted, a card just found, a hat just earned.
    /// Each is a path naming where it lives — "hobbies/Birding/robin", "wardrobe/Hat/Hat_Yuzu",
    /// "collection/12" — so a red dot can light every step of the way to it: the Menu button, the
    /// Hobbies tile, the Birdwatching card, its journal, and the bird itself. Looking at the thing
    /// (opening the page it is on) clears it, and with it every dot on the way.
    ///
    /// Kept in PlayerPrefs beside the wardrobe and hobbies, outside the island save.
    /// </summary>
    public static class NewStuff
    {
        const string Key = "CapyIsland.New";

        static HashSet<string> _keys;

        static HashSet<string> Keys
        {
            get
            {
                if (_keys != null) return _keys;
                _keys = new HashSet<string>();
                foreach (var k in PlayerPrefs.GetString(Key, "").Split('\n'))
                    if (k.Length > 0) _keys.Add(k);
                return _keys;
            }
        }

        public static void Add(string path)
        {
            if (!string.IsNullOrEmpty(path) && Keys.Add(path)) Save();
        }

        public static bool Has(string path) => Keys.Contains(path);

        /// <summary>Anything new under <paramref name="prefix"/> ("" for anything at all)?</summary>
        public static bool Any(string prefix = "")
        {
            foreach (var k in Keys)
                if (k.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Seen: forgets just this one.</summary>
        public static void Remove(string path)
        {
            if (Keys.Remove(path)) Save();
        }

        /// <summary>Seen: forgets everything under <paramref name="prefix"/>.</summary>
        public static void Clear(string prefix)
        {
            if (Keys.RemoveWhere(k => k.StartsWith(prefix, StringComparison.Ordinal)) > 0) Save();
        }

        static void Save()
        {
            PlayerPrefs.SetString(Key, string.Join("\n", Keys));
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => _keys = null;
    }
}
