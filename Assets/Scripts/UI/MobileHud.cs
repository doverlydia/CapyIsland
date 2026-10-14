using System.Collections;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The phone-first layer of the HUD, added to the island builder's canvas at startup: edge
    /// arrows, the goal tracker, round Menu and Map buttons, and the full-screen sheets behind them.
    /// It also gives the builder's dialogs the same rounded, solid look.
    ///
    /// Built at runtime so rebuilding the island never loses it, and so there is one place that
    /// decides how the HUD is layered:
    ///   resource bar → edge arrows → tracker and buttons → ghost, dialogs → screens → cards.
    /// </summary>
    public class MobileHud : MonoBehaviour
    {
        public static MobileHud Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Attach();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach();

        static void Attach()
        {
            if (Instance != null) return;
            var hud = GameObject.Find("HUD");
            if (hud == null || hud.GetComponent<Canvas>() == null) return;
            hud.AddComponent<MobileHud>();
        }

        Transform _safe;

        void Awake()
        {
            Instance = this;
            _safe = transform.Find("SafeArea");
            if (_safe == null) _safe = transform;
            SettingsScreen.ApplySaved();
            BorrowResourceIcons();
            Restyle();
            Build();
            TextWatcher.Sweep();
        }

        void OnEnable()
        {
            Wardrobe.Unlocked += AnnounceOutfit;
            JournalRewards.Completed += AnnounceJournal;
        }

        void OnDisable()
        {
            Wardrobe.Unlocked -= AnnounceOutfit;
            JournalRewards.Completed -= AnnounceJournal;
        }

        static void AnnounceJournal(Hobby h, WearItem hat)
        {
            string page = h switch { Hobby.Birding => "bird", Hobby.Bugs => "bug", _ => "fish" };
            string body = $"You found every {page} on the islands! Here are {JournalRewards.Gems} gems"
                          + (hat != null ? $", and the <b>{hat.Name}</b> is waiting in your wardrobe." : ".");
            CardPopup.Show($"The {page} journal is complete!", body, hat != null ? Wardrobe.Art(hat) : IconPainter.Trophy);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>A new outfit waits its turn (never during the opening).</summary>
        static void AnnounceOutfit(WearItem item) =>
            Attention.Ask("outfit:" + item.Id, () =>
            {
                // The very first one also says where the wardrobe is (there's no menu to find it in).
                string where = "";
                if (!HudTour.Seen("wardrobe"))
                {
                    HudTour.MarkSeen("wardrobe");
                    where = " To try it on, tap your picture at the top left, then Change outfit.";
                }
                CardPopup.Show("New outfit!", "<b>" + item.Name + "</b> is waiting for you in the Wardrobe." + where, Wardrobe.Art(item));
            });

        GameObject _giftDot, _eventsDot;

        /// <summary>
        /// A contest that ended while you were away: how you did, once, when the screen is free.
        /// (The daily gift no longer opens by itself — its red dot says it's there.)
        /// </summary>
        IEnumerator Start()
        {
            HudTour.BeginIfFirstTime(this);
            while (true)
            {
                yield return new WaitForSecondsRealtime(1.5f);
                if (Attention.TutorialRunning) continue;
                if (Visit.Active || Visit.Travelling) yield break;   // results wait until you are home
                var result = GameEvents.TakeResult();
                if (result != null) Attention.Ask("contest", () => EventsScreen.ShowResult(result));
                yield break;
            }
        }

        GameObject _addFriend, _stopWalking;
        PlayerAvatar _king;

        void Update()
        {
            Attention.Tick();
            if (_tuckDot != null && Time.frameCount % 15 == 0)
            {
                bool dot = _tucked && SideHasDot();
                if (_tuckDot.activeSelf != dot) _tuckDot.SetActive(dot);
                PlaceTuck();
            }
            if (_stopWalking != null)
            {
                if (_king == null) _king = FindFirstObjectByType<PlayerAvatar>();
                // A short walk to a tapped spot needs no stop button; a long trip somewhere does.
                bool walking = _king != null && _king.AutoWalking && !_king.TapWalking && !UiScreen.AnyOpen;
                if (_stopWalking.activeSelf != walking) _stopWalking.SetActive(walking);
            }
            // No red dots while Old Yuzu is still showing you round: one thing at a time.
            bool calm = !Attention.TutorialRunning;
            bool gift = calm && DailyGifts.CanClaim;
            if (_giftDot != null && _giftDot.activeSelf != gift) _giftDot.SetActive(gift);
            if (_eventsDot != null && Time.frameCount % 30 == 0) _eventsDot.SetActive(calm && GameEvents.NeedsAttention);
            if (_addFriend != null && _addFriend.activeSelf != (Visit.Active && !Visit.HostIsFriend))
                _addFriend.SetActive(Visit.Active && !Visit.HostIsFriend);
        }

        // ---------------------------------------------------------------- tucking the side buttons away

        const string TuckedKey = "CapyIsland.SideTucked";
        const float SideX = -16f, SideSpacing = 14f;
        RectTransform _side, _tuck, _tuckChevron;
        TextMeshProUGUI _tuckLabel;
        GameObject _tuckDot;
        bool _tucked;

        /// <summary>
        /// A pill under the column of buttons on the right, reading "Hide ›": tap it and the
        /// column slides away off the edge, leaving the island clear, and the pill turns to
        /// "‹ Show" to bring it back. Tucked away, it wears a red dot if any hidden button has one.
        /// Remembered between sessions.
        /// </summary>
        void BuildTuck(RectTransform side)
        {
            _side = side;
            var pill = UiKit.Image(_safe, "SideTuck", UiKit.Rounded(26), UiKit.HudGlass, true);
            _tuck = pill.rectTransform;
            UiKit.Place(_tuck, new Vector2(1f, 1f), new Vector2(SideX - 4f, side.anchoredPosition.y), new Vector2(128f, 56f));
            _tuck.SetSiblingIndex(side.GetSiblingIndex() + 1);
            var h = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(14, 14, 8, 8);
            h.spacing = 6f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            RtlLayout.Add(h);
            _tuckLabel = UiKit.Text(pill.transform, "Label", "Hide", 28f, Color.white, true, TextAlignmentOptions.Center);
            _tuckLabel.textWrappingMode = TextWrappingModes.NoWrap;
            var chevron = UiKit.Image(pill.transform, "Chevron", IconPainter.Chevron, Color.white);
            chevron.preserveAspect = true;
            var cle = chevron.gameObject.AddComponent<LayoutElement>();
            cle.preferredWidth = cle.preferredHeight = 24f;
            _tuckChevron = chevron.rectTransform;
            var b = pill.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { Sfx.Play(SfxKind.Tap, 0.5f); Tuck(!_tucked, true); });
            pill.gameObject.AddComponent<PressSquash>();
            _tuckDot = NewDot.Fixed(pill.transform, 24f);
            _tuckDot.AddComponent<LayoutElement>().ignoreLayout = true;
            _tuckDot.SetActive(false);
            Tuck(PlayerPrefs.GetInt(TuckedKey, 0) == 1, false);
        }

        void Tuck(bool tucked, bool animate)
        {
            _tucked = tucked;
            PlayerPrefs.SetInt(TuckedKey, tucked ? 1 : 0);
            PlayerPrefs.Save();
            float x = tucked ? _side.sizeDelta.x + 40f : SideX;   // all the way past the edge
            _side.DOKill();
            if (animate) _side.DOAnchorPosX(x, 0.3f).SetEase(tucked ? Ease.InBack : Ease.OutBack).SetUpdate(true);
            else _side.anchoredPosition = new Vector2(x, _side.anchoredPosition.y);
            _tuckLabel.text = tucked ? "Show" : "Hide";
            // The chevron points the way the buttons will go: out to the edge, or back in.
            _tuckChevron.localScale = new Vector3(tucked ? -1f : 1f, 1f, 1f);
            _tuckChevron.SetSiblingIndex(tucked ? 0 : 1);
        }

        /// <summary>Keeps the pill just under the last button (the column changes on a visit).</summary>
        void PlaceTuck()
        {
            float height = 0f;
            int shown = 0;
            foreach (RectTransform child in _side)
            {
                if (!child.gameObject.activeSelf) continue;
                height += child.sizeDelta.y;
                shown++;
            }
            height += Mathf.Max(0, shown - 1) * SideSpacing + 8f;   // the last caption hangs a little lower
            // A short screen (a phone on its side): the column shrinks to fit, pill and all.
            float top = -_side.anchoredPosition.y;
            float room = ((RectTransform)_safe).rect.height - top - _tuck.sizeDelta.y - 40f;
            float scale = height > 0f ? Mathf.Clamp(room / height, 0.55f, 1f) : 1f;
            _side.localScale = new Vector3(scale, scale, 1f);
            _tuck.anchoredPosition = new Vector2(_tuck.anchoredPosition.x, -(top + height * scale + 18f));
        }

        /// <summary>Is any button in the column showing a red dot right now?</summary>
        bool SideHasDot()
        {
            foreach (Transform button in _side)
                foreach (var t in button.GetComponentsInChildren<Transform>(false))
                    if (t.name == "Dot" || t.name == "NewDot") return true;
            return false;
        }

        void BorrowResourceIcons()
        {
            foreach (var (name, kind) in new[] { ("Wood", ResourceKind.Wood), ("Gold", ResourceKind.Gold), ("Gem", ResourceKind.Gem) })
            {
                var icon = _safe.Find("ResourceBar/" + name + "/Icon");
                var image = icon != null ? icon.GetComponent<Image>() : null;
                if (image != null) UiKit.SetResourceIcon(kind, image.sprite);
            }
        }

        // ---------------------------------------------------------------- new HUD

        void Build()
        {
            int afterBar = 1;
            var bar = _safe.Find("ResourceBar");
            if (bar != null) afterBar = bar.GetSiblingIndex() + 1;

            // Edge arrows sit right above the island, under every panel.
            var edges = UiKit.Rect("EdgeIndicators", _safe);
            UiKit.Stretch(edges);
            edges.SetSiblingIndex(afterBar);
            edges.gameObject.AddComponent<EdgeIndicators>();

            // Emojis over heads sit just above the arrows.
            var emojis = UiKit.Rect("EmojiBubbles", _safe);
            UiKit.Stretch(emojis);
            emojis.SetSiblingIndex(afterBar + 1);
            emojis.gameObject.AddComponent<EmojiBubbles>();
            afterBar++;
            gameObject.AddComponent<VisitDirector>();
            gameObject.AddComponent<StationGuide>();
            gameObject.AddComponent<Ambience>();
            gameObject.AddComponent<Atmosphere>();
            gameObject.AddComponent<DayNight>();
            gameObject.AddComponent<WorldMoments>();
            gameObject.AddComponent<NightLights>();
            gameObject.AddComponent<MusicPlayer>();
            gameObject.AddComponent<TipTriggers>();
            gameObject.AddComponent<Fishing>();
            gameObject.AddComponent<Birding>();
            gameObject.AddComponent<BugCatching>();
            gameObject.AddComponent<Gardening>();
            gameObject.AddComponent<Farm>();
            gameObject.AddComponent<CatCare>();
            gameObject.AddComponent<DishOrders>();
            gameObject.AddComponent<SeaLibrary>();
            Airport.Restore(this);

            var tracker = ObjectiveTracker.Create(_safe);
            tracker.transform.SetSiblingIndex(afterBar + 1);
            NewDot.Live(tracker.transform, () => !Attention.TutorialRunning && NewStuff.Any("goals/"));   // a goal just reached
            var badge = ProfileBadge.Create(_safe);
            badge.transform.SetSiblingIndex(afterBar + 1);
            NewDot.Live(badge.transform, () => !Attention.TutorialRunning && NewStuff.Any("wardrobe/"));   // a new piece to try on
            ClockPill.Create(_safe).transform.SetSiblingIndex(afterBar + 1);
            VisitorsColumn.Create(_safe).transform.SetSiblingIndex(afterBar + 1);

            var side = UiKit.Rect("SideButtons", _safe);
            side.anchorMin = side.anchorMax = side.pivot = new Vector2(1f, 1f);
            side.anchoredPosition = new Vector2(-16f, -102f);
            side.sizeDelta = new Vector2(120f, 860f);
            side.SetSiblingIndex(afterBar + 2);
            BuildTuck(side);
            var v = side.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 14f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = v.childControlHeight = false;
            v.childForceExpandWidth = v.childForceExpandHeight = false;

            // Screens draw over the dialogs but under the discovery card, which pauses the game.
            var screens = UiKit.Rect("Screens", _safe);
            UiKit.Stretch(screens);
            var card = _safe.Find("CardPopupRoot");
            if (card != null) screens.SetSiblingIndex(card.GetSiblingIndex());
            else screens.SetAsLastSibling();

            MakeScreen<ObjectivesScreen>(screens, "GoalsScreen");
            var map = MakeScreen<MapScreen>(screens, "MapScreen");
            var collection = MakeScreen<CollectionScreen>(screens, "CollectionScreen");
            var settings = MakeScreen<SettingsScreen>(screens, "SettingsScreen");
            var gifts = MakeScreen<DailyGiftScreen>(screens, "DailyGiftScreen");
            MakeScreen<WardrobeScreen>(screens, "WardrobeScreen");
            MakeScreen<ProfileScreen>(screens, "ProfileScreen");
            var friends = MakeScreen<FriendsScreen>(screens, "FriendsScreen");
            MakeScreen<PromptScreen>(screens, "PromptScreen");
            MakeScreen<HobbiesScreen>(screens, "HobbiesScreen");
            MakeScreen<LibraryScreen>(screens, "LibraryScreen");
            MakeScreen<SewingScreen>(screens, "SewingScreen");
            MakeScreen<CookingScreen>(screens, "CookingScreen");
            MakeScreen<BurrowScreen>(screens, "BurrowScreen");
            MakeScreen<FurnitureShopScreen>(screens, "FurnitureShopScreen");
            MakeScreen<BaitShopScreen>(screens, "BaitShopScreen");
            var events = MakeScreen<EventsScreen>(screens, "EventsScreen");
            // No menu: everything has its own way in. The column on the right (Settings first,
            // then what you visit most), your picture for the profile and wardrobe, the goal card
            // for goals. Red dots (NewStuff) on each say when something new is waiting there.
            UiKit.RoundButton(side, "Settings", IconPainter.Gear, "Settings", UiKit.Hex("6B8FA8"), () => settings.Open());
            var mapButton = UiKit.RoundButton(side, "Map", IconPainter.Map, "Map", UiKit.Spring, () => map.Open());
            NewDot.Live(mapButton.transform, () => !Attention.TutorialRunning && NewStuff.Any("map/"));
            var hobbiesButton = UiKit.RoundButton(side, "Hobbies", IconPainter.Rod, "Hobbies", UiKit.Hex("3E9C8F"), () => HobbiesScreen.Show());
            NewDot.Live(hobbiesButton.transform, () => !Attention.TutorialRunning && NewStuff.Any("hobbies/"));
            var collectionButton = UiKit.RoundButton(side, "Collection", IconPainter.Cards, "Collection", UiKit.Hex("A77BD6"), () => collection.Open());
            NewDot.Live(collectionButton.transform, () => !Attention.TutorialRunning && CollectionScreen.AnyNews());
            if (Visit.Active)
            {
                // Away from home: the way back, and a way to keep this island.
                UiKit.RoundButton(side, "Home", IconPainter.House, "Home", UiKit.Hex("5B9BD5"), Visit.FlyHome);
                _addFriend = UiKit.RoundButton(side, "AddFriend", IconPainter.AddFriend, "Add", UiKit.Leaf, () =>
                {
                    Visit.BefriendHost();
                    Hints.Show(Visit.Host.name + " is now your friend! Their island will be here next time.");
                    if (VisitorBot.Current != null) VisitorBot.Current.Emote(Game.Emoji.Heart);
                }).transform.parent.gameObject;
            }
            else
            {
                var giftButton = UiKit.RoundButton(side, "Gifts", IconPainter.Gift, "Gifts", UiKit.Hex("E3A92B"), () => gifts.Open());
                var dot = UiKit.Image(giftButton.transform, "Dot", UiKit.Circle, UiKit.Danger);
                UiKit.Place(dot.rectTransform, new Vector2(1f, 1f), new Vector2(2f, 2f), new Vector2(34f, 34f));
                _giftDot = dot.gameObject;
                var friendsButton = UiKit.RoundButton(side, "Friends", IconPainter.Plane, "Friends", UiKit.Hex("5B9BD5"), () => friends.Open());
                NewDot.Live(friendsButton.transform, () => !Attention.TutorialRunning && NewStuff.Any("friends/"));
                var eventsButton = UiKit.RoundButton(side, "Events", IconPainter.Trophy, "Events", UiKit.Hex("E3A92B"), () => events.Open());
                var edot = UiKit.Image(eventsButton.transform, "Dot", UiKit.Circle, UiKit.Danger);
                UiKit.Place(edot.rectTransform, new Vector2(1f, 1f), new Vector2(2f, 2f), new Vector2(34f, 34f));
                _eventsDot = edot.gameObject;
                _eventsDot.SetActive(GameEvents.NeedsAttention);
            }
            // While walking somewhere by itself (an arrow or "Show me" was tapped): a way to stop.
            var stop = UiKit.Button(_safe, "StopWalking", "Stop walking", UiKit.ButtonStyle.Secondary, PlayerAvatar.StopAutoWalk, 30f, IconPainter.Close);
            var sr = (RectTransform)stop.transform.parent;
            sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 1f);
            sr.anchoredPosition = new Vector2(0f, -250f);
            sr.sizeDelta = new Vector2(320f, 88f);
            sr.SetSiblingIndex(screens.GetSiblingIndex());
            _stopWalking = sr.gameObject;
            _stopWalking.SetActive(false);

            EmojiBar.Create(_safe).transform.SetSiblingIndex(screens.GetSiblingIndex());
            FishingButton.Create(_safe).transform.SetSiblingIndex(screens.GetSiblingIndex());

            UiKit.ApplyFonts(side);
        }

        static T MakeScreen<T>(Transform parent, string name) where T : UiScreen
        {
            var r = UiKit.Rect(name, parent);
            UiKit.Stretch(r);
            return r.gameObject.AddComponent<T>();
        }

        // ---------------------------------------------------------------- the builder's panels

        void Restyle()
        {
            UiKit.ApplyFonts(transform);

            RestyleResourceBar(_safe.Find("ResourceBar"));

            var ghost = _safe.Find("GhostDialogueRoot/Panel");
            if (ghost != null)
            {
                var bubble = ghost.Find("Bubble");
                if (bubble != null)
                {
                    UiKit.Restyle(bubble.GetComponent<Image>(), UiKit.Surface, 36);
                    var line = bubble.GetComponentInChildren<TMP_Text>(true);
                    if (line != null) line.color = UiKit.Ink;
                }
                PortraitDisc(ghost.Find("Portrait"), UiKit.Spring);
            }

            var speech = _safe.Find("SpeechDialogRoot/Panel");
            if (speech != null)
            {
                UiKit.Restyle(speech.GetComponent<Image>(), UiKit.Surface, 44);
                Colour(speech.Find("Name"), UiKit.PrimaryDark);
                Colour(speech.Find("Body"), UiKit.Ink);
                Colour(speech.Find("Hint"), UiKit.InkSoft);
                PortraitDisc(speech.Find("Portrait"), UiKit.SurfaceSunk);
                var choices = speech.Find("Choices");
                if (choices != null)
                    for (int i = 0; i < choices.childCount; i++)
                        UiKit.RestyleButton(choices.GetChild(i).GetComponent<Button>(), i == 0);
            }

            var purchase = _safe.Find("PurchaseDialogRoot/PurchaseDialog");
            if (purchase != null)
            {
                UiKit.Restyle(purchase.GetComponent<Image>(), UiKit.Surface, 44);
                UiKit.RestyleButton(purchase.Find("Cancel")?.GetComponent<Button>(), false);
                UiKit.RestyleButton(purchase.Find("Purchase")?.GetComponent<Button>(), true);
            }

            var cardRoot = _safe.Find("CardPopupRoot/Panel");
            if (cardRoot != null)
            {
                var dim = cardRoot.GetComponent<Image>();
                if (dim != null) dim.color = UiKit.Scrim;
                var card = cardRoot.Find("Card");
                if (card != null)
                {
                    UiKit.Restyle(card.GetComponent<Image>(), UiKit.Surface, 48);
                    var border = card.Find("Border");
                    if (border != null)
                    {
                        UiKit.Restyle(border.GetComponent<Image>(), UiKit.Sun, 38, false);
                        var inner = border.Find("Inner");
                        if (inner != null) UiKit.Restyle(inner.GetComponent<Image>(), UiKit.Surface, 32, false);
                    }
                }
            }
        }

        static void RestyleResourceBar(Transform bar)
        {
            if (bar == null) return;
            var backing = bar.GetComponent<Image>();
            if (backing != null) backing.color = new Color(0f, 0f, 0f, 0f);

            // Each counter becomes a floating pill.
            for (int i = 0; i < bar.childCount; i++)
            {
                var group = bar.GetChild(i) as RectTransform;
                if (group == null || group.Find("Pill") != null) continue;
                var pill = UiKit.Image(group, "Pill", UiKit.Rounded(32), UiKit.HudGlass);
                pill.transform.SetAsFirstSibling();
                pill.rectTransform.anchorMin = Vector2.zero;
                pill.rectTransform.anchorMax = Vector2.one;
                pill.rectTransform.offsetMin = new Vector2(-6f, 2f);
                pill.rectTransform.offsetMax = new Vector2(-14f, -2f);
                CounterInfo.Add(group, pill);
                foreach (var t in group.GetComponentsInChildren<TMP_Text>(true))
                {
                    t.font = UiKit.HeadingFont != null ? UiKit.HeadingFont : t.font;
                    t.fontSize = 38f;
                }
            }
        }

        /// <summary>A soft coloured circle behind a character portrait.</summary>
        static void PortraitDisc(Transform portrait, Color colour)
        {
            if (portrait == null || portrait.parent == null || portrait.parent.Find("PortraitDisc") != null) return;
            var r = (RectTransform)portrait;
            var disc = UiKit.Image(portrait.parent, "PortraitDisc", UiKit.Circle, colour);
            var d = disc.rectTransform;
            d.anchorMin = r.anchorMin;
            d.anchorMax = r.anchorMax;
            d.pivot = r.pivot;
            d.anchoredPosition = r.anchoredPosition;
            d.sizeDelta = r.sizeDelta;
            disc.transform.SetSiblingIndex(portrait.GetSiblingIndex());
        }

        static void Colour(Transform t, Color c)
        {
            if (t == null) return;
            var text = t.GetComponent<TMP_Text>();
            if (text != null) text.color = c;
        }
    }
}
