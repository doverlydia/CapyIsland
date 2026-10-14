using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The save as one line of text you can keep in a note and paste back later, on this device or
    /// another (a phone and a browser keep separate saves). Everything the island remembers rides
    /// along — the island itself, hobbies, wardrobe, home, friends, story steps — but not this
    /// device's settings (language, sound, Easy Reading, layout).
    ///
    /// The code is "CAPY1:" and base64 of a small JSON list of keys and values: plain on purpose,
    /// so it reads back the same on every platform, and safe to paste through notes apps.
    /// </summary>
    public static class SaveTransfer
    {
        const string Prefix = "CAPY1:";

        /// <summary>Everything kept for the island, by key.</summary>
        static readonly string[] Keys =
        {
            SaveGame.Key, "CapyIsland.PlayerName", "CapyIsland.IntroSeen", "CapyIsland.TourSeen", "CapyIsland.TutorialOff",
            "CapyIsland.YuzuStage", "CapyIsland.Finale", "CapyIsland.LanternStorySeen", "CapyIsland.MochiMet",
            "CapyIsland.Hobbies", "CapyIsland.Home", "CapyIsland.Friends", "CapyIsland.Events", "CapyIsland.Visited",
            "CapyIsland.Airport", "CapyIsland.Bait", "CapyIsland.DishOrders", "CapyIsland.CookStirs",
            "CapyIsland.ChefSpot", "CapyIsland.FarmStoveGiven",
            "CapyIsland.CatBegSeen", "CapyIsland.CatRideSeen", "CapyIsland.ShrineSpot",
            "CapyIsland.MyBottles", "CapyIsland.LeftNotes", "CapyIsland.ShoreNotes",
            "CapyIsland.Daily.Last", "CapyIsland.Daily.Streak", "CapyIsland.ClockOffset", "CapyIsland.New",
            "CapyIsland.Wardrobe.Owned", "CapyIsland.Wardrobe.Worn", "CapyIsland.TowerCentred",
        };

        static IEnumerable<string> AllKeys()
        {
            foreach (var k in Keys) yield return k;
            foreach (Hobby h in Enum.GetValues(typeof(Hobby))) yield return "CapyIsland.JournalDone." + h;
        }

        [Serializable] class Entry { public string k, t, v; }
        [Serializable] class Bundle { public int version = 1; public List<Entry> keys = new(); }

        /// <summary>The whole island as one line of text.</summary>
        public static string Export()
        {
            var bundle = new Bundle();
            foreach (var k in AllKeys())
            {
                if (!PlayerPrefs.HasKey(k)) continue;
                const string none = "\u0001";
                var s = PlayerPrefs.GetString(k, none);
                if (s != none) { bundle.keys.Add(new Entry { k = k, t = "s", v = s }); continue; }
                int i = PlayerPrefs.GetInt(k, int.MinValue);
                if (i != int.MinValue) { bundle.keys.Add(new Entry { k = k, t = "i", v = i.ToString() }); continue; }
                float f = PlayerPrefs.GetFloat(k, float.NaN);
                if (!float.IsNaN(f)) bundle.keys.Add(new Entry { k = k, t = "f", v = f.ToString("R", System.Globalization.CultureInfo.InvariantCulture) });
            }
            return Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonUtility.ToJson(bundle)));
        }

        /// <summary>
        /// Reads a pasted code; false (with a reason to show) if it isn't one. Nothing is changed
        /// until <see cref="Apply"/>.
        /// </summary>
        public static bool TryRead(string code, out object island, out string why)
        {
            island = null;
            why = "That doesn't look like a Capy Island save code.";
            if (string.IsNullOrWhiteSpace(code)) { why = "Paste your save code first."; return false; }
            // Notes apps wrap long lines and add spaces: only the code's own characters count.
            var clean = new StringBuilder();
            foreach (char c in code) if (!char.IsWhiteSpace(c)) clean.Append(c);
            var text = clean.ToString();
            int at = text.IndexOf(Prefix, StringComparison.Ordinal);
            if (at < 0) return false;
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(text.Substring(at + Prefix.Length)));
                var bundle = JsonUtility.FromJson<Bundle>(json);
                if (bundle == null || bundle.keys == null || !bundle.keys.Exists(e => e.k == SaveGame.Key))
                {
                    why = "That save code is missing its island. Copy the whole code and try again.";
                    return false;
                }
                island = bundle;
                return true;
            }
            catch (Exception)
            {
                why = "That save code is cut short or changed. Copy the whole code and try again.";
                return false;
            }
        }

        /// <summary>Replaces this island with the one read, then reloads the game into it.</summary>
        public static void Apply(object island)
        {
            if (island is not Bundle bundle) return;
            SaveGame.Suspended = true;   // nothing saves the old island over the new one
            foreach (var k in AllKeys()) PlayerPrefs.DeleteKey(k);
            foreach (var e in bundle.keys)
            {
                if (string.IsNullOrEmpty(e.k) || !e.k.StartsWith("CapyIsland.", StringComparison.Ordinal)) continue;
                switch (e.t)
                {
                    case "i": if (int.TryParse(e.v, out var i)) PlayerPrefs.SetInt(e.k, i); break;
                    case "f": if (float.TryParse(e.v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f)) PlayerPrefs.SetFloat(e.k, f); break;
                    default: PlayerPrefs.SetString(e.k, e.v ?? ""); break;
                }
            }
            PlayerPrefs.Save();
            SaveGame.ResetStatics();
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
