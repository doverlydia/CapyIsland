#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace CapyIsland.Core
{
    /// <summary>
    /// Test builds only (Development Build, never the ones players get): lets a second copy of the
    /// game be driven from outside, so two-player features can be tried with one person at the
    /// keyboard. In the browser, the page gets <c>capyDev("Type.Method arg1|arg2")</c>; it calls
    /// a public static method in the game's code (awaiting a Task) and logs the answer as
    /// "[dev] ...", readable in the browser console.
    /// </summary>
    public class DevCommands : MonoBehaviour
    {
        const string ObjectName = "[DevCommands]";

#if UNITY_WEBGL
        [DllImport("__Internal")] static extern void CapyDevHook(string receiver);
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (GameObject.Find(ObjectName) != null) return;
            var go = new GameObject(ObjectName);
            DontDestroyOnLoad(go);
            go.AddComponent<DevCommands>();
#if UNITY_WEBGL
            CapyDevHook(ObjectName);
#endif
            // What the player is told, readable from outside (hooked again after island swaps reset events).
            Game.Hints.Shown += LogHint;
            Game.Hints.Answered += LogAnswer;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) =>
            {
                Game.Hints.Shown -= LogHint; Game.Hints.Shown += LogHint;
                Game.Hints.Answered -= LogAnswer; Game.Hints.Answered += LogAnswer;
            };
            Debug.Log("[dev] ready");
        }

        static void LogHint(string line) => Debug.Log("[hint] " + line);
        static void LogAnswer(string line) => Debug.Log("[answer] " + line);

        /// <summary>Ends the opening tutorial at once (a test player may fly straight away).</summary>
        public static string FinishTutorial()
        {
            var t = Game.TutorialDirector.Instance;
            if (t == null) return "no tutorial";
            t.SkipForReturningPlayer();
            return "finished=" + t.Finished;
        }

        /// <summary>Sends a friend a test message by name (they log it as "[RealFriends] Message from ...").</summary>
        public static async Task<string> Ping(string name)
        {
            foreach (var f in Game.RealFriends.All)
                if (f.name == name || f.code == name) return "sent=" + await Game.RealFriends.Send(f, "ping");
            return "no friend " + name;
        }

        /// <summary>"CapyIsland.Game.RealFriends.Add Miso#1234" — a static method or property, and its arguments split by |.</summary>
        public async void Run(string command)
        {
            try
            {
                command = command.Trim();
                int space = command.IndexOf(' ');
                string path = space < 0 ? command : command.Substring(0, space);
                string[] args = space < 0 ? new string[0] : command.Substring(space + 1).Split('|');
                int dot = path.LastIndexOf('.');
                var type = FindType(path.Substring(0, dot));
                string member = path.Substring(dot + 1);
                if (type == null) { Debug.Log("[dev] no type " + path.Substring(0, dot)); return; }
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

                var prop = type.GetProperty(member, flags);
                if (prop != null && args.Length == 0) { Debug.Log("[dev] " + path + " = " + Show(prop.GetValue(null))); return; }
                var field = type.GetField(member, flags);
                if (field != null && args.Length == 0) { Debug.Log("[dev] " + path + " = " + Show(field.GetValue(null))); return; }

                foreach (var m in type.GetMethods(flags))
                {
                    if (m.Name != member) continue;
                    var ps = m.GetParameters();
                    if (ps.Length < args.Length) continue;
                    var values = new object[ps.Length];
                    bool ok = true;
                    for (int i = 0; i < ps.Length; i++)
                    {
                        if (i >= args.Length) { if (!ps[i].HasDefaultValue) { ok = false; break; } values[i] = ps[i].DefaultValue; continue; }
                        try { values[i] = Convert(args[i], ps[i].ParameterType); } catch (Exception) { ok = false; break; }
                    }
                    if (!ok) continue;
                    object result = m.Invoke(null, values);
                    if (result is Task task)
                    {
                        await task;
                        var resultProp = task.GetType().GetProperty("Result");
                        result = resultProp != null && task.GetType().IsGenericType ? resultProp.GetValue(task) : "done";
                    }
                    Debug.Log("[dev] " + path + " -> " + Show(result));
                    return;
                }
                Debug.Log("[dev] no member " + path + " taking " + args.Length + " argument(s)");
            }
            catch (Exception e)
            {
                Debug.Log("[dev] error: " + (e.InnerException ?? e));
            }
        }

        // ---------------------------------------------------------------- shortcuts for tests
        // Called as "CapyIsland.Core.DevCommands.Setup Tess" and so on.

        /// <summary>A ready test player: named, intro and tutorial skipped, an airstrip by the king.</summary>
        public static void Setup(string name)
        {
            PlayerPrefs.SetString("CapyIsland.PlayerName", name);
            PlayerPrefs.SetInt("CapyIsland.IntroSeen", 1);
            PlayerPrefs.SetInt("CapyIsland.TutorialOff", 1);
            PlayerPrefs.SetInt("CapyIsland.TourSeen", 1);
            var king = FindFirstObjectByType<Game.PlayerAvatar>();
            Vector2 at = king != null ? (Vector2)king.transform.position + new Vector2(3f, -1f) : Vector2.zero;
            PlayerPrefs.SetString("CapyIsland.Airport", at.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + at.y.ToString(System.Globalization.CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
            Game.SaveGame.ResetStatics();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        public static string Friends()
        {
            var b = new System.Text.StringBuilder("code=" + Game.RealFriends.Code + " ready=" + Game.RealFriends.Ready + " live=" + Game.Live.Now + "/" + Game.Live.Connected + "; ");
            foreach (var f in Game.RealFriends.All) b.Append(f.name + "(" + f.code + (Game.RealFriends.IsOnline(f) ? ", online" : "") + ") ");
            foreach (var (id, name) in Game.RealFriends.Requests) b.Append("request:" + name + " ");
            return b.ToString();
        }

        public static async Task<bool> AcceptAll()
        {
            bool all = true;
            foreach (var (id, _) in new System.Collections.Generic.List<(string, string)>(Game.RealFriends.Requests)) all &= await Game.RealFriends.Accept(id);
            return all;
        }

        /// <summary>Flies to (or knocks on) a real friend by name.</summary>
        public static string Visit(string name)
        {
            foreach (var f in Game.RealFriends.All)
                if (f.name == name || f.code == name) { Game.Visit.Fly(f); return "flying to " + f.name; }
            return "no friend " + name;
        }

        public static void Home() => Game.Visit.FlyHome();

        /// <summary>Presses Yes on whatever prompt is open (a knock, a request).</summary>
        public static string Yes()
        {
            var p = UI.PromptScreen.Instance;
            if (p == null || !p.IsOpen) return "no prompt";
            var yes = typeof(UI.PromptScreen).GetField("_yes", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(p) as UnityEngine.UI.Button;
            yes?.onClick.Invoke();
            return "yes";
        }

        public static void Emote(int e) => Game.VisitorBot.PlayerEmote((Game.Emoji)e);

        /// <summary>Pops the bubble nearest the king and plants what was in it, dx/dy from the king (like a drag and drop).</summary>
        public static string PopAndPlant(float dx, float dy)
        {
            var king = FindFirstObjectByType<Game.PlayerAvatar>();
            if (king == null) return "no king";
            Game.Bubble best = null;
            float bestDistance = 999f;
            foreach (var b in FindObjectsByType<Game.Bubble>(FindObjectsSortMode.None))
            {
                if (b.Contents == null || b.Contents.GetComponent<Game.Placeable>() == null) continue;
                float d = Vector2.Distance(b.transform.position, king.transform.position);
                if (d < bestDistance) { bestDistance = d; best = b; }
            }
            if (best == null) return "no bubble";
            var item = best.Contents;
            string name = item.GetComponent<Game.CatalogItem>()?.StoreName;
            typeof(Game.Bubble).GetMethod("Collect", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(best, null);
            Vector2 spot = (Vector2)king.transform.position + new Vector2(dx, dy);
            item.transform.position = new Vector3(spot.x, spot.y, item.transform.position.z);
            item.SendMessage("OnDropped");
            return name + " placed=" + item.GetComponent<Game.Placeable>().Placed + " at " + spot;
        }

        public static string Walk(float dx, float dy)
        {
            var king = FindFirstObjectByType<Game.PlayerAvatar>();
            if (king == null) return "no king";
            Vector2 to = (Vector2)king.transform.position + new Vector2(dx, dy);
            king.WalkTo(to, 0.15f);
            return "walking to " + to;
        }

        public static string Where()
        {
            var king = FindFirstObjectByType<Game.PlayerAvatar>();
            var puppet = Game.Live.Puppet;
            return "me=" + (king != null ? (Vector2)king.transform.position : Vector2.zero) + " friend=" + (puppet != null ? (Vector2)puppet.transform.position : Vector2.zero)
                + " visiting=" + Game.Visit.Active + " live=" + Game.Live.Now + "/" + Game.Live.Connected;
        }

        static Type FindType(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(name);
                if (t != null) return t;
            }
            return null;
        }

        static object Convert(string text, Type to)
        {
            if (to == typeof(string)) return text;
            if (to == typeof(bool)) return bool.Parse(text);
            if (to == typeof(int)) return int.Parse(text);
            if (to == typeof(float)) return float.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
            if (to.IsEnum) return Enum.Parse(to, text);
            if (to == typeof(object)) return text;
            throw new InvalidCastException();
        }

        static string Show(object value)
        {
            if (value == null) return "null";
            if (value is string s) return s;
            if (value is System.Collections.IEnumerable list)
            {
                var b = new System.Text.StringBuilder("[");
                foreach (var item in list) b.Append(Show(item)).Append(", ");
                return b.Append("]").ToString();
            }
            if (value.GetType().IsSerializable || value.GetType().GetCustomAttribute<SerializableAttribute>() != null)
            {
                var json = JsonUtility.ToJson(value);
                if (json != "{}") return json;
            }
            return value.ToString();
        }
    }
}
#endif
