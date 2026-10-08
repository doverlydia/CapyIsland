using System;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>Who you are on the island: your name, shown on your profile, the server map and to visitors.</summary>
    public static class PlayerProfile
    {
        const string NameKey = "CapyIsland.PlayerName";
        public const string DefaultName = "Capy";
        public const int MaxNameLength = 16;

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => Changed = null;

        public static string Name
        {
            get
            {
                var n = PlayerPrefs.GetString(NameKey, "");
                return string.IsNullOrWhiteSpace(n) ? DefaultName : n;
            }
            set
            {
                var n = Clean(value);
                if (n == Name) return;
                PlayerPrefs.SetString(NameKey, n);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>Trimmed, one line, not too long; blank goes back to the default.</summary>
        public static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return DefaultName;
            var n = raw.Replace("\n", " ").Replace("\r", " ").Replace("<", "").Replace(">", "").Trim();
            if (n.Length > MaxNameLength) n = n.Substring(0, MaxNameLength).Trim();
            return n.Length == 0 ? DefaultName : n;
        }
    }
}
