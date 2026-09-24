using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A little hop while walking, for any animal — workers, deer, monsters. It watches how far the
    /// animal moved this frame, so it needs no hook into whatever is moving it. Presentation only.
    /// </summary>
    [DefaultExecutionOrder(95)]
    public class WalkBob : MonoBehaviour
    {
        [SerializeField] float height = 0.09f;
        [SerializeField] float hopsPerUnit = 1.6f;

        Transform _body;
        Vector3 _rest;
        Vector3 _last;
        float _phase;

        void Awake()
        {
            _body = transform.Find("Sprite");
            if (_body != null) _rest = _body.localPosition;
            _last = transform.position;
        }

        void LateUpdate()
        {
            if (_body == null) return;
            float moved = Vector2.Distance(transform.position, _last);
            _last = transform.position;

            if (moved > 0.0005f && Time.deltaTime > 0f)
            {
                _phase += moved * hopsPerUnit * Mathf.PI;
                _body.localPosition = _rest + new Vector3(0f, Mathf.Abs(Mathf.Sin(_phase)) * height, 0f);
            }
            else
            {
                _phase = 0f;
                _body.localPosition = Vector3.Lerp(_body.localPosition, _rest, Mathf.Min(1f, Time.deltaTime * 14f));
            }
        }
    }
}
