using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Typing to each other during a live visit, with the host's say-so — the same shape as
    /// building together (CoBuild). Chat starts off. When a real person lands on your island
    /// you're asked once whether to chat with them, and you can turn it on or off for each
    /// visitor at any time from their face in the corner (VisitorsColumn). Their game hears your
    /// answer over the live link, and their chat button comes and goes with it.
    ///
    /// The host's game is the hub, as for everything live: a visitor's line goes to the host,
    /// who only takes it from someone they let chat, and passes it on only to the others they
    /// let chat. So a visitor with chat off can neither send nor see a line.
    ///
    /// A party or a contest (Gathering) is a crowd, so nobody is asked: everyone who comes may
    /// chat (Live opens it for them as they land), and the host can still turn it off for one
    /// person from their face. Because a crowd mixes friends and strangers, every game shows only
    /// its own player's friends' words: a line from anyone else is drawn as "…" (their name still
    /// shows, so you can see someone spoke). Your own lines always show. This holds on an
    /// ordinary visit too, where the people chatting are friends anyway.
    ///
    /// Lines are what players typed, so they are never translated. They are cleaned on the way
    /// out and again on the way in: no rich-text tags, no line breaks, at most
    /// <see cref="MaxLength"/> letters, and a short list of rude words masked.
    /// </summary>
    public static class VisitChat
    {
        /// <summary>The longest line anyone can send.</summary>
        public const int MaxLength = 80;

        /// <summary>How many lines the chat panel keeps.</summary>
        const int Keep = 20;

        /// <summary>One line of chat.</summary>
        public class Line
        {
            public string From;   // the sender's id on the live link
            public string Name;
            public string Text;
            public bool Mine;
        }

        /// <summary>On someone's island: they turned chat on for you.</summary>
        public static bool Allowed { get; private set; }

        /// <summary>Allowed (or, at home, who may chat) changed: the chat button shows or hides.</summary>
        public static event Action Changed;

        /// <summary>A line was said (by you or anyone you can hear): the panel and the speech bubbles show it.</summary>
        public static event Action<Line> Heard;

        static readonly HashSet<string> _guests = new();   // hosting: visitors you let chat, by player id
        static readonly HashSet<string> _hushed = new();   // hosting a gathering: those you turned off, kept off if they come back
        static readonly List<Line> _lines = new();

        /// <summary>What a line from someone who isn't your friend shows instead of their words.</summary>
        public const string Hidden = "…";

        /// <summary>How chat works at a party or contest, said once to the host and to each guest.</summary>
        const string PartyRule = "Everyone here can chat! You'll see what your friends say; others show as …";

        /// <summary>
        /// May you read this line? Your own, and your real friends', yes; anyone else's (a stranger
        /// at a party) is shown as <see cref="Hidden"/>. Asked each time a line is drawn, so a
        /// line from someone you befriend mid-party shows from then on.
        /// </summary>
        public static bool Readable(Line line) => line != null && (line.Mine || RealFriends.Find(line.From) != null);

        /// <summary>The words to draw for a line: as typed, or "…" (see <see cref="Readable"/>).</summary>
        public static string Shown(Line line) => line == null ? "" : Readable(line) ? line.Text : Hidden;

        /// <summary>The last few lines, oldest first.</summary>
        public static IReadOnlyList<Line> Lines => _lines;

        // Once per launch, like Live: the visit (and its chat) outlives the flight over, and the
        // HUD's chat button unhooks itself when its island goes.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void ResetSession()
        {
            Allowed = false;
            _guests.Clear();
            _hushed.Clear();
            _lines.Clear();
            Changed = null;
            Heard = null;
        }

        /// <summary>A live visit ended or a new one is starting: nobody may chat, and the old lines go.</summary>
        public static void Reset()
        {
            bool had = Allowed || _guests.Count > 0 || _hushed.Count > 0 || _lines.Count > 0;
            Allowed = false;
            _guests.Clear();
            _hushed.Clear();
            _lines.Clear();
            if (had) Changed?.Invoke();
        }

        /// <summary>Can you chat right now (someone real is here and the host said yes)?</summary>
        public static bool CanChat
        {
            get
            {
                if (!Live.Connected) return false;
                if (Live.Now == Live.Mode.Visiting) return Allowed;
                if (Live.Now != Live.Mode.Hosting) return false;
                foreach (var g in Live.Guests) if (g != null && GuestAllowed(g)) return true;
                return false;
            }
        }

        // ---------------------------------------------------------------- the host decides

        /// <summary>At home: may this visitor chat?</summary>
        public static bool GuestAllowed(FriendProfile guest) => guest?.member != null && _guests.Contains(guest.member);

        /// <summary>
        /// A real person has landed on your island: chat with them? Asked once, when the screen
        /// is free (Attention), so it waits its turn behind the "build together?" question.
        /// </summary>
        public static void AskHost(FriendProfile guest)
        {
            if (guest == null || !guest.IsReal || Visit.Active || GuestAllowed(guest)) return;
            Attention.Ask("chat:" + guest.member, () =>
                UI.PromptScreen.Ask("Chat?", guest,
                    UI.Loc.F("Chat with <b>{0}</b>? You can turn it off any time: tap their face in the corner.", guest.name),
                    "Allow chat", "Not now",
                    () => SetGuest(guest, true), null, null, UI.IconPainter.Chat),
                () => Live.Now == Live.Mode.Hosting && !Visit.Active && VisitorBot.IsHere(guest.id) && !GuestAllowed(guest)
                      && !RealFriends.IsBlocked(guest.member) && !Gathering.Hosting);
        }

        /// <summary>
        /// Hosting a party or contest: this guest may chat without being asked (Live calls it as
        /// they land, and for anyone already here when it starts). Not someone you blocked, nor
        /// someone you turned off earlier in this gathering. True if chat just opened for them,
        /// and Live then tells their game.
        /// </summary>
        public static bool Welcome(FriendProfile guest)
        {
            if (guest?.member == null || _guests.Contains(guest.member) || _hushed.Contains(guest.member)) return false;
            if (RealFriends.IsBlocked(guest.member)) return false;
            bool first = _guests.Count == 0 && _hushed.Count == 0;
            _guests.Add(guest.member);
            Changed?.Invoke();
            // The host learns the party's rule once, with the first guest, as each guest does on arrival.
            if (first) Hints.Show(PartyRule);
            return true;
        }

        /// <summary>Turns chat on or off for one visitor; their game hears it straight away.</summary>
        public static void SetGuest(FriendProfile guest, bool on)
        {
            if (guest?.member == null) return;
            if (on && RealFriends.IsBlocked(guest.member)) return;
            // At a gathering everyone starts on, so turning someone off has to stick (Welcome skips them).
            if (on) _hushed.Remove(guest.member);
            else if (Gathering.Hosting) _hushed.Add(guest.member);
            bool changed = on ? _guests.Add(guest.member) : _guests.Remove(guest.member);
            if (!changed) return;
            Live.AnswerChat(guest, on);
            Changed?.Invoke();
            Hints.Show(on ? UI.Loc.F("You can chat with {0} now.", guest.name) : UI.Loc.F("Chat with {0} is off.", guest.name));
        }

        /// <summary>A visitor went home: next time they come, you're asked again.</summary>
        public static void GuestGone(string member)
        {
            if (member != null && _guests.Remove(member)) Changed?.Invoke();
        }

        // ---------------------------------------------------------------- the visitor hears

        /// <summary>The host turned chat on or off for you.</summary>
        public static void Granted(bool on, string hostName)
        {
            if (Allowed == on) return;
            Allowed = on;
            Changed?.Invoke();
            if (on)
            {
                Sfx.Play(SfxKind.Plop, 0.5f);
                // At a party everyone may talk, and strangers' words are hidden: say so once.
                Hints.Show(Gathering.Active || Gathering.JoiningSomething
                    ? PartyRule
                    : UI.Loc.F("{0} turned chat on. Tap the chat button to talk!", hostName));
            }
            else Hints.Show(UI.Loc.F("{0} turned chat off.", hostName));
        }

        // ---------------------------------------------------------------- lines

        /// <summary>You say something: it goes out over the live link, and shows here too.</summary>
        public static void Say(string typed)
        {
            if (!CanChat) return;
            string text = Clean(typed);
            if (text.Length == 0) return;
            Live.SendChat(text);
            Add(new Line { From = Live.MyId, Name = Clean(PlayerProfile.Name), Text = text, Mine = true });
        }

        /// <summary>A line from someone else (Live has already checked they may chat with you).</summary>
        public static void Received(string from, string name, string typed)
        {
            if (RealFriends.IsBlocked(from)) return;
            string text = Clean(typed);
            if (text.Length == 0) return;
            string who = Clean(name);
            Add(new Line { From = from, Name = who.Length > 0 ? who : "Capy", Text = text });
        }

        static void Add(Line line)
        {
            _lines.Add(line);
            while (_lines.Count > Keep) _lines.RemoveAt(0);
            Heard?.Invoke(line);
        }

        // ---------------------------------------------------------------- cleaning a line

        /// <summary>
        /// A typed line made safe to show: tags and line breaks out, trimmed to
        /// <see cref="MaxLength"/>, rude words masked. Empty if nothing is left.
        /// </summary>
        public static string Clean(string typed)
        {
            if (string.IsNullOrEmpty(typed)) return "";
            var sb = new StringBuilder(typed.Length);
            foreach (char c in typed)
            {
                if (c == '<' || c == '>') continue;          // no rich text: nobody else's colours or sizes
                if (char.IsControl(c)) { sb.Append(' '); continue; }
                sb.Append(c);
            }
            string s = sb.ToString().Trim();
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            if (s.Length > MaxLength) s = s.Substring(0, MaxLength).TrimEnd();
            return Mask(s);
        }

        // A small, simple list: it catches the obvious, not the determined. Stems match inside a
        // word ("shitty", "fuuuck"); short words only on their own ("ass" but never "class", and
        // the Hebrew for cup is left alone).
        static readonly string[] Stems =
        {
            "fuck", "fuk", "shit", "cunt", "bitch", "asshole", "bastard", "slut", "whore", "faggot",
            "nigger", "nigga", "retard", "wank", "twat", "dickhead", "motherf", "bollock",
            "שרמוט", "זונה", "זונות", "תזדיין", "מזדיין", "לזיין", "כוסאמק", "כוסעמק", "כוסית", "מניאק", "בנזונה",
        };

        static readonly string[] Words =
        {
            "ass", "dick", "cock", "pussy", "fag", "piss", "tits", "wtf", "stfu",
            "זין", "זיין", "חרא",
        };

        static string[] _stems;
        static HashSet<string> _words;

        static string Mask(string s)
        {
            if (_stems == null)
            {
                // The lists folded the same way as what's typed.
                _stems = Array.ConvertAll(Stems, w => Squeeze(Fold(w)));
                _words = new HashSet<string>(Array.ConvertAll(Words, Fold));
            }
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                if (!IsWordChar(s[i])) { sb.Append(s[i++]); continue; }
                int start = i;
                while (i < s.Length && IsWordChar(s[i])) i++;
                string word = s.Substring(start, i - start);
                sb.Append(Rude(word) ? "***" : word);
            }
            return sb.ToString();
        }

        static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '$' || c == '@' || c == '*' || c == '_' || c == '!';

        static bool Rude(string word)
        {
            string w = Fold(word);
            if (w.Length == 0) return false;
            if (_words.Contains(w)) return true;
            string squeezed = Squeeze(w);
            foreach (var stem in _stems) if (squeezed.Contains(stem)) return true;
            return false;
        }

        /// <summary>Lower case, l33t undone, Hebrew final letters made ordinary, stars and dots dropped.</summary>
        static string Fold(string word)
        {
            var sb = new StringBuilder(word.Length);
            foreach (char raw in word.ToLowerInvariant())
            {
                char c = raw switch
                {
                    '0' => 'o', '1' => 'i', '3' => 'e', '4' => 'a', '5' => 's', '7' => 't',
                    '$' => 's', '@' => 'a', '!' => 'i',
                    'ך' => 'כ', 'ם' => 'מ', 'ן' => 'נ', 'ף' => 'פ', 'ץ' => 'צ',
                    '*' or '_' => '\0',
                    _ => raw,
                };
                if (c != '\0') sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Repeated letters once ("fuuuck" → "fuck").</summary>
        static string Squeeze(string w)
        {
            var sb = new StringBuilder(w.Length);
            foreach (char c in w) if (sb.Length == 0 || sb[sb.Length - 1] != c) sb.Append(c);
            return sb.ToString();
        }
    }
}
