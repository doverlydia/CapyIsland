using System;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// The little red dot that says "something new in here" (<see cref="Game.NewStuff"/>), in the
    /// top corner of a button, tile or card. A live dot keeps asking whether it should show; a fixed
    /// one is put on things already known to be new as a page is built.
    /// </summary>
    public class NewDot : MonoBehaviour
    {
        Func<bool> _lit;
        GameObject _dot;
        int _phase;

        /// <summary>A dot that comes and goes on its own as <paramref name="lit"/> changes.</summary>
        public static NewDot Live(Transform on, Func<bool> lit, float size = 34f)
        {
            var d = on.gameObject.AddComponent<NewDot>();
            d._lit = lit;
            d._dot = Fixed(on, size);
            d._phase = UnityEngine.Random.Range(0, 10);
            d._dot.SetActive(SafeLit(lit));
            return d;
        }

        /// <summary>A dot that is simply there (on something new as the page is drawn).</summary>
        public static GameObject Fixed(Transform on, float size = 30f)
        {
            var dot = UiKit.Image(on, "NewDot", UiKit.Circle, UiKit.Danger);
            UiKit.Place(dot.rectTransform, new Vector2(1f, 1f), new Vector2(2f, 2f), new Vector2(size, size));
            // On a card with a layout group the dot would otherwise be laid out (and stretched) as one more item.
            dot.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            dot.transform.SetAsLastSibling();
            return dot.gameObject;
        }

        static bool SafeLit(Func<bool> lit)
        {
            try { return lit(); } catch { return false; }
        }

        void Update()
        {
            if ((Time.frameCount + _phase) % 10 != 0 || _dot == null) return;
            bool on = SafeLit(_lit);
            if (_dot.activeSelf != on) _dot.SetActive(on);
        }
    }
}
