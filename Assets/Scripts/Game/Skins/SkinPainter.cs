using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.Game
{
    /// <summary>
    /// Dresses the island in the holiday skin's sprites: every SpriteRenderer and UI Image showing a
    /// sprite the skin has a picture for shows the skin's picture instead (<see cref="Skins.Swap"/>).
    ///
    /// Nothing in the game has to know. Once a second it looks over everything drawn (new spawns,
    /// panels opened, the island rebuilt for a visit) and remembers the ones that wear a skin's
    /// sprite; then, last thing every frame, it puts the skin back on any of those that code has
    /// changed since (a capybara's awake and asleep frames). When the skin comes off, the same
    /// work gives every one of them the island's own sprite back — including a skin's sprite some
    /// code kept hold of and sets again later.
    ///
    /// Idle — no searching, no per-frame work — until a skin with sprites has been worn.
    /// </summary>
    [DefaultExecutionOrder(10000)]   // after everything that sets a sprite this frame
    public class SkinPainter : MonoBehaviour
    {
        const float SweepEvery = 1f;

        readonly List<SpriteRenderer> _renderers = new();
        readonly List<Image> _images = new();
        readonly HashSet<int> _tracked = new();
        float _nextSweep;
        bool _sweepImages;

        void LateUpdate()
        {
            if (!Skins.Painting) return;
            if (Time.unscaledTime >= _nextSweep)
            {
                // Renderers and images take turns, half a sweep apart, so no one frame does both.
                _nextSweep = Time.unscaledTime + SweepEvery * 0.5f;
                _sweepImages = !_sweepImages;
                if (_sweepImages) SweepImages(); else SweepRenderers();
            }

            for (int i = _renderers.Count - 1; i >= 0; i--)
            {
                var r = _renderers[i];
                if (r == null) { _renderers.RemoveAt(i); continue; }
                var s = r.sprite;
                if (s == null) continue;
                var worn = Skins.Want(s);
                if (worn != s) r.sprite = worn;
            }
            for (int i = _images.Count - 1; i >= 0; i--)
            {
                var img = _images[i];
                if (img == null) { _images.RemoveAt(i); continue; }
                var s = img.sprite;
                if (s == null) continue;
                var worn = Skins.Want(s);
                if (worn != s) img.sprite = worn;
            }
        }

        void SweepRenderers()
        {
            foreach (var r in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var s = r.sprite;
                if (s == null || _tracked.Contains(r.GetInstanceID())) continue;
                var worn = Skins.Want(s);
                if (worn == s) continue;
                r.sprite = worn;
                _renderers.Add(r);
                _tracked.Add(r.GetInstanceID());
            }
        }

        void SweepImages()
        {
            foreach (var img in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var s = img.sprite;
                if (s == null || _tracked.Contains(img.GetInstanceID())) continue;
                var worn = Skins.Want(s);
                if (worn == s) continue;
                img.sprite = worn;
                _images.Add(img);
                _tracked.Add(img.GetInstanceID());
            }
        }
    }
}
