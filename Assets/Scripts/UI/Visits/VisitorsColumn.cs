using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Who else is on this island right now, as round faces under the clock (your host first, then
    /// up to a couple more; a party's crowd beyond that is a "+N"). At home, tap a visitor to ask
    /// them to leave (or, a real player, to turn chat on or off, or block them); on someone else's
    /// island, tap your host to add them as a friend (or unfriend or block them).
    /// </summary>
    public class VisitorsColumn : MonoBehaviour
    {
        const float Size = 84f, Gap = 14f;
        const int MaxFaces = 3;

        RectTransform _faces;
        readonly List<VisitorBot> _shown = new();
        int _more;
        float _next;

        public static VisitorsColumn Create(Transform parent)
        {
            var root = UiKit.Rect("Visitors", parent);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 1f);
            // Under the clock pill (ClockPill: 52 tall, starting 52 under the badge), with a gap.
            root.anchoredPosition = new Vector2(16f + (ProfileBadge.Size - Size) * 0.5f, -104f - ProfileBadge.Size - 52f - 52f - 16f);
            root.sizeDelta = new Vector2(Size, Size + 30f);
            var c = root.gameObject.AddComponent<VisitorsColumn>();
            return c;
        }

        void OnEnable() => CoBuild.Changed += Rebuild;
        void OnDisable() => CoBuild.Changed -= Rebuild;

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            var now = new List<VisitorBot>();
            foreach (var b in VisitorBot.All)
                if (b != null && !b.Leaving) now.Add(b);
            // Your host first; then in the order they came.
            now.Sort((a, b) => (b.As == VisitorBot.Role.Host).CompareTo(a.As == VisitorBot.Role.Host));
            int more = Mathf.Max(0, now.Count - MaxFaces);
            if (now.Count > MaxFaces) now.RemoveRange(MaxFaces, now.Count - MaxFaces);
            if (more == _more && Same(now)) return;
            _shown.Clear();
            _shown.AddRange(now);
            _more = more;
            Rebuild();
        }

        bool Same(List<VisitorBot> now)
        {
            if (now.Count != _shown.Count) return false;
            for (int i = 0; i < now.Count; i++) if (now[i] != _shown[i]) return false;
            return true;
        }

        void Rebuild()
        {
            if (_faces != null) Destroy(_faces.gameObject);
            _faces = null;
            if (_shown.Count == 0) return;
            _faces = UiKit.Rect("Faces", transform);
            UiKit.Stretch(_faces);
            float y = 0f;
            for (int i = 0; i < _shown.Count; i++)
            {
                var bot = _shown[i];
                if (bot == null) continue;
                y = Face(bot, y);
            }
            if (_more > 0)
            {
                var chip = UiKit.Image(_faces, "More", UiKit.Rounded(16), UiKit.HudGlass);
                UiKit.Place(chip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -y), new Vector2(Size, 40f));
                chip.rectTransform.pivot = new Vector2(0.5f, 1f);
                chip.raycastTarget = false;
                var t = UiKit.Text(chip.transform, "Count", "+" + _more, 24f, Color.white, true, TextAlignmentOptions.Center);
                UiKit.Stretch(t.rectTransform);
            }
            _faces.localScale = Vector3.one * 0.3f;
            _faces.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        /// <summary>One face (and name) from <paramref name="top"/> down; returns where the next one starts.</summary>
        float Face(VisitorBot bot, float top)
        {
            var who = bot.Who;
            var block = UiKit.Rect("Face", _faces);
            block.anchorMin = block.anchorMax = block.pivot = new Vector2(0.5f, 1f);
            block.anchoredPosition = new Vector2(0f, -top);
            block.sizeDelta = new Vector2(Size, Size + 30f);

            var avatar = UiKit.Avatar(block, who, Size);
            avatar.anchorMin = avatar.anchorMax = avatar.pivot = new Vector2(0.5f, 1f);
            avatar.anchoredPosition = Vector2.zero;
            var dot = UiKit.Image(block, "Online", UiKit.Circle, UiKit.Hex("5DBB63"));
            UiKit.Place(dot.rectTransform, new Vector2(1f, 1f), new Vector2(-4f, -Size + 22f), new Vector2(22f, 22f));
            dot.raycastTarget = false;

            var pill = UiKit.Image(block, "NamePill", UiKit.Rounded(14), UiKit.HudGlass);
            UiKit.Place(pill.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(Size + 30f, 30f));
            pill.raycastTarget = false;
            var name = UiKit.Text(pill.transform, "Name", who.name, 20f, Color.white, true, TextAlignmentOptions.Center);
            UiKit.Stretch(name.rectTransform, 4f, 0f, 4f, 0f);
            name.enableAutoSizing = true;
            name.fontSizeMin = 12f;
            name.fontSizeMax = 20f;
            name.textWrappingMode = TextWrappingModes.NoWrap;

            var hit = UiKit.Image(block, "Hit", UiKit.Circle, new Color(0f, 0f, 0f, 0f));
            UiKit.Place(hit.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(Size, Size));
            var b = hit.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => Tapped(bot));
            hit.gameObject.AddComponent<PressSquash>();

            float next = top + Size + 30f + Gap;
            if (BuildButton(block, bot)) next += 54f;
            return next;
        }

        /// <summary>Under your host's face: ask to build together, or a note that you may. True if anything went there.</summary>
        static bool BuildButton(RectTransform block, VisitorBot bot)
        {
            if (bot == null || bot.As != VisitorBot.Role.Host || !Visit.HostIsFriend) return false;
            if (CoBuild.Allowed)
            {
                var note = UiKit.Image(block, "Building", UiKit.Rounded(14), UiKit.Hex("5DBB63"));
                UiKit.Place(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -40f), new Vector2(Size + 50f, 34f));
                note.raycastTarget = false;
                var t = UiKit.Text(note.transform, "Label", "Building", 20f, Color.white, true, TextAlignmentOptions.Center);
                UiKit.Stretch(t.rectTransform, 4f, 0f, 4f, 0f);
                return true;
            }
            var b = UiKit.Button(block, "BuildTogether", CoBuild.Asking ? "Asking…" : "Build together", UiKit.ButtonStyle.Spring, CoBuild.Ask, 18f, IconPainter.House);
            var rt = (RectTransform)b.transform.parent;
            UiKit.Place(rt, new Vector2(0.5f, 0f), new Vector2(0f, -46f), new Vector2(Size + 96f, 48f));
            b.interactable = !CoBuild.Asking;
            return true;
        }

        static void Tapped(VisitorBot bot)
        {
            if (bot == null) return;
            var who = bot.Who;
            Sfx.Play(SfxKind.Tap, 0.5f);
            if (bot.As == VisitorBot.Role.Guest)
            {
                // A fellow guest at someone else's island: wave, or make friends (or block them).
                if (Visit.Active)
                {
                    var blockGuest = BlockChoice(who);
                    if (Friends.IsFriend(who.id))
                        PromptScreen.Ask("Your friend", who, Loc.F("{0} is here too!", who.name), "Wave", "Close", () => VisitorBot.PlayerEmote(Emoji.Wave), null, blockGuest);
                    else
                        PromptScreen.Ask("Make a friend?", who, Loc.F("{0} is here too! Ask them to be friends?", who.name), "Add friend", "Not now", () => Befriend(who), null, blockGuest);
                    return;
                }
                // Your visitor: a real player can also have chat turned on or off, or be blocked
                // (which sends them home too).
                List<(string, System.Action)> more = null;
                if (bot.Remote && who.IsReal)
                {
                    bool chatting = VisitChat.GuestAllowed(who);
                    more = new List<(string, System.Action)>
                    {
                        (chatting ? "Turn chat off" : "Turn chat on", () => VisitChat.SetGuest(who, !chatting)),
                        ("Block", () => BlockPrompt.Ask(who)),
                    };
                }
                PromptScreen.Ask("Visitor", who, Loc.F("{0} is visiting your island and helping out.", who.name), "Ask to leave", "Let them stay", () =>
                {
                    if (bot.Remote) Live.AskToLeave(who);   // a real player: their game flies them home
                    else bot.SendHome();
                    Hints.Show(Loc.F("{0} waves goodbye and heads home.", who.name));
                }, null, more);
                return;
            }
            // On their island.
            var blockHost = BlockChoice(who);
            if (Friends.IsFriend(who.id))
                PromptScreen.Ask("Your friend", who, $"You're on {who.name}'s island.", "Unfriend", "Close", () =>
                {
                    Friends.Remove(who);
                    Hints.Show(who.name + " is no longer your friend.");
                }, null, blockHost);
            else if (who.IsReal)
                PromptScreen.Ask("Make a friend?", who, Loc.F("You're on {0}'s island. Ask them to be friends?", who.name), "Add friend", "Not now", () => Befriend(who), null, blockHost);
            else
                PromptScreen.Ask("Make a friend?", who, $"You're on {who.name}'s island. Add them as a friend to visit again — their island will be kept.", "Add friend", "Not now", () =>
                {
                    Visit.BefriendHost();
                    Hints.Show(who.name + " is your friend now!");
                });
        }

        /// <summary>A "Block" choice for a real player's card (none for an islander the game made up).</summary>
        static List<(string, System.Action)> BlockChoice(FriendProfile who) =>
            who != null && who.IsReal ? new List<(string, System.Action)> { ("Block", () => BlockPrompt.Ask(who)) } : null;

        /// <summary>A real player gets a friend request (they say yes on their side); an islander is simply added.</summary>
        static async void Befriend(FriendProfile who)
        {
            if (!who.IsReal)
            {
                Friends.Add(who);
                Hints.Show(who.name + " is your friend now!");
                return;
            }
            if (string.IsNullOrEmpty(who.code)) { Hints.Show("Couldn't find their friend code just now. Try again in a moment."); return; }
            string problem = await RealFriends.Add(who.code);
            Hints.Show(problem ?? Loc.F("Request sent to {0}. You'll be friends once they accept.", who.name));
        }
    }
}
