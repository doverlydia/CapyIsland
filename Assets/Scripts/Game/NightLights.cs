using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CapyIsland.Game
{
    /// <summary>
    /// After dark, windows glow: a warm light spills from every house, shop, mill and the tower,
    /// and a soft lantern glow follows your capybara so you can always see where you are.
    ///
    /// Each light is a 2D point light, which costs a little on a phone, so only the buildings
    /// nearest the view are lit (the rest are too far off to see anyway). Lights fade in with
    /// dusk and out with dawn, and are off entirely by day.
    /// </summary>
    public class NightLights : MonoBehaviour
    {
        const int MaxLit = 8;
        static readonly Color Window = new(1f, 0.76f, 0.42f);
        static readonly HashSet<StoreItemKind> Lit = new()
        {
            StoreItemKind.House, StoreItemKind.Store, StoreItemKind.Workplace, StoreItemKind.Church,
            StoreItemKind.Tower, StoreItemKind.Stove,
        };

        readonly List<Light2D> _pool = new();
        readonly List<(Transform at, float dist)> _candidates = new();
        Light2D _lantern;
        Transform _root;
        float _nextScan;
        readonly List<Transform> _buildings = new();

        void Start()
        {
            _root = new GameObject("NightLights").transform;
            _lantern = MakeLight("Lantern", new Color(1f, 0.85f, 0.6f), 3.6f);
        }

        void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
        }

        /// <summary>0 by day … 1 deep in the night, easing through dusk and dawn.</summary>
        static float Darkness()
        {
            float h = GameClock.Hour;
            if (h >= 19.5f || h < 4.6f) return 1f;
            if (h >= 17f) return Mathf.InverseLerp(17f, 19.5f, h);   // dusk
            if (h < 7f) return Mathf.InverseLerp(7f, 4.6f, h);       // dawn
            return 0f;
        }

        void LateUpdate()
        {
            float dark = Darkness();
            var cam = Camera.main;
            var king = FindFirstObjectByType<PlayerAvatar>();

            if (_lantern != null)
            {
                _lantern.enabled = dark > 0.01f && king != null;
                if (_lantern.enabled)
                {
                    _lantern.transform.position = king.transform.position + Vector3.up * 0.9f;
                    _lantern.intensity = 0.55f * dark;
                }
            }

            if (dark <= 0.01f || cam == null) { HideFrom(0); return; }

            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 2f;
                _buildings.Clear();
                foreach (var tag in FindObjectsByType<CatalogItem>(FindObjectsSortMode.None))
                {
                    var def = tag.Def;
                    if (string.IsNullOrEmpty(def.Name) || !Lit.Contains(def.Kind)) continue;   // unknown items come back empty
                    var p = tag.GetComponent<Placeable>();
                    if (p != null && !p.Placed) continue;
                    _buildings.Add(tag.transform);
                }
            }

            // The nearest few to the middle of the view get a light.
            Vector2 c = cam.transform.position;
            _candidates.Clear();
            foreach (var b in _buildings)
                if (b != null) _candidates.Add((b, ((Vector2)b.position - c).sqrMagnitude));
            _candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

            int used = 0;
            foreach (var (at, _) in _candidates)
            {
                if (used >= MaxLit) break;
                var l = Get(used++);
                var body = at.GetComponentInChildren<SpriteRenderer>();
                Vector3 pos = body != null ? new Vector3(body.bounds.center.x, body.bounds.min.y + body.bounds.size.y * 0.35f, 0f) : at.position;
                l.transform.position = pos;
                // A gentle flicker, as if from a lamp inside.
                float flicker = 1f + Mathf.Sin(Time.time * 3.1f + used * 1.7f) * 0.04f;
                l.intensity = 0.9f * dark * flicker;
            }
            HideFrom(used);
        }

        Light2D Get(int i)
        {
            while (_pool.Count <= i) _pool.Add(MakeLight("Window", Window, 3.2f));
            var l = _pool[i];
            if (!l.enabled) l.enabled = true;
            return l;
        }

        void HideFrom(int i)
        {
            for (; i < _pool.Count; i++) if (_pool[i].enabled) _pool[i].enabled = false;
        }

        Light2D MakeLight(string name, Color colour, float radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = colour;
            l.pointLightInnerRadius = radius * 0.15f;
            l.pointLightOuterRadius = radius;
            l.falloffIntensity = 0.7f;
            l.intensity = 0f;
            l.enabled = false;
            return l;
        }
    }
}
