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
    public class VisitorBot : MonoBehaviour
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
        Component _task;   // a hungry worker or a pest the guest is heading for
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
                if (_task == null) _task = FindTask(me);
                if (_task != null) { Head(_task.transform.position); return; }
                if (_giftAt > 0f && Time.time >= _giftAt) { LeaveGift(you); return; }
            }

            float distance = Vector2.Distance(me, you);
            if (distance > 4.5f) Head(you + UnityEngine.Random.insideUnitCircle * 2f);
            else if (UnityEngine.Random.value < 0.45f) Head(me + UnityEngine.Random.insideUnitCircle * 3f);
        }

        /// <summary>A guest's chores: the nearest hungry worker, else the nearest pest.</summary>
        static Component FindTask(Vector2 from)
        {
            Component best = null;
            float bestDistance = float.MaxValue;
            foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
            {
                if (!w.IsHungry || !w.IsAssigned) continue;
                float d = Vector2.Distance(from, w.transform.position);
                if (d < bestDistance) { best = w; bestDistance = d; }
            }
            if (best != null) return best;
            foreach (var deer in FindObjectsByType<DeerAnimal>(FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(from, deer.transform.position);
                if (d < bestDistance) { best = deer; bestDistance = d; }
            }
            foreach (var m in FindObjectsByType<MonsterAnimal>(FindObjectsSortMode.None))
            {
                if (m.Tame) continue;
                float d = Vector2.Distance(from, m.transform.position);
                if (d < bestDistance) { best = m; bestDistance = d; }
            }
            return best;
        }

        void WorkOnTask()
        {
            if (_task == null || As != Role.Guest) return;
            if (Vector2.Distance(transform.position, _task.transform.position) > 1.3f) return;

            if (_task is Worker w)
            {
                w.FeedFromFriend();
                Emote(Emoji.Heart);
                Hints.Show(Who.name + " fed one of your hungry workers.");
            }
            else
            {
                var fx = HarvestEffects.Instance;
                if (fx != null) fx.Poof(_task.transform.position, new Color(0.7f, 0.65f, 0.6f));
                Sfx.PlayAt(SfxKind.Boom, _task.transform.position, 0.5f);
                var pest = _task.gameObject;
                pest.transform.DOKill();
                pest.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() => { if (pest != null) Destroy(pest); });
                Emote(Emoji.Star);
                Hints.Show(Who.name + " chased a pest off your island!");
            }
            _task = null;
            _nextThink = Time.time + 1.5f;
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
            transform.DOScale(transform.localScale * 1.1f, 1.4f).OnComplete(() =>
            {
                var fx = HarvestEffects.Instance;
                if (fx != null) fx.Poof(transform.position, Color.white);
                GuestLeft?.Invoke(Who);
                Destroy(gameObject);
            });
        }

        // ---------------------------------------------------------------- moving

        void Head(Vector2 to)
        {
            if (!BunniSpace.PointOnLand(to)) return;
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
                s.x = Mathf.Abs(s.x) * (dx < 0f ? -1f : 1f);
                _bodyTransform.localScale = s;
                _hop += Time.deltaTime * 9f;
                _bodyTransform.localPosition = _bodyRest + new Vector3(0f, Mathf.Abs(Mathf.Sin(_hop)) * 0.14f, 0f);
            }
        }
    }
}
