using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Hobbies open one at a time, chosen by the player (in the spirit of Heartopia). Fishing is
    /// open from the start; every other hobby waits for a hobby card, and warm hearts earn the
    /// cards: one at each of <see cref="Thresholds"/>. A card waits its turn like any popup
    /// (Attention), opens the choice (UI.HobbyCardScreen), and the hobby picked is opened with a
    /// word from its own character on where to start. A card not used yet waits in the Hobbies hub.
    ///
    /// Opening a hobby makes it available (HobbyLog.IsOpened): its gear goes on sale, strays start
    /// to visit, Old Mochi lets you write. Taking it up is still buying the gear (HobbyLog.IsUnlocked).
    ///
    /// Except the first card: it is a hobby with gear you carry — fishing (if you have no rod yet),
    /// bug catching or birdwatching — and the gear comes with it, free, in a bubble by the king
    /// (<see cref="GearGift"/>). Picking a hobby and then having to go and buy something looked
    /// like the card had done nothing. Never gardening first: its way in is a whole island.
    /// </summary>
    public class HobbyCards : MonoBehaviour
    {
        /// <summary>The warm hearts at which each card is earned.</summary>
        public static readonly int[] Thresholds = { 3, 6, 10, 14, 18, 23, 28 };

        /// <summary>The hobbies a card can open, in the order the choice shows them.</summary>
        public static readonly Hobby[] Choosable =
            { Hobby.Cats, Hobby.Writing, Hobby.Bugs, Hobby.Birding, Hobby.Cooking, Hobby.Gardening, Hobby.Sewing };

        const string NewKey = "hobbies/card";   // the Hobbies button's dot while a card waits
        const string AskKey = "hobby-card";

        public static HobbyCards Instance { get; private set; }

        static bool _remindedThisLaunch;
        float _loadedAt, _nextCheck;

        // ---------------------------------------------------------------- counting

        /// <summary>Warm hearts found so far.</summary>
        public static int Hearts => Motes.CollectedCount;

        /// <summary>Cards the hearts have earned in all, spent or not.</summary>
        public static int Earned
        {
            get
            {
                int hearts = Hearts, n = 0;
                foreach (var t in Thresholds) if (hearts >= t) n++;
                return n;
            }
        }

        /// <summary>The hobbies still closed that a card could open now, in choosing order.</summary>
        public static List<Hobby> Closed()
        {
            var list = new List<Hobby>();
            foreach (var h in Choosable) if (!HobbyLog.IsOpened(h) && CanOpenYet(h)) list.Add(h);
            return list;
        }

        /// <summary>
        /// Cooking waits for Sunflower Farm: Chef Tofu, who sells the stove, has his stall there, so
        /// until its bridge is up there would be nobody to go and see. (A card earned meanwhile waits
        /// for it, or for another hobby.)
        /// </summary>
        public static bool CanOpenYet(Hobby h) => h != Hobby.Cooking || Farm.IsOpen;

        /// <summary>The first card is still to be spent: it comes with its gear.</summary>
        public static bool FirstCard => HobbyLog.CardsSpent == 0 && FirstChoices().Count > 0;

        /// <summary>What the first card can take up: the hobbies whose gear you carry, and haven't got.</summary>
        static List<Hobby> FirstChoices()
        {
            var list = new List<Hobby>();
            if (!HobbyLog.IsUnlocked(Hobby.Fishing)) list.Add(Hobby.Fishing);
            if (!HobbyLog.IsOpened(Hobby.Bugs)) list.Add(Hobby.Bugs);
            if (!HobbyLog.IsOpened(Hobby.Birding)) list.Add(Hobby.Birding);
            return list;
        }

        /// <summary>What a card can open now, in choosing order: for the first, only hobbies with gear to give.</summary>
        public static List<Hobby> Choices() => HobbyLog.CardsSpent == 0 && FirstChoices() is { Count: > 0 } first ? first : Closed();

        /// <summary>The gear a first card gives with its hobby.</summary>
        static string GearFor(Hobby h) => h switch
        {
            Hobby.Fishing => "Fishing Rod",
            Hobby.Bugs => "Bug Net",
            Hobby.Birding => "Binoculars",
            _ => null,
        };

        /// <summary>Cards earned and not spent yet (never more than there are hobbies left to open).</summary>
        public static int Waiting => Mathf.Clamp(Earned - HobbyLog.CardsSpent, 0, Closed().Count);

        /// <summary>
        /// Warm hearts still to find before the next card that can be spent, or null if no more are
        /// coming (every card earned, or every hobby open).
        /// </summary>
        public static int? HeartsToNext
        {
            get
            {
                if (Closed().Count == 0) return null;
                int next = Mathf.Max(Earned, HobbyLog.CardsSpent);
                if (next >= Thresholds.Length) return null;
                return Mathf.Max(1, Thresholds[next] - Hearts);
            }
        }

        /// <summary>Spends a card on <paramref name="h"/>, then its character says where to start.</summary>
        public static bool Choose(Hobby h)
        {
            var gear = FirstCard ? GearFor(h) : null;
            if (Waiting <= 0 || !HobbyLog.OpenWithCard(h)) return false;
            Sfx.Play(SfxKind.Coin, 0.7f);
            if (Waiting == 0) NewStuff.Remove(NewKey);
            if (gear == null) { Welcome(h); return true; }

            // The first card: its gear in a bubble by you, and a word on popping it.
            HobbyLog.FreeGear = gear;
            var bubble = OfferGear(gear);
            var (face, who, line) = GiftGreeting(h);
            UI.SpeechDialog.Show(face, who, new[] { line }, () => PointAt(bubble));
            return true;
        }

        // ---------------------------------------------------------------- the first card's gift

        static Bubble OfferGear(string gear)
        {
            var art = HobbyData.Art(gear switch { "Fishing Rod" => "Rod", "Bug Net" => "Net", _ => "Binoculars" });
            return HobbyItems.Offer(GearGift.Make(gear, art));
        }

        static void PointAt(Bubble bubble)
        {
            if (bubble == null || bubble.Taken) return;
            var pointer = FindFirstObjectByType<TutorialPointer>();
            if (pointer != null) pointer.PointAt(bubble.transform, 8f);
        }

        static (Sprite face, string who, string line) GiftGreeting(Hobby h)
        {
            var yuzu = Resources.Load<Sprite>("Cutscenes/Cutscene_IntroYuzu");
            return h switch
            {
                Hobby.Fishing => (HobbyData.Art("Sailor"), SeaLibrary.Skipper, StoryText.HobbyCardLines.GiftRod),
                Hobby.Bugs => (yuzu, StoryText.Guide, StoryText.HobbyCardLines.GiftNet),
                _ => (yuzu, StoryText.Guide, StoryText.HobbyCardLines.GiftBinoculars),
            };
        }

        /// <summary>
        /// Back on the island with the gift still unpopped (the app was closed, the island
        /// reloaded): its bubble comes back by the king. Bought in the meantime, it's dropped.
        /// </summary>
        void RestoreGift()
        {
            var gear = HobbyLog.FreeGear;
            if (gear == null || Visit.Active) return;
            if (StoreData.HobbyFor(gear) is Hobby h && HobbyLog.IsUnlocked(h)) { HobbyLog.FreeGear = null; return; }
            foreach (var b in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
                if (!b.Taken && b.Contents != null && b.Contents.TryGetComponent<GearGift>(out _)) return;
            OfferGear(gear);
        }

        // ---------------------------------------------------------------- a word from its character

        /// <summary>The hobby's own character, on where to begin.</summary>
        public static void Welcome(Hobby h)
        {
            var (face, who, line) = Greeting(h);
            UI.SpeechDialog.Show(face, who, new[] { line }, null);
        }

        static (Sprite face, string who, string line) Greeting(Hobby h)
        {
            var yuzu = Resources.Load<Sprite>("Cutscenes/Cutscene_IntroYuzu");
            switch (h)
            {
                case Hobby.Fishing: return (HobbyData.Art("Sailor"), SeaLibrary.Skipper, StoryText.HobbyCardLines.Fishing);
                case Hobby.Cooking: return (HobbyData.Art("Chef"), StoryText.Chef, StoryText.HobbyCardLines.Cooking);
                case Hobby.Writing: return (HobbyData.Art("Mochi"), StoryText.Keeper, StoryText.HobbyCardLines.Writing);
                case Hobby.Cats: return (yuzu, StoryText.Guide, StoryText.HobbyCardLines.Cats);
                case Hobby.Bugs:
                    return (yuzu, StoryText.Guide, Built("Bronze Store") ? StoryText.HobbyCardLines.BugsReady : StoryText.HobbyCardLines.BugsLater);
                case Hobby.Birding:
                    return (yuzu, StoryText.Guide, Built("Silver Store") ? StoryText.HobbyCardLines.BirdingReady : StoryText.HobbyCardLines.BirdingLater);
                case Hobby.Sewing:
                    return (MisoFace(), StoryText.Sweetheart, Built("Steel Store") ? StoryText.HobbyCardLines.SewingReady : StoryText.HobbyCardLines.SewingLater);
                case Hobby.Gardening:
                    var land = LandMap.Instance;
                    var cove = land != null ? land.Bridge("second_bridge") : null;
                    return (yuzu, StoryText.Guide, cove != null && cove.open ? StoryText.HobbyCardLines.GardeningReady : StoryText.HobbyCardLines.GardeningLater);
                default: return (yuzu, StoryText.Guide, "");
            }
        }

        /// <summary>A shop of yours stands somewhere (its heart has come, or one is planted).</summary>
        public static bool Built(string store)
        {
            if (Motes.Has(store)) return true;
            foreach (var tag in FindObjectsByType<CatalogItem>(FindObjectsSortMode.None))
            {
                if (tag.StoreName != store) continue;
                var placeable = tag.GetComponent<Placeable>();
                if (placeable == null || placeable.Placed) return true;
            }
            return false;
        }

        /// <summary>Miso's own picture, wherever she is (even waiting in a bubble).</summary>
        static Sprite MisoFace()
        {
            foreach (var giver in FindObjectsByType<QuestGiver>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (giver.GiverKind != QuestGiver.Kind.Coriander) continue;
                var body = giver.transform.Find("Sprite");
                var sr = body != null ? body.GetComponent<SpriteRenderer>() : null;
                if (sr != null && sr.sprite != null) return sr.sprite;
            }
            return null;
        }

        // ---------------------------------------------------------------- watching for cards

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            Attach();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) => Attach();
        }

        static void Attach()
        {
            if (Instance != null) return;
            // A brand-new island (a first launch, or starting over): the shops pace themselves
            // afresh. Decided now, before the island's first save exists.
            if (!SaveGame.HasSave && !Visit.Active) HobbyLog.NewIsland();
            new GameObject("HobbyCards").AddComponent<HobbyCards>();
        }

        void Awake()
        {
            Instance = this;
            _loadedAt = Time.unscaledTime;
            HobbyItems.AfterLoad(this, RestoreGift);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 1f;

            ShopPacing.NoteOldStock(Time.unscaledTime - _loadedAt);
            if (Visit.Active || Attention.TutorialRunning) return;

            int waiting = Waiting;
            if (waiting <= 0)
            {
                NewStuff.Remove(NewKey);
                return;
            }
            NewStuff.Add(NewKey);

            // A new card is offered as soon as the screen is free; one still unspent is offered
            // again once a launch, in case it was put off.
            int earned = Earned;
            bool fresh = earned > HobbyLog.CardsShown;
            if (!fresh && _remindedThisLaunch) return;
            if (Attention.Waiting(AskKey)) return;
            Attention.Ask(AskKey, () =>
            {
                HobbyLog.CardsShown = Mathf.Max(HobbyLog.CardsShown, Earned);
                _remindedThisLaunch = true;
                UI.HobbyCardScreen.Offer();
            }, () => Waiting > 0 && !Visit.Active, calm: fresh ? 5f : 10f);
        }
    }
}
