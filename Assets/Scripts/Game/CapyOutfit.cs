using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Dresses the player's capybara in whatever the wardrobe says it is wearing: each piece is a
    /// sprite layered on the body, so it hops and turns with it. Until the wardrobe's art exists the
    /// king keeps his original crowned sprite and nothing changes.
    ///
    /// Also where achievements turn into clothes: it hears every achievement and hands over its piece.
    /// The dressing itself (<see cref="Dress"/>) is shared with friends' capybaras.
    /// </summary>
    [DefaultExecutionOrder(110)]   // after YSorter, so pieces sort against this frame's body order
    public class CapyOutfit : MonoBehaviour
    {
        SpriteRenderer _body;
        readonly Dictionary<WearSlot, SpriteRenderer> _pieces = new();

        void Awake()
        {
            var sprite = transform.Find("Sprite");
            _body = sprite != null ? sprite.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        }

        void OnEnable()
        {
            Wardrobe.Changed += Apply;
            Motes.Triggered += Wardrobe.OnAchievement;
        }

        void OnDisable()
        {
            Wardrobe.Changed -= Apply;
            Motes.Triggered -= Wardrobe.OnAchievement;
        }

        IEnumerator Start()
        {
            Apply();
            // Wait for the save to restore achievements, then hand over anything already earned.
            yield return null;
            Wardrobe.CatchUp();
            Apply();
        }

        /// <summary>Re-dresses the capybara from the wardrobe.</summary>
        public void Apply()
        {
            if (_body == null || !Wardrobe.Ready) return;
            _body.sprite = Wardrobe.BaseSprite;
            Dress(_body, _pieces, WornNow);
        }

        void LateUpdate() => SortPieces(_body, _pieces, WornNow);

        static string _hatOverride;

        /// <summary>
        /// For a moment, a different hat than the one chosen (the towel in the hot spring); null puts
        /// the real one back. Never saved: the wardrobe itself is untouched.
        /// </summary>
        public static void OverrideHat(string id)
        {
            _hatOverride = id;
            foreach (var outfit in FindObjectsByType<CapyOutfit>(FindObjectsSortMode.None)) outfit.Apply();
        }

        static WearItem WornNow(WearSlot slot) =>
            slot == WearSlot.Hat && _hatOverride != null ? Wardrobe.Find(_hatOverride) : Wardrobe.Worn(slot);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => _hatOverride = null;

        // ---------------------------------------------------------------- shared dressing

        /// <summary>Lays the pieces <paramref name="worn"/> says on a body sprite, one renderer per slot.</summary>
        public static void Dress(SpriteRenderer body, Dictionary<WearSlot, SpriteRenderer> pieces, Func<WearSlot, WearItem> worn)
        {
            if (body == null || body.sprite == null) return;
            foreach (WearSlot slot in Enum.GetValues(typeof(WearSlot)))
            {
                var item = worn(slot);
                var art = Wardrobe.Art(item);
                if (!pieces.TryGetValue(slot, out var piece) || piece == null)
                {
                    if (art == null) continue;
                    var go = new GameObject("Wear_" + slot);
                    go.transform.SetParent(body.transform, false);
                    piece = go.AddComponent<SpriteRenderer>();
                    pieces[slot] = piece;
                }
                piece.enabled = art != null;
                if (art == null) continue;
                piece.sprite = art;
                piece.color = Color.white;
                Place(piece.transform, item, body.sprite, art);
            }
        }

        /// <summary>Keeps pieces just in front of (or, for capes, just behind) the body as it sorts.</summary>
        public static void SortPieces(SpriteRenderer body, Dictionary<WearSlot, SpriteRenderer> pieces, Func<WearSlot, WearItem> worn)
        {
            if (body == null || pieces.Count == 0) return;
            int order = body.sortingOrder;
            foreach (var kv in pieces)
            {
                if (kv.Value == null || !kv.Value.enabled) continue;
                var item = worn(kv.Key);
                kv.Value.sortingOrder = item != null && item.Behind ? order - 1 : kv.Key switch
                {
                    WearSlot.Neck => order + 2,   // +1 is a sewn shirt
                    WearSlot.Back => order + 2,
                    WearSlot.Face => order + 3,
                    _ => order + 4,
                };
            }
        }

        /// <summary>Puts a piece where its item says, in the body's own space.</summary>
        public static void Place(Transform piece, WearItem item, Sprite body, Sprite art)
        {
            // The whole sprite rectangle, not its trimmed mesh: the wardrobe preview measures the same way.
            var size = body.rect.size / body.pixelsPerUnit;
            var min = -body.pivot / body.pixelsPerUnit;
            var artCentre = (art.rect.size * 0.5f - art.pivot) / art.pixelsPerUnit;
            float scale = item.Width * size.x / Mathf.Max(0.0001f, art.rect.width / art.pixelsPerUnit);
            piece.localPosition = new Vector3(min.x + item.Anchor.x * size.x, min.y + item.Anchor.y * size.y, 0f)
                                  - (Vector3)(artCentre * scale);
            piece.localScale = new Vector3(scale, scale, 1f);
            piece.localRotation = Quaternion.Euler(0f, 0f, item.Rotation);
        }
    }
}
