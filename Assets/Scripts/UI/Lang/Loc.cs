using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CapyIsland.UI
{
    public enum Language { English, Hebrew }

    /// <summary>
    /// The game is written in English, and every label is still set in English in code. Translation
    /// happens at the last moment, as TextMeshPro draws a string (<see cref="LocText"/>), by looking
    /// the English up in a table. So nothing that reads a label back, compares a line or builds a
    /// string out of parts has to know the language.
    ///
    /// Tables live in Resources/Lang/&lt;code&gt;/*.txt:
    ///   @ English
    ///   = translation
    /// An English side with {0}, {1}… is a template: it matches any string of that shape, and each
    /// captured piece (an item name, a friend's island) is itself translated before it is put back.
    /// A string that matches nothing is split into lines and each line tried on its own, so a title
    /// and body joined with "\n" still translate.
    ///
    /// Hebrew reads right to left. TextMeshPro lays an RTL line out in logical order, which keeps
    /// word wrap right; what it cannot do is keep a number or a Latin word inside the line reading
    /// left to right, so <see cref="Visual"/> pre-reverses those runs and mirrors brackets.
    /// </summary>
    public static class Loc
    {
        const string PrefKey = "CapyIsland.Language";

        /// <summary>Raised after the language changes. Every <see cref="LocText"/> redraws itself.</summary>
        public static event Action Changed;

        /// <summary>Bumped on every change, so a text can tell its cached translation is stale.</summary>
        public static int Version { get; private set; } = 1;

        static Language? _current;

        public static Language Current
        {
            get
            {
                if (!_current.HasValue) _current = (Language)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 0), 0, 1);
                return _current.Value;
            }
        }

        public static bool Rtl => Current == Language.Hebrew;

        /// <summary>True once the player has picked a language (the first-run welcome asks).</summary>
        public static bool Chosen => PlayerPrefs.HasKey(PrefKey);

        public static void Set(Language language)
        {
            bool same = Current == language;
            PlayerPrefs.SetInt(PrefKey, (int)language);
            PlayerPrefs.Save();
            if (same) return;
            _current = language;
            _loadedFor = null;
            _cache.Clear();
            Version++;
            UiKit.EnsureFallbacks();
            Changed?.Invoke();
        }

        /// <summary>The language's own name, written in that language, for the picker.</summary>
        public static string NativeName(Language l) => l == Language.Hebrew ? "עברית" : "English";

        static string Code(Language l) => l == Language.Hebrew ? "he" : "en";

        // ---------------------------------------------------------------- tables

        class Template
        {
            public Regex Pattern;
            public string Translation;
            public string Literal;   // the longest fixed piece of the English, for a cheap pre-check
            public int Specificity;
        }

        static readonly Dictionary<string, string> _exact = new(StringComparer.OrdinalIgnoreCase);
        static readonly List<Template> _templates = new();
        static readonly Dictionary<string, string> _cache = new();
        static Language? _loadedFor;

        static readonly Regex Placeholder = new(@"\{(\d)\}", RegexOptions.CultureInvariant);

        static void EnsureLoaded()
        {
            if (_loadedFor == Current) return;
            _loadedFor = Current;
            _exact.Clear();
            _templates.Clear();
            _cache.Clear();
            if (Current == Language.English) return;

            foreach (var asset in Resources.LoadAll<TextAsset>("Lang/" + Code(Current)))
                Parse(asset.text, asset.name);

            // Most specific first: "Day {0} of your streak" must win over "Day {0}".
            _templates.Sort((a, b) => b.Specificity.CompareTo(a.Specificity));
        }

        static void Parse(string text, string file)
        {
            string key = null;
            int line = 0;
            foreach (var raw in text.Split('\n'))
            {
                line++;
                var l = raw.TrimEnd('\r');
                if (l.StartsWith("@ ")) { key = Unescape(l.Substring(2)); continue; }
                if (!l.StartsWith("= ") || key == null) continue;
                Add(key, Unescape(l.Substring(2)), file, line);
                key = null;
            }
        }

        static void Add(string english, string translation, string file, int line)
        {
            if (string.IsNullOrEmpty(english) || string.IsNullOrEmpty(translation)) return;
            if (!Placeholder.IsMatch(english))
            {
                _exact[english] = translation;
                var trimmed = english.Trim();
                if (trimmed != english && !_exact.ContainsKey(trimmed)) _exact[trimmed] = translation.Trim();
                return;
            }

            // Build ^literal(?<p0>.+?)literal…$ from the English. A one-line template's
            // placeholders never span a line break, so a generic wrapper ("<color=…>{0}</color>")
            // cannot swallow a several-line message whose lines each have their own entry.
            bool multiline = english.IndexOf('\n') >= 0;
            string capture = multiline ? ".+?" : "[^\n]+?";
            var pattern = new StringBuilder("^");
            int at = 0, literal = 0;
            string longest = "";
            foreach (Match m in Placeholder.Matches(english))
            {
                var piece = english.Substring(at, m.Index - at);
                pattern.Append(Regex.Escape(piece));
                pattern.Append("(?<p").Append(m.Groups[1].Value).Append('>').Append(capture).Append(')');
                literal += piece.Length;
                if (piece.Trim().Length > longest.Length) longest = piece.Trim();
                at = m.Index + m.Length;
            }
            var tail = english.Substring(at);
            pattern.Append(Regex.Escape(tail)).Append('$');
            literal += tail.Length;
            if (tail.Trim().Length > longest.Length) longest = tail.Trim();
            if (literal == 0) return;   // "{0}" alone would match everything

            try
            {
                _templates.Add(new Template
                {
                    Pattern = new Regex(pattern.ToString(), RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant),
                    Translation = translation,
                    Literal = longest,
                    Specificity = literal,
                });
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning($"Lang/{file}:{line}: bad template \"{english}\": {e.Message}");
            }
        }

        static string Unescape(string s)
        {
            if (s.IndexOf('\\') < 0) return s;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char n = s[++i];
                    sb.Append(n == 'n' ? '\n' : n == 't' ? '\t' : n);
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        // ---------------------------------------------------------------- lookup

        /// <summary>The string in the current language, or the English if there is no translation.</summary>
        public static string T(string english)
        {
            if (string.IsNullOrEmpty(english) || Current == Language.English || !HasLetter(english)) return english;
            EnsureLoaded();
            if (_cache.TryGetValue(english, out var hit)) return hit;

            var result = Lookup(english, 0);
            if (result == null)
            {
                result = english;
                ReportMissing(english);
            }
            if (_cache.Count > 4000) _cache.Clear();   // counters and timers churn through strings
            _cache[english] = result;
            return result;
        }

        /// <summary>Formats a template the same way the tables do: T("{0} wood") then fills it.</summary>
        public static string F(string englishTemplate, params object[] args) => string.Format(T(englishTemplate), args);

        // One line only: tags opened on one line and closed on another wrap several entries.
        static readonly Regex WrappingTags = new(@"^((?:<[^<>]+>)+)([^\n]*?)((?:</[^<>]+>)+)$", RegexOptions.CultureInvariant);

        static string Lookup(string s, int depth)
        {
            if (depth > 3 || string.IsNullOrEmpty(s)) return null;
            if (!HasLetter(s)) return s;
            if (_exact.TryGetValue(s, out var exact)) return exact;

            var trimmed = s.Trim();
            if (trimmed.Length != s.Length && trimmed.Length > 0 && _exact.TryGetValue(trimmed, out exact))
                return s.Substring(0, s.IndexOf(trimmed, StringComparison.Ordinal)) + exact + s.Substring(s.IndexOf(trimmed, StringComparison.Ordinal) + trimmed.Length);

            foreach (var t in _templates)
            {
                if (t.Literal.Length > 0 && s.IndexOf(t.Literal, StringComparison.OrdinalIgnoreCase) < 0) continue;
                var m = t.Pattern.Match(s);
                if (!m.Success) continue;
                return Placeholder.Replace(t.Translation, p =>
                {
                    var g = m.Groups["p" + p.Groups[1].Value];
                    if (!g.Success) return p.Value;
                    return Lookup(g.Value, depth + 1) ?? g.Value;
                });
            }

            // "<b>Sawmill</b>": translate what is inside the tags.
            var wrapped = WrappingTags.Match(s);
            if (wrapped.Success)
            {
                var inner = Lookup(wrapped.Groups[2].Value, depth + 1);
                if (inner != null) return wrapped.Groups[1].Value + inner + wrapped.Groups[3].Value;
            }

            // A title and a body joined by line breaks: each line on its own.
            if (s.IndexOf('\n') >= 0)
            {
                var lines = s.Split('\n');
                bool any = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    var l = Lookup(lines[i], depth + 1);
                    if (l == null) continue;
                    any |= l != lines[i];
                    lines[i] = l;
                }
                if (any) return string.Join("\n", lines);
            }
            return null;
        }

        static bool HasLetter(string s)
        {
            foreach (char c in s)
                if (char.IsLetter(c)) return true;
            return false;
        }

        public static bool HasHebrew(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
                if (c >= '֐' && c <= '׿') return true;
            return false;
        }

        // ---------------------------------------------------------------- right to left

        static bool IsRtl(char c) => (c >= '֐' && c <= 'ࣿ') || (c >= 'יִ' && c <= '﷿') || (c >= 'ﹰ' && c <= '﻿');
        static bool IsLtrStrong(char c) => !IsRtl(c) && (char.IsLetterOrDigit(c) || c == '%');

        /// <summary>
        /// Prepares logical Hebrew for TextMeshPro's right-to-left layout: runs of digits and Latin
        /// letters are reversed so they still read left to right, and brackets swap so they face
        /// the right way. Rich-text tags pass through untouched.
        /// </summary>
        public static string Visual(string logical)
        {
            if (string.IsNullOrEmpty(logical)) return logical;
            var chars = logical.ToCharArray();
            var isTag = new bool[chars.Length];
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] != '<') continue;
                int close = logical.IndexOf('>', i + 1);
                if (close < 0) break;
                int open = logical.IndexOf('<', i + 1);
                if (open >= 0 && open < close) continue;
                if (close == i + 1) continue;
                for (int k = i; k <= close; k++) isTag[k] = true;
                i = close;
            }

            int n = chars.Length;
            var inRun = new bool[n];
            int idx = 0;
            while (idx < n)
            {
                if (isTag[idx] || !IsLtrStrong(chars[idx])) { idx++; continue; }

                // An LTR run: from this strong character to the last strong LTR character before
                // the next RTL letter, line break or tag; neutrals in between ride along.
                int start = idx, last = idx, j = idx + 1;
                while (j < n && !isTag[j] && chars[j] != '\n' && !IsRtl(chars[j]))
                {
                    if (IsLtrStrong(chars[j])) last = j;
                    j++;
                }
                Array.Reverse(chars, start, last - start + 1);
                for (int k = start; k <= last; k++) inRun[k] = true;
                idx = last + 1;
            }

            // Brackets in the right-to-left flow face the other way; those inside a reversed run
            // already read correctly.
            for (int i = 0; i < n; i++)
            {
                if (isTag[i] || inRun[i]) continue;
                chars[i] = chars[i] switch
                {
                    '(' => ')', ')' => '(',
                    '[' => ']', ']' => '[',
                    '{' => '}', '}' => '{',
                    '«' => '»', '»' => '«',
                    _ => chars[i],
                };
            }
            return new string(chars);
        }

        // ---------------------------------------------------------------- finding gaps

        static readonly HashSet<string> _missing = new();

        /// <summary>
        /// In the editor and development builds, English that reached the screen untranslated is
        /// appended to Logs/loc_missing_&lt;code&gt;.txt, in the table format, ready to fill in.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        static void ReportMissing(string english)
        {
            int latin = 0;
            foreach (char c in english) if (c < 128 && char.IsLetter(c)) latin++;
            if (latin < 2 || !_missing.Add(english)) return;
            try
            {
                var path = System.IO.Path.Combine(Application.dataPath, "..", "Logs", "loc_missing_" + Code(Current) + ".txt");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                System.IO.File.AppendAllText(path, "@ " + english.Replace("\\", "\\\\").Replace("\n", "\\n") + "\n= \n\n");
            }
            catch (Exception) { /* a log is a convenience, never a failure */ }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            Changed = null;
            _current = null;
            _loadedFor = null;
            _cache.Clear();
        }
    }
}
