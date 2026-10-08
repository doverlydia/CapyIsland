using System.Collections;
using CapyIsland.Game;
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
        }

        void OnEnable() => Wardrobe.Unlocked += AnnounceOutfit;
        void OnDisable() => Wardrobe.Unlocked -= AnnounceOutfit;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        static void AnnounceOutfit(WearItem item) =>
            CardPopup.Show("New outfit!", "<b>" + item.Name + "</b> is waiting for you in the Wardrobe.", Wardrobe.Art(item));

        GameObject _giftDot;

        /// <summary>
        /// The daily gift opens by itself once a session, as soon as the island is yours (after the
        /// tutorial) and nothing else is on screen.
        /// </summary>
        IEnumerator Start()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1.5f);
                var tutorial = TutorialDirector.Instance;
                if (tutorial != null && !tutorial.Finished) continue;
                if (Visit.Active || Visit.Travelling) yield break;   // gifts wait until you are home
                if (UiScreen.AnyOpen || SpeechDialog.IsOpen || CardPopup.IsShowing || PurchaseDialog.IsOpen) continue;
                if (DailyGifts.CanClaim && DailyGiftScreen.Instance != null) DailyGiftScreen.Instance.Open();
                yield break;
            }
        }

        GameObject _addFriend;

        void Update()
        {
            if (_giftDot != null && _giftDot.activeSelf != DailyGifts.CanClaim) _giftDot.SetActive(DailyGifts.CanClaim);
            if (_addFriend != null && _addFriend.activeSelf != (Visit.Active && !Visit.HostIsFriend))
                _addFriend.SetActive(Visit.Active && !Visit.HostIsFriend);
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
            gameObject.AddComponent<Fishing>();
            gameObject.AddComponent<Birding>();
            gameObject.AddComponent<BugCatching>();
            gameObject.AddComponent<Gardening>();
            gameObject.AddComponent<CatCare>();
            gameObject.AddComponent<DishOrders>();
            gameObject.AddComponent<SeaLibrary>();
            Airport.Restore(this);

            var tracker = ObjectiveTracker.Create(_safe);
            tracker.transform.SetSiblingIndex(afterBar + 1);
            ProfileBadge.Create(_safe).transform.SetSiblingIndex(afterBar + 1);
            VisitorsColumn.Create(_safe).transform.SetSiblingIndex(afterBar + 1);

            var side = UiKit.Rect("SideButtons", _safe);
            side.anchorMin = side.anchorMax = side.pivot = new Vector2(1f, 1f);
            side.anchoredPosition = new Vector2(-16f, -102f);
            side.sizeDelta = new Vector2(120f, 700f);
            side.SetSiblingIndex(afterBar + 2);
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
            var wardrobe = MakeScreen<WardrobeScreen>(screens, "WardrobeScreen");
            var profile = MakeScreen<ProfileScreen>(screens, "ProfileScreen");
            var friends = MakeScreen<FriendsScreen>(screens, "FriendsScreen");
            MakeScreen<PromptScreen>(screens, "PromptScreen");
            var hobbies = MakeScreen<HobbiesScreen>(screens, "HobbiesScreen");
            MakeScreen<LibraryScreen>(screens, "LibraryScreen");
            MakeScreen<SewingScreen>(screens, "SewingScreen");
            MakeScreen<CookingScreen>(screens, "CookingScreen");
            var menu = MakeScreen<MenuScreen>(screens, "MenuScreen");

            menu.AddEntry("Profile", IconPainter.FaceSmile, UiKit.Hex("E58FB0"), () => profile.Open());
            menu.AddEntry("Goals", IconPainter.Goals, UiKit.Primary, () => ObjectivesScreen.Show(ObjectivesScreen.Tab.Quests));
            menu.AddEntry("Map", IconPainter.Map, UiKit.Spring, () => map.Open());
            menu.AddEntry("Collection", IconPainter.Cards, UiKit.Hex("A77BD6"), () => collection.Open());
            menu.AddEntry("Wardrobe", IconPainter.Hanger, UiKit.Hex("E58FB0"), () => wardrobe.Open(), () => Wardrobe.Ready);
            menu.AddEntry("Daily Gifts", IconPainter.Calendar, UiKit.Sun, () => gifts.Open());
            menu.AddEntry("Hobbies", IconPainter.Rod, UiKit.Hex("3E9C8F"), () => HobbiesScreen.Show());
            menu.AddEntry("Friends", IconPainter.Plane, UiKit.Hex("5B9BD5"), () => friends.Open());
            menu.AddEntry("Settings", IconPainter.Gear, UiKit.Hex("6B8FA8"), () => settings.Open());

            UiKit.RoundButton(side, "Menu", IconPainter.Menu, "Menu", UiKit.Primary, () => menu.Open());
            UiKit.RoundButton(side, "Map", IconPainter.Map, "Map", UiKit.Spring, () => map.Open());
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
                UiKit.RoundButton(side, "Friends", IconPainter.Plane, "Friends", UiKit.Hex("5B9BD5"), () => friends.Open());
            }
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
