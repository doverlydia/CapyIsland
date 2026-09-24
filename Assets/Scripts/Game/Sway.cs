using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Bends a tree or flower in the <see cref="Wind"/>, pivoting at its base, and lets anything
    /// that strikes or shakes it add a ringing wobble on top with <see cref="Kick"/>. When a gust
    /// passes, leafy things shed a leaf or two that blow away downwind.
    ///
    /// Owns the rotation of the node's "Sprite" child: everything that wants to rock the node goes
    /// through here, so wind and hits never fight over the same transform.
    /// </summary>
    [DefaultExecutionOrder(90)]
    public class Sway : MonoBehaviour
    {
        [Tooltip("How much of the wind this bends to. Trees 1, flowers a little more.")]
        [SerializeField] float flexibility = 1f;

        [Tooltip("Leaves blown off per second at the height of a gust. 0 for things without leaves.")]
        [SerializeField] float leavesPerGustSecond = 1.2f;

        [SerializeField] Color leafColour = new Color(0.42f, 0.66f, 0.30f);

        Transform _body;
        float _phase;
        float _kickAmplitude;
        float _kickTimer;
        float _kickDuration = 0.35f;
        float _kickFrequency = 46f;
        float _leafDebt;

        public Transform Body => _body;

        public void Configure(float flex, float leavesPerSecond, Color leaf)
        {
            flexibility = flex;
            leavesPerGustSecond = leavesPerSecond;
            leafColour = leaf;
        }

        public void CopyFrom(Sway other) => Configure(other.flexibility, other.leavesPerGustSecond, other.leafColour);

        void Awake()
        {
            _body = transform.Find("Sprite");
            _phase = Random.value * Mathf.PI * 2f + transform.position.x * 0.35f;
        }

        /// <summary>A strike or shake: a decaying wobble of <paramref name="degrees"/>.</summary>
        public void Kick(float degrees, float seconds = 0.35f, float frequency = 46f)
        {
            _kickAmplitude = degrees;
            _kickDuration = Mathf.Max(0.05f, seconds);
            _kickTimer = _kickDuration;
            _kickFrequency = frequency;
        }

        void LateUpdate()
        {
            if (_body == null) return;

            var wind = Wind.Instance;
            Vector2 at = transform.position;
            float lean = wind != null ? wind.LeanAt(at, _phase) * flexibility : 0f;

            float kick = 0f;
            if (_kickTimer > 0f)
            {
                _kickTimer -= Time.deltaTime;
                float remaining = Mathf.Clamp01(_kickTimer / _kickDuration);
                kick = Mathf.Sin(_kickTimer * _kickFrequency) * _kickAmplitude * remaining;
            }

            _body.localRotation = Quaternion.Euler(0f, 0f, lean + kick);

            if (wind != null && leavesPerGustSecond > 0f) ShedLeaves(wind, at);
        }

        void ShedLeaves(Wind wind, Vector2 at)
        {
            float gust = wind.GustAt(at);
            if (gust <= 0.05f) return;

            _leafDebt += gust * leavesPerGustSecond * Time.deltaTime;
            if (_leafDebt < 1f) return;
            _leafDebt -= 1f;

            var fx = HarvestEffects.Instance;
            if (fx == null) return;
            var canopy = CanopyPoint();
            fx.BlowLeaf(canopy, wind.Direction, leafColour);
        }

        /// <summary>A random point in the upper part of the art, where the leaves are.</summary>
        public Vector2 CanopyPoint()
        {
            var sr = _body != null ? _body.GetComponent<SpriteRenderer>() : null;
            if (sr == null) return transform.position;
            var b = sr.bounds;
            return new Vector2(
                b.center.x + Random.Range(-0.4f, 0.4f) * b.size.x,
                b.min.y + b.size.y * Random.Range(0.5f, 0.9f));
        }
    }
}
