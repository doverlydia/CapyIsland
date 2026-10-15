using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
using PlayerSaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;
using PlayerLoadOptions = Unity.Services.CloudSave.Models.Data.Player.LoadOptions;

namespace CapyIsland.Game
{
    /// <summary>
    /// Your island, kept in the cloud (Unity Cloud Save) as well as on the device. Every player
    /// gets an invisible account the first time the game starts; nobody sees a login.
    ///
    /// What goes up is the same as a save code (SaveTransfer): the whole island, hobbies and all.
    /// It goes up when it has changed, every few minutes and as the game is put away, never more
    /// often. When the game starts it compares: an island changed only in the cloud comes down, one
    /// changed only here goes up, and one changed in both places is the player's choice — Old Yuzu
    /// shows both and asks which to keep. Nothing is ever replaced silently.
    ///
    /// Offline, or if the cloud can't be reached, the game carries on exactly as before and tries
    /// again later.
    /// </summary>
    public class CloudSync : MonoBehaviour
    {
        const string IslandKey = "island";
        const string MetaKey = "island_meta";
        /// <summary>The cloud version this device last sent or fetched, and what its island looked like then.</summary>
        const string StampPref = "CapyIsland.Cloud.Stamp", HashPref = "CapyIsland.Cloud.Hash";
        const float UploadEvery = 180f;

        /// <summary>What the cloud knows about the island beside it, so the two can be compared without downloading it.</summary>
        [Serializable]
        class Meta
        {
            public string stamp;
            public long savedAt;   // UTC ticks
            public int hearts, wood, gold;
            public string name;
        }

        public static CloudSync Instance { get; private set; }

        /// <summary>Signed in and caught up with the cloud: it's safe to upload.</summary>
        public static bool Ready => Instance != null && Instance._ready;

        bool _ready, _busy, _blocked;
        float _nextUpload;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        async void Start()
        {
            if (Visit.Active) return;   // a friend's island is never sent up as yours
            // Let the island load first: its save is what gets compared.
            await Task.Yield();
            await Task.Yield();
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                await CatchUp();
            }
            catch (Exception e)
            {
                Debug.Log("[CloudSync] Not connected, the island stays on this device for now: " + e.Message);
            }
        }

        void Update()
        {
            if (!_ready || _busy || _blocked || Time.unscaledTime < _nextUpload) return;
            _nextUpload = Time.unscaledTime + UploadEvery;
            _ = UploadIfChanged();
        }

        // Put away (a phone going to the background, a browser tab hidden): send what's new.
        void OnApplicationPause(bool paused) { if (paused) _ = UploadIfChanged(); }
        void OnApplicationFocus(bool focused) { if (!focused) _ = UploadIfChanged(); }

        // ---------------------------------------------------------------- comparing

        /// <summary>On starting: bring this device and the cloud into line, asking the player if both changed.</summary>
        async Task CatchUp()
        {
            var cloud = await LoadMeta();
            string local = SaveTransfer.Export();
            string stamp = PlayerPrefs.GetString(StampPref, "");
            bool localChanged = Hash(local) != PlayerPrefs.GetString(HashPref, "");

            if (cloud == null)
            {
                // Nothing up there yet: this island is the first.
                if (SaveGame.HasSave) await Upload(local);
                _ready = true;
                return;
            }
            if (cloud.stamp == stamp)
            {
                // The cloud hasn't moved since this device last spoke to it.
                _ready = true;
                if (localChanged) await Upload(local);
                return;
            }
            if (!SaveGame.HasSave || (!localChanged && stamp.Length > 0))
            {
                // Played elsewhere since, and nothing new here: the cloud's island comes down.
                await Download();
                return;
            }
            Ask(cloud);
        }

        /// <summary>Two different islands: Old Yuzu shows both and the player picks.</summary>
        void Ask(Meta cloud)
        {
            _blocked = true;   // nothing goes up until the player decides
            var store = ResourceStore.Instance;
            string here = string.Format(UI.Loc.T(StoryText.Cloud.Here), Motes.CollectedCount,
                store != null ? store.Get(ResourceKind.Wood) : 0, store != null ? store.Get(ResourceKind.Gold) : 0);
            string online = string.Format(UI.Loc.T(StoryText.Cloud.Online), cloud.hearts, cloud.wood, cloud.gold);
            var face = UI.GhostDialogue.Instance != null ? UI.GhostDialogue.Instance.GhostFace : null;
            UI.SpeechDialog.Show(face, StoryText.Guide, new[] { StoryText.Cloud.TwoIslands, here + "\n" + online }, null,
                (StoryText.Cloud.KeepHere, () => { _blocked = false; _ready = true; _ = Upload(SaveTransfer.Export(), force: true); }),
                (StoryText.Cloud.KeepOnline, () => { _ = Download(); }));
        }

        // ---------------------------------------------------------------- up and down

        async Task UploadIfChanged()
        {
            if (!_ready || _busy || _blocked || !CanSend) return;
            string local = SaveTransfer.Export();
            if (Hash(local) == PlayerPrefs.GetString(HashPref, "")) return;
            await Upload(local);
        }

        /// <summary>The game may send its island: its own, loaded and saving normally.</summary>
        static bool CanSend => SaveGame.Instance != null && SaveGame.Instance.SavingAllowed && !Visit.Active && SaveGame.HasSave;

        /// <summary>
        /// Sends the island up. Unless <paramref name="force"/>d, it first checks nobody else changed
        /// the cloud's copy in the meantime (another device): then it asks instead.
        /// </summary>
        async Task Upload(string island, bool force = false)
        {
            if (_busy || !CanSend) return;
            _busy = true;
            try
            {
                if (!force)
                {
                    var cloud = await LoadMeta();
                    string stamp = PlayerPrefs.GetString(StampPref, "");
                    if (cloud != null && cloud.stamp != stamp) { _busy = false; Ask(cloud); return; }
                }
                var store = ResourceStore.Instance;
                var meta = new Meta
                {
                    stamp = Guid.NewGuid().ToString("N"),
                    savedAt = DateTime.UtcNow.Ticks,
                    hearts = Motes.CollectedCount,
                    wood = store != null ? store.Get(ResourceKind.Wood) : 0,
                    gold = store != null ? store.Get(ResourceKind.Gold) : 0,
                    name = PlayerProfile.Name,
                };
                await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object>
                {
                    { IslandKey, island },
                    { MetaKey, JsonUtility.ToJson(meta) },
                }, new PlayerSaveOptions());
                Remember(meta.stamp, island);
            }
            catch (Exception e)
            {
                Debug.Log("[CloudSync] Couldn't send the island up, will try again: " + e.Message);
            }
            _busy = false;
        }

        /// <summary>Brings the cloud's island down and reloads the game into it.</summary>
        async Task Download()
        {
            _busy = true;
            try
            {
                var items = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { IslandKey, MetaKey }, new PlayerLoadOptions());
                if (!items.TryGetValue(IslandKey, out var islandItem) || !items.TryGetValue(MetaKey, out var metaItem)) { _busy = false; _ready = true; return; }
                string island = islandItem.Value.GetAs<string>();
                var meta = JsonUtility.FromJson<Meta>(metaItem.Value.GetAs<string>());
                if (!SaveTransfer.TryRead(island, out var bundle, out _)) { _busy = false; _ready = true; return; }
                Remember(meta.stamp, island);
                SaveTransfer.Apply(bundle);   // reloads the game
            }
            catch (Exception e)
            {
                Debug.Log("[CloudSync] Couldn't fetch the island from the cloud: " + e.Message);
                _busy = false;
                _ready = true;
            }
        }

        async Task<Meta> LoadMeta()
        {
            var items = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { MetaKey }, new PlayerLoadOptions());
            if (!items.TryGetValue(MetaKey, out var item)) return null;
            var meta = JsonUtility.FromJson<Meta>(item.Value.GetAs<string>());
            return meta != null && !string.IsNullOrEmpty(meta.stamp) ? meta : null;
        }

        static void Remember(string stamp, string island)
        {
            PlayerPrefs.SetString(StampPref, stamp);
            PlayerPrefs.SetString(HashPref, Hash(island));
            PlayerPrefs.Save();
        }

        /// <summary>A fingerprint of the island, to tell whether it changed (FNV-1a; no crypto needed, and it runs in the browser too).</summary>
        static string Hash(string text)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in text) { h ^= c; h *= 1099511628211UL; }
            return h.ToString("x16") + ":" + text.Length;
        }
    }
}
