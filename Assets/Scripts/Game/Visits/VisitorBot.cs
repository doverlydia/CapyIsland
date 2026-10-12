using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CapyIsland.Game
{
    public enum Emoji { Wave, Heart, Smile, Laugh, Wow, Star }

    /// <summary>
    /// Another "player" on the island — the friend whose island you are visiting, or a friend who
    /// came to visit you. They wander, tag along behind you, answer your emojis (and start a few of
    /// their own), and a visiting guest pitches in: feeds a hungry worker, shoos a pest, leaves you
    /// a little gift, then "logs off".
    ///
    /// No text from them, ever: just emojis, the way strangers in an online game actually talk.
    /// </summary>
    // After YSorter (100): outfit pieces sort against this frame's body order, not last frame's —
    // otherwise a cape flickers in front of and behind the body as they walk up and down.
    [DefaultExecutionOrder(110)]
    public class VisitorBot : MonoBehaviour, IMapMarker
    {
        public enum Role { Host, Guest }

        /// <summary>The visitor or host currently on the island, if any.</summary>
        public static VisitorBot Current { get; private set; }

        /// <summary>A guest has logged off, with who it was.</summary>
        public static event Action<FriendProfile> GuestLeft;

        /// <summary>An emoji someone showed, and who showed it (null for the player).</summary>
        public static event Action<Transform, Emoji> Emoted;

        public FriendProfile Who { get; private set; }
        public Role As { get; private set; }

        const float Speed = 3.6f;

        SpriteRenderer _body;
        Transform _bodyTransform;
        Vector3 _bodyRest;
        readonly Dictionary<WearSlot, SpriteRenderer> _pieces = new();
        Vector2 _target;
        bool _moving;
        float _hop;
        float _nextThink, _nextEmote, _leaveAt = -1f, _giftAt = -1f;
        bool _leaving;
        Component _task;   // a hungry worker, a pest, or a workplace to plant flowers by
        float _taskSince, _lastProgress;
        Vector2 _progressAt;
        readonly HashSet<Component> _skip = new();   // chores it could not get to
        int _fed, _pests, _planted, _plantsLeft = 2;
        float _nextPlant;
        readonly Queue<(float at, Emoji e)> _replies = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            Current = null;
            GuestLeft = null;
            Emoted = null;
        }

        // ---------------------------------------------------------------- arriving

        public static VisitorBot Spawn(FriendProfile who, Role role, Vector2 at)
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            var go = new GameObject("Visitor_" + who.name);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            go.transform.localScale = king != null ? king.transform.localScale : Vector3.one * 1.3f;

            // A shadow like the king's, and a body.
            var kingShadow = king != null ? king.transform.Find("Shadow") : null;
            if (kingShadow != null)
            {
                var shadow = Instantiate(kingShadow.gameObject, go.transform);
                shadow.name = "Shadow";
                shadow.transform.localPosition = kingShadow.localPosition;
                shadow.transform.localScale = kingShadow.localScale;
            }
            var bodyGo = new GameObject("Sprite");
            bodyGo.transform.SetParent(go.transform, false);
            var body = bodyGo.AddComponent<SpriteRenderer>();
            var catalog = ItemCatalog.Instance;
            body.sprite = Wardrobe.Ready ? Wardrobe.BaseSprite : catalog != null ? catalog.Icon("Capybara") : null;
            float t = Mathf.Abs(who.tint) * 0.45f;
            body.color = Color.Lerp(Color.white, who.tint > 0 ? new Color(0.84f, 0.72f, 0.6f) : new Color(0.95f, 0.88f, 0.8f), t);

            var bot = go.AddComponent<VisitorBot>();
            bot.Who = who;
            bot.As = role;
            bot._body = body;
            bot._bodyTransform = bodyGo.transform;
            bot._bodyRest = bodyGo.transform.localPosition;
            bot.NameTag(who.name);
            go.AddComponent<YSorter>();
            if (Wardrobe.Ready) CapyOutfit.Dress(body, bot._pieces, bot.Wearing);

            Current = bot;
            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Poof(at, new Color(1f, 1f, 1f));
            go.transform.localScale *= 0.3f;
            go.transform.DOScale(king != null ? king.transform.localScale : Vector3.one * 1.3f, 0.4f).SetEase(Ease.OutBack);

            bot._nextThink = Time.time + 0.8f;
            bot._nextEmote = Time.time + UnityEngine.Random.Range(14f, 26f);
            bot.Later(0.9f, Emoji.Wave);
            if (role == Role.Guest)
            {
                bot._leaveAt = Time.time + UnityEngine.Random.Range(150f, 260f);
                bot._giftAt = Time.time + UnityEngine.Random.Range(35f, 70f);
                bot._nextPlant = Time.time + UnityEngine.Random.Range(12f, 25f);
            }
            return bot;
        }

        WearItem Wearing(WearSlot slot)
        {
            foreach (var id in Who.outfit)
            {
                var item = Wardrobe.Find(id);
                if (item != null && item.Slot == slot) return item;
            }
            return null;
        }

        void NameTag(string text)
        {
            var go = new GameObject("NameTag");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 2.05f, 0f);
            var tmp = go.AddComponent<TextMeshPro>();
            if (UI.UiKit.HeadingFont != null) tmp.font = UI.UiKit.HeadingFont;
            tmp.text = text;
            tmp.fontSize = 3.2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(60, 40, 25, 255);
            tmp.rectTransform.sizeDelta = new Vector2(6f, 1f);
            tmp.sortingOrder = 31000;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        // Shown at the screen edge and on the map, so you can always find who is with you.
        void OnEnable() => MapMarkers.Register(this);
        void OnDisable() => MapMarkers.Unregister(this);
        public MarkerKind MarkerKind => MarkerKind.Friend;
        public bool MarkerActive => !_leaving;
        public Vector2 MarkerPosition => transform.position;
        public Sprite MarkerIcon => _body != null ? _body.sprite : null;

        public bool Leaving => _leaving;

        /// <summary>You asked them to go: a wave, and off they go — no "add friend" afterwards.</summary>
        public void SendHome()
        {
            if (_leaving) return;
            _sentHome = true;
            LogOff();
        }

        bool _sentHome;

        // ---------------------------------------------------------------- talking

        /// <summary>The player sent an emoji: answer it, like a person would, after a beat.</summary>
        public void HearPlayer(Emoji e)
        {
            Emoji reply = e switch
            {
                Emoji.Heart => UnityEngine.Random.value < 0.6f ? Emoji.Heart : Emoji.Smile,
                Emoji.Smile => UnityEngine.Random.value < 0.5f ? Emoji.Smile : Emoji.Heart,
                Emoji.Laugh => UnityEngine.Random.value < 0.7f ? Emoji.Laugh : Emoji.Smile,
                Emoji.Wow => UnityEngine.Random.value < 0.5f ? Emoji.Laugh : Emoji.Wow,
                Emoji.Star => UnityEngine.Random.value < 0.5f ? Emoji.Heart : Emoji.Star,
                _ => Emoji.Wave,
            };
            Later(UnityEngine.Random.Range(0.6f, 1.6f), reply);
            _nextEmote = Mathf.Max(_nextEmote, Time.time + 10f);
            // Being greeted makes people come over.
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king != null && Vector2.Distance(king.transform.position, transform.position) > 3f)
                Head((Vector2)king.transform.position + UnityEngine.Random.insideUnitCircle * 1.8f);
        }

        public void Emote(Emoji e)
        {
            Emoted?.Invoke(_bodyTransform != null ? _bodyTransform : transform, e);
            if (_bodyTransform != null) _bodyTransform.DOPunchScale(new Vector3(0.12f, 0.16f, 0f), 0.35f, 6, 0.6f);
        }

        /// <summary>Shows an emoji bubble over anyone (a cat, a fish-happy worker…).</summary>
        public static void ShowEmoji(Transform over, Emoji e) => Emoted?.Invoke(over, e);

        /// <summary>The player sent an emoji (shown over the king).</summary>
        public static void PlayerEmote(Emoji e)
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            var body = king != null ? king.transform.Find("Sprite") : null;
            Emoted?.Invoke(body != null ? body : king != null ? king.transform : null, e);
            if (Current != null) Current.HearPlayer(e);
        }

        void Later(float seconds, Emoji e) => _replies.Enqueue((Time.time + seconds, e));

        // ---------------------------------------------------------------- living

        void Update()
        {
            while (_replies.Count > 0 && Time.time >= _replies.Peek().at) Emote(_replies.Dequeue().e);
            if (_leaving) return;

            if (_leaveAt > 0f && Time.time >= _leaveAt) { LogOff(); return; }

            if (Time.time >= _nextEmote)
            {
                _nextEmote = Time.time + UnityEngine.Random.Range(18f, 40f);
                var options = new[] { Emoji.Wave, Emoji.Heart, Emoji.Smile, Emoji.Laugh, Emoji.Star };
                Emote(options[UnityEngine.Random.Range(0, options.Length)]);
            }

            if (Time.time >= _nextThink) Think();
            Move();
            WorkOnTask();
        }

        void LateUpdate() => CapyOutfit.SortPieces(_body, _pieces, Wearing);

        void Think()
        {
            _nextThink = Time.time + UnityEngine.Random.Range(1.2f, 2.6f);
            var king = FindFirstObjectByType<PlayerAvatar>();
            Vector2 me = transform.position;
            Vector2 you = king != null ? (Vector2)king.transform.position : me;

            if (As == Role.Guest)
            {
                if (_task == null) PickTask(me);
                if (_task != null) { Head(_task.transform.position); return; }
                if (_giftAt > 0f && Time.time >= _giftAt) { LeaveGift(you); return; }
            }

            float distance = Vector2.Distance(me, you);
            if (distance > 4.5f) Head(you + UnityEngine.Random.insideUnitCircle * 2f);
            else if (UnityEngine.Random.value < 0.45f) Head(me + UnityEngine.Random.insideUnitCircle * 3f);
        }

        // ---------------------------------------------------------------- helping out

        /// <summary>
        /// A guest does for you what you do on a friend's island: feeds hungry workers, bombs pests,
        /// and plants flower gardens by your stations so the workers there have food. It only takes
        /// on chores it can walk to — a pest on an island you have not opened yet is not its job.
        /// </summary>
        void PickTask(Vector2 from)
        {
            _task = Nearest(from, FindObjectsByType<Worker>(FindObjectsSortMode.None), w => w.IsHungry && w.IsAssigned);
            if (_task == null) _task = Nearest(from, FindObjectsByType<DeerAnimal>(FindObjectsSortMode.None), _ => true);
            if (_task == null) _task = Nearest(from, FindObjectsByType<MonsterAnimal>(FindObjectsSortMode.None), m => !m.Tame);
            if (_task == null && _plantsLeft > 0 && Time.time >= _nextPlant)
                _task = Nearest(from, FindObjectsByType<Workplace>(FindObjectsSortMode.None), HasWorkers);
            if (_task == null) return;
            _taskSince = Time.time;
            _lastProgress = Time.time;
            _progressAt = from;
        }

        static bool HasWorkers(Workplace place)
        {
            foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
                if (w.Workplace == place) return true;
            return false;
        }

        T Nearest<T>(Vector2 from, T[] options, Func<T, bool> wanted) where T : Component
        {
            T best = null;
            float bestDistance = float.MaxValue;
            foreach (var o in options)
            {
                if (o == null || _skip.Contains(o) || !wanted(o)) continue;
                Vector2 at = o.transform.position;
                float d = Vector2.Distance(from, at);
                if (d >= bestDistance || !Reachable(from, at)) continue;
                best = o;
                bestDistance = d;
            }
            return best;
        }

        /// <summary>A walk over land the whole way (no swimming to islands you have not bridged).</summary>
        static bool Reachable(Vector2 from, Vector2 to)
        {
            float length = Vector2.Distance(from, to);
            int steps = Mathf.CeilToInt(length / 0.3f);
            for (int i = 1; i <= steps; i++)
                if (!BunniSpace.PointOnLand(Vector2.Lerp(from, to, i / (float)steps))) return false;
            return true;
        }

        void WorkOnTask()
        {
            if (As != Role.Guest) return;
            if (_task == null) return;   // done, or the pest was dealt with by someone else
            Vector2 me = transform.position;

            // Stuck (blocked, or chasing something it cannot catch): leave this chore for good.
            if (Vector2.Distance(me, _progressAt) > 0.4f) { _progressAt = me; _lastProgress = Time.time; }
            if (Time.time - _lastProgress > 3f || Time.time - _taskSince > 20f) { GiveUp(); return; }

            float reach = _task is Workplace ? 2.2f : 1.3f;
            if (Vector2.Distance(me, _task.transform.position) > reach) return;

            switch (_task)
            {
                case Worker w:
                    if (!w.IsHungry) break;
                    w.FeedFromFriend();
                    _fed++;
                    Emote(Emoji.Heart);
                    Hints.Show(Who.name + " fed one of your hungry workers.");
                    break;
                case Workplace place:
                    if (PlantFlowersBy(place)) _planted++;
                    else _skip.Add(place);
                    _plantsLeft--;
                    _nextPlant = Time.time + UnityEngine.Random.Range(30f, 50f);
                    break;
                default:
                    BombPest(_task.gameObject);
                    _pests++;
                    break;
            }
            _task = null;
            _nextThink = Time.time + 1.5f;
        }

        void GiveUp()
        {
            _skip.Add(_task);
            _task = null;
            _moving = false;
            _nextThink = Time.time + 0.5f;
        }

        /// <summary>A fizz bomb, the way you would do it: a pop, a puff of smoke, and the pest is gone.</summary>
        void BombPest(GameObject pest)
        {
            Vector2 at = pest.transform.position;
            var fx = HarvestEffects.Instance;
            if (fx != null) { fx.Poof(at, new Color(0.7f, 0.65f, 0.6f)); fx.Twinkle(at + Vector2.up * 0.5f, 6); }
            Sfx.PlayAt(SfxKind.Boom, at, 0.6f);
            pest.transform.DOKill();
            foreach (var c in pest.GetComponents<MonoBehaviour>()) c.enabled = false;
            pest.transform.DOScale(0f, 0.35f).SetEase(Ease.InBack).OnComplete(() => { if (pest != null) Destroy(pest); });
            Emote(Emoji.Star);
            Hints.Show(Who.name + " bombed a pest on your island!");
        }

        /// <summary>Plants a small flower garden within reach of a station's workers, if there is room.</summary>
        bool PlantFlowersBy(Workplace place)
        {
            var catalog = ItemCatalog.Instance;
            if (catalog == null) return false;
            var gardens = new[] { "Small Posie Garden", "Small Tulip Garden", "Small Rose Garden" };
            string name = gardens[UnityEngine.Random.Range(0, gardens.Length)];
            if (!catalog.Has(name)) return false;
            var item = catalog.Create(name);
            if (item == null) return false;
            var placeable = item.GetComponent<Placeable>();
            if (placeable == null) { Destroy(item); return false; }

            Vector2 centre = place.transform.position;
            for (int i = 0; i < 40; i++)
            {
                var p = centre + UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(1.2f, 3f);
                if (!placeable.CanPlaceAt(p)) continue;
                item.transform.position = new Vector3(p.x, p.y, 0f);
                item.SetActive(true);
                placeable.MarkPlaced(silent: true);
                var fx = HarvestEffects.Instance;
                if (fx != null) fx.Twinkle(p + Vector2.up * 0.4f, 8);
                Sfx.PlayAt(SfxKind.Plop, p, 0.7f);
                Emote(Emoji.Smile);
                Hints.Show(Who.name + " planted flowers by your " + StoryText.Name(place.GetComponent<CatalogItem>() is { } tag ? tag.StoreName : "station") + " for your workers.");
                return true;
            }
            Destroy(item);
            return false;
        }

        void LeaveGift(Vector2 near)
        {
            _giftAt = -1f;
            var catalog = ItemCatalog.Instance;
            if (catalog == null) return;
            var gifts = new[] { "Small Posie Garden", "Small Tulip Garden", "Small Pine Forest", "Apple Tree", "Small Rose Garden" };
            var at = near + UnityEngine.Random.insideUnitCircle.normalized * 2.2f;
            if (!BunniSpace.PointOnLand(at)) at = transform.position;
            catalog.MakeStore(gifts[UnityEngine.Random.Range(0, gifts.Length)], at);
            Emote(Emoji.Heart);
            Hints.Show(Who.name + " left you a gift bubble!");
        }

        void LogOff()
        {
            _leaving = true;
            Emote(Emoji.Wave);
            if (As == Role.Guest)
            {
                var did = new List<string>();
                if (_fed > 0) did.Add($"fed {_fed} worker{(_fed > 1 ? "s" : "")}");
                if (_pests > 0) did.Add($"bombed {_pests} pest{(_pests > 1 ? "s" : "")}");
                if (_planted > 0) did.Add($"planted {_planted} flower garden{(_planted > 1 ? "s" : "")}");
                if (did.Count > 0) Hints.Show(Who.name + " " + string.Join(", ", did) + " before heading home.");
                // Sometimes they leave you a message in a bottle on your beach.
                if (!_sentHome && UnityEngine.Random.value < 0.45f) SeaLibrary.LeaveNoteFrom(Who);
            }
            transform.DOScale(transform.localScale * 1.1f, 1.4f).OnComplete(() =>
            {
                var fx = HarvestEffects.Instance;
                if (fx != null) fx.Poof(transform.position, Color.white);
                if (!_sentHome) GuestLeft?.Invoke(Who);
                Destroy(gameObject);
            });
        }

        // ---------------------------------------------------------------- moving

        void Head(Vector2 to)
        {
            if (!BunniSpace.PointOnLand(to))
            {
                if (_task != null) GiveUp();
                return;
            }
            _target = to;
            _moving = true;
        }

        void Move()
        {
            if (!_moving)
            {
                if (_bodyTransform != null) _bodyTransform.localPosition = Vector3.Lerp(_bodyTransform.localPosition, _bodyRest, Time.deltaTime * 10f);
                return;
            }
            Vector2 here = transform.position;
            var step = Vector2.MoveTowards(here, _target, Speed * Time.deltaTime);
            if (!BunniSpace.PointOnLand(step)) { _moving = false; return; }
            transform.position = new Vector3(step.x, step.y, transform.position.z);
            if ((step - _target).sqrMagnitude < 0.01f) _moving = false;

            float dx = _target.x - here.x;
            if (_bodyTransform != null && Mathf.Abs(dx) > 0.01f)
            {
                var s = _bodyTransform.localScale;
                // the pictures face left: mirror them to walk right
                s.x = Mathf.Abs(s.x) * (dx > 0f ? -1f : 1f);
                _bodyTransform.localScale = s;
                _hop += Time.deltaTime * 9f;
                _bodyTransform.localPosition = _bodyRest + new Vector3(0f, Mathf.Abs(Mathf.Sin(_hop)) * 0.14f, 0f);
            }
        }
    }
}
