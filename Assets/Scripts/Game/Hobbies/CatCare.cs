using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Cat care. Stray cats wander onto your island. Pet them, share a fish from your bag, and as
    /// their trust grows they will let you adopt them — it makes its home right where it was,
    /// naps and potters about there for good, goes by whatever name you give it, wears whatever
    /// you lend it from your wardrobe, and now and then brings you something. Each cat can only be fussed over every so often, so it is a
    /// little daily ritual rather than a button to mash.
    /// </summary>
    public class CatCare : MonoBehaviour
    {
        public const int AdoptAt = 6, MaxBond = 10;
        static readonly TimeSpan FussCooldown = TimeSpan.FromMinutes(5);

        float _nextStray;

        void Start()
        {
            if (Visit.Active) return;   // cats live on your island
            foreach (var c in HobbyLog.Cats)
            {
                if (!c.adopted) continue;
                var kind = HobbyData.CatById(c.id);
                var cat = CatView.MakeAdopted(kind);
                if (cat == null) continue;
                // No home yet (older saves, or saved while still in a bubble or your trail): it
                // settles near you instead, never back into a bubble.
                if (!c.unplaced && c.home != Vector2.zero) cat.Settle(c.home);
                else HobbyItems.AfterLoad(this, () => { if (cat != null) cat.Settle(SpotNearKing()); });
            }
            _nextStray = Time.time + UnityEngine.Random.Range(20f, 45f);
            _nextWhim = Time.time + UnityEngine.Random.Range(20f, 40f);
        }

        /// <summary>A patch of clear ground a few steps from you, for a cat with nowhere to live yet.</summary>
        static Vector2 SpotNearKing()
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            Vector2 around = king != null ? (Vector2)king.transform.position : Vector2.zero;
            for (int i = 0; i < 40; i++)
            {
                var p = around + UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1.6f, 3f + i * 0.05f);
                if (BunniSpace.CircleOnMap(p, 40f)) return p;
            }
            return around;
        }

        public const int NameLimit = 16;

        /// <summary>What a cat is called: the name you gave it once adopted, otherwise its own.</summary>
        public static string NameOf(CatKind kind)
        {
            if (kind == null) return "";
            var state = HobbyLog.CatState(kind.Id);
            return state != null && state.adopted && !string.IsNullOrWhiteSpace(state.name) ? state.name : kind.Name;
        }

        float _nextGiftCheck, _nextWhim;

        void Update()
        {
            if (Visit.Active) return;
            if (Time.time >= _nextGiftCheck)
            {
                _nextGiftCheck = Time.time + 2f;
                CheckPresents();
            }
            if (Time.time >= _nextWhim)
            {
                _nextWhim = Time.time + WhimCheck;
                CheckWhims();
            }
            // No strays during the opening: the first one turns up a minute or two after it, and
            // only when nothing else has the screen.
            if (Attention.TutorialRunning) { _nextStray = Time.time + UnityEngine.Random.Range(60f, 120f); return; }
            if (Time.time < _nextStray) return;
            if (!Attention.Free(6f)) { _nextStray = Time.time + 5f; return; }
            _nextStray = Time.time + UnityEngine.Random.Range(90f, 180f);

            // At most two strays about at once; never one that is already here.
            int strays = 0;
            var here = new HashSet<string>();
            foreach (var cat in FindObjectsByType<CatView>(FindObjectsSortMode.None))
            {
                here.Add(cat.Kind.Id);
                if (!HobbyLog.CatState(cat.Kind.Id).adopted) strays++;
            }
            if (strays >= 2) return;
            var options = new List<CatKind>();
            foreach (var k in HobbyData.Cats) if (!here.Contains(k.Id) && !HobbyLog.CatState(k.Id).adopted) options.Add(k);
            if (options.Count == 0) return;
            var kind = options[UnityEngine.Random.Range(0, options.Count)];
            if (Spawn(kind) != null) Hints.Show("A stray cat has wandered onto your island. Go and say hello!");
        }

        /// <summary>A cat whose present is due comes over with it — one at a time, and never mid-menu.</summary>
        static void CheckPresents()
        {
            // Adopted cats go looking for something to bring you, fish or no fish.
            foreach (var c in HobbyLog.Cats) CatGifts.Tick(c);
            if (UI.UiScreen.AnyOpen || UI.Cutscene.Playing || Time.timeScale <= 0f) return;
            foreach (var cat in FindObjectsByType<CatView>(FindObjectsSortMode.None))
                if (cat.Delivering) return;
            foreach (var cat in FindObjectsByType<CatView>(FindObjectsSortMode.None))
            {
                if (!cat.CanDeliver || !CatGifts.Due(HobbyLog.CatState(cat.Kind.Id))) continue;
                cat.BringPresent();
                return;
            }
        }

        // ---------------------------------------------------------------- whims

        const float WhimCheck = 15f;
        /// <summary>Average seconds between visits from a cat you've fed, meowing for another fish.</summary>
        const float BegEvery = 240f;

        /// <summary>
        /// Average seconds between head rides for a cat at <paramref name="hearts"/>: never below
        /// three, a rare treat at three (about half an hour), every ten minutes or so at four, and
        /// every couple of minutes at ten.
        /// </summary>
        static float RideEvery(int hearts) => hearts < 3 ? float.PositiveInfinity
            : hearts == 3 ? 1800f
            : 600f * Mathf.Pow(120f / 600f, (hearts - 4) / 6f);

        /// <summary>
        /// Now and then a cat does something just because. One that loves you hops onto your head
        /// for a ride; one you've ever given a fish trots over to meow at you for another. One cat
        /// at a time, only if it's near you, and never mid-menu.
        /// </summary>
        static void CheckWhims()
        {
            if (UI.UiScreen.AnyOpen || UI.Cutscene.Playing || UI.SpeechDialog.IsOpen || Time.timeScale <= 0f) return;
            if (Fishing.Instance != null && Fishing.Instance.Mode) return;
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null) return;
            var cats = FindObjectsByType<CatView>(FindObjectsSortMode.None);
            foreach (var c in cats) if (c.Busy) return;
            int start = UnityEngine.Random.Range(0, Mathf.Max(1, cats.Length));
            for (int i = 0; i < cats.Length; i++)
            {
                var cat = cats[(start + i) % cats.Length];
                if (!cat.CanDeliver || Vector2.Distance(cat.transform.position, king.transform.position) > 12f) continue;
                var state = HobbyLog.CatState(cat.Kind.Id);
                if (CatGifts.Due(state)) continue;   // a present comes first
                if (UnityEngine.Random.value < WhimCheck / RideEvery(state.bond)) { cat.ComeRide(); return; }
                bool justFed = DateTime.Now - new DateTime(state.lastFussTicks) < TimeSpan.FromMinutes(2);
                if (state.EverFed && !justFed && UnityEngine.Random.value < WhimCheck / BegEvery) { cat.ComeBeg(); return; }
            }
        }

        static CatView Spawn(CatKind kind)
        {
            if (kind == null) return null;
            var art = HobbyData.Art("Cat_" + kind.Id);
            if (art == null) return null;
            var king = FindFirstObjectByType<PlayerAvatar>();
            Vector2 around = king != null ? (Vector2)king.transform.position : Vector2.zero;
            for (int i = 0; i < 30; i++)
            {
                var p = around + UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(3f, 7f);
                if (!BunniSpace.PointOnLand(p)) continue;
                return CatView.Make(kind, art, p);
            }
            return null;
        }

        /// <summary>One cat on the island.</summary>
        [DefaultExecutionOrder(110)]   // after YSorter, so a cat on your head sorts over you
        public class CatView : MonoBehaviour, ITappable
        {
            public CatKind Kind { get; private set; }
            SpriteRenderer _sr;
            Sprite _sit, _walk;   // sitting about, and up on four paws for walking
            Placeable _placeable;   // adopted cats only: they can be bubbled and put down
            Vector2 _target;
            float _nextMove, _hop;
            bool _moving;

            public static CatView Make(CatKind kind, Sprite art, Vector2 at)
            {
                var go = new GameObject("Cat_" + kind.Id);
                go.transform.position = at;
                var body = new GameObject("Sprite");
                body.transform.SetParent(go.transform, false);
                var view = go.AddComponent<CatView>();
                view.Kind = kind;
                view._sr = body.AddComponent<SpriteRenderer>();
                view._sr.sprite = art;
                // Sprites are centred: lift the cat so its paws sit on the spot.
                body.transform.localPosition = new Vector3(0f, art.bounds.extents.y, 0f);
                go.transform.localScale = Vector3.one * (0.85f / Mathf.Max(0.01f, art.bounds.size.x));
                go.AddComponent<YSorter>();
                view._nextMove = Time.time + UnityEngine.Random.Range(1f, 4f);
                view._sr.color = new Color(1f, 1f, 1f, 0f);
                view._sr.DOFade(1f, 0.8f);
                return view;
            }

            /// <summary>An adopted cat as an item: inactive, ready for a bubble or to be settled at home.</summary>
            public static CatView MakeAdopted(CatKind kind)
            {
                var art = kind != null ? HobbyData.Art("Cat_" + kind.Id) : null;
                if (art == null) return null;
                var go = HobbyItems.Make(kind.Name, art, 0.85f, 1f, 15f);
                var view = go.AddComponent<CatView>();
                view.Kind = kind;
                view._sr = go.transform.Find("Sprite").GetComponent<SpriteRenderer>();
                view._placeable = go.GetComponent<Placeable>();
                view._nextMove = Time.time + UnityEngine.Random.Range(1f, 4f);
                return view;
            }

            /// <summary>Back at home from a save.</summary>
            public void Settle(Vector2 home)
            {
                transform.position = new Vector3(home.x, home.y, 0f);
                gameObject.SetActive(true);
                _placeable.MarkPlaced(silent: true);
            }

            bool Loose => _placeable == null || _placeable.Placed;

            // ---- bringing a present
            public bool Delivering { get; private set; }
            float _deliverUntil;

            WorldBubble _present;   // a heart over its head: it has brought you something

            /// <summary>On its feet in the world (not in a bubble or your trail), and not already holding a present.</summary>
            public bool CanDeliver => Loose && isActiveAndEnabled && !Delivering && _present == null && _whim == Whim.None && !_fading;

            /// <summary>Up to something: bringing a present, waiting with one, on your head, or begging.</summary>
            public bool Busy => Delivering || _present != null || _whim != Whim.None;

            public void BringPresent()
            {
                Delivering = true;
                _deliverUntil = Time.time + 10f;
                _moving = false;
                VisitorBot.ShowEmoji(_sr.transform, Emoji.Star);
            }

            /// <summary>It has arrived at your feet: it sits there, a heart over its head, until you tap it.</summary>
            void Deliver()
            {
                Delivering = false;
                Pose(false);
                _sr.transform.DOPunchScale(new Vector3(0.15f, 0.2f, 0f), 0.4f, 6, 0.6f);
                Sfx.Play(SfxKind.Heart, 0.6f);
                float top = _sr.bounds.max.y - transform.position.y;
                _present = WorldBubble.Show(transform, UI.IconPainter.Heart, top + 0.4f, OpenPresent, UI.UiKit.Hex("F2668B"));
                Hints.Show((HobbyLog.CatState(Kind.Id).adopted ? NameOf(Kind) : "The stray cat") + " has brought you something! Tap it to see.");
            }

            void OpenPresent()
            {
                if (_present == null) return;
                _present.Pop();
                _present = null;
                _nextMove = Time.time + UnityEngine.Random.Range(3f, 6f);
                _sr.transform.DOPunchScale(new Vector3(0.15f, 0.2f, 0f), 0.4f, 6, 0.6f);
                CatGifts.Open(Kind, HobbyLog.CatState(Kind.Id), transform.position, _sr.sprite);
            }

            // ---- whims: a ride on your head, or meowing for a fish
            enum Whim { None, ToRide, Jumping, Riding, Landing, ToBeg, Begging }
            Whim _whim;
            float _whimUntil, _jumpT, _nextLook, _homeBy;
            int _meows;
            float _nextMeow;
            bool _fading;
            Vector3 _jumpFrom, _jumpTo, _restScale;
            Transform _restParent, _kingBody;
            SpriteRenderer _kingSr;
            YSorter _sorter;
            WorldBubble _ask;   // a fish over its head: feed me

            /// <summary>Riding on (or hopping onto, or off) your head.</summary>
            public bool OnHead => _whim is Whim.Jumping or Whim.Riding or Whim.Landing;

            /// <summary>Where on your capybara a cat sits, as a fraction of its picture: just behind the eyes, on top.</summary>
            static readonly Vector2 HeadAnchor = new(0.37f, 0.79f);
            const string RideSeenKey = "CapyIsland.CatRideSeen", BegSeenKey = "CapyIsland.CatBegSeen";

            public void ComeRide() => Come(Whim.ToRide);
            public void ComeBeg() => Come(Whim.ToBeg);

            void Come(Whim w)
            {
                _whim = w;
                _deliverUntil = Time.time + 10f;
                _moving = false;
            }

            void EndWhim()
            {
                if (_ask != null) { _ask.Pop(); _ask = null; }
                _whim = Whim.None;
                _nextMove = Time.time + UnityEngine.Random.Range(1.5f, 3f);
            }

            bool NextToKing(out PlayerAvatar king)
            {
                king = FindFirstObjectByType<PlayerAvatar>();
                return king != null && Vector2.Distance(transform.position, king.transform.position) < 1.6f;
            }

            /// <summary>The top of your capybara's head, in its body's own space (which hops and turns with it).</summary>
            Vector3 HeadLocal()
            {
                var sp = _kingSr != null ? _kingSr.sprite : null;
                if (sp == null) return Vector3.up;
                var size = sp.rect.size / sp.pixelsPerUnit;
                var min = -sp.pivot / sp.pixelsPerUnit;
                return new Vector3(min.x + HeadAnchor.x * size.x, min.y + HeadAnchor.y * size.y, 0f);
            }

            void HopUp(PlayerAvatar king)
            {
                _kingBody = king.transform.Find("Sprite");
                if (_kingBody == null) _kingBody = king.transform;
                _kingSr = _kingBody.GetComponent<SpriteRenderer>();
                if (_kingSr == null) _kingSr = king.GetComponentInChildren<SpriteRenderer>();
                if (_kingSr == null) { EndWhim(); return; }
                Pose(false);
                _sorter = GetComponent<YSorter>();
                if (_sorter != null) _sorter.enabled = false;
                _restParent = transform.parent;
                _restScale = transform.localScale;
                _jumpFrom = transform.position;
                _jumpT = 0f;
                _whim = Whim.Jumping;
                _sr.transform.DOComplete();
                _sr.transform.DOPunchScale(new Vector3(0.12f, -0.15f, 0f), 0.2f, 4, 0.5f);   // a crouch before the leap
                Sfx.PlayAt(SfxKind.Plop, transform.position, 0.5f);
            }

            /// <summary>Landed on your head: it rides along, turning and hopping as you do.</summary>
            void SitOnHead()
            {
                var state = HobbyLog.CatState(Kind.Id);
                float world = Mathf.Abs(transform.lossyScale.y) * 0.9f;
                transform.SetParent(_kingBody, false);
                float parent = Mathf.Max(0.0001f, Mathf.Abs(_kingBody.lossyScale.y));
                transform.localScale = new Vector3(world / parent, world / parent, 1f);
                transform.localPosition = HeadLocal();
                transform.localRotation = Quaternion.identity;
                _sr.flipX = true;   // facing the same way you are
                _whim = Whim.Riding;
                _whimUntil = Time.time + UnityEngine.Random.Range(25f, 45f) + state.bond * 4f;
                _nextLook = Time.time + UnityEngine.Random.Range(3f, 7f);
                _sr.transform.DOPunchScale(new Vector3(0.15f, -0.12f, 0f), 0.3f, 5, 0.5f);
                VisitorBot.ShowEmoji(_sr.transform, Emoji.Heart);
                Sfx.Play(SfxKind.Heart, 0.35f);
                if (PlayerPrefs.GetInt(RideSeenKey, 0) == 0)
                {
                    PlayerPrefs.SetInt(RideSeenKey, 1);
                    Hints.Show(NameOf(Kind) + " hopped up onto your head! Take them for a walk.");
                }
            }

            void HopDown()
            {
                var king = FindFirstObjectByType<PlayerAvatar>();
                Vector2 at = king != null ? (Vector2)king.transform.position : (Vector2)transform.position;
                if (HotSpring.Poolside.HasValue) at = HotSpring.Poolside.Value;   // off a capybara in the water: onto the grass, not into it
                Vector2 land = at;
                float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                for (int tries = 0; tries < 2; tries++, side = -side)
                {
                    var p = at + new Vector2(side * 0.9f, -0.25f);
                    if (BunniSpace.PointOnLand(p)) { land = p; break; }
                }
                _jumpFrom = transform.position;
                transform.SetParent(_restParent, true);
                transform.localScale = _restScale;
                transform.rotation = Quaternion.identity;
                _jumpTo = land;
                _jumpT = 0f;
                _sr.flipX = land.x > _jumpFrom.x;
                _whim = Whim.Landing;
            }

            void Landed()
            {
                transform.position = _jumpTo;
                if (_sorter != null) _sorter.enabled = true;
                _whim = Whim.None;
                _sr.transform.DOComplete();
                _sr.transform.DOPunchScale(new Vector3(0.15f, -0.15f, 0f), 0.3f, 5, 0.5f);
                Sfx.PlayAt(SfxKind.Plop, transform.position, 0.5f);
                _nextMove = Time.time + UnityEngine.Random.Range(2f, 4f);
            }

            void LateUpdate()
            {
                Whims();
                DressUp();
            }

            // ---- its outfit (adopted cats, from your wardrobe: CatWear)
            readonly Dictionary<WearSlot, SpriteRenderer> _pieces = new();
            Sprite _dressedFor;
            bool _dressedFlip, _redress = true;

            void Redress() => _redress = true;

            /// <summary>Lays its pieces on whichever picture it is showing, facing its way, sorted with it.</summary>
            void DressUp()
            {
                if (_sr == null || _sr.sprite == null) return;
                var state = HobbyLog.CatState(Kind.Id);
                if (_pieces.Count == 0 && (!state.adopted || state.outfit == null || state.outfit.Count == 0)) return;
                if (_redress || _sr.sprite != _dressedFor || _sr.flipX != _dressedFlip)
                {
                    _redress = false;
                    _dressedFor = _sr.sprite;
                    _dressedFlip = _sr.flipX;
                    bool walking = _walk != null && _sr.sprite == _walk;
                    foreach (WearSlot slot in Enum.GetValues(typeof(WearSlot)))
                    {
                        var item = state.adopted ? CatWear.Worn(state, slot) : null;
                        var art = Wardrobe.Art(item);
                        if (!_pieces.TryGetValue(slot, out var piece) || piece == null)
                        {
                            if (art == null) continue;
                            var go = new GameObject("Wear_" + slot);
                            go.transform.SetParent(_sr.transform, false);
                            piece = go.AddComponent<SpriteRenderer>();
                            _pieces[slot] = piece;
                        }
                        piece.enabled = art != null;
                        if (art == null) continue;
                        piece.sprite = art;
                        CatWear.Place(piece, item, _sr.sprite, art, walking, _sr.flipX);
                    }
                }
                // Fades in and out with the cat.
                var tint = new Color(1f, 1f, 1f, _sr.color.a);
                foreach (var p in _pieces.Values) if (p != null) p.color = tint;
                CapyOutfit.SortPieces(_sr, _pieces, slot => CatWear.Worn(state, slot));
            }

            void Whims()
            {
                switch (_whim)
                {
                    case Whim.Jumping:
                    {
                        if (_kingBody == null) { Landed(); return; }
                        _jumpT += Time.deltaTime / 0.45f;
                        float k = Mathf.Clamp01(_jumpT);
                        var to = _kingBody.TransformPoint(HeadLocal());
                        transform.position = Vector3.Lerp(_jumpFrom, to, k * k * (3f - 2f * k)) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.9f;
                        _sr.sortingOrder = _kingSr.sortingOrder + 6;
                        if (k >= 1f) SitOnHead();
                        break;
                    }
                    case Whim.Riding:
                    {
                        if (_kingBody == null) { _whim = Whim.None; return; }
                        _sr.sortingOrder = _kingSr.sortingOrder + 6;
                        // Looking about: now and then it peers back the way you came.
                        if (Time.time >= _nextLook)
                        {
                            _sr.flipX = !_sr.flipX;
                            _nextLook = Time.time + (_sr.flipX ? UnityEngine.Random.Range(4f, 9f) : UnityEngine.Random.Range(1.5f, 3f));
                        }
                        bool calm = !UI.UiScreen.AnyOpen && !UI.Cutscene.Playing && Time.timeScale > 0f;
                        if (Time.time >= _whimUntil && calm) HopDown();
                        break;
                    }
                    case Whim.Landing:
                    {
                        _jumpT += Time.deltaTime / 0.4f;
                        float k = Mathf.Clamp01(_jumpT);
                        transform.position = Vector3.Lerp(_jumpFrom, _jumpTo, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.6f;
                        if (k >= 1f) Landed();
                        break;
                    }
                }
            }

            void StartBegging(PlayerAvatar king)
            {
                _whim = Whim.Begging;
                Pose(false);
                _sr.flipX = king.transform.position.x > transform.position.x;
                float top = _sr.bounds.max.y - transform.position.y;
                var fish = HobbyData.Fish.Length > 0 ? HobbyData.FishPicture(HobbyData.Fish[0]) : null;
                _ask = WorldBubble.Show(transform, fish, top + 0.4f, Tap);
                _meows = UnityEngine.Random.Range(2, 4);   // a second or two of it
                _nextMeow = Time.time + 0.1f;
                _whimUntil = Time.time + 8f;
                if (PlayerPrefs.GetInt(BegSeenKey, 0) == 0)
                {
                    PlayerPrefs.SetInt(BegSeenKey, 1);
                    Hints.Show(NameOf(Kind) + " remembers exactly who has the fish…");
                }
            }

            void Beg()
            {
                if (_meows > 0 && Time.time >= _nextMeow)
                {
                    _meows--;
                    _nextMeow = Time.time + UnityEngine.Random.Range(0.55f, 0.8f);
                    Sfx.PlayAt(SfxKind.Meow, transform.position, 0.8f, 0.12f);
                    _sr.transform.DOComplete();
                    _sr.transform.DOPunchScale(new Vector3(0.06f, 0.16f, 0f), 0.3f, 5, 0.5f);
                }
                // Ignored, it gives up and wanders off — with one last complaint, sometimes.
                if (Time.time >= _whimUntil && !UI.SpeechDialog.IsOpen)
                {
                    if (UnityEngine.Random.value < 0.4f) Sfx.PlayAt(SfxKind.Meow, transform.position, 0.5f, 0.2f);
                    EndWhim();
                }
            }

            /// <summary>Wandered off following you: it trots back home, or (across water) slips off and turns up there.</summary>
            bool FarFromHome => _placeable != null && Vector2.Distance(transform.position, HobbyLog.CatState(Kind.Id).home) > 5f;

            void SlipHome()
            {
                _moving = false;
                _fading = true;
                _sr.DOKill();
                _sr.DOFade(0f, 0.5f).OnComplete(() =>
                {
                    if (this == null) return;
                    transform.position = HobbyLog.CatState(Kind.Id).home;
                    _sr.DOFade(1f, 0.5f).OnComplete(() => _fading = false);
                });
            }

            /// <summary>Walking on four paws, or sitting: swaps the picture (and puts a sitting cat back on its feet).</summary>
            void Pose(bool walking)
            {
                if (_sit == null) _sit = _sr.sprite;
                if (_walk == null && walking) _walk = HobbyData.Art("Cat_" + Kind.Id + "_Walk");
                var want = walking && _walk != null ? _walk : _sit;
                if (_sr.sprite == want) return;
                _sr.sprite = want;
                // The standing pose is longer, so in the same width it would look smaller: give it back its size.
                _sr.transform.localScale = Vector3.one * (want == _sit ? 1f : 1.3f);
                if (!walking) _sr.transform.localPosition = new Vector3(0f, want.bounds.extents.y, 0f);
            }

            /// <summary>Trots over to the king, faster than a stroll. Returns true when it has arrived.</summary>
            bool TrotToKing()
            {
                Pose(true);
                var king = FindFirstObjectByType<PlayerAvatar>();
                if (king == null) return true;
                Vector2 here = transform.position;
                Vector2 goal = (Vector2)king.transform.position + new Vector2(here.x < king.transform.position.x ? -0.9f : 0.9f, -0.25f);
                if ((goal - here).sqrMagnitude < 0.04f || Time.time > _deliverUntil) return true;
                var next = Vector2.MoveTowards(here, goal, 2.6f * Time.deltaTime);
                if (!BunniSpace.PointOnLand(next)) return Time.time > _deliverUntil - 7f;   // blocked: hand it over from here
                transform.position = next;
                _sr.flipX = goal.x > here.x;
                _hop += Time.deltaTime * 14f;
                _sr.transform.localPosition = new Vector3(0f, _sr.sprite.bounds.extents.y * _sr.transform.localScale.y + Mathf.Abs(Mathf.Sin(_hop)) * 0.08f / transform.localScale.y, 0f);
                return false;
            }

            void OnPlacedInWorld()
            {
                var state = HobbyLog.CatState(Kind.Id);
                bool first = state.unplaced || state.home == Vector2.zero;
                state.home = transform.position;
                state.unplaced = false;
                HobbyLog.Save();
                _nextMove = Time.time + UnityEngine.Random.Range(1f, 3f);
                if (first) VisitorBot.ShowEmoji(_sr.transform, Emoji.Heart);
            }

            void OnReturnedToBubble()
            {
                var state = HobbyLog.CatState(Kind.Id);
                state.unplaced = true;
                HobbyLog.Save();
                _moving = false;
                if (_whim is Whim.ToBeg or Whim.Begging or Whim.ToRide) EndWhim();
            }

            void OnEnable()
            {
                TapTargets.Register(this);
                CatWear.Changed += Redress;
                Wardrobe.Changed += Redress;   // a sewn piece redrawn
                _redress = true;
            }

            void OnDisable()
            {
                TapTargets.Unregister(this);
                CatWear.Changed -= Redress;
                Wardrobe.Changed -= Redress;
            }

            public float TapDistance(Vector2 worldPoint)
            {
                if (_sr == null || !Loose || OnHead || _fading) return -1f;   // a tap on a rider is a tap on you
                var b = _sr.bounds;
                return b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z)) ? Vector2.Distance(b.center, worldPoint) : -1f;
            }

            void Update()
            {
                if (!Loose) return;   // riding in your trail, or in its bubble
                if (Delivering)
                {
                    if (TrotToKing()) Deliver();
                    return;
                }
                if (_present != null) return;   // sitting by you, waiting to be noticed
                switch (_whim)
                {
                    case Whim.ToRide:
                    case Whim.ToBeg:
                    {
                        if (!TrotToKing()) return;
                        // Couldn't reach you (water in the way, or you ran off): never mind.
                        if (!NextToKing(out var king)) { EndWhim(); return; }
                        if (_whim == Whim.ToRide) HopUp(king); else StartBegging(king);
                        return;
                    }
                    case Whim.Begging: Beg(); return;
                    case Whim.None: break;
                    default: return;   // on your head: LateUpdate has it
                }
                if (_fading) return;
                if (Time.time >= _nextMove)
                {
                    // Mostly sitting about; now and then a little stroll — a pet never far from home.
                    _nextMove = Time.time + UnityEngine.Random.Range(4f, 9f);
                    if (FarFromHome)
                    {
                        // Left behind somewhere after a ride or a beg: off home it goes.
                        _target = HobbyLog.CatState(Kind.Id).home;
                        _moving = true;
                        _homeBy = Time.time + 5f;
                    }
                    else if (UnityEngine.Random.value < 0.55f)
                    {
                        var from = _placeable != null ? HobbyLog.CatState(Kind.Id).home : (Vector2)transform.position;
                        var t = from + UnityEngine.Random.insideUnitCircle * (_placeable != null ? 1.6f : 2.5f);
                        if (BunniSpace.PointOnLand(t)) { _target = t; _moving = true; }
                    }
                }
                if (!_moving) { Pose(false); return; }
                Pose(true);
                Vector2 here = transform.position;
                var next = Vector2.MoveTowards(here, _target, 1.2f * Time.deltaTime);
                bool stuck = !BunniSpace.PointOnLand(next);
                if (_homeBy > 0f && (stuck || Time.time > _homeBy))
                {
                    _homeBy = 0f;
                    if (FarFromHome) { SlipHome(); return; }
                }
                if (stuck) { _moving = false; return; }
                transform.position = next;
                _sr.flipX = _target.x > here.x;
                _hop += Time.deltaTime * 10f;
                _sr.transform.localPosition = new Vector3(0f, _sr.sprite.bounds.extents.y * _sr.transform.localScale.y + Mathf.Abs(Mathf.Sin(_hop)) * 0.05f / transform.localScale.y, 0f);
                if ((next - _target).sqrMagnitude < 0.01f) { _moving = false; _homeBy = 0f; }
            }

            public void Tap()
            {
                if (_present != null) { OpenPresent(); return; }
                var state = HobbyLog.CatState(Kind.Id);
                bool begging = _whim == Whim.Begging;
                bool tired = DateTime.Now - new DateTime(state.lastFussTicks) < FussCooldown;
                string hearts = $"Trust {state.bond}/{MaxBond}";
                var pages = new[]
                {
                    begging
                        ? $"<b>{(state.adopted ? NameOf(Kind) : "The cat")}</b> meows at you. Then at your bag. Then at you again, louder."
                        : state.adopted
                        ? $"<b>{NameOf(Kind)}</b> purrs and bumps your hand.\n<size=80%>{Kind.Personality}</size>"
                        : $"A stray cat. It watches you carefully.\n<size=80%>{Kind.Personality}</size>",
                };
                var choices = new List<UI.SpeechDialog.Choice>
                {
                    new("Pet", () => Fuss(state, 1, 2, Emoji.Heart), !tired),
                    new($"Give a fish ({HobbyLog.FishInBag()})", () =>
                    {
                        if (!HobbyLog.Take("fish:any")) return;
                        state.fed++;
                        Fuss(state, 3, 5, Emoji.Star);
                        if (_whim == Whim.Begging) EndWhim();
                        bool already = CatGifts.FishGiftComing(state);
                        CatGifts.Fed(state);
                        if (!already) Hints.Show("The cat licks its whiskers and gives you a long, thoughtful look. It's planning something…");
                    }, (!tired || begging) && HobbyLog.FishInBag() > 0),   // a beggar can always be given one
                };
                if (!state.adopted && state.bond >= AdoptAt)
                    choices.Add(new UI.SpeechDialog.Choice("Adopt!", () => Adopt(state)));
                if (state.adopted)
                {
                    choices.Add(new UI.SpeechDialog.Choice("Dress up", () => UI.WardrobeScreen.ShowFor(Kind)));
                    choices.Add(new UI.SpeechDialog.Choice("Rename", () => Rename(state)));
                }
                choices.Add(new UI.SpeechDialog.Choice("Leave", null));
                string title = state.adopted ? NameOf(Kind) : "Stray cat";
                UI.SpeechDialog.ShowChoices(_sr.sprite, title + "  ·  " + hearts,
                    tired && !begging ? new[] { pages[0], "It's had plenty of fuss for now. Come back in a little while." } : pages,
                    null, choices);
            }

            void Rename(HobbyLog.Cat state)
            {
                UI.NamePrompt.Ask("Name your cat", _sr.sprite, NameOf(Kind), NameLimit, name =>
                {
                    // Cleared, or back to its own name: it goes by that again.
                    name = (name ?? "").Trim();
                    state.name = name.Length == 0 || name == Kind.Name ? null : name;
                    HobbyLog.Save();
                    VisitorBot.ShowEmoji(_sr.transform, Emoji.Heart);
                    Sfx.Play(SfxKind.Heart, 0.5f);
                });
            }

            void Fuss(HobbyLog.Cat state, int bond, int xp, Emoji e)
            {
                state.bond = Mathf.Min(MaxBond, state.bond + bond);
                state.lastFussTicks = DateTime.Now.Ticks;
                HobbyLog.Save();
                HobbyLog.AddXp(Hobby.Cats, xp);
                VisitorBot.ShowEmoji(_sr.transform, e);
                _sr.transform.DOPunchScale(new Vector3(0.1f, 0.15f, 0f), 0.35f, 6, 0.6f);
                Sfx.Play(SfxKind.Heart, 0.5f);
                if (!state.adopted && state.bond >= AdoptAt)
                    Hints.Show("This cat really trusts you now. Tap it to adopt it!");
            }

            void Adopt(HobbyLog.Cat state)
            {
                state.adopted = true;
                NewStuff.Add("hobbies/Cats/" + Kind.Id);
                state.unplaced = true;
                HobbyLog.Save();
                HobbyLog.AddXp(Hobby.Cats, 30);
                Motes.Trigger("hobby_cat");

                // The stray makes its home right where it was found: a cat is not luggage, so it
                // never rides in your trail.
                var pet = MakeAdopted(Kind);
                if (pet != null) pet.Settle(transform.position);
                var fx = HarvestEffects.Instance;
                if (fx != null) fx.Twinkle(transform.position + Vector3.up * 0.4f, 8);
                UI.CardPopup.Show(Kind.Name + " is yours!",
                    Kind.Personality + "\n" + Kind.Name + " has made a home right here.", _sr.sprite);
                _sr.DOKill();
                Destroy(gameObject);
            }
        }
    }
}
