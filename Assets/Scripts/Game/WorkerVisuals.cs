using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Makes a working capybara legible: the right tool in hand, swung in step with the real harvest
    /// frames, the node shaking on the actual hit, a log carried home, and a sleeping pose.
    ///
    /// Purely presentation. The swing follows <see cref="Worker.SwingPhase"/> and the shake fires on
    /// <see cref="Worker.Struck"/>, so the animation can never drift from the mechanics.
    /// </summary>
    [RequireComponent(typeof(Worker))]
    public class WorkerVisuals : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] Sprite axe;
        [SerializeField] Sprite pickaxe;
        [SerializeField] Sprite log;

        [Tooltip("Shown while the worker is asleep, so a stalled worker is obvious.")]
        [SerializeField] Sprite sleeping;

        [Header("Held prop")]
        [SerializeField] Vector2 holdOffset = new Vector2(0.42f, 0.30f);
        [SerializeField] float propScale = 0.5f;

        [Header("Swing")]
        [Tooltip("How far the tool rotates back before each strike, in degrees.")]
        [SerializeField] float windUpAngle = 62f;
        [Tooltip("How far the capybara leans into each strike.")]
        [SerializeField] float bodyLean = 9f;

        [Header("Target reaction")]
        [Tooltip("How far a struck node rocks, in degrees.")]
        [SerializeField] float targetShake = 6f;
        [SerializeField] float targetShakeSeconds = 0.3f;

        Worker _worker;
        Transform _body;
        SpriteRenderer _bodyRenderer;
        Sprite _awake;
        SpriteRenderer _prop;
        Transform _propPivot;

        Transform _shaking;
        float _shakeTimer;

        void Awake()
        {
            _worker = GetComponent<Worker>();

            var sprite = transform.Find("Sprite");
            _body = sprite != null ? sprite : transform;
            _bodyRenderer = _body.GetComponent<SpriteRenderer>();
            if (_bodyRenderer != null) _awake = _bodyRenderer.sprite;

            // Pivot at the shoulder so the tool swings in an arc rather than sliding.
            var pivotGo = new GameObject("PropPivot");
            _propPivot = pivotGo.transform;
            _propPivot.SetParent(transform, false);
            _propPivot.localPosition = new Vector3(holdOffset.x, holdOffset.y, 0f);

            var propGo = new GameObject("Prop");
            propGo.transform.SetParent(_propPivot, false);
            propGo.transform.localPosition = new Vector3(0.18f, 0f, 0f);
            propGo.transform.localScale = Vector3.one * propScale;
            _prop = propGo.AddComponent<SpriteRenderer>();
            _prop.enabled = false;
        }

        void OnEnable() => _worker.Struck += OnStruck;
        void OnDisable() => _worker.Struck -= OnStruck;

        void LateUpdate()
        {
            // YSorter re-sorts the body every frame; keep the prop just in front of it.
            if (_bodyRenderer != null && _prop != null) _prop.sortingOrder = _bodyRenderer.sortingOrder + 1;

            var activity = _worker.Activity;
            ShowSleepPose(activity == WorkerActivity.Asleep);

            switch (activity)
            {
                case WorkerActivity.Harvesting: ShowSwing(ToolFor()); break;
                case WorkerActivity.Eating: ShowSwing(null); break;   // no tool: just a nibbling bob
                case WorkerActivity.CarryingHome: ShowHeld(CarriedFor()); break;
                case WorkerActivity.WalkingToNode: ShowHeld(ToolFor()); break;
                default: Hide(); break;
            }

            TickShake();
        }

        void ShowSleepPose(bool asleep)
        {
            if (_bodyRenderer == null || sleeping == null) return;
            var want = asleep ? sleeping : _awake;
            if (_bodyRenderer.sprite != want) _bodyRenderer.sprite = want;
        }

        Sprite ToolFor()
        {
            var kind = _worker.Workplace != null ? _worker.Workplace.Accepts : ResourceKind.Wood;
            return kind == ResourceKind.Wood ? axe : pickaxe;
        }

        Sprite CarriedFor()
        {
            var kind = _worker.Workplace != null ? _worker.Workplace.Accepts : ResourceKind.Wood;
            return kind == ResourceKind.Wood ? log : null;
        }

        bool FacingLeft => _body.localScale.x < 0f;

        void ShowSwing(Sprite tool)
        {
            SetProp(tool);

            // The phase comes from the real harvest frames: wind up slowly, come down hard on the hit.
            float t = _worker.SwingPhase;
            float angle = t < 0.7f
                ? Mathf.Lerp(0f, -windUpAngle, t / 0.7f)
                : Mathf.Lerp(-windUpAngle, 18f, (t - 0.7f) / 0.3f);

            _propPivot.localRotation = Quaternion.Euler(0f, 0f, FacingLeft ? -angle : angle);
            _propPivot.localPosition = new Vector3(FacingLeft ? -holdOffset.x : holdOffset.x, holdOffset.y, 0f);

            float lean = Mathf.Lerp(0f, bodyLean, Mathf.Clamp01((t - 0.7f) / 0.3f));
            _body.localRotation = Quaternion.Euler(0f, 0f, FacingLeft ? lean : -lean);
        }

        void ShowHeld(Sprite sprite)
        {
            SetProp(sprite);
            _propPivot.localPosition = new Vector3(FacingLeft ? -holdOffset.x : holdOffset.x, holdOffset.y, 0f);
            _propPivot.localRotation = Quaternion.identity;
            _body.localRotation = Quaternion.identity;
        }

        void SetProp(Sprite sprite)
        {
            if (_prop == null) return;
            _prop.enabled = sprite != null;
            if (sprite != null) _prop.sprite = sprite;
        }

        void Hide()
        {
            if (_prop != null) _prop.enabled = false;
            if (_body != null) _body.localRotation = Quaternion.identity;
        }

        void OnStruck(Harvestable node)
        {
            if (node == null) return;
            var sprite = node.transform.Find("Sprite");
            if (_shaking != null && _shaking != sprite) _shaking.localRotation = Quaternion.identity;
            _shaking = sprite != null ? sprite : node.transform;
            _shakeTimer = targetShakeSeconds;

            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Hit(node);
        }

        void TickShake()
        {
            if (_shaking == null) return;

            if (_shakeTimer <= 0f)
            {
                _shaking.localRotation = Quaternion.identity;
                _shaking = null;
                return;
            }

            _shakeTimer -= Time.deltaTime;
            float remaining = Mathf.Clamp01(_shakeTimer / Mathf.Max(0.01f, targetShakeSeconds));
            // A decaying wobble, so it rings out rather than stopping dead.
            _shaking.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_shakeTimer * 46f) * targetShake * remaining);
        }
    }
}
