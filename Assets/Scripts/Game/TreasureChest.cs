using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A treasure chest shaken out of a fruit tree. It opens by itself — nobody has to tap it — shows
    /// what was inside, sends it to the resource bar, and fades away. (MapItems/Treasure.)
    ///
    /// Contents by tier, from a value table of 1, 5, 10, 20, 50, 100, 500:
    ///   tier 1 (apple): 50% wood, 40% gold, 10% gems; value from the first 3
    ///   tier 2 (pear):  30% wood, 50% gold, 20% gems; wood/gold from the first 2, gems the first 4
    ///   tier 3 (plum):  33% wood, 33% gold, 34% gems; value from the first 5
    ///   tier 4:         50% wood, 50% gold; value from the first 3
    ///
    /// Timing, in 30 Hz frames: 15 to settle, 6 of the lid opening, 20 before the payout flies off,
    /// 20 more, then a fade at 0.05 alpha per frame.
    /// </summary>
    public class TreasureChest : MonoBehaviour
    {
        static readonly int[] ValueTable = { 1, 5, 10, 20, 50, 100, 500 };

        enum Step { Settling, Opening, Showing, Paid, Fading }

        ResourceKind _type;
        int _value;
        Step _step;
        int _counter;
        FrameClock _clock;
        SpriteRenderer _body;
        SpriteRenderer _shadow;

        public ResourceKind Type => _type;
        public int Value => _value;

        /// <summary>Opens and pays without a sound: the tower's steady chests once the island is saved.</summary>
        public bool Silent { get; set; }

        /// <summary>Creates a chest at <paramref name="at"/> with random contents of the given tier.</summary>
        public static TreasureChest Spawn(Vector2 at, int tier, Transform parent)
        {
            var art = GameArt.Instance;
            var go = new GameObject("TreasureChest");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);

            if (art != null && art.shadowTemplate != null)
            {
                var shadow = Instantiate(art.shadowTemplate, go.transform);
                shadow.name = "Shadow";
                shadow.transform.localPosition = Vector3.zero;
                shadow.transform.localScale = art.shadowTemplate.transform.localScale * 0.55f;
                shadow.SetActive(true);
            }

            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = Vector3.one * 0.55f;
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = art != null ? art.chestClosed : null;

            go.AddComponent<YSorter>();
            var chest = go.AddComponent<TreasureChest>();
            chest.SetRandom(tier);
            return chest;
        }

        /// <summary>A chest with set contents (TreasureChest.SetValue): quest rewards, bank trades, Pete's payouts.</summary>
        public static TreasureChest SpawnValue(Vector2 at, ResourceKind type, int value, Transform parent)
        {
            var chest = Spawn(at, 1, parent);
            chest._type = type;
            chest._value = value;
            return chest;
        }

        void Awake()
        {
            var body = transform.Find("Sprite");
            _body = body != null ? body.GetComponent<SpriteRenderer>() : null;
            var shadow = transform.Find("Shadow");
            _shadow = shadow != null ? shadow.GetComponent<SpriteRenderer>() : null;
        }

        void SetRandom(int tier)
        {
            float r = Random.value;
            switch (tier)
            {
                case 1:
                    _type = r < 0.5f ? ResourceKind.Wood : r < 0.9f ? ResourceKind.Gold : ResourceKind.Gem;
                    _value = ValueTable[Random.Range(0, 3)];
                    break;
                case 2:
                    _type = r < 0.3f ? ResourceKind.Wood : r < 0.8f ? ResourceKind.Gold : ResourceKind.Gem;
                    _value = ValueTable[Random.Range(0, _type == ResourceKind.Gem ? 4 : 2)];
                    break;
                case 3:
                    _type = r < 0.33f ? ResourceKind.Wood : r < 0.66f ? ResourceKind.Gold : ResourceKind.Gem;
                    _value = ValueTable[Random.Range(0, 5)];
                    break;
                default:
                    _type = r < 0.5f ? ResourceKind.Wood : ResourceKind.Gold;
                    _value = ValueTable[Random.Range(0, 3)];
                    break;
            }
            _step = Step.Settling;
            _counter = 0;
        }

        void Update()
        {
            int frames = _clock.Consume(Time.deltaTime);
            for (int i = 0; i < frames; i++)
                if (Tick()) return;
        }

        /// <summary>One frame. True once the chest has gone.</summary>
        bool Tick()
        {
            _counter++;
            switch (_step)
            {
                case Step.Settling:
                    if (_counter < 15) return false;
                    _step = Step.Opening; _counter = 0;
                    return false;

                case Step.Opening:
                    if (_counter < 6) return false;
                    Open();
                    _step = Step.Showing; _counter = 0;
                    return false;

                case Step.Showing:
                    if (_counter <= 20) return false;
                    ResourcePing.Fly(_type, _value, (Vector2)transform.position + Vector2.up * 0.7f, Silent);
                    _step = Step.Paid; _counter = 0;
                    return false;

                case Step.Paid:
                    if (_counter <= 20) return false;
                    _step = Step.Fading; _counter = 0;
                    return false;

                case Step.Fading:
                    float alpha = 1f - 0.05f * _counter;
                    SetAlpha(alpha);
                    if (alpha > 0f) return false;
                    Destroy(gameObject);
                    return true;
            }
            return false;
        }

        void Open()
        {
            if (!Silent) Sfx.PlayAt(SfxKind.Chest, transform.position);
            var art = GameArt.Instance;
            if (_body != null && art != null && art.chestOpen != null) _body.sprite = art.chestOpen;
            if (_body != null)
            {
                _body.transform.DOKill();
                _body.transform.DOPunchScale(new Vector3(0.18f, 0.28f, 0f), 0.35f, 6, 0.6f);
            }
            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Twinkle((Vector2)transform.position + Vector2.up * 0.45f, 10);
        }

        void SetAlpha(float a)
        {
            a = Mathf.Clamp01(a);
            if (_body != null) { var c = _body.color; c.a = a; _body.color = c; }
            if (_shadow != null) { var c = _shadow.color; c.a = a * 0.6f; _shadow.color = c; }
        }

        void OnDestroy()
        {
            if (_body != null) _body.transform.DOKill();
        }
    }
}
