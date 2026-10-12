using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// While you hold a capybara, every planted mill and mine shows where to drop it: a ring the
    /// size of its drop zone and a "2/4" count over the roof. Green has room, grey is full, and the
    /// one the capybara would join if you let go now pulses — so posting a worker is aimed, not
    /// guessed, and a full station says so before you try it.
    ///
    /// One instance on the HUD, like <see cref="PlacementPreview"/> for items.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class StationGuide : MonoBehaviour
    {
        static readonly Color Room = new(0.36f, 0.82f, 0.36f, 0.5f);
        static readonly Color Full = new(0.45f, 0.45f, 0.45f, 0.4f);

        class Marker
        {
            public GameObject Root;
            public SpriteRenderer Ring;
            public TextMeshPro Count;
        }

        readonly Dictionary<Workplace, Marker> _markers = new();
        readonly List<Workplace> _gone = new();

        // In the world, not under the HUD canvas this component sits on (its scale would shrink them).
        Transform _root;

        void Awake() => _root = new GameObject("StationGuides").transform;

        void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
        }

        void LateUpdate()
        {
            var worker = HeldWorker();
            if (worker == null) { HideAll(); return; }

            Vector2 at = worker.transform.position;
            var target = Worker.StationAt(at, out _);

            foreach (var w in FindObjectsByType<Workplace>(FindObjectsSortMode.None))
            {
                var placeable = w.GetComponent<Placeable>();
                if (placeable != null && !placeable.Placed) continue;
                var m = Get(w);
                m.Root.SetActive(true);
                m.Root.transform.position = new Vector3(w.transform.position.x, w.transform.position.y, 0f);

                bool chosen = w == target;
                float pulse = chosen ? 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.06f : 1f;
                float size = Worker.DropOnStationRadius * 2f / Mathf.Max(0.01f, m.Ring.sprite.bounds.size.x);
                m.Ring.transform.localScale = Vector3.one * size * pulse;
                var c = w.HasRoom ? Room : Full;
                if (chosen) c.a = 0.9f;
                m.Ring.color = c;

                m.Count.text = w.WorkerCount + "/" + w.MaxWorkers;
                m.Count.color = w.HasRoom ? Color.white : new Color(0.85f, 0.85f, 0.85f);
            }

            // Stations that were sold or destroyed take their markers with them.
            _gone.Clear();
            foreach (var kv in _markers) if (kv.Key == null) _gone.Add(kv.Key);
            foreach (var w in _gone) { if (_markers[w].Root != null) Destroy(_markers[w].Root); _markers.Remove(w); }
        }

        static Worker HeldWorker()
        {
            foreach (var a in FindObjectsByType<DraggableActor>(FindObjectsSortMode.None))
                if (a.IsHeld && a.TryGetComponent<Worker>(out var w)) return w;
            return null;
        }

        void HideAll()
        {
            foreach (var m in _markers.Values)
                if (m.Root != null && m.Root.activeSelf) m.Root.SetActive(false);
        }

        Marker Get(Workplace w)
        {
            if (_markers.TryGetValue(w, out var m) && m.Root != null) return m;
            m = new Marker { Root = new GameObject("StationGuide_" + w.name) };
            m.Root.transform.SetParent(_root, false);

            var ring = new GameObject("Ring");
            ring.transform.SetParent(m.Root.transform, false);
            m.Ring = ring.AddComponent<SpriteRenderer>();
            m.Ring.sprite = UI.IconPainter.Ring;
            m.Ring.sortingOrder = Bubble.SortBase - 100;   // over the ground and buildings, under bubbles

            var label = new GameObject("Count");
            label.transform.SetParent(m.Root.transform, false);
            label.transform.localPosition = new Vector3(0f, 2.9f, 0f);
            m.Count = label.AddComponent<TextMeshPro>();
            if (UI.UiKit.HeadingFont != null) m.Count.font = UI.UiKit.HeadingFont;
            m.Count.fontSize = 6f;
            m.Count.alignment = TextAlignmentOptions.Center;
            m.Count.outlineWidth = 0.25f;
            m.Count.outlineColor = new Color32(60, 40, 25, 255);
            m.Count.rectTransform.sizeDelta = new Vector2(4f, 1.4f);
            m.Count.sortingOrder = Bubble.SortBase - 99;

            _markers[w] = m;
            return m;
        }
    }
}
