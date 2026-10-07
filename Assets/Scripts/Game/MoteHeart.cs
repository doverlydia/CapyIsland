using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A bit of love (MapItems/Mote). When a mote fires, a glowing heart appears beside the king and
    /// joins the trail, beating. Tap it to read what it was for; then it flies to the Tower of Love
    /// (15 units a frame) and adds to your score. With no Tower yet, it just dissolves.
    /// </summary>
    public class MoteHeart : MonoBehaviour, ITappable
    {
        static Sprite _sprite;

        string _mote;
        string _description;
        string _title;
        SpriteRenderer _renderer;
        Follower _follower;
        bool _flying;

        /// <summary>Called once at start: every mote that fires from now on gets a heart.</summary>
        public static void Listen(Sprite heart)
        {
            _sprite = heart;
            Motes.Triggered -= OnMote;
            Motes.Triggered += OnMote;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => _sprite = null;

        static void OnMote(MoteDef def)
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null) return;
            var at = (Vector2)king.transform.position + BunniSpace.ToUnity(new Vector2(0f, -75f));

            var go = new GameObject("Mote_" + def.Func);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            if (_sprite != null) body.transform.localScale = Vector3.one * (0.75f / Mathf.Max(0.01f, _sprite.bounds.size.x));
            body.transform.localPosition = new Vector3(0f, 0.6f, 0f);

            var heart = go.AddComponent<MoteHeart>();
            heart._mote = def.Name;
            heart._description = def.Description;
            heart._title = def.Title;
            heart._renderer = sr;
            heart._follower = go.AddComponent<Follower>();
            heart._follower.PickUpIntoInventory();
            go.AddComponent<YSorter>();

            // A 32-frame appear: swell up from nothing.
            go.transform.localScale = Vector3.zero;
            go.transform.DOScale(1f, 32f / 30f).SetEase(Ease.OutBack);
            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Twinkle(at + Vector2.up * 0.6f, 10);
            Sfx.Play(SfxKind.Heart, 0.8f, 0f);
        }

        void OnEnable() => TapTargets.Register(this);
        void OnDisable() => TapTargets.Unregister(this);

        void Update()
        {
            if (_renderer == null || _flying) return;
            // A heartbeat.
            float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.time * 2.6f)), 8f);
            _renderer.transform.localScale = Vector3.one * (0.75f / Mathf.Max(0.01f, _renderer.sprite != null ? _renderer.sprite.bounds.size.x : 1f)) * (1f + beat * 0.15f);
        }

        public float TapDistance(Vector2 worldPoint)
        {
            if (_flying || _renderer == null) return -1f;
            float d = Vector2.Distance(_renderer.bounds.center, worldPoint);
            return d < 0.8f ? d * 0.5f : -1f;   // small, so it wins over what is behind it
        }

        public void Tap()
        {
            if (_flying) return;
            _flying = true;
            UI.CardPopup.Show(_title ?? _mote, _description, _renderer != null ? _renderer.sprite : null, FlyHome);
        }

        void FlyHome()
        {
            if (_follower != null) _follower.enabled = false;
            var tower = TowerOfLove.Instance;
            if (tower == null)
            {
                transform.DOScale(0f, 0.6f).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject));
                return;
            }

            // 15 world units a frame.
            Vector2 from = transform.position, to = tower.TopOfTower;
            float seconds = Mathf.Clamp(BunniSpace.WorldDistance(from, to) / (15f * 30f), 0.6f, 6f);
            CameraRig.CameraFocus.Show(to, seconds + 0.8f);
            transform.DOMove(new Vector3(to.x, to.y, transform.position.z), seconds).SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    tower.MoteFound(_mote);
                    Destroy(gameObject);
                });
        }
    }
}
