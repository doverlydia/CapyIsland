using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A red torii stands over the middle of every open bridge, so crossing to a new island means
    /// walking under a gate. It rises the moment a bridge opens.
    ///
    /// Bridges running up and down the screen get the gate face-on; those running across get it
    /// turned, and that one is two layers — the near pillar and the far one, each sorted at its own
    /// foot — so the capybara walks between them, behind one and in front of the other.
    ///
    /// A holiday skin may dress the gates (its Torii_Front / Torii_Side pictures, drawn on the same
    /// layout at any size): they are put up again in it when the skin goes on or comes off.
    /// </summary>
    public class BridgeGates : MonoBehaviour
    {
        /// <summary>How wide a gate stands, in Unity units: across the whole bridge.</summary>
        const float Width = 5.4f;

        // Measured on the 512px art: where the feet are, where the turned gate's two halves part.
        const float FrontFoot = 457f, SideNearFoot = 464f, SideFarFoot = 404f, SideSplit = 264f, Px = 512f;

        readonly Dictionary<string, GameObject> _gates = new();
        Texture2D _front, _side;

        void Start()
        {
            _front = Resources.Load<Sprite>("World/Torii_Front")?.texture;
            _side = Resources.Load<Sprite>("World/Torii_Side")?.texture;
            if (_front == null || _side == null) { enabled = false; return; }
            LandMap.BridgeOpened += OnOpened;
            Skins.Changed += Rebuild;
            BuildAll();
        }

        void OnDestroy()
        {
            LandMap.BridgeOpened -= OnOpened;
            Skins.Changed -= Rebuild;
        }

        void BuildAll()
        {
            var land = LandMap.Instance;
            if (land == null) return;
            foreach (var p in land.Pieces)
                if (p.isBridge && p.open) Build(p, false);
        }

        void Rebuild()
        {
            if (this == null) return;
            foreach (var g in _gates.Values) if (g != null) Destroy(g);
            _gates.Clear();
            BuildAll();
        }

        void OnOpened(string name)
        {
            var land = LandMap.Instance;
            var bridge = land != null ? land.Bridge(name) : null;
            if (bridge != null) Build(bridge, !SaveGame.Restoring);
        }

        void Build(LandMap.Piece bridge, bool rise)
        {
            if (_gates.ContainsKey(bridge.name) || bridge.circles.Count < 2) return;
            int n = bridge.circles.Count;
            Vector2 W(Vector3 c) => LandMap.WorldToUnity(new Vector2(c.x, c.y));
            Vector2 start = W(bridge.circles[0]), end = W(bridge.circles[n - 1]);
            Vector2 mid = n % 2 == 1 ? W(bridge.circles[n / 2]) : (W(bridge.circles[n / 2 - 1]) + W(bridge.circles[n / 2])) * 0.5f;
            var dir = end - start;
            bool across = Mathf.Abs(dir.x) > Mathf.Abs(dir.y) * 0.9f;

            var gate = new GameObject("Torii " + bridge.name);
            gate.transform.SetParent(transform, false);
            gate.transform.position = mid;
            _gates[bridge.name] = gate;

            if (!across)
            {
                var tex = Skins.Texture("Torii_Front") ?? _front;
                float k = tex.width / Px;   // a skin's picture may be any size on the same layout
                float ppu = Px * 0.87f / Width;   // the gate fills ~87% of its picture's width
                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, (Px - FrontFoot) / Px), ppu * k, 0, SpriteMeshType.FullRect);
                Layer(gate.transform, "Gate", sprite, mid.y, false);
            }
            else
            {
                var tex = Skins.Texture("Torii_Side") ?? _side;
                float k = tex.width / Px;
                // Drawn running down to the right; a bridge rising to the right gets it mirrored.
                bool mirror = dir.x != 0f && dir.y / dir.x > 0.3f;
                float ppu = Px * 0.6f / Width;   // turned, it fills less of its picture
                float footMid = (SideNearFoot + SideFarFoot) * 0.5f;
                float pivotY = (Px - footMid) / Px;
                var near = Sprite.Create(tex, new Rect(0, 0, SideSplit * k, tex.height), new Vector2(Px * 0.5f / SideSplit, pivotY), ppu * k, 0, SpriteMeshType.FullRect);
                var far = Sprite.Create(tex, new Rect(SideSplit * k, 0, tex.width - SideSplit * k, tex.height), new Vector2((Px * 0.5f - SideSplit) / (Px - SideSplit), pivotY), ppu * k, 0, SpriteMeshType.FullRect);
                Layer(gate.transform, "Near", near, mid.y - (SideNearFoot - footMid) / ppu, mirror);
                Layer(gate.transform, "Far", far, mid.y + (footMid - SideFarFoot) / ppu, mirror);
            }

            if (!rise) return;
            // Up out of the bridge, with a little bounce.
            gate.transform.localScale = new Vector3(1f, 0f, 1f);
            gate.transform.DOScaleY(1f, 0.9f).SetEase(Ease.OutBack).SetDelay(0.6f);
        }

        static void Layer(Transform parent, string name, Sprite sprite, float footY, bool mirror)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.flipX = mirror;
            sr.sortingOrder = YSorter.PropSortBase + Mathf.RoundToInt(-footY * 100f);
        }
    }
}
