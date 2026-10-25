using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Something drifting down over the island while a holiday skin is on — snowflakes, autumn
    /// leaves, little hearts — from a picture the skin brings (<see cref="Skins.Fall"/>).
    ///
    /// One particle system spread over the whole archipelago (every island and bridge, and a
    /// margin of sea): pieces are let go along its top edge and fall the full height of it, so
    /// the whole area is evenly covered and stays put as the camera pans — you move past them as
    /// you do past the trees. Started already full (prewarmed), so there is no first wave. The
    /// skin's numbers are per screen (how many on screen at once, how big, how fast, at the zoom
    /// the island opens at); the system works out how many the whole area needs from that.
    /// Rebuilt for each island loaded (yours, a friend's). Lit like the island: dimmer at night.
    /// </summary>
    public class HolidayFall : MonoBehaviour
    {
        const int SortingOrder = 32000;   // over every prop
        const int MaxParticles = 6000;

        ParticleSystem _system;
        Material _material;
        Skins.Falling _for;
        LandMap _land;

        void LateUpdate()
        {
            var fall = Skins.Fall;
            var land = LandMap.Instance;
            if (fall == _for && land == _land) return;
            var cam = Camera.main;
            if (fall != null && (land == null || cam == null || !cam.orthographic)) return;   // wait for the island
            _for = fall;
            _land = land;
            Build(fall, land, cam);
        }

        void OnDestroy() => Clear();

        void Clear()
        {
            if (_system != null) Destroy(_system.gameObject);
            if (_material != null) Destroy(_material);
            _system = null;
            _material = null;
        }

        void Build(Skins.Falling fall, LandMap land, Camera cam)
        {
            Clear();
            if (fall == null || fall.Sprite == null || land == null) return;

            // The archipelago: every painted piece of land, plus half a screen of sea around it.
            var area = new Bounds();
            bool any = false;
            foreach (var p in land.Pieces)
                foreach (var layer in p.layers)
                {
                    if (layer == null) continue;
                    if (!any) { area = layer.bounds; any = true; }
                    else area.Encapsulate(layer.bounds);
                }
            if (!any) return;
            float viewH = cam.orthographicSize * 2f, viewW = viewH * cam.aspect;
            area.Expand(new Vector3(viewW, viewH, 0f));

            // The skin's per-screen numbers, in the world.
            float speed = fall.Speed * viewH;             // world units a second
            float size = fall.Size * viewH;
            // One speed for all: each piece crosses the area exactly and ends at its bottom edge, so
            // none is wasted below the islands (the sway and the sizes keep them from looking alike).
            float lifetime = area.size.y / speed;
            float perArea = fall.Count / (viewW * viewH);
            int count = Mathf.Min(MaxParticles, Mathf.CeilToInt(perArea * area.size.x * area.size.y));

            var go = new GameObject("HolidayFall");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(area.center.x, area.max.y, 0f);
            _system = go.AddComponent<ParticleSystem>();
            _system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _system.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = lifetime;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startRotation = fall.Spin ? new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f) : 0f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.55f), new Color(1f, 1f, 1f, 0.95f));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = count + count / 4;

            var emission = _system.emission;
            emission.rateOverTime = count / lifetime;

            // A long thin box along the top edge, pointing down: its +Z turned to face -Y.
            var shape = _system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.rotation = new Vector3(90f, 0f, 0f);
            shape.scale = new Vector3(area.size.x, 0.01f, 0.5f);

            // The sideways sway: slow, wide, gentle.
            var noise = _system.noise;
            noise.enabled = fall.Sway > 0f;
            noise.strength = fall.Sway * viewH;
            noise.frequency = 0.15f;
            noise.scrollSpeed = 0.1f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            noise.damping = true;

            if (fall.Spin)
            {
                var rotation = _system.rotationOverLifetime;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-Mathf.PI / 4f, Mathf.PI / 4f);   // a lazy turn, ±45° a second
            }

            // Drawn like any sprite on the island (the same lit sprite material), over every prop.
            var probe = new GameObject("probe").AddComponent<SpriteRenderer>();
            _material = new Material(probe.sharedMaterial) { mainTexture = fall.Sprite.texture, name = "HolidayFall" };
            Destroy(probe.gameObject);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingOrder = SortingOrder;

            _system.Play();
            Debug.Log($"[Skins] Falling {fall.Sprite.name}: {count} over {area.size.x:F0}x{area.size.y:F0}.");
        }
    }
}
