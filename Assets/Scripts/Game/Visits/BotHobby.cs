using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Another capybara doing a hobby where you can see it: fishing from the water's edge with a
    /// rod, a bobber and a line; creeping up on a bug and swooping the net over it; or watching a
    /// bird through binoculars. A catch is held up for everyone to see, and only then does it
    /// count: a contest's scoreboard moves when you see an islander land something, not before.
    ///
    /// Two halves. <b>The show</b> (<see cref="Do"/>) is what everyone sees, in steps: "cast",
    /// "bite", "land", "lose" for a line; "bug", "net" for the net; "bird", "seen", "flew" for
    /// the binoculars; "stop" puts it all away. An islander your game plays performs its steps and
    /// Live passes each one on, so everyone at your contest sees the same thing. A real friend's
    /// capybara is shown their steps as their own game sends them (Fishing, WildBug and WildBird
    /// tell Live), so they're seen fishing too, not just standing about.
    ///
    /// <b>The doing</b> (<see cref="Pursue"/>, <see cref="Potter"/>) is an islander's own: off to
    /// a good spot (the water's edge, a flower, a tree), a try, and another, at a pace that keeps a
    /// contest fair; and now and then, on a visit, just for the fun of it. The fish, bug or bird
    /// it goes after is its own: the island's wildlife only lives inside your view, and an
    /// islander's has to be there wherever it is.
    /// </summary>
    [DefaultExecutionOrder(115)]   // after VisitorBot (110): the rod and binoculars sort against this frame's body
    public class BotHobby : MonoBehaviour
    {
        static readonly List<BotHobby> _all = new();

        VisitorBot _bot;

        void Awake()
        {
            _bot = GetComponent<VisitorBot>();
            _all.Add(this);
        }

        void OnDestroy()
        {
            _all.Remove(this);
            ClearLine();
            ClearBug();
            ClearBird();
            foreach (var go in _loose) if (go != null) Destroy(go);
            _loose.Clear();
        }

        // ---------------------------------------------------------------- the show

        enum Line { None, Out, Biting, Fight, Finale }

        Line _line;
        SpriteRenderer _rod, _glasses;
        GameObject _bobber;
        Transform _bobberBody;
        LineRenderer _cord;
        SpriteRenderer _fish;                 // the shadow coming to the bobber, then the fish on the line
        Vector2 _castFrom, _bobberAt, _hookAt;
        float _fightSince, _castAt = -10f, _nextRipple, _tension;
        Coroutine _show;
        readonly List<GameObject> _loose = new();   // a bug flying off, a fish swimming away, a catch held up
        static Material _cordMaterial;

        /// <summary>Is this an islander your game plays, while you host? Then the others are told each step.</summary>
        bool Telling => _bot != null && !_bot.Remote && _bot.NetId != null && _bot.NetId.StartsWith("bot:");

        /// <summary>A step of the show, here and (an islander you play) on everyone else's screen.</summary>
        public void Do(string step, Vector2 at, string id = null, int e = 0)
        {
            if (Telling) Live.SendHobby(_bot.NetId, step, at, id, e);
            Play(step, at, id, e);
        }

        /// <summary>A step someone else's game sent (Live): shown, never passed on.</summary>
        public void Play(string step, Vector2 at, string id, int e)
        {
            switch (step)
            {
                case "cast": Cast(at); break;
                case "bite": Bite(at, id, quick: e == 1); break;
                case "land": Land(at, id); break;
                case "lose": Lose(); break;
                case "bug": ShowBug(at, id); break;
                case "net": Net(at, id, e == 1); break;
                case "bird": ShowBird(at, id, e); break;
                case "seen": Seen(at, id); break;
                case "flew": BirdOff(); Glasses(false); break;
                case "stop": PutAway(); break;
            }
        }

        /// <summary>Everything away (a catch being held up finishes first).</summary>
        void PutAway()
        {
            if (_line != Line.Finale)
            {
                StopShow();
                ClearLine();
                RodAway();
            }
            BugOff();
            BirdOff();
            Glasses(false);
        }

        void Run(IEnumerator step)
        {
            StopShow();
            _show = StartCoroutine(step);
        }

        void StopShow()
        {
            if (_show != null) StopCoroutine(_show);
            _show = null;
        }

        Transform Body => _bot != null ? _bot.Body : null;

        /// <summary>Which way it's looking: +1 right, -1 left (the pictures face left).</summary>
        float Facing => Body != null && Body.localScale.x < 0f ? 1f : -1f;

        void Face(Vector2 towards)
        {
            var body = Body;
            if (body == null || Mathf.Abs(towards.x) < 0.01f) return;
            var s = body.localScale;
            s.x = Mathf.Abs(s.x) * (towards.x > 0f ? -1f : 1f);
            body.localScale = s;
        }

        Vector2 Paws => (Vector2)transform.position + new Vector2(0.35f * Facing, 0.55f);

        void Update()
        {
            Flutter();
            Fight();
            // A friend's capybara that walked off has reeled in.
            if (_bot != null && _bot.Remote && _line is Line.Out or Line.Biting or Line.Fight
                && Vector2.Distance(transform.position, _castFrom) > 1.2f)
                PutAway();
        }

        void LateUpdate()
        {
            var body = _bot != null ? _bot.BodySprite : null;
            if (_rod != null && _rod.gameObject.activeSelf)
            {
                if (body != null) _rod.sortingOrder = body.sortingOrder + 6;
                float bend = _line == Line.Fight ? _tension * 22f : 0f;
                // The cast: the rod swung back, and forward as the line flies out.
                float since = Time.time - _castAt;
                if (since < 0.35f) bend += 40f * (1f - since / 0.35f);
                _rod.transform.localRotation = Quaternion.Euler(0f, 0f, bend);
            }
            if (_glasses != null && body != null) _glasses.sortingOrder = body.sortingOrder + 7;
            DrawCord();
        }

        // ---------------------------------------------------------------- fishing

        void Cast(Vector2 spot)
        {
            StopShow();
            ClearBug();
            ClearBird();
            Glasses(false);
            ClearLine();
            Vector2 me = transform.position;
            Face(spot - me);
            RodOut();
            _castFrom = me;
            _bobberAt = _hookAt = spot;
            MakeBobber(spot);
            _line = Line.Out;
            _castAt = Time.time;
            Sfx.PlayAt(SfxKind.Plop, spot, 0.5f);
        }

        /// <summary>
        /// A fish comes over from further out, nibbles, and bites with a splash: the fight's on.
        /// <paramref name="quick"/>: a friend's bite (their nibbles have been and gone), straight in.
        /// </summary>
        void Bite(Vector2 at, string id, bool quick)
        {
            if (_line == Line.None || _bobber == null) Cast(at);
            var kind = HobbyData.FishById(id) ?? HobbyData.Fish[0];
            Run(Biting(kind, quick));
        }

        IEnumerator Biting(FishKind kind, bool quick)
        {
            _line = Line.Biting;
            var away = (_bobberAt - _castFrom).sqrMagnitude > 0.01f ? (_bobberAt - _castFrom).normalized : Vector2.down;
            var from = _bobberAt + away * 2.4f + new Vector2(-away.y, away.x) * UnityEngine.Random.Range(-1f, 1f);
            if (BunniSpace.PointOnLand(from)) from = _bobberAt + away * 1.2f;
            MakeShadow(kind, from);
            var to = _bobberAt + (from - _bobberAt).normalized * 0.35f;
            _fish.flipX = to.x > from.x;   // the shadow's nose points left
            float swim = quick ? 0.5f : 1.4f;
            _fish.transform.DOMove(to, swim).SetEase(Ease.OutSine).SetLink(_fish.gameObject);
            yield return new WaitForSeconds(swim);
            if (!quick)
                for (int i = 0; i < 2; i++)
                {
                    Nibble();
                    yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 0.9f));
                }
            // The big splash.
            Fishing.Splash(_bobberAt, 0.8f);
            Fishing.Ripple(_bobberAt, 1.6f);
            Sfx.PlayAt(SfxKind.Plop, _bobberAt, 0.8f, 0.1f);
            if (_bobberBody != null)
            {
                _bobberBody.DOKill();
                _bobberBody.localPosition = new Vector3(0f, -0.25f, 0f);
                _bobberBody.localScale = new Vector3(0.26f, 0.16f, 1f);
            }
            _line = Line.Fight;
            _fightSince = Time.time;
            _show = null;
        }

        void Nibble()
        {
            if (_bobberBody == null) return;
            _bobberBody.DOKill();
            _bobberBody.localPosition = Vector3.zero;
            _bobberBody.DOLocalMoveY(-0.1f, 0.1f).SetLoops(2, LoopType.Yoyo).SetLink(_bobberBody.gameObject);
            Sfx.PlayAt(SfxKind.Plop, _bobberAt, 0.3f);
            Fishing.Ripple(_bobberAt, 0.9f);
        }

        /// <summary>The fish on the line: it pulls and thrashes, rests, pulls again, and slowly comes in.</summary>
        void Fight()
        {
            if (_line != Line.Fight || _bobber == null) return;
            float t = Time.time - _fightSince;
            bool surging = Mathf.Repeat(t, 2.2f) < 0.9f;
            var end = RodTip() + Vector2.down * 0.5f;
            // Reeled a little closer all the time, never up onto the sand.
            float reel = 1f - 0.45f * (1f - Mathf.Exp(-t / 3f));
            var along = Vector2.Lerp(end, _hookAt, reel);
            for (float r = reel; r < 1f && BunniSpace.PointOnLand(along); r += 0.05f) along = Vector2.Lerp(end, _hookAt, r);
            if (BunniSpace.PointOnLand(along)) along = _hookAt;
            var dir = (_hookAt - end).sqrMagnitude > 0.0001f ? (_hookAt - end).normalized : Vector2.down;
            var side = new Vector2(-dir.y, dir.x);
            float thrash = surging ? Mathf.Sin(Time.time * 14f) * 0.3f : Mathf.Sin(Time.time * 3f) * 0.08f;
            var p = along + side * thrash;
            if (BunniSpace.PointOnLand(p)) p = along;
            _bobberAt = p;
            _bobber.transform.position = p;
            _tension = surging ? 0.6f + Mathf.Sin(Time.time * 9f) * 0.12f : 0.3f;
            if (_fish != null)
            {
                _fish.transform.position = p + dir * 0.35f;
                _fish.flipX = dir.x < 0f;
                _fish.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * (surging ? 22f : 8f)) * (surging ? 18f : 6f));
            }
            if (surging && Time.time >= _nextRipple)
            {
                _nextRipple = Time.time + 0.3f;
                Fishing.Ripple(p, 0.8f);
            }
        }

        /// <summary>Reeled in: out of the water, up to the rod, and held up for all to see. <paramref name="id"/>: a fish, or a piece of junk's picture.</summary>
        void Land(Vector2 at, string id)
        {
            if (_line == Line.None || _bobber == null) Cast(at);
            var kind = HobbyData.FishById(id ?? "");
            var picture = kind != null ? HobbyData.FishPicture(kind) : !string.IsNullOrEmpty(id) ? HobbyData.Art(id) : null;
            if (_fish == null) MakeShadow(kind ?? HobbyData.Fish[0], _bobberAt);
            Run(Landing(picture, kind));
        }

        IEnumerator Landing(Sprite picture, FishKind kind)
        {
            _line = Line.Finale;
            Vector2 start = _bobberAt;
            Fishing.Ripple(start, 1.4f);
            Sfx.PlayAt(SfxKind.Plop, start, 0.7f, 0.1f);
            float width = 0.55f + (kind != null ? kind.Shadow : 2) * 0.12f;
            if (picture != null && _fish != null)
            {
                _fish.DOKill();
                _fish.sprite = picture;
                _fish.color = Color.white;
                _fish.flipX = false;
                _fish.sortingOrder = 29999;   // just under the line
                _fish.transform.localScale = Vector3.one * (width / Mathf.Max(0.01f, picture.bounds.size.x));
            }
            // Up out of the water in an arc, to the end of the rod.
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
            {
                var p = Vector2.Lerp(start, RodTip() + Vector2.down * 0.5f, t * t * (3f - 2f * t)) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 0.6f;
                Dangle(p, kind != null);
                yield return null;
            }
            // A moment wriggling on the hook.
            for (float until = Time.time + 0.45f; Time.time < until;)
            {
                Dangle(RodTip() + Vector2.down * 0.5f, kind != null);
                yield return null;
            }
            Sfx.PlayAt(SfxKind.Chest, transform.position, 0.5f);
            ClearLine();
            RodAway();
            Trophy(picture, width);
            _line = Line.None;
            _show = null;
        }

        void Dangle(Vector2 hook, bool fish)
        {
            _bobberAt = hook;
            if (_bobber != null) _bobber.transform.position = hook;
            if (_fish == null) return;
            float wiggle = Mathf.Sin(Time.time * 18f) * 14f;
            _fish.transform.position = hook + Vector2.down * 0.32f;
            // Nose-up from the hook, wriggling (junk just swings, the right way up).
            _fish.transform.localRotation = Quaternion.Euler(0f, 0f, fish ? -90f + wiggle : wiggle * 0.5f);
        }

        /// <summary>It got away: off it swims, and the line comes in empty.</summary>
        void Lose() => Run(Losing());

        IEnumerator Losing()
        {
            _line = Line.Finale;
            if (_fish != null)
            {
                var f = _fish;
                _fish = null;
                _loose.Add(f.gameObject);
                var away = ((Vector2)f.transform.position - _castFrom).normalized;
                f.transform.DOMove((Vector2)f.transform.position + away * 3f, 1.2f).SetLink(f.gameObject);
                f.DOFade(0f, 1.2f).SetLink(f.gameObject).OnComplete(() => Destroy(f.gameObject));
            }
            Sfx.PlayAt(SfxKind.Pop, _bobberAt, 0.4f);
            Fishing.Ripple(_bobberAt, 1f);
            _tension = 0f;
            if (_bobberBody != null)
            {
                _bobberBody.localPosition = Vector3.zero;
                _bobberBody.localScale = Vector3.one * 0.26f;
            }
            yield return new WaitForSeconds(0.6f);
            ClearLine();
            RodAway();
            _line = Line.None;
            _show = null;
        }

        void RodOut()
        {
            var body = Body;
            if (body == null) return;
            if (_rod == null)
            {
                var art = HobbyData.Art("Rod");
                if (art == null) return;
                var go = new GameObject("FishingRod");
                go.transform.SetParent(body, false);
                _rod = go.AddComponent<SpriteRenderer>();
                _rod.sprite = art;
                _rod.flipX = true;   // tip towards the head, out over the water (as yours)
                go.transform.localScale = Vector3.one * (1.5f / Mathf.Max(0.01f, art.bounds.size.x));
                go.transform.localPosition = new Vector3(-0.55f, 0.85f, 0f);
            }
            _rod.gameObject.SetActive(true);
        }

        void RodAway()
        {
            if (_rod != null) _rod.gameObject.SetActive(false);
        }

        Vector2 RodTip()
        {
            if (_rod == null || !_rod.gameObject.activeInHierarchy) return (Vector2)transform.position + new Vector2(0.55f * Facing, 1.25f);
            var e = _rod.sprite.bounds.extents;
            return _rod.transform.TransformPoint(new Vector3(-e.x * 0.95f, e.y * 0.95f, 0f));
        }

        void MakeBobber(Vector2 at)
        {
            _bobber = new GameObject("Bobber");
            _bobber.transform.position = at;
            var body = new GameObject("Body").transform;
            body.SetParent(_bobber.transform, false);
            _bobberBody = body;
            var white = body.gameObject.AddComponent<SpriteRenderer>();
            white.sprite = UI.UiKit.Circle;
            white.color = Color.white;
            white.sortingOrder = Fishing.ShadowOrder + 400;
            body.localScale = Vector3.one * 0.26f;
            var red = new GameObject("Cap").AddComponent<SpriteRenderer>();
            red.transform.SetParent(body, false);
            red.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            red.transform.localScale = new Vector3(0.9f, 0.6f, 1f);
            red.sprite = UI.UiKit.Circle;
            red.color = UI.UiKit.Danger;
            red.sortingOrder = white.sortingOrder + 1;
            _bobber.transform.localScale = Vector3.zero;
            _bobber.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetLink(_bobber);
            Fishing.Ripple(at, 1.2f);

            _cord = new GameObject("FishingLine").AddComponent<LineRenderer>();
            _cord.positionCount = 12;
            _cord.useWorldSpace = true;
            if (_cordMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                _cordMaterial = shader != null ? new Material(shader) : white.sharedMaterial;
            }
            _cord.material = _cordMaterial;
            _cord.sortingOrder = 30000;
        }

        void DrawCord()
        {
            if (_cord == null) return;
            Vector2 tip = RodTip();
            Vector2 bob = _bobberBody != null ? (Vector2)_bobberBody.position : _bobberAt;
            bool tight = _line == Line.Fight || _line == Line.Finale;
            float sag = _line == Line.Fight ? Mathf.Lerp(0.3f, 0.05f, _tension) : tight ? 0.05f : 0.35f;
            for (int i = 0; i < _cord.positionCount; i++)
            {
                float t = i / (float)(_cord.positionCount - 1);
                var p = Vector2.Lerp(tip, bob, t) + Vector2.down * Mathf.Sin(t * Mathf.PI) * sag;
                _cord.SetPosition(i, new Vector3(p.x, p.y, 0f));
            }
            var c = _line == Line.Fight ? Fishing.TensionColour(_tension) : new Color(1f, 1f, 1f, 0.85f);
            _cord.startColor = _cord.endColor = c;
            _cord.startWidth = _cord.endWidth = _line == Line.Fight ? 0.045f : 0.025f;
        }

        void MakeShadow(FishKind kind, Vector2 at)
        {
            if (_fish != null) Destroy(_fish.gameObject);
            var go = new GameObject("BotFish_" + kind.Id);
            go.transform.position = at;
            _fish = go.AddComponent<SpriteRenderer>();
            _fish.sprite = UI.IconPainter.FishShadow;
            _fish.color = new Color(0.08f, 0.14f, 0.22f, 0f);
            _fish.sortingOrder = Fishing.ShadowOrder;
            float scale = 0.45f + kind.Shadow * 0.22f;
            go.transform.localScale = new Vector3(scale, scale * 0.8f, 1f);
            _fish.DOFade(0.42f, 0.6f).SetLink(go);
        }

        void ClearLine()
        {
            if (_bobber != null) Destroy(_bobber);
            if (_cord != null) Destroy(_cord.gameObject);
            if (_fish != null) Destroy(_fish.gameObject);
            _bobber = null;
            _bobberBody = null;
            _cord = null;
            _fish = null;
            _tension = 0f;
            _line = Line.None;
        }

        // ---------------------------------------------------------------- the net

        SpriteRenderer _bug;
        Sprite _bugStill;
        Sprite[] _bugFlap;
        BugKind _bugKind;
        Vector2 _bugHome;
        bool _bugSettled;
        float _bugPhase;

        /// <summary>A bug comes to a flower (or is found on its leaf, bark or grass), and the capybara has its eye on it.</summary>
        void ShowBug(Vector2 at, string id)
        {
            StopShow();
            ClearLine();
            RodAway();
            ClearBird();
            Glasses(false);
            ClearBug();
            var kind = HobbyData.Bug(id ?? "");
            if (kind == null) return;
            MakeBug(kind, at, flyIn: true);
            Face(at - (Vector2)transform.position);
        }

        void MakeBug(BugKind kind, Vector2 at, bool flyIn)
        {
            var art = HobbyData.Art("Bug_" + kind.Id);
            if (art == null) return;
            var go = new GameObject("BotBug_" + kind.Id);
            _bug = go.AddComponent<SpriteRenderer>();
            _bug.sprite = _bugStill = art;
            _bug.sortingOrder = 29000;
            var up = HobbyData.Art("Bug_" + kind.Id + "_Fly1");
            var down = HobbyData.Art("Bug_" + kind.Id + "_Fly2");
            _bugFlap = up != null && down != null ? new[] { up, down } : null;
            go.transform.localScale = Vector3.one * (WildBug.WidthOf(kind) / Mathf.Max(0.01f, art.bounds.size.x));
            _bugKind = kind;
            _bugHome = at;
            _bugPhase = UnityEngine.Random.value * 10f;
            if (flyIn && kind.Flies)
            {
                // In from a little way off, to the flower it's after.
                var from = at + new Vector2(UnityEngine.Random.value < 0.5f ? -3f : 3f, UnityEngine.Random.Range(1f, 2.5f));
                go.transform.position = from;
                _bug.flipX = at.x > from.x;
                _bugSettled = false;
                go.transform.DOMove(at, 1.3f).SetEase(Ease.InOutSine).SetLink(go).OnComplete(() => _bugSettled = true);
            }
            else
            {
                go.transform.position = at;
                _bug.color = new Color(1f, 1f, 1f, 0f);
                _bug.DOFade(1f, flyIn ? 0.6f : 0.15f).SetLink(go);
                _bugSettled = true;
            }
        }

        /// <summary>The bug about its flower: hovering and wavering if it flies, a slow creep if it crawls.</summary>
        void Flutter()
        {
            if (_bug == null) return;
            float t = Time.time + _bugPhase;
            if (_bugKind.Flies)
            {
                if (_bugFlap != null) _bug.sprite = _bugFlap[(int)(t * 8f) % 2];
                if (_bugSettled) _bug.transform.position = _bugHome + new Vector2(Mathf.Sin(t * 1.3f) * 0.22f, Mathf.Sin(t * 2.6f) * 0.12f);
            }
            else if (_bugSettled)
            {
                _bug.transform.position = _bugHome + new Vector2(Mathf.Sin(t * 0.25f) * 0.2f, 0f);
                _bug.flipX = Mathf.Cos(t * 0.25f) > 0f;
            }
        }

        Vector2? BugAt => _bug != null ? _bug.bounds.center : null;

        /// <summary>The net comes down over the bug: in it goes (and is held up), or off it darts.</summary>
        void Net(Vector2 at, string id, bool caught)
        {
            StopShow();
            ClearLine();
            RodAway();
            ClearBird();
            Glasses(false);
            var kind = HobbyData.Bug(id ?? "");
            if (_bug == null && kind != null) MakeBug(kind, at, flyIn: false);   // a friend's: their bug, here a moment
            if (_bug == null) return;
            kind = _bugKind;
            var bug = _bug;
            _bug = null;   // it stops still: here comes the net
            _loose.Add(bug.gameObject);
            Vector2 target = bug.bounds.center;
            Face(target - (Vector2)transform.position);
            var picture = _bugStill;
            WildBug.NetSwoop(Paws, target, () =>
            {
                if (bug == null) return;
                if (caught)
                {
                    bug.transform.DOKill();
                    bug.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetLink(bug.gameObject);
                    return;
                }
                // Missed: off it darts.
                var away = target + new Vector2(target.x >= transform.position.x ? 3.5f : -3.5f, 2.5f);
                bug.transform.DOKill();
                bug.transform.DOMove(away, 0.8f).SetEase(Ease.OutQuad).SetLink(bug.gameObject);
                bug.flipX = away.x > target.x;
                bug.DOFade(0f, 0.8f).SetLink(bug.gameObject).OnComplete(() => Destroy(bug.gameObject));
            }, () =>
            {
                if (!caught || this == null) return;
                if (bug != null) Destroy(bug.gameObject);
                Sfx.PlayAt(SfxKind.Pop, target, 0.5f);
                Trophy(picture, WildBug.WidthOf(kind) * 1.1f);
            }, heard: target, hit: caught);
        }

        /// <summary>The bug it was watching goes on its way (the capybara's off to do something else).</summary>
        void BugOff()
        {
            if (_bug == null) return;
            var bug = _bug;
            _bug = null;
            _loose.Add(bug.gameObject);
            if (!_bugKind.Flies)
            {
                bug.DOFade(0f, 0.5f).SetLink(bug.gameObject).OnComplete(() => Destroy(bug.gameObject));
                return;
            }
            Vector2 from = bug.transform.position;
            var to = from + new Vector2(UnityEngine.Random.value < 0.5f ? -4f : 4f, 2.5f);
            bug.flipX = to.x > from.x;
            bug.transform.DOMove(to, 1.6f).SetLink(bug.gameObject);
            bug.DOFade(0f, 1.6f).SetEase(Ease.InQuad).SetLink(bug.gameObject).OnComplete(() => Destroy(bug.gameObject));
        }

        void ClearBug()
        {
            if (_bug != null) Destroy(_bug.gameObject);
            _bug = null;
        }

        // ---------------------------------------------------------------- binoculars

        SpriteRenderer _bird;
        Sprite _birdStill;
        Sprite[] _birdFlap;
        BirdKind _birdKind;

        /// <summary>A bird flies in to a perch, and up come the binoculars.</summary>
        void ShowBird(Vector2 at, string id, int order)
        {
            StopShow();
            ClearLine();
            RodAway();
            ClearBug();
            ClearBird();
            var kind = HobbyData.BirdById(id ?? "");
            if (kind == null) return;
            MakeBird(kind, at, order, flyIn: true);
            Face(at - (Vector2)transform.position);
            Run(GlassesUpIn(1.3f));
        }

        IEnumerator GlassesUpIn(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (_bird != null) Face((Vector2)_bird.transform.position - (Vector2)transform.position);
            Glasses(true);
            _show = null;
        }

        void MakeBird(BirdKind kind, Vector2 at, int order, bool flyIn)
        {
            _birdKind = kind;
            _birdStill = HobbyData.BirdPicture(kind);
            if (_birdStill == null) return;
            var go = new GameObject("BotBird_" + kind.Id);
            _bird = go.AddComponent<SpriteRenderer>();
            _bird.sprite = _birdStill;
            var up = HobbyData.Art("Bird_" + kind.Id + "_Fly1");
            var down = HobbyData.Art("Bird_" + kind.Id + "_Fly2");
            _birdFlap = up != null && down != null ? new[] { up, down } : null;
            go.transform.localScale = Vector3.one * (WildBird.WidthOf(kind) / Mathf.Max(0.01f, _birdStill.bounds.size.x));
            int perched = order > 0 ? order : 29000;
            if (!flyIn)
            {
                go.transform.position = at;
                _bird.sortingOrder = perched;
                _bird.color = new Color(1f, 1f, 1f, 0f);
                _bird.DOFade(1f, 0.3f).SetLink(go);
                return;
            }
            // In on an arc from up and away, wings going, and down onto the perch.
            var from = at + new Vector2(UnityEngine.Random.value < 0.5f ? -5f : 5f, 3.5f);
            var bend = (from + at) * 0.5f + Vector2.up * 1.5f;
            go.transform.position = from;
            _bird.sortingOrder = 29010;
            FaceBird(_bird, kind, at.x - from.x);
            var bird = _bird;
            DOVirtual.Float(0f, 1f, 1.3f, t =>
            {
                if (bird == null) return;
                float e = 1f - (1f - t) * (1f - t);   // slows to land
                bird.transform.position = Vector2.Lerp(Vector2.Lerp(from, bend, e), Vector2.Lerp(bend, at, e), e);
                if (_birdFlap != null) bird.sprite = _birdFlap[(int)(t * 16f) % 2];
            }).SetLink(go).OnComplete(() =>
            {
                if (bird == null) return;
                bird.sprite = _birdStill;
                bird.sortingOrder = perched;
                bird.transform.DOPunchScale(new Vector3(0.08f, -0.08f, 0f), 0.25f, 4, 0.6f).SetLink(bird.gameObject);
                Sfx.PlayAt(SfxKind.Flutter, at, 0.3f, 0.15f);
            });
        }

        static void FaceBird(SpriteRenderer bird, BirdKind kind, float dx)
        {
            if (Mathf.Abs(dx) < 0.01f) return;
            bool right = dx > 0f;
            bird.flipX = WildBird.FacesRight(kind) ? !right : right;
        }

        /// <summary>Seen it: a twinkle, off it flies, and the binoculars come down.</summary>
        void Seen(Vector2 at, string id)
        {
            bool fresh = _bird == null;
            if (fresh)
            {
                // A friend's sighting: their bird, here a moment.
                StopShow();
                ClearLine();
                RodAway();
                ClearBug();
                var kind = HobbyData.BirdById(id ?? "");
                if (kind == null) return;
                MakeBird(kind, at, 0, flyIn: false);
                if (_bird == null) return;
            }
            Run(Seeing(fresh));
        }

        IEnumerator Seeing(bool fresh)
        {
            Face((Vector2)_bird.transform.position - (Vector2)transform.position);
            Glasses(true);
            if (fresh) yield return new WaitForSeconds(1f);
            var kind = _birdKind;
            Vector2 at = _bird != null ? (Vector2)_bird.bounds.center : (Vector2)transform.position;
            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Twinkle(at + Vector2.up * 0.2f, 6);
            Sfx.PlayAt(SfxKind.Heart, at, 0.45f);
            yield return new WaitForSeconds(0.35f);
            BirdOff();
            yield return new WaitForSeconds(0.4f);
            Glasses(false);
            Trophy(HobbyData.BirdPicture(kind), WildBird.WidthOf(kind));
            _show = null;
        }

        /// <summary>Off out of sight: up and away, wings going, fading as it goes.</summary>
        void BirdOff()
        {
            if (_bird == null) return;
            var bird = _bird;
            _bird = null;
            _loose.Add(bird.gameObject);
            Vector2 from = bird.transform.position;
            var to = from + new Vector2(from.x >= transform.position.x ? 6f : -6f, 4f);
            var flap = _birdFlap;
            FaceBird(bird, _birdKind, to.x - from.x);
            bird.sortingOrder = 29010;
            Sfx.PlayAt(SfxKind.Flutter, from, 0.4f, 0.15f);
            DOVirtual.Float(0f, 1f, 1.4f, t =>
            {
                if (bird == null) return;
                bird.transform.position = Vector2.Lerp(from, to, t * t);
                if (flap != null) bird.sprite = flap[(int)(t * 18f) % 2];
                bird.color = new Color(1f, 1f, 1f, t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f);
            }).SetLink(bird.gameObject).OnComplete(() => { if (bird != null) Destroy(bird.gameObject); });
        }

        void ClearBird()
        {
            if (_bird != null) Destroy(_bird.gameObject);
            _bird = null;
        }

        /// <summary>Binoculars up to its eyes, or down again.</summary>
        void Glasses(bool up)
        {
            if (!up)
            {
                if (_glasses != null && _glasses.gameObject.activeSelf)
                {
                    _glasses.DOKill();
                    var g = _glasses;
                    g.DOFade(0f, 0.2f).SetLink(g.gameObject).OnComplete(() => g.gameObject.SetActive(false));
                }
                return;
            }
            var body = Body;
            if (body == null) return;
            if (_glasses == null)
            {
                var art = HobbyData.Art("Binoculars");
                if (art == null) return;
                var go = new GameObject("Binoculars");
                go.transform.SetParent(body, false);
                _glasses = go.AddComponent<SpriteRenderer>();
                _glasses.sprite = art;
                go.transform.localScale = Vector3.one * (0.5f / Mathf.Max(0.01f, art.bounds.size.x));
                go.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            }
            _glasses.DOKill();
            _glasses.transform.DOKill();
            _glasses.gameObject.SetActive(true);
            _glasses.color = new Color(1f, 1f, 1f, 0f);
            _glasses.transform.localPosition = GlassesAt + new Vector3(0f, -0.3f, 0f);
            _glasses.DOFade(1f, 0.15f).SetLink(_glasses.gameObject);
            _glasses.transform.DOLocalMove(GlassesAt, 0.25f).SetEase(Ease.OutBack).SetLink(_glasses.gameObject);
        }

        /// <summary>Where the binoculars sit on the body's picture: up at its eyes (the head is on the left).</summary>
        static readonly Vector3 GlassesAt = new(-0.66f, 0.95f, 0f);

        // ---------------------------------------------------------------- held up

        /// <summary>The catch, held up over the capybara's paws for a moment, then tucked away.</summary>
        void Trophy(Sprite picture, float width)
        {
            if (picture == null) return;
            var go = new GameObject("Caught");
            _loose.Add(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = picture;
            sr.sortingOrder = 30100;
            Vector2 at = (Vector2)transform.position + new Vector2(0.4f * Facing, 1.3f);
            go.transform.position = at;
            float unit = Mathf.Max(0.45f, width) / Mathf.Max(0.01f, Mathf.Max(picture.bounds.size.x, picture.bounds.size.y));
            go.transform.localScale = Vector3.zero;
            var fx = HarvestEffects.Instance;
            DOTween.Sequence()
                .Append(go.transform.DOScale(unit * 1.15f, 0.25f).SetEase(Ease.OutBack))
                .Join(go.transform.DOMoveY(at.y + 0.35f, 0.25f).SetEase(Ease.OutQuad))
                .AppendCallback(() => { if (fx != null) fx.Twinkle(at + Vector2.up * 0.35f, 5); })
                .Append(go.transform.DOScale(unit, 0.12f))
                .AppendInterval(1f)
                .Append(go.transform.DOMove((Vector2)transform.position + Vector2.up * 0.6f, 0.3f).SetEase(Ease.InBack))
                .Join(go.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack))
                .SetLink(go)
                .OnComplete(() => Destroy(go));
        }

        // ---------------------------------------------------------------- the doing (an islander's own)

        Coroutine _pursuit;
        Vector2? _stand, _quarry;   // where it's set up, and what it's after: the others keep clear
        bool _tried, _landed, _arrived;

        /// <summary>Busy with a hobby of its own: it doesn't wander off or follow you meanwhile.</summary>
        public bool Busy => _pursuit != null;

        /// <summary>
        /// A contest: catch after catch, on <paramref name="island"/> (its shore, fishing), about one
        /// every <paramref name="pace"/> seconds, misses and all. <paramref name="caught"/> runs as
        /// each catch is held up. Until Stop.
        /// </summary>
        public void Pursue(Hobby hobby, float pace, Action caught, int island)
        {
            Stop();
            _pursuit = StartCoroutine(Pursuing(hobby, pace, 0, caught, island));
        }

        /// <summary>
        /// An islander with nothing else to do: a little fishing, bug catching or birdwatching close
        /// by, a try or three, if there's somewhere for it. False if there isn't.
        /// </summary>
        public bool Potter()
        {
            if (Busy || _bot == null || _bot.Remote) return false;
            var hobby = PickHobby();
            if (hobby == null) return false;
            float pace = hobby == Hobby.Fishing ? 20f : hobby == Hobby.Bugs ? 11f : 14f;
            _pursuit = StartCoroutine(Pursuing(hobby.Value, pace, UnityEngine.Random.Range(1, 4), null, -1));
            return true;
        }

        /// <summary>Puts it all away (a catch being held up finishes first), here and on the others' screens.</summary>
        public void Stop()
        {
            if (_pursuit != null) StopCoroutine(_pursuit);
            _pursuit = null;
            _stand = _quarry = null;
            if (_line != Line.None || _bug != null || _bird != null || (_glasses != null && _glasses.gameObject.activeSelf))
                Do("stop", transform.position);
        }

        static float MissChance(Hobby h) => h switch { Hobby.Fishing => 0.2f, Hobby.Bugs => 0.25f, _ => 0.12f };

        /// <summary>How good at it they are: their island's level, as a hobby level (what rarer things they turn up).</summary>
        int Skill => Mathf.Clamp(1 + (_bot != null && _bot.Who != null ? _bot.Who.level : 1) / 3, 1, 10);

        IEnumerator Pursuing(Hobby hobby, float pace, int tries, Action caught, int island)
        {
            int done = 0, fails = 0;
            while (tries <= 0 || done < tries)
            {
                // Misses take time too: each try is a little quicker, so the catches still come at the pace.
                float goal = pace * (1f - MissChance(hobby)) * UnityEngine.Random.Range(0.6f, 1.5f);
                _tried = _landed = false;
                yield return hobby switch
                {
                    Hobby.Fishing => FishOnce(goal, island),
                    Hobby.Bugs => NetOnce(goal, island),
                    _ => WatchOnce(goal, island),
                };
                if (!_tried)
                {
                    // Nowhere to try just now (or it couldn't get there): a look about, and again.
                    _stand = _quarry = null;
                    if (++fails >= 4 && tries > 0) break;
                    yield return new WaitForSeconds(UnityEngine.Random.Range(1.5f, 3f));
                    continue;
                }
                fails = 0;
                done++;
                if (_landed)
                {
                    caught?.Invoke();
                    _bot.Emote(UnityEngine.Random.value < 0.5f ? Emoji.Star : Emoji.Smile);
                }
                else if (UnityEngine.Random.value < 0.5f) _bot.Emote(UnityEngine.Random.value < 0.5f ? Emoji.Laugh : Emoji.Wow);
                _quarry = null;
                yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 2.5f));
            }
            _pursuit = null;
            _stand = _quarry = null;
            _bot.PastimeOver();
        }

        IEnumerator Walk(Vector2 to)
        {
            _arrived = Vector2.Distance(transform.position, to) < 0.5f;
            if (_arrived) yield break;
            if (!_bot.WalkTo(to)) yield break;
            for (float until = Time.time + 8f; _bot.Walking && Time.time < until;) yield return null;
            _arrived = Vector2.Distance(transform.position, to) < 0.6f;
        }

        IEnumerator FishOnce(float goal, int island)
        {
            float t0 = Time.time;
            var place = ShoreSpot(island);
            if (place == null) yield break;
            var (stand, water) = place.Value;
            _stand = stand;
            yield return Walk(stand);
            if (!_arrived) yield break;
            var cast = CastSpot(stand, water);
            if (cast == null) yield break;
            _tried = true;
            Do("cast", cast.Value);
            float fight = UnityEngine.Random.Range(2f, 3.5f);
            yield return new WaitForSeconds(Mathf.Max(2f, t0 + goal - Time.time - (2.6f + fight + 2f)));
            var kind = HobbyData.RollFish(Skill);
            Do("bite", cast.Value, kind.Id);
            yield return new WaitForSeconds(2.6f + fight + kind.Stars * 0.3f);
            _landed = UnityEngine.Random.value >= MissChance(Hobby.Fishing);
            Do(_landed ? "land" : "lose", _bobberAt, kind.Id);
            yield return new WaitForSeconds(_landed ? 2.4f : 1f);
        }

        IEnumerator NetOnce(float goal, int island)
        {
            float t0 = Time.time;
            var found = BugSpot(island);
            if (found == null) yield break;
            var (spot, kind) = found.Value;
            var stand = StandNear(spot, 1.6f, 1.9f, island);
            if (stand == null) yield break;
            _quarry = spot;
            _stand = stand;
            yield return Walk(stand.Value);
            if (!_arrived) yield break;
            _tried = true;
            Do("bug", spot, kind.Id);
            // Watching it a moment, then a careful step closer, and the swoop.
            float stalk = Mathf.Max(1.6f, t0 + goal - Time.time - 2.2f);
            yield return new WaitForSeconds(stalk * 0.6f);
            var closer = Vector2.MoveTowards(stand.Value, spot, 0.5f);
            if (BunniSpace.PointOnLand(closer)) _bot.WalkTo(closer);
            yield return new WaitForSeconds(stalk * 0.4f);
            _bot.Halt();
            _landed = UnityEngine.Random.value >= MissChance(Hobby.Bugs);
            Do("net", BugAt ?? spot, kind.Id, _landed ? 1 : 0);
            yield return new WaitForSeconds(_landed ? 2.2f : 1.2f);
        }

        IEnumerator WatchOnce(float goal, int island)
        {
            float t0 = Time.time;
            Vector2 me = transform.position;
            var near = new Rect(me.x - 8f, me.y - 6f, 16f, 12f);
            WildBird.Perch? perch = null;
            BirdKind kind = null;
            Vector2? stand = null;
            for (int i = 0; i < 8 && stand == null; i++)
            {
                bool wader = UnityEngine.Random.value < 0.25f;
                kind = HobbyData.RollBird(Skill, wader) ?? HobbyData.RollBird(Skill, wader = !wader);
                if (kind == null) yield break;   // everyone's asleep
                perch = wader ? Wildlife.ShorePerch(near)
                    : kind.Id == "hummingbird" ? Wildlife.FlowerPerch(near)
                    : kind.Id is "sparrow" or "dove" or "pheasant" && UnityEngine.Random.value < 0.4f ? Wildlife.GroundPerch(near)
                    : Wildlife.TreePerch(near);
                if (perch == null || !InPlace(perch.Value.At, island, false) || Crowded(perch.Value.At)) continue;
                stand = StandNear(perch.Value.At, 3f, 4.2f, island);
            }
            if (stand == null || perch == null) yield break;
            _quarry = perch.Value.At;
            _stand = stand;
            yield return Walk(stand.Value);
            if (!_arrived) yield break;
            _tried = true;
            Do("bird", perch.Value.At, kind.Id, perch.Value.Order);
            float watch = Mathf.Max(1.5f, t0 + goal - Time.time - 2f);
            yield return new WaitForSeconds(1.8f + watch);
            _landed = UnityEngine.Random.value >= MissChance(Hobby.Birding);
            Do(_landed ? "seen" : "flew", perch.Value.At, kind.Id);
            yield return new WaitForSeconds(_landed ? 2.2f : 1f);
        }

        // ---------------------------------------------------------------- where

        /// <summary>What there's somewhere for close by, favouring the one this islander likes best.</summary>
        Hobby? PickHobby()
        {
            Vector2 me = transform.position;
            var near = new Rect(me.x - 7f, me.y - 5f, 14f, 10f);
            var options = new List<Hobby>();
            if (Fishing.WaterNear(me, 8f).HasValue) options.Add(Hobby.Fishing);
            if (Wildlife.BugActivity > 0.3f && (Wildlife.FlowerPerch(near) != null || Wildlife.TreePerch(near, trunk: true) != null)) options.Add(Hobby.Bugs);
            if (Wildlife.BirdActivity > 0.3f && Wildlife.TreePerch(near) != null) options.Add(Hobby.Birding);
            if (options.Count == 0) return null;
            int seed = _bot != null && _bot.Who != null ? _bot.Who.seed : 0;
            var likes = new[] { Hobby.Fishing, Hobby.Bugs, Hobby.Birding }[(seed % 3 + 3) % 3];
            if (options.Contains(likes) && UnityEngine.Random.value < 0.6f) return likes;
            return options[UnityEngine.Random.Range(0, options.Count)];
        }

        static bool InPlace(Vector2 p, int island, bool shore) => island < 0 || Gathering.AreaHas(p, island, shore);

        /// <summary>Somebody else (an islander at it, a friend, you) is already there.</summary>
        bool Crowded(Vector2 p)
        {
            foreach (var o in _all)
            {
                if (o == this) continue;
                if (o._stand.HasValue && Vector2.Distance(o._stand.Value, p) < 1.8f) return true;
                if (o._quarry.HasValue && Vector2.Distance(o._quarry.Value, p) < 1.5f) return true;
            }
            foreach (var b in VisitorBot.All)
                if (b != null && b != _bot && Vector2.Distance(b.transform.position, p) < 1.2f) return true;
            var king = FindFirstObjectByType<PlayerAvatar>();
            return king != null && Vector2.Distance(king.transform.position, p) < 1.4f;
        }

        /// <summary>A place at the water's edge to fish from, and the water in front of it.</summary>
        (Vector2 stand, Vector2 water)? ShoreSpot(int island)
        {
            Vector2 me = transform.position;
            var here = Fishing.WaterNear(me, 2.4f);
            if (here.HasValue && InPlace(me, island, true) && !Crowded(me)) return (me, here.Value);
            for (int i = 0; i < 16; i++)
            {
                var probe = island >= 0 ? Gathering.SpotIn(island, true, me, 7f) : me + UnityEngine.Random.insideUnitCircle * 8f;
                if (!BunniSpace.PointOnLand(probe)) continue;
                var water = Fishing.WaterNear(probe, 4f);
                if (!water.HasValue) continue;
                var stand = EdgeToward(probe, water.Value);
                if (!InPlace(stand, island, true) || Crowded(stand) || !VisitorBot.Reachable(me, stand)) continue;
                var w = Fishing.WaterNear(stand, 2.4f);
                if (w.HasValue) return (stand, w.Value);
            }
            return null;
        }

        /// <summary>Down to the water's edge: as far towards it as the land goes, then a step back from the lip.</summary>
        static Vector2 EdgeToward(Vector2 land, Vector2 water)
        {
            var d = water - land;
            float length = d.magnitude;
            if (length < 0.01f) return land;
            d /= length;
            var last = land;
            for (float s = 0.2f; s <= length + 1f; s += 0.2f)
            {
                var p = land + d * s;
                if (!BunniSpace.PointOnLand(p)) break;
                last = p;
            }
            return Vector2.MoveTowards(last, land, 0.5f);
        }

        /// <summary>Where the bobber goes: a comfortable way out, in open water clear of the beach.</summary>
        static Vector2? CastSpot(Vector2 stand, Vector2 water)
        {
            var aim = (water - stand).sqrMagnitude > 0.0001f ? (water - stand).normalized : Vector2.down;
            for (float d = 2.4f; d < 7f; d += 0.3f)
                if (Fishing.OpenWater(stand + aim * d, 0.7f)) return stand + aim * d;
            for (float r = 2f; r <= 6f; r += 0.5f)
                for (int a = 0; a < 16; a++)
                {
                    float ang = a * Mathf.PI / 8f;
                    var q = stand + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.6f) * r;
                    if (Fishing.OpenWater(q, 0.7f)) return q;
                }
            return null;
        }

        /// <summary>A flower, a tree's bark or a patch of grass close by, and the bug to be found there.</summary>
        (Vector2 at, BugKind kind)? BugSpot(int island)
        {
            Vector2 me = transform.position;
            var near = new Rect(me.x - 7f, me.y - 5f, 14f, 10f);
            for (int i = 0; i < 8; i++)
            {
                float r = UnityEngine.Random.value;
                var perch = r < 0.55f ? Wildlife.FlowerPerch(near) : r < 0.85f ? Wildlife.TreePerch(near, trunk: true) : Wildlife.GroundPerch(near);
                if (perch == null) continue;
                var at = perch.Value.At;
                if (!InPlace(at, island, false) || Crowded(at)) continue;
                var home = perch.Value.Place switch { WildBird.Places.Flower => BugHome.Flower, WildBird.Places.Tree => BugHome.Tree, _ => BugHome.Ground };
                var kind = HobbyData.RollBug(Skill, home);
                if (kind == null || HobbyData.Art("Bug_" + kind.Id) == null) continue;
                if (kind.Flies && home != BugHome.Ground) at += Vector2.up * 0.25f;
                return (at, kind);
            }
            return null;
        }

        /// <summary>Somewhere to stand, between <paramref name="min"/> and <paramref name="max"/> from a spot, that it can walk to.</summary>
        Vector2? StandNear(Vector2 spot, float min, float max, int island)
        {
            Vector2 me = transform.position;
            var toMe = (me - spot).sqrMagnitude > 0.01f ? (me - spot).normalized : Vector2.right;
            for (int i = 0; i < 12; i++)
            {
                // Mostly on this side of it, kept side-on (it's easier to see a capybara swing a net from the side).
                float ang = (i == 0 ? 0f : UnityEngine.Random.Range(-100f, 100f)) * Mathf.Deg2Rad;
                var dir = new Vector2(toMe.x * Mathf.Cos(ang) - toMe.y * Mathf.Sin(ang), toMe.x * Mathf.Sin(ang) + toMe.y * Mathf.Cos(ang));
                dir.y *= 0.5f;
                if (dir.sqrMagnitude < 0.01f) continue;
                var p = spot + dir.normalized * UnityEngine.Random.Range(min, max);
                if (!BunniSpace.PointOnLand(p) || !InPlace(p, island, false) || Crowded(p) || !VisitorBot.Reachable(me, p)) continue;
                return p;
            }
            return null;
        }
    }
}
