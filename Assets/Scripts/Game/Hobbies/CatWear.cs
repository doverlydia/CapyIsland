using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Dressing an adopted cat from your own wardrobe: whatever you own, it can wear too (the two of
    /// you share the closet, so a piece can be on both of you at once).
    ///
    /// Pieces are fitted to the capybara (<see cref="WearItem.Anchor"/>, in the capybara picture). A
    /// cat's head is somewhere else and a different size, so each piece is moved over: its offset
    /// from where that kind of piece usually sits on the capybara (the top of the head, between the
    /// eyes, under the chin, the back) is scaled and laid on the same spot on the cat. Every sitting
    /// cat is drawn to one layout and every walking cat to another, so one fitting per pose does.
    /// </summary>
    public static class CatWear
    {
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => Changed = null;

        /// <summary>One pose's fitting: where each spot is (pixels from the picture's top left) and how much smaller.</summary>
        struct Fitting
        {
            public Vector2 Hat, Face, Neck, Pack, Cape;
            public float HeadScale, BodyScale;
        }

        // The capybara picture (256 × 256) the pieces were fitted on: where each kind usually sits.
        const float CapySize = 256f;
        static readonly Fitting Capy = new()
        {
            Hat = new(118f, 33f), Face = new(95f, 86f), Neck = new(120f, 166f), Pack = new(189f, 93f), Cape = new(154f, 141f),
            HeadScale = 1f, BodyScale = 1f,
        };

        // Sitting, three-quarter view, head to the left: the head is about as big as the capybara's.
        static readonly Fitting Sitting = new()
        {
            Hat = new(80f, 18f), Face = new(60f, 82f), Neck = new(66f, 134f), Pack = new(140f, 120f), Cape = new(110f, 150f),
            HeadScale = 0.95f, BodyScale = 0.8f,
        };

        // Walking, side on, head to the left: a smaller head out in front of a long body.
        static readonly Fitting Walking = new()
        {
            Hat = new(47f, 28f), Face = new(42f, 70f), Neck = new(54f, 104f), Pack = new(115f, 72f), Cape = new(115f, 100f),
            HeadScale = 0.6f, BodyScale = 0.6f,
        };

        // ---------------------------------------------------------------- what it wears

        public static WearItem Worn(HobbyLog.Cat cat, WearSlot slot)
        {
            if (cat?.outfit == null) return null;
            if (slot == WearSlot.Hat && Horaira.WornHalo(cat) is { } halo) return halo;   // his own, owned or not
            foreach (var id in cat.outfit)
            {
                var item = Wardrobe.Find(id);
                if (item != null && item.Slot == slot && Wardrobe.Owns(item)) return item;
            }
            return null;
        }

        /// <summary>Puts a piece on the cat (replacing what was in its slot), or takes it off if it's already on.</summary>
        public static void Toggle(HobbyLog.Cat cat, WearItem item)
        {
            if (cat == null || item == null || !Wardrobe.Owns(item)) return;
            cat.outfit ??= new List<string>();
            if (Horaira.Is(cat) && item.Id == Horaira.HaloId)
            {
                Horaira.ToggleHalo(cat);
                HobbyLog.Save();
                Changed?.Invoke();
                return;
            }
            if (Horaira.Is(cat) && item.Slot == WearSlot.Hat) cat.bareHead = false;   // the hat comes off again: the halo's back
            bool wearing = cat.outfit.Contains(item.Id);
            cat.outfit.RemoveAll(id => Wardrobe.Find(id) is { } w && w.Slot == item.Slot);
            if (!wearing) cat.outfit.Add(item.Id);
            HobbyLog.Save();
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- where it goes

        /// <summary>
        /// Where a piece sits on a cat picture: its centre (0–1 from the bottom left, like
        /// <see cref="WearItem.Anchor"/>) and its width as a share of the picture's width.
        /// </summary>
        public static void Fit(WearItem item, Sprite cat, bool walking, out Vector2 centre, out float width)
        {
            var fit = walking ? Walking : Sitting;
            bool cape = item.Slot == WearSlot.Back && item.Behind;
            (Vector2 capySpot, Vector2 catSpot, float scale) = item.Slot switch
            {
                WearSlot.Hat => (Capy.Hat, fit.Hat, fit.HeadScale),
                WearSlot.Face => (Capy.Face, fit.Face, fit.HeadScale),
                WearSlot.Neck => (Capy.Neck, fit.Neck, fit.HeadScale),
                _ => cape ? (Capy.Cape, fit.Cape, fit.BodyScale) : (Capy.Pack, fit.Pack, fit.BodyScale),
            };
            var px = new Vector2(item.Anchor.x * CapySize, (1f - item.Anchor.y) * CapySize);   // from the top left
            var onCat = catSpot + (px - capySpot) * scale;
            var size = cat.rect.size;
            centre = new Vector2(onCat.x / size.x, 1f - onCat.y / size.y);
            width = item.Width * CapySize * scale / size.x;
        }

        /// <summary>Lays a piece on a cat's sprite, in its own space; mirrored when the cat faces the other way.</summary>
        public static void Place(SpriteRenderer piece, WearItem item, Sprite cat, Sprite art, bool walking, bool flipped)
        {
            Fit(item, cat, walking, out var centre, out var width);
            var size = cat.rect.size / cat.pixelsPerUnit;
            var min = -cat.pivot / cat.pixelsPerUnit;
            var artCentre = (art.rect.size * 0.5f - art.pivot) / art.pixelsPerUnit;
            float scale = width * size.x / Mathf.Max(0.0001f, art.rect.width / art.pixelsPerUnit);
            var at = new Vector3(min.x + centre.x * size.x, min.y + centre.y * size.y, 0f) - (Vector3)(artCentre * scale);
            // Flipping a sprite mirrors it about its pivot: the piece goes to the other side, mirrored too.
            if (flipped) at.x = -at.x;
            piece.flipX = flipped;
            piece.transform.localPosition = at;
            piece.transform.localScale = new Vector3(scale, scale, 1f);
            piece.transform.localRotation = Quaternion.Euler(0f, 0f, flipped ? -item.Rotation : item.Rotation);
        }
    }
}
