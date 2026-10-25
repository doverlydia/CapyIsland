using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A packet of seeds from the farm's barn. Like anything you buy it rides in your trail, and you
    /// drag it out to use it — but the only place it goes is an open, empty plot on the farm, where
    /// it plants itself and is used up. Anywhere else it hops back into the trail with a word on why,
    /// and while it's held the placing ring is green only over a plot it can go in.
    /// </summary>
    public class SeedPacket : MonoBehaviour, IDropSpot
    {
        [SerializeField] string cropId;

        public void Configure(string crop) => cropId = crop;

        CropKind Crop => HobbyData.Crop(cropId);

        public Vector2 DropSize => new Vector2(1.6f, 0.8f);

        public string DropName => StoryText.Name(StoreData.SeedName(Crop));

        public bool CanDropAt(Vector2 at)
        {
            var plot = Gardening.PlotView.Under(at);
            return plot != null && plot.CanPlant;
        }

        /// <summary>Let go (DraggableActor.EndDrag): planted if it's over a plot that can take it.</summary>
        void OnDropped()
        {
            var crop = Crop;
            var plot = Gardening.PlotView.Under(transform.position);
            if (crop != null && plot != null && plot.CanPlant)
            {
                plot.Plant(crop);
                Destroy(gameObject);
                StoreData.InvalidateCounts();
                return;
            }

            Hints.Answer(plot != null && plot.WhyNot != null ? plot.WhyNot : "Seeds go on an empty plot on Sunflower Farm.");
            // Back into the trail at your feet, with a little bounce so the eye follows it home.
            if (TryGetComponent<Placeable>(out var placeable)) placeable.MarkCarried();
            var body = transform.Find("Sprite");
            if (body != null)
            {
                body.DOKill(true);
                body.DOPunchScale(new Vector3(0.25f, -0.2f, 0f), 0.45f, 6, 0.6f);
            }
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king != null)
            {
                var behind = (Vector2)king.transform.position + new Vector2(-0.9f, -0.5f);
                transform.position = new Vector3(behind.x, behind.y, transform.position.z);
            }
        }
    }
}
