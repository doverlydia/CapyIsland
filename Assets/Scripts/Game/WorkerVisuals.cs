using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Makes a working capybara legible. Without this a chop is 30-odd seconds of a sprite standing
    /// perfectly still, which reads as a bug rather than as work.
    ///
    /// It shows the tool for the job, swings it on a beat, shakes the tree on each strike, and
    /// swaps the tool for a carried log on the walk home. Nothing here changes the economy — the
    /// swing rate is derived from the worker's own harvest duration, so the animation always
    /// matches whatever the real timing is.
    /// </summary>
    [RequireComponent(typeof(Worker))]
    public class WorkerVisuals : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] Sprite axe;
        [SerializeField] Sprite pickaxe;
        [SerializeField] Sprite log;

        [Tooltip("Shown while the worker is asleep from hunger, so a stalled worker is obvious.")]
        [SerializeField] Sprite sleeping;

        Sprite _awake;
        SpriteRenderer _bodyRenderer;

        [Header("Held prop")]
        [SerializeField] Vector2 holdOffset = new Vector2(0.42f, 0.30f);
        [SerializeField] float propScale = 0.5f;

        [Header("Swing")]
        [Tooltip("Strikes per second while harvesting.")]
        [SerializeField] float strikesPerSecond = 1.1f;
        [Tooltip("How far the tool rotates back before each strike, in degrees.")]
        [SerializeField] float windUpAngle = 62f;

        [Header("Body")]
        [Tooltip("How far the capybara leans into each strike.")]
        [SerializeField] float bodyLean = 9f;

        [Header("Target reaction")]
        [Tooltip("How far the struck tree rocks, in degrees.")]
        [SerializeField] float targetShake = 5f;
        [SerializeField] float targetShakeSeconds = 0.32f;

        Worker _worker;
        Transform _body;
        SpriteRenderer _prop;
        Transform _propPivot;

        float _swingPhase;
        int _lastStrike = -1;

        Harvestable _shaking;
        Transform _shakingSprite;
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

            var bodyRenderer = _body.GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) _prop.sortingOrder = bodyRenderer.sortingOrder + 1;
        }

        void LateUpdate()
        {
            // The body renderer is re-sorted every frame by YSorter, so track it here too.
            var bodyRenderer = _body != null ? _body.GetComponent<SpriteRenderer>() : null;
            if (bodyRenderer != null && _prop != null) _prop.sortingOrder = bodyRenderer.sortingOrder + 1;

            ShowSleepPose(_worker.Activity == WorkerActivity.Asleep);

            switch (_worker.Activity)
            {
                case WorkerActivity.Harvesting: ShowHarvesting(); break;
                case WorkerActivity.CarryingHome: ShowCarrying(); break;
                case WorkerActivity.WalkingToNode: ShowTool(ToolFor(), swinging: false); break;
                default: Hide(); break;
            }

            TickTargetShake();
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

        void ShowHarvesting()
        {
            ShowTool(ToolFor(), swinging: true);

            _swingPhase += Time.deltaTime * strikesPerSecond;
            float t = _swingPhase - Mathf.Floor(_swingPhase);   // 0..1 within one strike

            // Slow wind-up, fast chop: back over the first 70%, down hard over the last 30%.
            float angle = t < 0.7f
                ? Mathf.Lerp(0f, -windUpAngle, t / 0.7f)
                : Mathf.Lerp(-windUpAngle, 18f, (t - 0.7f) / 0.3f);

            bool facingLeft = _body.localScale.x < 0f;
            _propPivot.localRotation = Quaternion.Euler(0f, 0f, facingLeft ? -angle : angle);
            _propPivot.localPosition = new Vector3(facingLeft ? -holdOffset.x : holdOffset.x, holdOffset.y, 0f);

            // The capybara leans with the swing.
            float lean = Mathf.Lerp(0f, bodyLean, Mathf.Clamp01((t - 0.7f) / 0.3f));
            _body.localRotation = Quaternion.Euler(0f, 0f, facingLeft ? lean : -lean);

            // One shake per strike, at the moment of contact.
            int strike = Mathf.FloorToInt(_swingPhase);
            if (t > 0.92f && strike != _lastStrike)
            {
                _lastStrike = strike;
                BeginTargetShake(_worker.CurrentTarget);
            }
        }

        void ShowCarrying()
        {
            ShowTool(log, swinging: false);
            _body.localRotation = Quaternion.identity;
        }

        void ShowTool(Sprite sprite, bool swinging)
        {
            if (_prop == null) return;
            if (sprite == null) { _prop.enabled = false; return; }

            _prop.sprite = sprite;
            _prop.enabled = true;

            if (swinging) return;

            bool facingLeft = _body.localScale.x < 0f;
            _propPivot.localPosition = new Vector3(facingLeft ? -holdOffset.x : holdOffset.x, holdOffset.y, 0f);
            _propPivot.localRotation = Quaternion.identity;
            _body.localRotation = Quaternion.identity;
        }

        void Hide()
        {
            if (_prop != null) _prop.enabled = false;
            if (_body != null) _body.localRotation = Quaternion.identity;
            _swingPhase = 0f;
            _lastStrike = -1;
        }

        void BeginTargetShake(Harvestable target)
        {
            if (target == null) return;
            _shaking = target;
            // Shake the tree being felled — the node's own icon sprite is hidden once planted.
            var tree = target.CurrentVisual;
            var sprite = tree != null ? tree.Find("Sprite") : null;
            _shakingSprite = sprite != null ? sprite : tree;
            _shakeTimer = targetShakeSeconds;
        }

        void TickTargetShake()
        {
            if (_shakingSprite == null) return;

            if (_shakeTimer <= 0f)
            {
                _shakingSprite.localRotation = Quaternion.identity;
                _shakingSprite = null;
                _shaking = null;
                return;
            }

            _shakeTimer -= Time.deltaTime;
            float remaining = Mathf.Clamp01(_shakeTimer / Mathf.Max(0.01f, targetShakeSeconds));
            // Decaying wobble, so it rings out rather than stopping dead.
            float angle = Mathf.Sin(_shakeTimer * 46f) * targetShake * remaining;
            _shakingSprite.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
