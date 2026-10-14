using System.Collections;
using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Cooking at your stove, the way Heartopia does it — in a little kitchen of its own rather
    /// than a page. The stove and pot stand big in the middle; your ingredients sit on a shelf
    /// (along the bottom on a phone held upright, down the right side when it is turned). Tap them,
    /// or drag them up into the pot, and the card by the Cook button says what they will make — or
    /// "???" if nobody has a recipe for that.
    ///
    /// Then you cook: the shelf goes, the stove comes to the very middle of the screen, and the
    /// heat creeps up the thermometer beside it. Every so often a spatula pops up over the pot with
    /// a green ring running out. Tap it in time to stir and cool it down; miss it and the heat
    /// jumps. Let it reach the top and it burns. Cook well for more stars; burn it (or cook
    /// nonsense) and you get Bizarre Food.
    /// </summary>
    public class CookingScreen : UiScreen
    {
        public static CookingScreen Instance { get; private set; }

        protected override string Title => "Cooking";
        protected override Sprite TitleIcon => IconPainter.Flame;
        protected override Color AccentColour => UiKit.Hex("E07A3F");
        protected override bool FullBleed => true;

        static readonly Color Wood = UiKit.Hex("8A5A32"), WoodDark = UiKit.Hex("5E3B20"), Glow = UiKit.Hex("FFB35C");

        RectTransform _body, _center, _panel, _shelf, _shelfContent, _slotsRow, _ghost, _statusPill, _thermo, _steamLayer;
        ScrollRect _shelfScroll;
        CanvasGroup _panelGroup;
        Image _resultPic, _heatFill, _hearthGlow;
        TextMeshProUGUI _resultName, _status;
        Button _cook;
        readonly List<string> _pot = new();
        bool _cooking, _wide;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Show()
        {
            if (Instance != null) Instance.Open();
        }

        /// <summary>Room in the pot: enough for the biggest recipe you know, plus a spare as you level up.</summary>
        static int Slots
        {
            get
            {
                int level = HobbyLog.Level(Hobby.Cooking), most = 3;
                foreach (var d in HobbyData.Dishes)
                {
                    if (d.MinLevel > level) continue;
                    int n = 0;
                    foreach (var (_, c) in d.Needs) n += c;
                    most = Mathf.Max(most, n);
                }
                return Mathf.Min(6, most + (level >= 4 ? 1 : 0));
            }
        }

        protected override void Build(RectTransform body) => _body = body;

        protected override void OnOpen()
        {
            _pot.Clear();
            _cooking = false;
            Layout();
        }

        protected override void OnClose()
        {
            StopAllCoroutines();
            _cooking = false;
            if (_fireSound != null) { _fireSound.Stop(); _fireSound.volume = 0f; }
            if (_simmerSound != null) { _simmerSound.Stop(); _simmerSound.volume = 0f; }
            if (_ghost != null) Destroy(_ghost.gameObject);
        }

        protected override void Update()
        {
            base.Update();
            if (!IsOpen || _body == null) return;
            // Turned the phone: lay the kitchen out again (between dishes, never mid-cook).
            if (!_cooking && IsWide() != _wide) Layout();
            Firelight();
            Simmer();
        }

        /// <summary>Where the stove's fire opening is, in the hearth square.</summary>
        static readonly Vector2 FireAt = new(0.33f, 0.22f);

        Image _firelight, _floorLight;
        float _heat, _nextSimmer;

        AudioSource _fireSound, _simmerSound;

        /// <summary>A quiet looping sound for the kitchen (the stove's fire, the pot simmering).</summary>
        AudioSource Loop(string clip)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = Resources.Load<AudioClip>("Sfx/" + clip);
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        /// <summary>The kitchen's sound: a soft crackle always, a simmer once there's food in the pot; both swell with the heat.</summary>
        void Soundscape()
        {
            if (_fireSound == null) { _fireSound = Loop("Amb_Fire"); _simmerSound = Loop("Amb_Simmer"); }
            float mix = AudioMix.Effects;
            float fire = (0.22f + _heat * 0.25f) * mix;
            float simmer = (_cooking ? 0.3f + _heat * 0.45f : _pot.Count > 0 ? 0.18f : 0f) * mix;
            foreach (var (s, want) in new[] { (_fireSound, fire), (_simmerSound, simmer) })
            {
                if (s.clip == null) continue;
                if (!s.isPlaying && want > 0f) { s.time = Random.Range(0f, s.clip.length); s.Play(); }
                s.volume = Mathf.MoveTowards(s.volume, want, Time.unscaledDeltaTime * 0.6f);
            }
        }

        /// <summary>The fire flickers, brighter and redder the hotter the stove.</summary>
        void Firelight()
        {
            Soundscape();
            if (_firelight == null) return;
            float t = Time.unscaledTime;
            float flicker = 0.8f + 0.12f * Mathf.Sin(t * 9.1f) + 0.08f * Mathf.Sin(t * 23.7f + 1.3f);
            var c = Color.Lerp(new Color(1f, 0.6f, 0.22f), new Color(1f, 0.38f, 0.16f), _heat);
            _firelight.color = new Color(c.r, c.g, c.b, (0.5f + _heat * 0.4f) * flicker);
            _firelight.rectTransform.localScale = Vector3.one * (1f + _heat * 0.35f) * (0.96f + 0.06f * flicker);
            _floorLight.color = new Color(c.r, c.g, c.b, (0.22f + _heat * 0.25f) * flicker);
        }

        /// <summary>A gentle simmer while you fill the pot: wisps of steam, thicker once there's something in it.</summary>
        void Simmer()
        {
            if (_cooking || _center == null || Time.unscaledTime < _nextSimmer) return;
            bool full = _pot.Count > 0;
            _nextSimmer = Time.unscaledTime + (full ? 0.35f : 0.8f);
            Puff(Random.Range(0.4f, 0.6f), 0.8f, full ? 0.6f : 0.3f);
        }

        bool IsWide() => _body.rect.width > _body.rect.height * 1.1f;

        // ---------------------------------------------------------------- the kitchen

        void Layout()
        {
            ClearChildren(_body);
            _heat = 0f;
            _wide = IsWide();
            float w = _body.rect.width, h = _body.rect.height;

            // Where the stove's feet go: just above the counter upright, low down when turned
            // (with room under it for the pot slots). The painting is then lined up under it.
            float panelTop = 24f + Mathf.Min(560f, h * 0.3f);
            float floorY = _wide ? 150f : panelTop + 140f;
            float size = _wide ? Mathf.Min(h - floorY - 250f, w * 0.5f) : Mathf.Min(w - 160f, h - floorY - 330f, h * 0.4f);

            Backdrop(floorY);
            TopBar();

            // The stove and pot, standing on the kitchen floor. Its pivot is its feet, so growing
            // it for the cooking keeps it planted.
            _center = UiKit.Rect("Hearth", _body);
            _center.anchorMin = _center.anchorMax = new Vector2(PrepX, 0f);
            _center.pivot = new Vector2(0.5f, 0f);
            _center.sizeDelta = new Vector2(size, size);
            _center.anchoredPosition = new Vector2(0f, floorY);
            Hearth(_center);

            // What is in the pot, just under the stove: tap one to take it back out.
            _slotsRow = UiKit.Rect("InThePot", _center);
            _slotsRow.anchorMin = new Vector2(0f, 0f);
            _slotsRow.anchorMax = new Vector2(1f, 0f);
            _slotsRow.pivot = new Vector2(0.5f, 1f);
            _slotsRow.sizeDelta = new Vector2(120f, 100f);
            _slotsRow.anchoredPosition = new Vector2(0f, -14f);
            var sh = _slotsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            sh.spacing = 12f;
            sh.childAlignment = TextAnchor.MiddleCenter;
            sh.childControlWidth = sh.childControlHeight = false;
            sh.childForceExpandWidth = sh.childForceExpandHeight = false;

            Thermometer(_center);
            Panel(w, h);
            StatusPill();
            Refresh();
        }

        /// <summary>Across the screen, where the stove stands while you fill the pot (left of the counter when turned).</summary>
        float PrepX => _wide ? 0.3f : 0.5f;

        /// <summary>
        /// Where in the painting (from its bottom) the stove stands: on the floorboards just in
        /// front of the brick nook. The floor meets the wall at 0.34.
        /// </summary>
        const float PaintedFloor = 0.3f;

        RectTransform _painting;

        void Backdrop(float floorY)
        {
            _painting = null;
            // A warm wash under everything (and if the painting is missing).
            var frame = UiKit.Image(_body, "Kitchen", null, UiKit.Hex("C98F5A"), true);
            UiKit.Stretch(frame.rectTransform, -400f, -400f, -400f, -400f);
            var art = HobbyData.Art("Cook_Kitchen");
            if (art != null)
            {
                // Lined up so the floor in the picture is right under the stove's feet, and big
                // enough to fill the screen from there.
                float w = _body.rect.width + 120f, h = _body.rect.height + 120f, y = floorY + 60f;
                float aspect = art.rect.width / art.rect.height;
                float tall = Mathf.Max(h, w / aspect, y / PaintedFloor, (h - y) / (1f - PaintedFloor));
                var area = UiKit.Rect("Wall", _body);
                UiKit.Stretch(area, -60f, -60f, -60f, -60f);
                area.gameObject.AddComponent<RectMask2D>();
                var pic = UiKit.Image(area, "Painting", art, Color.white);
                _painting = pic.rectTransform;
                pic.rectTransform.anchorMin = pic.rectTransform.anchorMax = new Vector2(PrepX, 0f);
                pic.rectTransform.pivot = new Vector2(0.5f, PaintedFloor);
                pic.rectTransform.sizeDelta = new Vector2(tall * aspect, tall);
                pic.rectTransform.anchoredPosition = new Vector2(0f, y);
            }
            // A soft warm dusk over it, so the stove and the words stand out.
            var wash = UiKit.Image(_body, "Wash", null, new Color(0.18f, 0.1f, 0.05f, 0.22f));
            UiKit.Stretch(wash.rectTransform, -400f, -400f, -400f, -400f);
        }

        void TopBar()
        {
            var chip = UiKit.Image(_body, "TitleChip", UiKit.Rounded(40), UiKit.HudGlass).rectTransform;
            UiKit.Place(chip, new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(300f, 88f));
            chip.pivot = new Vector2(0f, 1f);
            chip.anchoredPosition = new Vector2(28f, -28f);
            var icon = UiKit.Image(chip, "Icon", IconPainter.Flame, Glow);
            icon.preserveAspect = true;
            UiKit.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(52f, 52f));
            var t = UiKit.Text(chip, "Title", Title, 44f, Color.white, true, TextAlignmentOptions.MidlineLeft);
            UiKit.Stretch(t.rectTransform, 86f, 0f, 16f, 0f);
            t.textWrappingMode = TextWrappingModes.NoWrap;

            var close = UiKit.Image(_body, "Close", UiKit.Circle, UiKit.HudGlass, true).rectTransform;
            close.anchorMin = close.anchorMax = close.pivot = new Vector2(1f, 1f);
            close.anchoredPosition = new Vector2(-28f, -28f);
            close.sizeDelta = new Vector2(96f, 96f);
            var x = UiKit.Image(close, "X", IconPainter.Close, Color.white);
            UiKit.Stretch(x.rectTransform, 28f, 28f, 28f, 28f);
            var b = close.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(Close);
            close.gameObject.AddComponent<PressSquash>();

            // The recipe book, beside the way out.
            var book = UiKit.Button(_body, "Recipes", "Recipes", UiKit.ButtonStyle.Secondary, OpenBook, 30f, IconPainter.Book);
            var br = (RectTransform)book.transform.parent;
            br.anchorMin = br.anchorMax = br.pivot = new Vector2(1f, 1f);
            br.anchoredPosition = new Vector2(-140f, -32f);
            br.sizeDelta = new Vector2(230f, 88f);
        }

        // ---------------------------------------------------------------- the recipe book

        RectTransform _book;

        /// <summary>The recipe book (RecipeBook): a tap on a dish you have everything for fills the pot with it.</summary>
        void OpenBook()
        {
            if (_cooking) return;
            CloseBook();
            _book = RecipeBook.Open(_body, FillPot);
        }

        void CloseBook()
        {
            if (_book != null) Destroy(_book.gameObject);
            _book = null;
        }

        /// <summary>Empties the pot and puts in exactly what the recipe takes.</summary>
        void FillPot(DishKind d)
        {
            CloseBook();
            _pot.Clear();
            foreach (var (item, count) in d.Needs) for (int i = 0; i < count && _pot.Count < Slots; i++) _pot.Add(item);
            Sfx.Play(SfxKind.Plop, 0.7f);
            _status.text = "Drag ingredients into the pot";
            Refresh();
            Splash();
        }

        void StatusPill()
        {
            _statusPill = UiKit.Image(_body, "Status", UiKit.Rounded(36), UiKit.HudGlass).rectTransform;
            _statusPill.anchorMin = _statusPill.anchorMax = new Vector2(_wide ? PrepX : 0.5f, 1f);
            _statusPill.pivot = new Vector2(0.5f, 1f);
            _statusPill.anchoredPosition = new Vector2(0f, -140f);   // under the title and the close button
            // Turned sideways it stays over the stove, clear of the counter on the right.
            _statusPill.sizeDelta = new Vector2(_wide ? Mathf.Min(_body.rect.width * 0.55f - 60f, 760f) : Mathf.Min(_body.rect.width - 80f, 860f), 92f);
            _status = UiKit.Text(_statusPill, "Text", "Drag ingredients into the pot", 32f, Color.white, true, TextAlignmentOptions.Center);
            UiKit.Stretch(_status.rectTransform, 24f, 6f, 24f, 6f);
            _status.enableAutoSizing = true;
            _status.fontSizeMin = 20f;
            _status.fontSizeMax = 32f;
        }

        /// <summary>The stove, its fire, the pot on top, and a warm glow behind them all.</summary>
        void Hearth(RectTransform c)
        {
            _hearthGlow = UiKit.Image(c, "Glow", IconPainter.Glow, new Color(Glow.r, Glow.g, Glow.b, 0.45f));
            UiKit.Stretch(_hearthGlow.rectTransform, -140f, -120f, -140f, -100f);

            var stove = UiKit.Image(c, "Stove", HobbyData.Art("Cook_Stove") ?? HobbyData.Art("Stove"), Color.white);
            stove.preserveAspect = true;
            stove.rectTransform.anchorMin = new Vector2(0.02f, 0f);
            stove.rectTransform.anchorMax = new Vector2(0.98f, 0.66f);
            stove.rectTransform.offsetMin = stove.rectTransform.offsetMax = Vector2.zero;

            // Firelight: a warm flickering glow over the stove's open mouth, and a little of it
            // spilling onto the floorboards in front.
            _floorLight = UiKit.Image(c, "FloorLight", IconPainter.Glow, new Color(1f, 0.62f, 0.25f, 0.3f));
            _floorLight.rectTransform.anchorMin = _floorLight.rectTransform.anchorMax = new Vector2(FireAt.x, 0.02f);
            _floorLight.rectTransform.sizeDelta = new Vector2(c.sizeDelta.x * 0.7f, c.sizeDelta.x * 0.2f);
            _firelight = UiKit.Image(c, "Firelight", IconPainter.Glow, new Color(1f, 0.6f, 0.22f, 0.6f));
            _firelight.rectTransform.anchorMin = _firelight.rectTransform.anchorMax = FireAt;
            _firelight.rectTransform.sizeDelta = Vector2.one * c.sizeDelta.x * 0.42f;

            var pot = UiKit.Image(c, "Pot", HobbyData.Art("Cook_Pot") ?? HobbyData.Art("Pot"), Color.white);
            pot.preserveAspect = true;
            // Sitting on the iron plate, not hovering over it.
            pot.rectTransform.anchorMin = new Vector2(0.2f, 0.37f);
            pot.rectTransform.anchorMax = new Vector2(0.8f, 0.87f);
            pot.rectTransform.offsetMin = pot.rectTransform.offsetMax = Vector2.zero;

            _steamLayer = UiKit.Rect("Steam", c);
            UiKit.Stretch(_steamLayer);
        }

        /// <summary>A tall thermometer beside the pot, hidden until the cooking starts.</summary>
        void Thermometer(RectTransform c)
        {
            _thermo = UiKit.Image(c, "Heat", UiKit.Rounded(26), new Color(0.12f, 0.08f, 0.05f, 0.6f)).rectTransform;
            _thermo.anchorMin = new Vector2(0.86f, 0.08f);
            _thermo.anchorMax = new Vector2(0.86f, 0.84f);
            _thermo.pivot = new Vector2(0f, 0.5f);
            _thermo.sizeDelta = new Vector2(52f, 0f);
            _thermo.anchoredPosition = new Vector2(18f, 0f);
            // The red zone at the top: where it starts to burn.
            var danger = UiKit.Image(_thermo, "Red", UiKit.Rounded(22), new Color(UiKit.Danger.r, UiKit.Danger.g, UiKit.Danger.b, 0.35f));
            danger.rectTransform.anchorMin = new Vector2(0f, 0.82f);
            danger.rectTransform.anchorMax = Vector2.one;
            danger.rectTransform.offsetMin = new Vector2(6f, 0f);
            danger.rectTransform.offsetMax = new Vector2(-6f, -6f);
            _heatFill = UiKit.Image(_thermo, "Fill", UiKit.Rounded(22), UiKit.Leaf);
            _heatFill.rectTransform.anchorMin = Vector2.zero;
            _heatFill.rectTransform.anchorMax = new Vector2(1f, 0f);
            _heatFill.rectTransform.offsetMin = new Vector2(6f, 6f);
            _heatFill.rectTransform.offsetMax = new Vector2(-6f, 0f);
            var bulb = UiKit.Image(_thermo, "Bulb", IconPainter.Flame, Glow);
            bulb.preserveAspect = true;
            UiKit.Place(bulb.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -40f), new Vector2(56f, 56f));
            _thermo.gameObject.SetActive(false);
        }

        void SetHeat(float heat)
        {
            _heatFill.rectTransform.anchorMax = new Vector2(1f, Mathf.Clamp01(heat));
            _heatFill.color = heat > 0.82f ? UiKit.Danger : heat > 0.6f ? UiKit.Sun : UiKit.Leaf;
        }

        /// <summary>The bottom (or right-hand) panel: what it makes, the Cook button, and the shelf.</summary>
        void Panel(float w, float h)
        {
            _panel = UiKit.Rect("Counter", _body);
            if (_wide)
            {
                _panel.anchorMin = new Vector2(0.58f, 0f);
                _panel.anchorMax = new Vector2(1f, 1f);
                _panel.offsetMin = new Vector2(0f, 28f);
                _panel.offsetMax = new Vector2(-28f, -148f);
            }
            else
            {
                _panel.anchorMin = Vector2.zero;
                _panel.anchorMax = new Vector2(1f, 0f);
                _panel.pivot = new Vector2(0.5f, 0f);
                _panel.sizeDelta = new Vector2(-48f, Mathf.Min(560f, h * 0.3f));
                _panel.anchoredPosition = new Vector2(0f, 24f);
            }
            _panelGroup = _panel.gameObject.AddComponent<CanvasGroup>();

            // Result card and Cook button, side by side along the top of the panel.
            var top = UiKit.Rect("Recipe", _panel);
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = Vector2.one;
            top.pivot = new Vector2(0.5f, 1f);
            top.sizeDelta = new Vector2(0f, 132f);
            top.anchoredPosition = Vector2.zero;

            var card = UiKit.Card(top, "Makes", UiKit.Surface, 30, true, false).rectTransform;
            card.anchorMin = Vector2.zero;
            card.anchorMax = new Vector2(0.58f, 1f);
            card.offsetMin = Vector2.zero;
            card.offsetMax = new Vector2(-10f, 0f);
            _resultPic = UiKit.Image(card, "Pic", null, Color.white);
            _resultPic.preserveAspect = true;
            UiKit.Place(_resultPic.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(100f, 100f));
            _resultName = UiKit.Text(card, "Name", "", 30f, UiKit.Ink, true, TextAlignmentOptions.MidlineLeft);
            UiKit.Stretch(_resultName.rectTransform, 124f, 8f, 14f, 8f);
            _resultName.enableAutoSizing = true;
            _resultName.fontSizeMin = 18f;
            _resultName.fontSizeMax = 30f;

            _cook = UiKit.Button(top, "Cook", "Cook!", UiKit.ButtonStyle.Primary, StartCooking, 40f, IconPainter.Flame);
            var cr = (RectTransform)_cook.transform.parent;
            cr.anchorMin = new Vector2(0.58f, 0f);
            cr.anchorMax = Vector2.one;
            cr.offsetMin = new Vector2(10f, 6f);
            cr.offsetMax = new Vector2(0f, -6f);

            // The shelf: a wooden board holding your ingredients.
            var board = UiKit.Card(_panel, "Shelf", Wood, 30, true, false).rectTransform;
            board.anchorMin = Vector2.zero;
            board.anchorMax = Vector2.one;
            board.offsetMin = Vector2.zero;
            board.offsetMax = new Vector2(0f, -150f);
            var lip = UiKit.Image(board, "Lip", UiKit.Rounded(30), WoodDark);
            lip.rectTransform.anchorMin = Vector2.zero;
            lip.rectTransform.anchorMax = new Vector2(1f, 0f);
            lip.rectTransform.pivot = new Vector2(0.5f, 0f);
            lip.rectTransform.sizeDelta = new Vector2(0f, 16f);
            _shelf = board;

            var view = UiKit.Rect("View", board);
            UiKit.Stretch(view, 14f, 22f, 14f, 14f);
            view.gameObject.AddComponent<RectMask2D>();
            _shelfContent = UiKit.Rect("Items", view);
            var grid = _shelfContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(132f, 150f);
            grid.spacing = new Vector2(12f, 12f);
            grid.padding = new RectOffset(4, 4, 4, 4);
            var fitter = _shelfContent.gameObject.AddComponent<ContentSizeFitter>();
            _shelfScroll = view.gameObject.AddComponent<ScrollRect>();
            _shelfScroll.content = _shelfContent;
            _shelfScroll.viewport = view;
            _shelfScroll.movementType = ScrollRect.MovementType.Clamped;
            _shelfScroll.scrollSensitivity = 30f;
            // Upright, the pot is above, so the shelf scrolls sideways; turned, the pot is to the
            // left, so it scrolls up and down. A drag towards the pot is never mistaken for a scroll.
            if (_wide)
            {
                _shelfScroll.horizontal = false;
                _shelfScroll.vertical = true;
                _shelfContent.anchorMin = new Vector2(0f, 1f);
                _shelfContent.anchorMax = new Vector2(1f, 1f);
                _shelfContent.pivot = new Vector2(0.5f, 1f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = Mathf.Max(1, Mathf.FloorToInt((w * 0.42f - 60f) / 144f));
                grid.childAlignment = TextAnchor.UpperCenter;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                _shelfScroll.horizontal = true;
                _shelfScroll.vertical = false;
                _shelfContent.anchorMin = new Vector2(0f, 0f);
                _shelfContent.anchorMax = new Vector2(0f, 1f);
                _shelfContent.pivot = new Vector2(0f, 0.5f);
                grid.startAxis = GridLayoutGroup.Axis.Vertical;
                grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                float shelfH = _panel.sizeDelta.y - 150f - 36f;
                grid.constraintCount = Mathf.Clamp(Mathf.FloorToInt((shelfH + 12f) / 162f), 1, 3);
                grid.childAlignment = TextAnchor.MiddleLeft;
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            _shelfContent.offsetMin = _shelfContent.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Bag ingredients: each crop some recipe uses (not cotton, say), Chef Tofu's pantry goods,
        /// and all fish together (recipes want "any fish").
        /// </summary>
        static List<(string id, int count)> Ingredients()
        {
            var food = new HashSet<string>();
            foreach (var d in HobbyData.Dishes) foreach (var (item, _) in d.Needs) food.Add(item);
            var list = new List<(string, int)>();
            int fish = HobbyLog.FishInBag();
            if (fish > 0) list.Add(("fish:any", fish));
            foreach (var e in HobbyLog.Bag)
                if ((e.id.StartsWith("crop:") || e.id.StartsWith("pantry:")) && e.count > 0 && food.Contains(e.id)) list.Add((e.id, e.count));
            return list;
        }

        int InPot(string id)
        {
            int n = 0;
            foreach (var p in _pot) if (p == id) n++;
            return n;
        }

        void Refresh()
        {
            // Pot slots.
            ClearChildren(_slotsRow);
            int slots = Slots;
            float cell = Mathf.Min(96f, (_center.sizeDelta.x + 140f - 12f * (slots - 1)) / slots);
            for (int i = 0; i < slots; i++)
            {
                bool full = i < _pot.Count;
                var slot = UiKit.Image(_slotsRow, "Slot", UiKit.Circle, full ? Color.white : new Color(1f, 1f, 1f, 0.35f), full);
                slot.rectTransform.sizeDelta = new Vector2(cell, cell);
                if (!full) continue;
                var item = _pot[i];
                var pic = UiKit.Image(slot.transform, "Pic", Art(item), Color.white);
                pic.preserveAspect = true;
                UiKit.Stretch(pic.rectTransform, cell * 0.12f, cell * 0.12f, cell * 0.12f, cell * 0.12f);
                int index = i;
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() =>
                {
                    if (_cooking) return;
                    _pot.RemoveAt(index);
                    Sfx.Play(SfxKind.Tap, 0.4f);
                    Refresh();
                });
                slot.gameObject.AddComponent<PressSquash>();
            }

            // The shelf.
            ClearChildren(_shelfContent);
            var have = Ingredients();
            var empty = _shelf.Find("None");
            if (empty != null) Destroy(empty.gameObject);
            if (have.Count == 0)
            {
                var none = UiKit.Text(_shelf, "None", "No ingredients yet — catch fish and grow vegetables!", 28f, Color.white, true, TextAlignmentOptions.Center);
                UiKit.Stretch(none.rectTransform, 30f, 20f, 30f, 20f);
            }
            foreach (var (id, count) in have)
            {
                int left = count - InPot(id);
                var tile = UiKit.Image(_shelfContent, id, UiKit.Rounded(26), left > 0 ? UiKit.Surface : new Color(1f, 1f, 1f, 0.4f), left > 0);
                var pic = UiKit.Image(tile.transform, "Pic", Art(id), left > 0 ? Color.white : new Color(1f, 1f, 1f, 0.4f));
                pic.preserveAspect = true;
                pic.rectTransform.anchorMin = new Vector2(0f, 0.28f);
                pic.rectTransform.anchorMax = Vector2.one;
                pic.rectTransform.offsetMin = new Vector2(14f, 0f);
                pic.rectTransform.offsetMax = new Vector2(-14f, -10f);
                var n = UiKit.Text(tile.transform, "Count", "×" + left, 26f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
                n.rectTransform.anchorMin = Vector2.zero;
                n.rectTransform.anchorMax = new Vector2(1f, 0.3f);
                n.rectTransform.offsetMin = n.rectTransform.offsetMax = Vector2.zero;
                if (left > 0) tile.gameObject.AddComponent<Draggable>().Init(this, id, Art(id));
            }

            // What it makes.
            var dish = _pot.Count > 0 ? Cooking.Match(_pot) : null;
            _resultPic.color = Color.white;
            if (_pot.Count == 0) { _resultPic.enabled = false; _resultName.text = "<color=#8A7563>Empty pot</color>"; }
            else if (dish != null && !HobbyLog.KnowsRecipe(dish.Id))
            {
                // A recipe you don't know yet: a hint of something, not its name. Cooking it teaches it.
                _resultPic.enabled = true;
                _resultPic.sprite = HobbyData.ItemArt("dish:" + dish.Id);
                _resultPic.color = new Color(0.25f, 0.18f, 0.14f, 0.85f);
                _resultName.text = "???\n<size=70%><color=#8A7563>Something new…?</color></size>";
            }
            else if (dish != null) { _resultPic.enabled = true; _resultPic.sprite = HobbyData.ItemArt("dish:" + dish.Id); _resultName.text = dish.Name; }
            else { _resultPic.enabled = true; _resultPic.sprite = HobbyData.Art("Dish_Bizarre"); _resultName.text = "???\n<size=70%><color=#8A7563>No recipe for this…</color></size>"; }
            _cook.interactable = _pot.Count > 0 && !_cooking;
        }

        /// <summary>An ingredient's picture; "any fish" shows the fish you have most of.</summary>
        internal static Sprite Art(string id)
        {
            if (id != "fish:any") return HobbyData.ItemArt(id);
            string best = null;
            int most = 0;
            foreach (var e in HobbyLog.Bag)
                if (e.id.StartsWith("fish:") && e.count > most) { most = e.count; best = e.id; }
            return best != null ? HobbyData.ItemArt(best) : HobbyData.FishPicture(HobbyData.Fish[0]);
        }

        void Add(string id)
        {
            if (_cooking) return;
            if (_pot.Count >= Slots) { _status.text = "The pot is full! Tap something in it to take it out."; return; }
            int have = 0;
            foreach (var (i, c) in Ingredients()) if (i == id) have = c;
            if (InPot(id) >= have) return;
            _pot.Add(id);
            Sfx.Play(SfxKind.Plop, 0.6f);
            _status.text = "Drag ingredients into the pot";
            Refresh();
            Splash();
        }

        /// <summary>A little plop out of the pot as something goes in.</summary>
        void Splash()
        {
            var pot = _center.Find("Pot");
            if (pot != null) pot.DOPunchScale(new Vector3(0.06f, -0.05f, 0f), 0.3f, 6, 0.6f).SetUpdate(true);
            for (int i = 0; i < 3; i++) Puff(Random.Range(0.4f, 0.6f), 0.8f, 0.5f);
        }

        /// <summary>Anywhere over the stove, the pot or the slots counts as "in the pot".</summary>
        bool OverPot(Vector2 screen, Camera cam) =>
            RectTransformUtility.RectangleContainsScreenPoint(_center, screen, cam) ||
            RectTransformUtility.RectangleContainsScreenPoint(_slotsRow, screen, cam);

        // ---------------------------------------------------------------- dragging

        /// <summary>
        /// An ingredient on the shelf: drag it into the pot, or just tap it. A drag along the
        /// shelf scrolls the shelf instead.
        /// </summary>
        class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
        {
            CookingScreen _screen;
            string _id;
            Sprite _art;
            bool _dragged, _scrolling;

            public void Init(CookingScreen screen, string id, Sprite art) { _screen = screen; _id = id; _art = art; }

            public void OnPointerClick(PointerEventData e)
            {
                if (!_dragged) _screen.Add(_id);
                _dragged = false;
            }

            public void OnBeginDrag(PointerEventData e)
            {
                _dragged = true;
                var scroll = _screen._shelfScroll;
                bool along = scroll.horizontal ? Mathf.Abs(e.delta.x) > Mathf.Abs(e.delta.y) : Mathf.Abs(e.delta.y) > Mathf.Abs(e.delta.x);
                _scrolling = along;
                if (_scrolling) { scroll.OnBeginDrag(e); return; }

                var root = (RectTransform)_screen.transform;
                var ghost = UiKit.Image(root, "Ghost", _art, Color.white);
                ghost.preserveAspect = true;
                ghost.rectTransform.sizeDelta = new Vector2(130f, 130f);
                _screen._ghost = ghost.rectTransform;
                OnDrag(e);
            }

            public void OnDrag(PointerEventData e)
            {
                if (_scrolling) { _screen._shelfScroll.OnDrag(e); return; }
                if (_screen._ghost == null) return;
                var root = (RectTransform)_screen.transform;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, e.position, e.pressEventCamera, out var local))
                {
                    _screen._ghost.anchorMin = _screen._ghost.anchorMax = new Vector2(0.5f, 0.5f);
                    _screen._ghost.anchoredPosition = local;
                }
            }

            public void OnEndDrag(PointerEventData e)
            {
                if (_scrolling) { _screen._shelfScroll.OnEndDrag(e); _scrolling = false; return; }
                if (_screen._ghost != null) Destroy(_screen._ghost.gameObject);
                _screen._ghost = null;
                if (_screen.OverPot(e.position, e.pressEventCamera)) _screen.Add(_id);
            }
        }

        // ---------------------------------------------------------------- cooking

        void StartCooking()
        {
            if (_cooking || _pot.Count == 0) return;
            StartCoroutine(Cook());
        }

        /// <summary>A puff of steam rising out of the pot, at a spot across it (0 … 1).</summary>
        void Puff(float across, float height, float strength)
        {
            if (_steamLayer == null) return;
            // Soft-edged wisps that swell, drift and thin out as they rise.
            var p = UiKit.Image(_steamLayer, "Puff", IconPainter.Glow, new Color(1f, 1f, 1f, 0f)).rectTransform;
            float s = _center.sizeDelta.x * Random.Range(0.12f, 0.2f);
            p.anchorMin = p.anchorMax = new Vector2(across, height);
            p.sizeDelta = new Vector2(s, s);
            p.localScale = Vector3.one * 0.6f;
            var img = p.GetComponent<Image>();
            float life = Random.Range(1.4f, 2f);
            float rise = _center.sizeDelta.x * Random.Range(0.18f, 0.3f);
            p.DOAnchorPos(new Vector2(Random.Range(-30f, 30f), rise), life).SetEase(Ease.OutSine).SetUpdate(true);
            p.DOScale(2f, life).SetEase(Ease.OutSine).SetUpdate(true);
            DOTween.Sequence().SetUpdate(true)
                .Append(img.DOFade(0.7f * strength, life * 0.25f))
                .Append(img.DOFade(0f, life * 0.75f))
                .OnComplete(() => { if (p != null) Destroy(p.gameObject); });
        }

        // ---------------------------------------------------------------- the pointing hand

        const string StirsKey = "CapyIsland.CookStirs";
        const int StirsToLearn = 3;

        /// <summary>Stirs made, ever: the hand stops pointing once you have the hang of it.</summary>
        static int StirsLearned
        {
            get => PlayerPrefs.GetInt(StirsKey, 0);
            set { if (value <= StirsToLearn) PlayerPrefs.SetInt(StirsKey, value); }
        }

        /// <summary>The tutorial's hand, tapping at a spatula: it goes with the prompt.</summary>
        static void PointAt(RectTransform prompt, float size)
        {
            var sprite = TutorialPointer.HandSprite;
            if (sprite == null) return;
            var hand = UiKit.Image(prompt, "Hand", sprite, Color.white);
            hand.preserveAspect = true;
            hand.raycastTarget = false;   // the tap is for the spatula under it
            var rt = hand.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.62f, 0.38f);
            rt.pivot = new Vector2(0.48f, 0.06f);   // the fingertip
            rt.sizeDelta = new Vector2(size * 0.9f, size * 0.9f);
            rt.localRotation = Quaternion.Euler(0f, 0f, -150f);   // reaching up at it from below right
            rt.anchoredPosition = Vector2.zero;
            // Tap, tap: back along the arm and in again.
            rt.DOAnchorPos(new Vector2(size * 0.06f, -size * 0.1f), 0.35f).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(hand.gameObject);
        }

        IEnumerator Cook()
        {
            _cooking = true;
            Refresh();
            var dish = Cooking.Match(_pot);
            int level = HobbyLog.Level(Hobby.Cooking);

            // The shelf goes; the stove comes to the very middle and steps up.
            _panelGroup.blocksRaycasts = false;
            _panelGroup.DOFade(0f, 0.3f).SetUpdate(true);
            _panel.DOAnchorPos(_panel.anchoredPosition + (_wide ? new Vector2(260f, 0f) : new Vector2(0f, -260f)), 0.35f).SetEase(Ease.InBack).SetUpdate(true);
            // Sideways only (the floor is level): the stove slides to the middle with the room behind it.
            void Across(RectTransform r) =>
                DOTween.To(() => r.anchorMin.x, x => r.anchorMin = r.anchorMax = new Vector2(x, 0f), 0.5f, 0.5f).SetEase(Ease.OutCubic).SetUpdate(true);
            Across(_center);
            if (_painting != null) Across(_painting);
            _center.DOScale(1.12f, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);   // grows from its feet
            _statusPill.anchorMin = _statusPill.anchorMax = _statusPill.pivot = new Vector2(0.5f, 1f);
            _statusPill.DOAnchorPos(new Vector2(0f, -150f), 0.3f).SetUpdate(true);
            _thermo.gameObject.SetActive(true);
            _thermo.localScale = new Vector3(1f, 0f, 1f);
            _thermo.DOScaleY(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true).SetDelay(0.3f);
            yield return new WaitForSecondsRealtime(0.5f);

            float heat = 0.25f, done = 0f, length = 6f + _pot.Count, redTime = 0f;
            float window = Mathf.Min(2.2f, 1.5f + level * 0.08f);   // a kinder window as you level up
            float nextPrompt = 1.2f, nextPuff = 0f;
            int misses = 0, hits = 0, perfect = 0;
            bool burnt = false;
            RectTransform prompt = null;
            Image ring = null;
            float promptLeft = 0f;
            bool tapped = false;
            bool missedLast = false;

            _status.text = "Tap the spatula when it appears, to stir and cool it down!";
            Sfx.Play(SfxKind.Fuse, 0.5f);

            while (done < length)
            {
                float dt = Time.unscaledDeltaTime;
                done += dt;
                heat += dt * (0.09f + _pot.Count * 0.01f);

                // Steam: more of it, and quicker, the hotter it gets.
                nextPuff -= dt;
                if (nextPuff <= 0f)
                {
                    nextPuff = Mathf.Lerp(0.5f, 0.12f, heat);
                    Puff(Random.Range(0.36f, 0.64f), 0.8f, 0.5f + heat * 0.5f);
                }

                // A spatula prompt: pops up over the pot, its ring runs out.
                nextPrompt -= dt;
                if (prompt == null && nextPrompt <= 0f)
                {
                    tapped = false;
                    var disc = UiKit.Image(_center, "Stir", UiKit.Circle, Color.white, true);
                    prompt = disc.rectTransform;
                    prompt.anchorMin = prompt.anchorMax = new Vector2(Random.Range(0.3f, 0.7f), Random.Range(0.62f, 0.9f));
                    float ps = Mathf.Clamp(_center.sizeDelta.x * 0.3f, 130f, 190f);
                    prompt.sizeDelta = new Vector2(ps, ps);
                    UiKit.AddShadow(prompt, 16f, 6f, 0.3f);
                    ring = UiKit.Image(prompt, "Ring", UiKit.Circle, UiKit.Leaf);
                    ring.type = Image.Type.Filled;
                    ring.fillMethod = Image.FillMethod.Radial360;
                    ring.fillOrigin = (int)Image.Origin360.Top;
                    UiKit.Stretch(ring.rectTransform, -12f, -12f, -12f, -12f);
                    ring.transform.SetAsFirstSibling();
                    var icon = UiKit.Image(prompt, "Spatula", IconPainter.Spatula, Wood);
                    icon.preserveAspect = true;
                    UiKit.Stretch(icon.rectTransform, ps * 0.2f, ps * 0.2f, ps * 0.2f, ps * 0.2f);
                    var b = disc.gameObject.AddComponent<Button>();
                    b.transition = Selectable.Transition.None;
                    b.onClick.AddListener(() => tapped = true);
                    if (StirsLearned < StirsToLearn || missedLast) PointAt(prompt, ps);
                    prompt.localScale = Vector3.zero;
                    prompt.DOScale(1f, 0.15f).SetEase(Ease.OutBack).SetUpdate(true);
                    promptLeft = window;
                    Sfx.Play(SfxKind.Tap, 0.5f);
                }
                if (prompt != null)
                {
                    promptLeft -= dt;
                    ring.fillAmount = Mathf.Clamp01(promptLeft / window);
                    ring.color = ring.fillAmount > 0.5f ? UiKit.Leaf : ring.fillAmount > 0.2f ? UiKit.Sun : UiKit.Danger;
                    if (tapped || promptLeft <= 0f)
                    {
                        if (tapped)
                        {
                            hits++;
                            missedLast = false;
                            StirsLearned++;
                            bool great = ring.fillAmount > 0.45f;
                            if (great) perfect++;
                            heat = Mathf.Max(0f, heat - 0.32f);
                            _status.text = great ? "<color=#8EE59A><b>Perfect stir!</b></color>" : "Good stir!";
                            Sfx.Play(SfxKind.Stir, 0.8f);
                            for (int i = 0; i < 4; i++) Puff(prompt.anchorMin.x + Random.Range(-0.08f, 0.08f), prompt.anchorMin.y, 0.9f);
                            _center.Find("Pot")?.DOPunchRotation(new Vector3(0f, 0f, 6f), 0.35f, 8, 0.5f).SetUpdate(true);
                        }
                        else
                        {
                            misses++;
                            missedLast = true;
                            heat += 0.22f;
                            _status.text = "<color=#FF9A8A><b>Too slow! It's getting hot…</b></color>";
                            Sfx.Play(SfxKind.Fuse, 0.7f);
                            _center.DOShakeAnchorPos(0.3f, 10f, 20).SetUpdate(true);
                        }
                        var gone = prompt;
                        gone.DOScale(0f, 0.12f).SetUpdate(true).OnComplete(() => { if (gone != null) Destroy(gone.gameObject); });
                        prompt = null;
                        nextPrompt = Random.Range(0.7f, 1.4f);
                    }
                }

                // The heat, shown on the thermometer and in the glow behind the stove.
                heat = Mathf.Clamp01(heat);
                if (heat > 0.82f) redTime += dt;
                SetHeat(heat);
                _heat = heat;
                var glow = Color.Lerp(Glow, UiKit.Danger, heat);
                glow.a = 0.18f + heat * 0.3f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f;
                _hearthGlow.color = glow;
                if (heat >= 1f) { burnt = true; break; }
                yield return null;
            }

            if (prompt != null) Destroy(prompt.gameObject);

            // How it turned out.
            int stars = Mathf.Clamp(5 - misses - (hits - perfect + 1) / 2 - (redTime > 1f ? 1 : 0), 1, 5);
            var cooked = new List<string>(_pot);
            bool discovered = dish != null && !burnt && !HobbyLog.KnowsRecipe(dish.Id);
            string id = Cooking.Finish(cooked, dish, burnt, stars);
            _pot.Clear();
            bool good = dish != null && !burnt;
            string title = good ? Loc.T(dish.Name) + "!" : "Bizarre Food…";
            string line = good
                ? (discovered ? "<b>You worked out a new recipe!</b> It's in your recipe book now.\n" : "")
                  + StarLine(stars) + "\nIt's in your bag. Look for someone with this dish in a bubble over their head!"
                : burnt ? "It burned to a crisp. Tap the spatula sooner to keep the heat down!"
                        : "Nobody has a recipe for that, and now we know why.";
            CardPopup.Show(title, line, HobbyData.ItemArt(id));
            _cooking = false;
            Layout();
            _status.text = good ? "Cook something else?" : "Try again?";
        }

        static string StarLine(int stars)
        {
            // The fonts have no star glyph: say it in words, warmly.
            return stars switch
            {
                5 => "<color=#E3A92B><b>Five stars — perfect!</b></color>",
                4 => "<color=#E3A92B><b>Four stars — delicious.</b></color>",
                3 => "<b>Three stars — pretty good.</b>",
                2 => "<b>Two stars — edible.</b>",
                _ => "<b>One star — it's… food.</b>",
            };
        }
    }
}
