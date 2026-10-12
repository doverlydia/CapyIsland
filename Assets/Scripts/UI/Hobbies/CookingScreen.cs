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
    /// Cooking at your stove, the way Heartopia does it. First you fill the pot: drag ingredients
    /// from your bag into it (or tap them), and it tells you what they will make — or "???" if
    /// nobody has a recipe for that. Then you cook: the heat creeps up, and every so often a
    /// spatula pops up over the pot with a green ring running out. Tap it in time to stir and turn
    /// the heat down; miss it and the heat jumps. Let it reach the top and it burns. Cook well for
    /// more stars; burn it (or cook nonsense) and you get Bizarre Food.
    /// </summary>
    public class CookingScreen : UiScreen
    {
        public static CookingScreen Instance { get; private set; }

        protected override string Title => "Cooking";
        protected override Sprite TitleIcon => IconPainter.Flame;
        protected override Color AccentColour => UiKit.Hex("E07A3F");
        protected override float MaxWidth => 900f;

        RectTransform _body, _stage, _tray, _slotsRow, _ghost;
        Image _resultPic, _heatFill;
        TextMeshProUGUI _resultName, _status;
        Button _cook;
        readonly List<string> _pot = new();
        readonly List<RectTransform> _flames = new();
        bool _cooking;

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
            if (_ghost != null) Destroy(_ghost.gameObject);
        }

        // ---------------------------------------------------------------- the stove and the pot

        void Layout()
        {
            ClearChildren(_body);
            _flames.Clear();

            // The stove, the pot on it, and the slots for what's in it.
            _stage = UiKit.Image(_body, "Stage", UiKit.Rounded(34), UiKit.Hex("F6E7D4")).rectTransform;
            _stage.anchorMin = new Vector2(0f, 0.42f);
            _stage.anchorMax = Vector2.one;
            _stage.offsetMin = new Vector2(0f, 8f);
            _stage.offsetMax = Vector2.zero;

            var stove = UiKit.Image(_stage, "Stove", HobbyData.Art("Stove"), Color.white);
            stove.preserveAspect = true;
            stove.raycastTarget = false;
            stove.rectTransform.anchorMin = new Vector2(0.06f, 0.04f);
            stove.rectTransform.anchorMax = new Vector2(0.56f, 0.62f);
            stove.rectTransform.offsetMin = stove.rectTransform.offsetMax = Vector2.zero;

            for (int i = 0; i < 3; i++)
            {
                var f = UiKit.Image(_stage, "Flame", IconPainter.Flame, UiKit.Hex("F59A3A")).rectTransform;
                f.GetComponent<Image>().raycastTarget = false;
                f.anchorMin = f.anchorMax = new Vector2(0.22f + i * 0.09f, 0.6f);
                f.pivot = new Vector2(0.5f, 0f);
                f.sizeDelta = new Vector2(40f, 50f);
                f.localScale = Vector3.one * 0.6f;
                _flames.Add(f);
            }

            var pot = UiKit.Image(_stage, "Pot", HobbyData.Art("Pot"), Color.white);
            pot.preserveAspect = true;
            pot.raycastTarget = false;
            pot.rectTransform.anchorMin = new Vector2(0.14f, 0.55f);
            pot.rectTransform.anchorMax = new Vector2(0.48f, 0.97f);
            pot.rectTransform.offsetMin = pot.rectTransform.offsetMax = Vector2.zero;

            _slotsRow = UiKit.Rect("Slots", _stage);
            _slotsRow.anchorMin = new Vector2(0.58f, 0.62f);
            _slotsRow.anchorMax = new Vector2(0.98f, 0.95f);
            _slotsRow.offsetMin = _slotsRow.offsetMax = Vector2.zero;
            var grid = _slotsRow.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(84f, 84f);
            grid.spacing = new Vector2(10f, 10f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            // What it will make, and the button to cook it.
            var result = UiKit.Image(_stage, "Result", UiKit.Rounded(26), Color.white).rectTransform;
            result.anchorMin = new Vector2(0.58f, 0.2f);
            result.anchorMax = new Vector2(0.98f, 0.58f);
            result.offsetMin = result.offsetMax = Vector2.zero;
            _resultPic = UiKit.Image(result, "Pic", null, Color.white);
            _resultPic.preserveAspect = true;
            _resultPic.raycastTarget = false;
            UiKit.Place(_resultPic.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(84f, 84f));
            _resultName = UiKit.Text(result, "Name", "", 26f, UiKit.Ink, true);
            _resultName.rectTransform.anchorMin = Vector2.zero;
            _resultName.rectTransform.anchorMax = Vector2.one;
            _resultName.rectTransform.offsetMin = new Vector2(108f, 6f);
            _resultName.rectTransform.offsetMax = new Vector2(-10f, -6f);
            _resultName.alignment = TextAlignmentOptions.MidlineLeft;
            _resultName.enableAutoSizing = true;
            _resultName.fontSizeMin = 16f;
            _resultName.fontSizeMax = 26f;

            _cook = UiKit.Button(_stage, "Cook", "Cook!", UiKit.ButtonStyle.Primary, StartCooking, 32f, IconPainter.Flame);
            var cr = (RectTransform)_cook.transform.parent;
            cr.anchorMin = new Vector2(0.58f, 0.03f);
            cr.anchorMax = new Vector2(0.98f, 0.17f);
            cr.offsetMin = cr.offsetMax = Vector2.zero;

            _status = UiKit.Text(_body, "Status", "Drag ingredients into the pot", 26f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
            _status.rectTransform.anchorMin = new Vector2(0f, 0.34f);
            _status.rectTransform.anchorMax = new Vector2(1f, 0.42f);
            _status.rectTransform.offsetMin = _status.rectTransform.offsetMax = Vector2.zero;

            // The ingredients you have.
            _tray = UiKit.Image(_body, "Tray", UiKit.Rounded(30), UiKit.SurfaceSunk).rectTransform;
            _tray.anchorMin = Vector2.zero;
            _tray.anchorMax = new Vector2(1f, 0.34f);
            _tray.offsetMin = _tray.offsetMax = Vector2.zero;
            var tg = _tray.gameObject.AddComponent<GridLayoutGroup>();
            tg.cellSize = new Vector2(118f, 132f);
            tg.spacing = new Vector2(10f, 10f);
            tg.padding = new RectOffset(14, 14, 14, 14);
            tg.childAlignment = TextAnchor.MiddleCenter;

            Refresh();
        }

        /// <summary>Bag ingredients: every crop on its own, all fish together (recipes want "any fish").</summary>
        static List<(string id, int count)> Ingredients()
        {
            var list = new List<(string, int)>();
            int fish = HobbyLog.FishInBag();
            if (fish > 0) list.Add(("fish:any", fish));
            foreach (var e in HobbyLog.Bag)
                if (e.id.StartsWith("crop:") && e.count > 0) list.Add((e.id, e.count));
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
            for (int i = 0; i < Slots; i++)
            {
                var slot = UiKit.Image(_slotsRow, "Slot", UiKit.Circle, Color.white);
                if (i >= _pot.Count) { slot.color = new Color(1f, 1f, 1f, 0.55f); continue; }
                var item = _pot[i];
                var pic = UiKit.Image(slot.transform, "Pic", Art(item), Color.white);
                pic.preserveAspect = true;
                pic.raycastTarget = false;
                UiKit.Stretch(pic.rectTransform, 10f, 10f, 10f, 10f);
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
            }

            // Ingredient tiles.
            ClearChildren(_tray);
            var have = Ingredients();
            if (have.Count == 0)
            {
                var none = UiKit.Text(_tray, "None", "No ingredients yet — catch fish and grow vegetables!", 26f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
                _tray.GetComponent<GridLayoutGroup>().cellSize = new Vector2(700f, 120f);
                none.raycastTarget = false;
            }
            else _tray.GetComponent<GridLayoutGroup>().cellSize = new Vector2(118f, 132f);
            foreach (var (id, count) in have)
            {
                int left = count - InPot(id);
                var tile = UiKit.Image(_tray, id, UiKit.Rounded(24), left > 0 ? Color.white : new Color(1f, 1f, 1f, 0.45f));
                var pic = UiKit.Image(tile.transform, "Pic", Art(id), left > 0 ? Color.white : new Color(1f, 1f, 1f, 0.4f));
                pic.preserveAspect = true;
                pic.raycastTarget = false;
                pic.rectTransform.anchorMin = new Vector2(0f, 0.3f);
                pic.rectTransform.anchorMax = Vector2.one;
                pic.rectTransform.offsetMin = new Vector2(12f, 0f);
                pic.rectTransform.offsetMax = new Vector2(-12f, -8f);
                var n = UiKit.Text(tile.transform, "Count", "×" + left, 24f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
                n.rectTransform.anchorMin = Vector2.zero;
                n.rectTransform.anchorMax = new Vector2(1f, 0.3f);
                n.rectTransform.offsetMin = n.rectTransform.offsetMax = Vector2.zero;
                n.raycastTarget = false;
                if (left > 0) tile.gameObject.AddComponent<Draggable>().Init(this, id, Art(id));
            }

            // What it makes.
            var dish = _pot.Count > 0 ? Cooking.Match(_pot) : null;
            if (_pot.Count == 0) { _resultPic.enabled = false; _resultName.text = "<color=#8A7563>Empty pot</color>"; }
            else if (dish != null) { _resultPic.enabled = true; _resultPic.sprite = HobbyData.ItemArt("dish:" + dish.Id); _resultName.text = dish.Name; }
            else { _resultPic.enabled = true; _resultPic.sprite = HobbyData.Art("Dish_Bizarre"); _resultName.text = "???\n<size=70%><color=#8A7563>No recipe for this…</color></size>"; }
            _cook.interactable = _pot.Count > 0 && !_cooking;
        }

        /// <summary>An ingredient's picture; "any fish" shows the fish you have most of.</summary>
        static Sprite Art(string id)
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
        }

        bool OverPot(Vector2 screen, Camera cam) => RectTransformUtility.RectangleContainsScreenPoint(_stage, screen, cam);

        // ---------------------------------------------------------------- dragging

        /// <summary>An ingredient tile: drag it into the pot, or just tap it.</summary>
        class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
        {
            CookingScreen _screen;
            string _id;
            Sprite _art;
            bool _dragged;

            public void Init(CookingScreen screen, string id, Sprite art) { _screen = screen; _id = id; _art = art; }

            public void OnPointerClick(PointerEventData e)
            {
                if (!_dragged) _screen.Add(_id);
                _dragged = false;
            }

            public void OnBeginDrag(PointerEventData e)
            {
                _dragged = true;
                var root = (RectTransform)_screen.transform;
                var ghost = UiKit.Image(root, "Ghost", _art, Color.white);
                ghost.preserveAspect = true;
                ghost.raycastTarget = false;
                ghost.rectTransform.sizeDelta = new Vector2(110f, 110f);
                _screen._ghost = ghost.rectTransform;
                OnDrag(e);
            }

            public void OnDrag(PointerEventData e)
            {
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

        IEnumerator Cook()
        {
            _cooking = true;
            Refresh();
            ClearChildren(_tray);
            var dish = Cooking.Match(_pot);
            int level = HobbyLog.Level(Hobby.Cooking);

            // The heat gauge across the top of the stage: green, then yellow, then red.
            var gauge = UiKit.Image(_stage, "Heat", UiKit.Rounded(14), new Color(0f, 0f, 0f, 0.15f)).rectTransform;
            gauge.anchorMin = new Vector2(0.06f, 0.0f);
            gauge.anchorMax = new Vector2(0.94f, 0.0f);
            gauge.pivot = new Vector2(0.5f, 0f);
            gauge.sizeDelta = new Vector2(0f, 26f);
            gauge.anchoredPosition = new Vector2(0f, -40f);
            _heatFill = UiKit.Image(gauge, "Fill", UiKit.Rounded(14), UiKit.Leaf);
            _heatFill.rectTransform.anchorMin = Vector2.zero;
            _heatFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _heatFill.rectTransform.offsetMin = _heatFill.rectTransform.offsetMax = Vector2.zero;

            float heat = 0.25f, done = 0f, length = 6f + _pot.Count, redTime = 0f;
            float window = Mathf.Min(2.2f, 1.5f + level * 0.08f);   // a kinder window as you level up
            float nextPrompt = 1.2f;
            int misses = 0, hits = 0, perfect = 0;
            bool burnt = false;
            RectTransform prompt = null;
            Image ring = null;
            float promptLeft = 0f;
            bool tapped = false;

            _status.text = "Tap the spatula when it appears, to stir and cool it down!";
            Sfx.Play(SfxKind.Fuse, 0.5f);

            while (done < length)
            {
                float dt = Time.unscaledDeltaTime;
                done += dt;
                heat += dt * (0.09f + _pot.Count * 0.01f);

                // A spatula prompt: appears, its ring runs out.
                nextPrompt -= dt;
                if (prompt == null && nextPrompt <= 0f)
                {
                    tapped = false;
                    var disc = UiKit.Image(_stage, "Stir", UiKit.Circle, Color.white);
                    prompt = disc.rectTransform;
                    prompt.anchorMin = prompt.anchorMax = new Vector2(Random.Range(0.18f, 0.44f), Random.Range(0.62f, 0.86f));
                    prompt.sizeDelta = new Vector2(120f, 120f);
                    ring = UiKit.Image(prompt, "Ring", UiKit.Circle, UiKit.Leaf);
                    ring.type = Image.Type.Filled;
                    ring.fillMethod = Image.FillMethod.Radial360;
                    ring.fillOrigin = (int)Image.Origin360.Top;
                    ring.raycastTarget = false;
                    UiKit.Stretch(ring.rectTransform, -10f, -10f, -10f, -10f);
                    ring.transform.SetAsFirstSibling();
                    var icon = UiKit.Image(prompt, "Spatula", IconPainter.Spatula, UiKit.Hex("8A5A32"));
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    UiKit.Stretch(icon.rectTransform, 22f, 22f, 22f, 22f);
                    var b = disc.gameObject.AddComponent<Button>();
                    b.transition = Selectable.Transition.None;
                    b.onClick.AddListener(() => tapped = true);
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
                            if (ring.fillAmount > 0.45f) perfect++;
                            heat = Mathf.Max(0f, heat - 0.32f);
                            _status.text = ring.fillAmount > 0.45f ? "<color=#3E9C4F><b>Perfect stir!</b></color>" : "Good stir!";
                            Sfx.Play(SfxKind.Plop, 0.8f);
                        }
                        else
                        {
                            misses++;
                            heat += 0.22f;
                            _status.text = "<color=#D9534F><b>Too slow! It's getting hot…</b></color>";
                            Sfx.Play(SfxKind.Fuse, 0.7f);
                        }
                        var gone = prompt;
                        gone.DOScale(0f, 0.12f).SetUpdate(true).OnComplete(() => { if (gone != null) Destroy(gone.gameObject); });
                        prompt = null;
                        nextPrompt = Random.Range(0.7f, 1.4f);
                    }
                }

                // The heat, shown on the gauge and in the flames.
                heat = Mathf.Clamp01(heat);
                if (heat > 0.82f) redTime += dt;
                UiKit.SetProgress(_heatFill, heat);
                _heatFill.color = heat > 0.82f ? UiKit.Danger : heat > 0.6f ? UiKit.Sun : UiKit.Leaf;
                for (int i = 0; i < _flames.Count; i++)
                {
                    float flicker = 1f + Mathf.Sin(Time.unscaledTime * (11f + i * 3f)) * 0.08f;
                    _flames[i].localScale = Vector3.one * (0.5f + heat * 0.9f) * flicker;
                    _flames[i].GetComponent<Image>().color = Color.Lerp(UiKit.Hex("F59A3A"), UiKit.Danger, heat);
                }
                if (heat >= 1f) { burnt = true; break; }
                yield return null;
            }

            if (prompt != null) Destroy(prompt.gameObject);
            Destroy(gauge.gameObject);

            // How it turned out.
            int stars = Mathf.Clamp(5 - misses - (hits - perfect + 1) / 2 - (redTime > 1f ? 1 : 0), 1, 5);
            var cooked = new List<string>(_pot);
            string id = Cooking.Finish(cooked, dish, burnt, stars);
            _pot.Clear();
            bool good = dish != null && !burnt;
            string title = good ? Loc.T(dish.Name) + "!" : "Bizarre Food…";
            string line = good
                ? StarLine(stars) + "\nIt's in your bag. Look for someone with this dish in a bubble over their head!"
                : burnt ? "It burned to a crisp. Tap the spatula sooner to keep the heat down!"
                        : "Nobody has a recipe for that, and now we know why.";
            CardPopup.Show(title, line, HobbyData.ItemArt(id));
            _cooking = false;
            Refresh();
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
