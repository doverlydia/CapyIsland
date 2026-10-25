using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
using PlayerSaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;
using PlayerLoadOptions = Unity.Services.CloudSave.Models.Data.Player.LoadOptions;
using PlayerDeleteAllOptions = Unity.Services.CloudSave.Models.Data.Player.DeleteAllOptions;

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

        /// <summary>The editor-only switch that keeps cloud sync out of a playtest (see Start).</summary>
        public const string EditorOffKey = "CapyIsland.Playtest.NoCloud";

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
#if UNITY_EDITOR
            // A playtest from a fresh start keeps the editor's own island in the cloud out of it
            // (otherwise "no island here, one up there" brings it straight back down). On for one
            // editor session: UnityEditor.SessionState.SetBool(EditorOffKey, true).
            if (UnityEditor.SessionState.GetBool(EditorOffKey, false))
            {
                Debug.Log("[CloudSync] Off for this editor session (playtest).");
                return;
            }
#endif
            // Let the island load first: its save is what gets compared.
            await Task.Yield();
            await Task.Yield();
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                await RefreshPlayerInfo();
                RealFriends.Start();
                await CatchUp();
            }
            catch (Exception e)
            {
                Debug.Log("[CloudSync] Not connected, the island stays on this device for now: " + e.Message);
            }
        }

        void Update()
        {
            // Friends see you're here while the game is open (RealFriends).
            RealFriends.Tick();
            if (Time.unscaledTime >= _nextHeartbeat && RealFriends.Ready)
            {
                _nextHeartbeat = Time.unscaledTime + HeartbeatEvery;
                _ = RealFriends.Heartbeat(visiting: Visit.Active);
            }
            if (!_ready || _busy || _blocked || Time.unscaledTime < _nextUpload) return;
            _nextUpload = Time.unscaledTime + UploadEvery;
            _ = UploadIfChanged();
        }

        const float HeartbeatEvery = 120f;
        float _nextHeartbeat;

        // Put away (a phone going to the background, a browser tab hidden): send what's new, and
        // friends see you go. Back again: they see you come back.
        void OnApplicationPause(bool paused)
        {
            if (paused) { _ = UploadIfChanged(); RealFriends.GoOffline(); }
            else _nextHeartbeat = 0f;
        }

        void OnApplicationFocus(bool focused) { if (!focused) _ = UploadIfChanged(); }

        void OnApplicationQuit() => RealFriends.GoOffline();

        // ---------------------------------------------------------------- accounts

        /// <summary>Connected to the cloud (account features can be offered).</summary>
        public static bool Online => UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;

        /// <summary>The username this island's account goes by, or null while it has none.</summary>
        public static string Username { get; private set; }

        /// <summary>This island's account opens with Google.</summary>
        public static bool HasGoogle { get; private set; }

        /// <summary>The account can be opened on another device (it has a username or Google), so it's no longer the invisible one.</summary>
        public static bool Linked => Username != null || HasGoogle;

        static async Task RefreshPlayerInfo()
        {
            try
            {
                var info = await AuthenticationService.Instance.GetPlayerInfoAsync();
                Username = string.IsNullOrEmpty(info?.Username) ? null : info.Username;
                HasGoogle = !string.IsNullOrEmpty(info?.GetGoogleId());
            }
            catch (Exception) { /* offline: keep what we knew */ }
            AccountChanged?.Invoke();
        }

        public static event Action AccountChanged;

        /// <summary>The rules Unity has for a username (3-20 letters, numbers and . - @ _), checked before asking it.</summary>
        public static string CheckUsername(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 3 || name.Length > 20) return "A username needs 3 to 20 letters or numbers.";
            foreach (char c in name)
                if (!(c < 128 && (char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '@' || c == '_'))) return "A username can only use English letters, numbers and . - @ _";
            return null;
        }

        /// <summary>The rules for a password (8-30 characters, with a capital, a small letter, a number and a symbol).</summary>
        public static string CheckPassword(string pass)
        {
            if (string.IsNullOrEmpty(pass) || pass.Length < 8 || pass.Length > 30) return "A password needs 8 to 30 characters.";
            bool upper = false, lower = false, digit = false, symbol = false;
            foreach (char c in pass)
            {
                if (char.IsUpper(c)) upper = true;
                else if (char.IsLower(c)) lower = true;
                else if (char.IsDigit(c)) digit = true;
                else symbol = true;
            }
            return upper && lower && digit && symbol ? null : "A password needs a capital letter, a small letter, a number and a symbol (like ! or #).";
        }

        /// <summary>
        /// Gives the invisible account a username and password, so the same island can be opened on
        /// another device. The island stays exactly where it is. Returns null, or what went wrong.
        /// </summary>
        public static async Task<string> MakeAccount(string user, string pass)
        {
            var why = CheckUsername(user) ?? CheckPassword(pass);
            if (why != null) return why;
            if (!Online) return "Can't reach the internet right now. Try again in a moment.";
            try
            {
                await AuthenticationService.Instance.AddUsernamePasswordAsync(user, pass);
                await RefreshPlayerInfo();
                return null;
            }
            catch (AuthenticationException e) when (e.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked || e.ErrorCode == CommonErrorCodes.Conflict)
            {
                return "That username is taken. Try another one.";
            }
            catch (RequestFailedException e) { return Problem(e); }
        }

        /// <summary>
        /// Signs this device in to an account made on another device. This island is sent up first,
        /// so it isn't lost; then the account's island is compared with it, and if they differ Old
        /// Yuzu asks which to keep. Returns null, or what went wrong (and this device stays as it was).
        /// </summary>
        public static async Task<string> SignIn(string user, string pass)
        {
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrEmpty(pass)) return "Type your username and password.";
            if (!Online || Instance == null) return "Can't reach the internet right now. Try again in a moment.";
            if (user == Username) return "This device is already signed in to that account.";
            return await SwitchTo(() => AuthenticationService.Instance.SignInWithUsernamePasswordAsync(user, pass),
                "That username and password don't match.");
        }

        /// <summary>
        /// With a Google ID token (GoogleLogin): if that Google account is new to the game, this
        /// island's account now opens with it, and the island stays where it is. If it already has an
        /// island (made on another device), this device signs in to that one, as <see cref="SignIn"/>
        /// does. Returns null, or what went wrong; <c>switched</c> says which of the two happened.
        /// </summary>
        public static async Task<(string problem, bool switched)> UseGoogle(string idToken)
        {
            if (!Online || Instance == null) return ("Can't reach the internet right now. Try again in a moment.", false);
            if (!HasGoogle)
            {
                try
                {
                    await AuthenticationService.Instance.LinkWithGoogleAsync(idToken);
                    await RefreshPlayerInfo();
                    return (null, false);
                }
                catch (AuthenticationException e) when (e.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
                {
                    // That Google account already has an island: open it instead.
                }
                catch (RequestFailedException e) { return (Problem(e), false); }
            }
            string problem = await SwitchTo(() => AuthenticationService.Instance.SignInWithGoogleAsync(idToken),
                "Google sign-in didn't work. Try again in a moment.");
            return (problem, problem == null);
        }

        /// <summary>
        /// Signs this device in to another account. This island is sent up first, so it isn't lost;
        /// if the sign-in fails, the old account comes back; if it works, the account's island is
        /// compared with this one and Old Yuzu asks which to keep.
        /// </summary>
        static async Task<string> SwitchTo(Func<Task> signIn, string refused)
        {
            var self = Instance;
            string before = AuthenticationService.Instance.PlayerId;
            self._ready = false;
            self._blocked = true;
            try
            {
                await self.UploadIfChanged(evenIfBlocked: true);
                // Not clearing the stored session: if the new sign-in fails, the old one comes back.
                AuthenticationService.Instance.SignOut(false);
                await signIn();
            }
            catch (RequestFailedException e)
            {
                try { await AuthenticationService.Instance.SignInAnonymouslyAsync(); } catch (Exception) { }
                self._blocked = false;
                self._ready = true;
                return e is AuthenticationException || e.ErrorCode != CommonErrorCodes.TransportError && e.ErrorCode != CommonErrorCodes.Timeout
                    ? refused
                    : Problem(e);
            }
            await RefreshPlayerInfo();
            self._blocked = false;
            if (AuthenticationService.Instance.PlayerId == before) { self._ready = true; return null; }   // the same account after all
            RealFriends.Restart();   // that account's friends
            // A different account: this device's link to the cloud starts over, and the two islands are compared.
            PlayerPrefs.DeleteKey(StampPref);
            PlayerPrefs.DeleteKey(HashPref);
            PlayerPrefs.Save();
            try { await self.CatchUp(); }
            catch (Exception e) { Debug.Log("[CloudSync] Signed in, but couldn't compare islands yet: " + e.Message); self._ready = true; }
            return null;
        }

        /// <summary>
        /// Deletes this player's island from the cloud and the account itself (its username, its
        /// Google link), for good. The island on this device stays. The cloud is left alone for the
        /// rest of this session; the next time the game starts, a new invisible account begins.
        /// Returns null, or what went wrong (and nothing was deleted, or only the cloud island).
        /// </summary>
        public static async Task<string> DeleteAccount()
        {
            if (!Online || Instance == null) return "Can't reach the internet right now. Try again in a moment.";
            var self = Instance;
            self._ready = false;
            self._blocked = true;
            try
            {
                await RealFriends.ForgetEveryone();
                await PrivateServer.Leave();
                await CloudSaveService.Instance.Data.Player.DeleteAllAsync(new PlayerDeleteAllOptions());
                await CloudSaveService.Instance.Data.Player.DeleteAllAsync(new PlayerDeleteAllOptions(
                    new Unity.Services.CloudSave.Models.Data.Player.PublicWriteAccessClassOptions()));   // what friends could see
                await AuthenticationService.Instance.DeleteAccountAsync();   // also signs out and forgets the session
            }
            catch (RequestFailedException e)
            {
                self._blocked = false;
                self._ready = true;
                return Problem(e);
            }
            PlayerPrefs.DeleteKey(StampPref);
            PlayerPrefs.DeleteKey(HashPref);
            PlayerPrefs.Save();
            Username = null;
            HasGoogle = false;
            RealFriends.Restart();   // no account, no friends (until the next start makes a new one)
            AccountChanged?.Invoke();
            return null;
        }

        /// <summary>Sends up what friends can see, right now (a friend is about to visit live).</summary>
        public static async Task PublishNow()
        {
            if (!Online || Visit.Active) return;
            try
            {
                await CloudSaveService.Instance.Data.Player.SaveAsync(RealFriends.PublicData(),
                    new PlayerSaveOptions(new Unity.Services.CloudSave.Models.Data.Player.PublicWriteAccessClassOptions()));
            }
            catch (Exception e) { Debug.Log("[CloudSync] Couldn't share the island: " + e.Message); }
        }

        static string Problem(RequestFailedException e) =>
            e.ErrorCode == CommonErrorCodes.TransportError || e.ErrorCode == CommonErrorCodes.Timeout
                ? "Can't reach the internet right now. Try again in a moment."
                : "Something went wrong. Try again in a moment.";

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

        async Task UploadIfChanged(bool evenIfBlocked = false)
        {
            if (_busy || !CanSend) return;
            if (!evenIfBlocked && (!_ready || _blocked)) return;
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
                // What friends can see: your profile and island, in public data.
                await CloudSaveService.Instance.Data.Player.SaveAsync(RealFriends.PublicData(),
                    new PlayerSaveOptions(new Unity.Services.CloudSave.Models.Data.Player.PublicWriteAccessClassOptions()));
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
