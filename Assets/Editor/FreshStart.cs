using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CapyIsland.EditorTools
{
    /// <summary>
    /// CapyIsland → Delete Save: a true first-time start for playtesting. Wipes the island and every
    /// bit of progress the game remembers outside it (tutorial and tips, the island clock, daily
    /// gifts, events, hobbies and the journal, wardrobe, friends, books read, the player's name…),
    /// so the next Play opens like a first launch: Old Yuzu waking in the dark, in English, asking for
    /// your language and name. Settings stay: Easy Reading, sound and volumes, arrow labels.
    ///
    /// Run during Play, it stops Play first and wipes once the editor is back, so the game can't
    /// save itself again on the way out.
    /// </summary>
    public static class FreshStart
    {
        /// <summary>Settings, not progress: kept. A trailing dot keeps a whole family.</summary>
        static readonly string[] Keep =
        {
            "CapyIsland.A11y.", "CapyIsland.ArrowLabels", "CapyIsland.Sound",
            "CapyIsland.Volume.", "CapyIsland.TrackerCollapsed",
        };

        /// <summary>Progress keys, for when the saved values can't be listed (not on Windows).</summary>
        static readonly string[] Known =
        {
            "CapyIsland.Save.v1", "CapyIsland.TourSeen", "CapyIsland.TutorialOff", "CapyIsland.ClockOffset",
            "CapyIsland.Daily.Last", "CapyIsland.Daily.Streak", "CapyIsland.Events", "CapyIsland.Hobbies",
            "CapyIsland.Wardrobe.Owned", "CapyIsland.Wardrobe.Worn", "CapyIsland.Airport", "CapyIsland.Bait",
            "CapyIsland.CatBegSeen", "CapyIsland.CatRideSeen", "CapyIsland.DishOrders", "CapyIsland.Friends",
            "CapyIsland.LeftNotes", "CapyIsland.ShoreNotes", "CapyIsland.PlayerName", "CapyIsland.Visited",
        };

        [MenuItem("CapyIsland/Delete Save")]
        public static void DeleteSave()
        {
            if (EditorApplication.isPlaying)
            {
                Game.SaveGame.Suspended = true;   // no last save on the way out
                EditorApplication.playModeStateChanged += AfterPlay;
                EditorApplication.isPlaying = false;
                return;
            }
            Erase();
        }

        static void AfterPlay(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= AfterPlay;
            Erase();
        }

        static void Erase()
        {
            var names = new HashSet<string>(Known);
#if UNITY_EDITOR_WIN
            // The editor keeps PlayerPrefs in the registry; list them all, so per-item keys (books
            // read, journal pages) go too.
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                       $@"Software\Unity\UnityEditor\{PlayerSettings.companyName}\{PlayerSettings.productName}"))
            {
                if (key != null)
                    foreach (var v in key.GetValueNames())
                        names.Add(Regex.Replace(v, @"_h\d+$", ""));
            }
#endif
            int erased = 0;
            foreach (var n in names)
            {
                if (!n.StartsWith("CapyIsland.")) continue;
                if (Keep.Any(k => n == k || (k.EndsWith(".") && n.StartsWith(k)))) continue;
                if (!PlayerPrefs.HasKey(n)) continue;
                PlayerPrefs.DeleteKey(n);
                erased++;
            }
            UI.HudTour.ResetAll();   // every tip, the tour, tutorial back on
            PlayerPrefs.Save();
            Debug.Log($"Fresh start: erased {erased} saved values (island and progress). Settings kept. Press Play for the welcome page.");
        }
    }
}
