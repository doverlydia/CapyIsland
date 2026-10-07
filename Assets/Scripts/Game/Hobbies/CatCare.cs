using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Cat care. Stray cats wander onto your island. Pet them, share a fish from your bag, and as
    /// their trust grows they will let you adopt them — the cat comes to you in a bubble, and
    /// wherever you put it down becomes its home, where it naps and potters about for good. Each cat can only be fussed over every so often, so it is a
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
                // Older saves have no home: those cats come back in a bubble to be given one.
                if (!c.unplaced && c.home != Vector2.zero) cat.Settle(c.home);
                else HobbyItems.AfterLoad(this, () => HobbyItems.Offer(cat.gameObject));
            }
            _nextStray = Time.time + UnityEngine.Random.Range(20f, 45f);
        }

        void Update()
        {
            if (Visit.Active || Time.time < _nextStray) return;
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
        public class CatView : MonoBehaviour, ITappable
        {
            public CatKind Kind { get; private set; }
            SpriteRenderer _sr;
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
            }

            void OnEnable() => TapTargets.Register(this);
            void OnDisable() => TapTargets.Unregister(this);

            public float TapDistance(Vector2 worldPoint)
            {
                if (_sr == null || !Loose) return -1f;
                var b = _sr.bounds;
                return b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z)) ? Vector2.Distance(b.center, worldPoint) : -1f;
            }

            void Update()
            {
                if (!Loose) return;   // riding in your trail, or in its bubble
                if (Time.time >= _nextMove)
                {
                    // Mostly sitting about; now and then a little stroll — a pet never far from home.
                    _nextMove = Time.time + UnityEngine.Random.Range(4f, 9f);
                    if (UnityEngine.Random.value < 0.55f)
                    {
                        var from = _placeable != null ? HobbyLog.CatState(Kind.Id).home : (Vector2)transform.position;
                        var t = from + UnityEngine.Random.insideUnitCircle * (_placeable != null ? 1.6f : 2.5f);
                        if (BunniSpace.PointOnLand(t)) { _target = t; _moving = true; }
                    }
                }
                if (!_moving) return;
                Vector2 here = transform.position;
                var next = Vector2.MoveTowards(here, _target, 1.2f * Time.deltaTime);
                if (!BunniSpace.PointOnLand(next)) { _moving = false; return; }
                transform.position = next;
                _sr.flipX = _target.x > here.x;
                _hop += Time.deltaTime * 10f;
                _sr.transform.localPosition = new Vector3(0f, _sr.sprite.bounds.extents.y + Mathf.Abs(Mathf.Sin(_hop)) * 0.05f / transform.localScale.y, 0f);
                if ((next - _target).sqrMagnitude < 0.01f) _moving = false;
            }

            public void Tap()
            {
                var state = HobbyLog.CatState(Kind.Id);
                bool tired = DateTime.Now - new DateTime(state.lastFussTicks) < FussCooldown;
                string hearts = $"Trust {state.bond}/{MaxBond}";
                var pages = new[]
                {
                    state.adopted
                        ? $"<b>{Kind.Name}</b> purrs and bumps your hand.\n<size=80%>{Kind.Personality}</size>"
                        : $"A stray cat. It watches you carefully.\n<size=80%>{Kind.Personality}</size>",
                };
                var choices = new List<UI.SpeechDialog.Choice>
                {
                    new("Pet", () => Fuss(state, 1, 2, Emoji.Heart), !tired),
                    new($"Give a fish ({HobbyLog.FishInBag()})", () =>
                    {
                        if (HobbyLog.Take("fish:any")) Fuss(state, 3, 5, Emoji.Star);
                    }, !tired && HobbyLog.FishInBag() > 0),
                };
                if (!state.adopted && state.bond >= AdoptAt)
                    choices.Add(new UI.SpeechDialog.Choice("Adopt!", () => Adopt(state)));
                choices.Add(new UI.SpeechDialog.Choice("Leave", null));
                string title = state.adopted ? Kind.Name : "Stray cat";
                UI.SpeechDialog.ShowChoices(_sr.sprite, title + "  ·  " + hearts,
                    tired ? new[] { pages[0], "It's had plenty of fuss for now. Come back in a little while." } : pages,
                    null, choices);
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
                state.unplaced = true;
                HobbyLog.Save();
                HobbyLog.AddXp(Hobby.Cats, 30);
                Motes.Trigger("hobby_cat");

                // The stray curls up in a bubble: you choose where its home will be.
                var pet = MakeAdopted(Kind);
                if (pet != null) BubbleSpawner.SpawnAt(pet.gameObject, transform.position, null);
                var fx = HarvestEffects.Instance;
                if (fx != null) fx.Twinkle(transform.position + Vector3.up * 0.4f, 8);
                UI.CardPopup.Show(Kind.Name + " is yours!",
                    Kind.Personality + "\nPop the bubble and put " + Kind.Name + " wherever you'd like their home to be.", _sr.sprite);
                _sr.DOKill();
                Destroy(gameObject);
            }
        }
    }
}
