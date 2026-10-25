using System;
using System.Collections.Generic;
using System.Globalization;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// The column of buttons on the right grows with you instead of arriving all at once. Settings
    /// and Help are there from the start; each of the others turns up the first time it means
    /// something:
    ///
    ///   Map         the first bridge rises (there is somewhere to travel to)
    ///   Collection  the opening is over (or "End tutorial" was pressed)
    ///   Hobbies     the first hobby is taken up
    ///   Events      a hobby a contest can use (fishing, bugs, birds) is taken up
    ///   Gifts       the second calendar day you play (the first time you come back)
    ///   Friends     Miso's second errand (Salty Kombu) is done — or at once if you already have a
    ///               friend, an airstrip or a friend request
    ///
    /// One at a time, each in its own calm moment: the button pops in, wears a "new" dot until it
    /// is tapped, and Old Yuzu says one line about what it's for. Never a tour. The map is the one
    /// that can arrive during the opening: it pops in with the bridge, and her word about it waits
    /// until she has finished showing you round (the pointing hand, MapNudge, follows it).
    ///
    /// Kept in preferences, per button: shown, still new (untapped), and told (her line said).
    /// An island from before this (or one brought back from the cloud) shows everything it has
    /// already reached at once, quietly. Visits keep their own short column and never come here.
    /// </summary>
    public class HudReveal : MonoBehaviour
    {
        public const string Map = "map", Collection = "collection", Hobbies = "hobbies", Events = "events", Gifts = "gifts", Friends = "friends";

        /// <summary>Who goes first when two are due together.</summary>
        static readonly string[] Order = { Map, Collection, Hobbies, Events, Gifts, Friends };

        const string ShownKey = "CapyIsland.Side.Shown.", NewKey = "CapyIsland.Side.New.", ToldKey = "CapyIsland.Side.Told.";
        /// <summary>The day this player first played ("yyyy-MM-dd"): Gifts waits for the next one.</summary>
        const string FirstDayKey = "CapyIsland.FirstPlayDay";
        const string LongAgo = "0001-01-01";
        /// <summary>The Attention key a new button waits its turn under.</summary>
        public const string TurnKey = "hud-reveal";
        /// <summary>A breather between one new button and the next.</summary>
        const float Gap = 10f;
        /// <summary>How long to wait for Old Yuzu to stop chatting before speaking over the end of it.</summary>
        const float MostWaitForQuiet = 20f;

        public static HudReveal Instance { get; private set; }

        /// <summary>Has this button joined the column?</summary>
        public static bool Shown(string id) => PlayerPrefs.GetInt(ShownKey + id, 0) == 1;

        /// <summary>Appeared, and not tapped yet: it wears a "new" dot.</summary>
        public static bool IsNew(string id) => PlayerPrefs.GetInt(NewKey + id, 0) == 1;

        static bool Told(string id) => PlayerPrefs.GetInt(ToldKey + id, 0) == 1;

        /// <summary>On show, but Old Yuzu still owes her word about it (the map, after the opening).</summary>
        public static bool Waiting(string id) => Shown(id) && IsNew(id) && !Told(id) && !Guidance.Off;

        /// <summary>The button was pressed: no more "new" dot.</summary>
        public static void Tapped(string id)
        {
            if (!IsNew(id)) return;
            PlayerPrefs.DeleteKey(NewKey + id);
            PlayerPrefs.Save();
        }

        /// <summary>Start over: a fresh island, whose column starts with Settings and Help again.</summary>
        public static void Forget()
        {
            foreach (var id in Order)
            {
                PlayerPrefs.DeleteKey(ShownKey + id);
                PlayerPrefs.DeleteKey(NewKey + id);
                PlayerPrefs.DeleteKey(ToldKey + id);
            }
            PlayerPrefs.SetString(FirstDayKey, Today);
            PlayerPrefs.Save();
        }

        static string Today => DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        // ---------------------------------------------------------------- when each is due

        /// <summary>Has the player reached the point where this button belongs?</summary>
        static bool Reached(string id)
        {
            switch (id)
            {
                case Map: return FirstBridgeOpen;
                case Collection: return !Attention.TutorialRunning || Guidance.Off;
                // A hobby card put off with "Later" comes back through the Hobbies button, so it shows for that too.
                case Hobbies: return AnyHobby(false) || CapyIsland.Game.HobbyCards.Waiting > 0;
                case Events: return AnyHobby(true);
                case Gifts: return string.CompareOrdinal(Today, PlayerPrefs.GetString(FirstDayKey, Today)) > 0;
                case Friends: return MisoPastKombu || Game.Friends.All.Count > 0 || Airport.Built || RealFriends.Requests.Count > 0;
            }
            return false;
        }

        static bool FirstBridgeOpen
        {
            get
            {
                var land = LandMap.Instance;
                if (land == null) return false;
                var first = land.Bridge("first_bridge");
                if (first != null) return first.open;
                foreach (var p in land.Pieces) if (p.isBridge && p.open) return true;   // a map without it: any bridge
                return false;
            }
        }

        /// <summary>Any hobby taken up (only those a contest counts, with <paramref name="contest"/>).</summary>
        static bool AnyHobby(bool contest)
        {
            if (contest) return HobbyLog.IsUnlocked(Hobby.Fishing) || HobbyLog.IsUnlocked(Hobby.Bugs) || HobbyLog.IsUnlocked(Hobby.Birding);
            foreach (Hobby h in Enum.GetValues(typeof(Hobby)))
                if (HobbyLog.IsUnlocked(h)) return true;
            return false;
        }

        /// <summary>Miso has thanked you for her second errand (Salty Kombu, quest 1).</summary>
        static bool MisoPastKombu
        {
            get
            {
                var quests = QuestSystem.Instance;
                if (quests == null) return false;
                foreach (var (giver, id, stage, _) in quests.Snapshot().quests)
                    if (giver == 1 && id == 1) return stage == (int)QuestSystem.Stage.GiftReceived;
                return false;
            }
        }

        static string Line(string id) => id switch
        {
            Map => StoryText.Hud.Map,
            Collection => StoryText.Hud.Collection,
            Hobbies => StoryText.Hud.Hobbies,
            Events => StoryText.Hud.Events,
            Gifts => StoryText.Hud.Gifts,
            Friends => StoryText.Hud.Friends,
            _ => "",
        };

        // ---------------------------------------------------------------- the column

        readonly Dictionary<string, GameObject> _buttons = new();
        bool _ready;
        float _readyAt, _nextCheck, _gapUntil, _talkingSince = -1f;

        /// <summary>A button that waits its turn: hidden until it has been shown once.</summary>
        public void Add(string id, GameObject root)
        {
            _buttons[id] = root;
            root.SetActive(Shown(id));
        }

        void Awake()
        {
            Instance = this;
            _readyAt = Time.unscaledTime + 0.6f;   // let the save load first
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.5f;
            if (Visit.Active || Visit.Travelling) return;
            if (!_ready)
            {
                if (Time.unscaledTime < _readyAt || SaveGame.Restoring) return;
                FirstLook();
                _ready = true;
            }
            Pump();
        }

        /// <summary>
        /// The first look at this island. A column with nothing on it yet, on an island past the
        /// opening, belongs to a game from before buttons came one at a time (or one just brought
        /// back from the cloud): what it has reached shows at once, with no fuss, and day one is long
        /// past. (A new game always has the map by the end of its opening, so it never lands here.)
        /// </summary>
        void FirstLook()
        {
            bool any = false;
            foreach (var id in Order) any |= Shown(id);
            if (!any && !Attention.TutorialRunning)
            {
                PlayerPrefs.SetString(FirstDayKey, LongAgo);
                foreach (var id in Order)
                    if (Reached(id)) Reveal(id, fuss: false);
            }
            if (!PlayerPrefs.HasKey(FirstDayKey)) PlayerPrefs.SetString(FirstDayKey, Today);
            PlayerPrefs.Save();
        }

        void Pump()
        {
            string next = null;
            foreach (var id in Order)
                if (!Shown(id) && Reached(id)) { next = id; break; }

            // During the opening only the map turns up, with the bridge: it pops in now, and Old
            // Yuzu's word about it waits (she is busy showing you across).
            if (Attention.TutorialRunning)
            {
                if (next == Map || (next != null && Guidance.Off)) Reveal(next, fuss: true);
                return;
            }
            // "End tutorial": the buttons still arrive one by one, but she says nothing.
            if (Guidance.Off)
            {
                if (next != null && Time.unscaledTime >= _gapUntil)
                {
                    Reveal(next, fuss: true);
                    _gapUntil = Time.unscaledTime + 1.5f;
                }
                return;
            }

            if (Time.unscaledTime < _gapUntil || Attention.Waiting(TurnKey) || MapNudge.Showing) return;
            // A word still owed (the map's, from the opening) before anything new.
            string turn = null;
            foreach (var id in Order)
                if (Waiting(id)) { turn = id; break; }
            turn ??= next;
            if (turn == null) return;

            // Let her finish what she's saying, though on a busy island not forever.
            if (HudTour.YuzuTalking)
            {
                if (_talkingSince < 0f) _talkingSince = Time.unscaledTime;
                if (Time.unscaledTime - _talkingSince < MostWaitForQuiet) return;
            }
            _talkingSince = -1f;
            string chosen = turn;
            Attention.Ask(TurnKey, () => TakeTurn(chosen),
                () => Instance != null && !Visit.Active && !Guidance.Off && (!Shown(chosen) || Waiting(chosen)), 3f);
        }

        /// <summary>Its turn: the button pops in (if it isn't there yet) and Old Yuzu says what it's for.</summary>
        static void TakeTurn(string id)
        {
            var me = Instance;
            if (me == null) return;
            if (!Shown(id)) me.Reveal(id, fuss: true);
            PlayerPrefs.SetInt(ToldKey + id, 1);
            PlayerPrefs.Save();
            // Dropped if, by the time she gets to it, it has already been tapped.
            Hints.Show(Line(id), () => IsNew(id));
            if (id == Map) MapNudge.Soon();
            me._gapUntil = Time.unscaledTime + Gap;
        }

        /// <summary>Joins the column: with <paramref name="fuss"/>, a bounce and a "new" dot; without, quietly (an old island).</summary>
        void Reveal(string id, bool fuss)
        {
            PlayerPrefs.SetInt(ShownKey + id, 1);
            if (fuss) PlayerPrefs.SetInt(NewKey + id, 1);
            // Her line is owed only for a fuss made while she is still guiding you.
            if (!fuss || Guidance.Off) PlayerPrefs.SetInt(ToldKey + id, 1);
            PlayerPrefs.Save();

            if (!_buttons.TryGetValue(id, out var root) || root == null) return;
            root.SetActive(true);
            if (!fuss) return;
            if (MobileHud.Instance != null) MobileHud.Instance.ShowSide();   // tucked away: bring the column back to show it
            Pop(root.transform);
        }

        static void Pop(Transform t)
        {
            t.DOKill();
            t.localScale = Vector3.one * 0.2f;
            DOTween.Sequence().SetUpdate(true).SetTarget(t)
                .Append(t.DOScale(1.15f, 0.25f).SetEase(Ease.OutQuad))
                .Append(t.DOScale(0.94f, 0.12f).SetEase(Ease.InOutQuad))
                .Append(t.DOScale(1f, 0.12f).SetEase(Ease.OutQuad))
                .AppendInterval(0.6f)
                .Append(t.DOPunchScale(Vector3.one * 0.12f, 0.5f, 6, 0.6f));
            Sfx.Play(SfxKind.Pop, 0.6f);
        }
    }
}
