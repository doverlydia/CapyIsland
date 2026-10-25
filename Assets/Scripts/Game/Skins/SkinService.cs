using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CapyIsland.Game
{
    /// <summary>
    /// Finds out which holiday skin is on and fetches it (<see cref="Skins"/> has the big picture).
    ///
    /// Every start it reads Skins/skins.json from this repo on GitHub — the list of skins and their
    /// dates — picks the one whose dates hold today (the device's own date, both ends included),
    /// downloads that skin's skin.json and pictures, and puts it on. So a new skin needs no new
    /// build: push its folder and a line in skins.json, and every installed game wears it on the day.
    ///
    /// The list is remembered, so a phone that starts offline still wears the skin it already has;
    /// the pictures are kept on the device and checked against GitHub each start (WebGL leaves that to the
    /// browser's cache). The date is looked at again every minute, so a skin comes off at midnight
    /// on its last day even mid-game, and GitHub is asked again when the game comes back from the
    /// background. Anything that fails (no network, a missing file) leaves the island in its own look.
    ///
    /// In the editor, CapyIsland ▸ Holiday Skins previews a skin straight from this repo's
    /// Skins folder, before it is pushed.
    /// </summary>
    public class SkinService : MonoBehaviour
    {
        /// <summary>Where skins.json and the skin folders are. skins.json may point elsewhere with "base".</summary>
        const string Home = "https://raw.githubusercontent.com/doverlydia/CapyIsland/main/Skins/";
        const string CachedScheduleKey = "CapyIsland.Skins.Schedule";
        const string GreetedKey = "CapyIsland.Skins.Greeted.";
        public const string EditorPreviewKey = "CapyIsland.SkinPreview";   // EditorPrefs: a skin id to preview

        [Serializable] class Schedule { public string @base; public Window[] skins; }
        [Serializable] class Window { public string id, from, to; }

        [Serializable] class Manifest
        {
            public int version;             // raise it to show the greeting again (pictures need nothing)
            public string[] sprites;       // sprite names; each picture is sprites/<name>.png
            public string tint;            // "#RRGGBB": multiplies the island's light
            public DecorEntry[] decor;
            public string music;           // e.g. "music/halloween.ogg": replaces the daytime tunes
            public FallEntry falling;      // drifting down over the island
            public Greeting greeting;
        }

        [Serializable] class DecorEntry { public string file; public int count = 6; public float height = 1f; }

        [Serializable] class FallEntry
        {
            public string file;
            public int count = 40;
            public float size = 0.025f;    // share of the screen's height
            public float speed = 0.08f;    // screen heights a second
            public float sway = 0.03f;
            public bool spin;
        }

        [Serializable] class Greeting
        {
            public string title, body;               // English
            public string titleHe, bodyHe;           // Hebrew, to a boy
            public string titleHeGirl, bodyHeGirl;   // Hebrew, to a girl (only where it differs)
            public string picture;                   // optional, e.g. "greeting.png"
        }

        static SkinService _instance;
        Schedule _schedule;
        string _wanted;        // the id the schedule asks for today, loaded or not
        bool _loading;
        float _nextDateCheck, _lastFetch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("Skins");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SkinService>();
            go.AddComponent<SkinPainter>();
            go.AddComponent<HolidayFall>();
        }

        async void Start()
        {
#if UNITY_EDITOR
            var preview = UnityEditor.EditorPrefs.GetString(EditorPreviewKey, "");
            if (preview.Length > 0)
            {
                var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Skins")).Replace('\\', '/');
                _schedule = new Schedule { @base = "file:///" + folder + "/", skins = new[] { new Window { id = preview } } };
                Debug.Log($"[Skins] Editor preview of \"{preview}\" from {folder}.");
                await Refresh();
                return;
            }
#endif
            _schedule = Parse(PlayerPrefs.GetString(CachedScheduleKey, ""));
            await Refresh();          // what was on last time, straight away
            await FetchSchedule();
            await Refresh();
        }

        void Update()
        {
            if (Time.unscaledTime < _nextDateCheck) return;
            _nextDateCheck = Time.unscaledTime + 60f;
            if (!_loading && Today() != _wanted) _ = Refresh();
        }

        async void OnApplicationPause(bool paused)
        {
            if (paused || Time.unscaledTime - _lastFetch < 15f * 60f) return;
#if UNITY_EDITOR
            if (UnityEditor.EditorPrefs.GetString(EditorPreviewKey, "").Length > 0) return;
#endif
            await FetchSchedule();
            await Refresh();
        }

        // ---------------------------------------------------------------- the schedule

        async Task FetchSchedule()
        {
            _lastFetch = Time.unscaledTime;
            // A new address every few minutes, so no cache between here and GitHub holds an old list for long.
            var json = await Text(Home + "skins.json?t=" + DateTime.UtcNow.Ticks / (TimeSpan.TicksPerMinute * 5));
            var fetched = Parse(json);
            if (fetched == null)
            {
                Debug.Log("[Skins] Couldn't read the skin list from GitHub; keeping the last one.");
                return;
            }
            if (string.IsNullOrEmpty(fetched.@base)) fetched.@base = Home;
            _schedule = fetched;
            PlayerPrefs.SetString(CachedScheduleKey, json);
            PlayerPrefs.Save();
        }

        static Schedule Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var s = JsonUtility.FromJson<Schedule>(json);
                if (s != null && string.IsNullOrEmpty(s.@base)) s.@base = Home;
                return s;
            }
            catch (Exception e) { Debug.LogWarning("[Skins] skins.json isn't valid JSON: " + e.Message); return null; }
        }

        /// <summary>The id today's date falls in (the first one listed, if two overlap), or null.</summary>
        string Today()
        {
            if (_schedule?.skins == null) return null;
            var today = DateTime.Now.Date;
            foreach (var w in _schedule.skins)
                if (w != null && !string.IsNullOrEmpty(w.id) && Holds(w, today)) return w.id.Trim();
            return null;
        }

        /// <summary>
        /// Dates are "2026-10-25" (that day only) or "12-11" (every year). A yearly window may run
        /// over New Year ("12-20" to "01-05"). A missing end is open.
        /// </summary>
        static bool Holds(Window w, DateTime today)
        {
            bool yearly = IsYearly(w.from) || IsYearly(w.to);
            if (!yearly)
            {
                if (!string.IsNullOrEmpty(w.from) && (!TryDate(w.from, out var from) || today < from)) return false;
                if (!string.IsNullOrEmpty(w.to) && (!TryDate(w.to, out var to) || today > to)) return false;
                return true;
            }
            int day = today.Month * 100 + today.Day;
            int start = DayOfYear(w.from, 101), end = DayOfYear(w.to, 1231);
            if (start < 0 || end < 0) return false;
            return start <= end ? day >= start && day <= end : day >= start || day <= end;
        }

        static bool IsYearly(string s) => !string.IsNullOrEmpty(s) && s.Trim().Length == 5;

        static int DayOfYear(string s, int fallback)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            s = s.Trim();
            if (s.Length != 5) s = s.Length >= 10 ? s.Substring(5, 5) : "";   // a full date in a yearly window: its month and day
            if (s.Length != 5 || s[2] != '-' || !int.TryParse(s.Substring(0, 2), out var m) || !int.TryParse(s.Substring(3, 2), out var d)) return -1;
            return m * 100 + d;
        }

        static bool TryDate(string s, out DateTime date) =>
            DateTime.TryParseExact(s.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

        // ---------------------------------------------------------------- putting it on

        async Task Refresh()
        {
            if (_loading) return;
            _wanted = Today();
            if (_wanted == Skins.ActiveId) return;
            if (_wanted == null) { Skins.Strip(); return; }

            _loading = true;
            try { await Load(_wanted, _schedule.@base); }
            catch (Exception e) { Debug.LogWarning($"[Skins] Couldn't load \"{_wanted}\": {e.Message}"); }
            finally { _loading = false; }
        }

        async Task Load(string id, string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl)) throw new Exception("the config has no \"base\" address");
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            string root = baseUrl + id + "/";
            // An editor preview reads the repo's own folder: never from (or into) the phone's copy,
            // so a picture changed on disk shows on the next Play straight away.
            bool fromDisk = root.StartsWith("file:", StringComparison.OrdinalIgnoreCase);
            bool cache = CanCache && !fromDisk;
            string Url(string file, string query) => fromDisk ? root + file : root + file + query;
            string cacheRoot = Path.Combine(Application.persistentDataPath, "Skins", id);

            // skin.json is small: always asked for fresh, and kept for the next offline start.
            var text = await Text(Url("skin.json", "?t=" + DateTime.UtcNow.Ticks / TimeSpan.TicksPerMinute));
            string kept = Path.Combine(cacheRoot, "skin.json");
            if (text != null) { if (cache) Save(kept, System.Text.Encoding.UTF8.GetBytes(text)); }
            else if (cache && File.Exists(kept)) text = File.ReadAllText(kept);
            if (text == null) throw new Exception("no skin.json at " + root);

            var m = JsonUtility.FromJson<Manifest>(text);
            string folder = Path.Combine(cacheRoot, "files");

            // Every file is checked with GitHub on every start, all at once: one the phone already
            // has comes back "not modified" (its ETag still matches) and is read from the phone; a
            // new or changed one is downloaded. So pictures added to or redrawn in a skin reach
            // players without anyone touching "version". Offline, the phone's copy is used.
            async Task<byte[]> Fetch(string file)
            {
                string local = Path.Combine(folder, file), tagFile = local + ".etag";
                bool have = cache && File.Exists(local);
                string tag = have && File.Exists(tagFile) ? File.ReadAllText(tagFile) : null;
                using var req = UnityWebRequest.Get(Url(file, ""));
                req.timeout = 30;
                if (!string.IsNullOrEmpty(tag)) req.SetRequestHeader("If-None-Match", tag);
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.responseCode == 304 && have) return File.ReadAllBytes(local);
                if (req.result == UnityWebRequest.Result.Success && req.downloadHandler.data is { Length: > 0 } bytes)
                {
                    if (cache)
                    {
                        Save(local, bytes);
                        var etag = req.GetResponseHeader("ETag");
                        if (!string.IsNullOrEmpty(etag)) Save(tagFile, System.Text.Encoding.UTF8.GetBytes(etag));
                    }
                    return bytes;
                }
                if (have) return File.ReadAllBytes(local);
                Debug.LogWarning($"[Skins] Missing {root + file}");
                return null;
            }

            var asked = new Dictionary<string, Task<byte[]>>();
            Task<byte[]> Get(string file) => asked.TryGetValue(file, out var t) ? t : asked[file] = Fetch(file);
            if (m.sprites != null) foreach (var n in m.sprites) if (!string.IsNullOrWhiteSpace(n)) Get("sprites/" + n.Trim() + ".png");
            if (m.decor != null) foreach (var d in m.decor) if (!string.IsNullOrEmpty(d?.file)) Get(d.file);
            if (!string.IsNullOrEmpty(m.falling?.file)) Get(m.falling.file);
            if (!string.IsNullOrEmpty(m.greeting?.picture)) Get(m.greeting.picture);

            var sprites = new Dictionary<string, Texture2D>();
            if (m.sprites != null)
                foreach (var name in m.sprites)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var tex = Picture(await Get("sprites/" + name.Trim() + ".png"), name);
                    if (tex != null) sprites[name.Trim()] = tex;
                }

            var decor = new List<Skins.Decor>();
            if (m.decor != null)
                foreach (var d in m.decor)
                {
                    if (d == null || string.IsNullOrEmpty(d.file) || d.count <= 0) continue;
                    var tex = Picture(await Get(d.file), d.file);
                    if (tex == null) continue;
                    var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0f), tex.height / Mathf.Max(0.1f, d.height));
                    sprite.name = Path.GetFileNameWithoutExtension(d.file);
                    decor.Add(new Skins.Decor { Sprite = sprite, Count = d.count, Height = d.height });
                }

            AudioClip music = null;
            if (!string.IsNullOrEmpty(m.music))
            {
                var bytes = cache ? await Get(m.music) : null;
                string url = cache && bytes != null ? new Uri(Path.Combine(folder, m.music)).AbsoluteUri : Url(m.music, "");
                music = await Clip(url, m.music);
            }

            Skins.Falling fall = null;
            if (m.falling != null && !string.IsNullOrEmpty(m.falling.file) && m.falling.count > 0)
            {
                var tex = Picture(await Get(m.falling.file), m.falling.file);
                if (tex != null)
                {
                    var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    sprite.name = Path.GetFileNameWithoutExtension(m.falling.file);
                    fall = new Skins.Falling
                    {
                        Sprite = sprite, Count = Mathf.Min(m.falling.count, 150), Size = m.falling.size,
                        Speed = m.falling.speed, Sway = m.falling.sway, Spin = m.falling.spin,
                    };
                }
            }

            var tint = Color.white;
            if (!string.IsNullOrEmpty(m.tint) && !ColorUtility.TryParseHtmlString(m.tint, out tint)) tint = Color.white;

            if (Today() != id)
            {
                // The date moved on while it downloaded.
                foreach (var t in sprites.Values) Destroy(t);
                foreach (var d in decor) { Destroy(d.Sprite.texture); Destroy(d.Sprite); }
                if (fall != null) { Destroy(fall.Sprite.texture); Destroy(fall.Sprite); }
                if (music != null) Destroy(music);
                return;
            }
            Skins.Wear(id, tint, sprites, decor, music, fall);
            Greet(id, m, Get);
        }

        // ---------------------------------------------------------------- the greeting

        async void Greet(string id, Manifest m, Func<string, Task<byte[]>> fetch)
        {
            var g = m.greeting;
            if (g == null || string.IsNullOrEmpty(g.title)) return;
            string key = GreetedKey + id + ".v" + m.version;
            if (PlayerPrefs.GetInt(key, 0) == 1) return;

            // Its words go into the translation tables, so the card speaks the player's language.
            UI.Loc.AddRuntime(g.title, g.titleHe, g.titleHeGirl);
            UI.Loc.AddRuntime(g.body, g.bodyHe, g.bodyHeGirl);

            Sprite picture = null;
            if (!string.IsNullOrEmpty(g.picture))
            {
                var tex = Picture(await fetch(g.picture), g.picture);
                if (tex != null) picture = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }

            // Once the island is up and nothing else has the screen (the opening, a visit's flight).
            while (this != null && (!UI.CardPopup.Ready || Visit.Active || UI.Cutscene.Playing || !UI.Loc.Chosen))
                await Task.Delay(1000);
            await Task.Delay(2500);
            if (this == null || Skins.ActiveId != id) return;
            UI.CardPopup.Show(g.title, g.body ?? "", picture);
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }

        // ---------------------------------------------------------------- downloads

        // WebGL's persistent storage needs syncing by hand; the browser cache does that job there.
        static bool CanCache => Application.platform != RuntimePlatform.WebGLPlayer;

        static void Save(string path, byte[] bytes)
        {
            if (!CanCache || bytes == null) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception e) { Debug.Log("[Skins] Couldn't keep " + path + ": " + e.Message); }
        }

        static async Task<byte[]> Bytes(string url)
        {
            using var req = UnityWebRequest.Get(url);
            req.timeout = 30;
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            return req.result == UnityWebRequest.Result.Success ? req.downloadHandler.data : null;
        }

        static async Task<string> Text(string url)
        {
            var bytes = await Bytes(url);
            return bytes != null ? System.Text.Encoding.UTF8.GetString(bytes) : null;
        }

        static Texture2D Picture(byte[] bytes, string name)
        {
            if (bytes == null) return null;
            // A PNG or a JPEG, nothing else: a server can answer with a page or an LFS pointer
            // instead, and Unity would log an error for each one it tried to read.
            bool png = bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == (byte)'P' && bytes[2] == (byte)'N' && bytes[3] == (byte)'G';
            bool jpeg = bytes.Length > 3 && bytes[0] == 0xFF && bytes[1] == 0xD8;
            if (!png && !jpeg) { Debug.LogWarning($"[Skins] {name} isn't a picture ({bytes.Length} bytes)."); return null; }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
            if (!tex.LoadImage(bytes, markNonReadable: true)) { Destroy(tex); return null; }
            return tex;
        }

        static async Task<AudioClip> Clip(string url, string file)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            var type = ext == ".mp3" ? AudioType.MPEG : ext == ".wav" ? AudioType.WAV : AudioType.OGGVORBIS;
            using var req = UnityWebRequestMultimedia.GetAudioClip(url, type);
            req.timeout = 60;
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            if (req.result != UnityWebRequest.Result.Success) { Debug.LogWarning("[Skins] No music at " + url); return null; }
            var clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip != null) clip.name = Path.GetFileNameWithoutExtension(file);
            return clip;
        }
    }
}
