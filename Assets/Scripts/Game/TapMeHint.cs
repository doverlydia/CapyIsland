using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The pointing hand taps on someone with something to say: the "!" over a character's head
    /// isn't enough on its own for a new player to know they should tap them. Only after the
    /// opening (it has the hand then), only for someone on screen, and only until the player has
    /// tapped someone with news a couple of times themselves. A newcomer still in a bubble (Miso
    /// at the start) gets the hand every time: that only happens a handful of times in a game.
    /// </summary>
    public class TapMeHint : MonoBehaviour
    {
        const string LearnedKey = "CapyIsland.Tip.tapNews";
        const int TapsToLearn = 2;

        TutorialPointer _pointer;
        Transform _pointing;
        float _nextLook;

        static bool Learned => PlayerPrefs.GetInt(LearnedKey, 0) >= TapsToLearn;

        /// <summary>Someone with news was tapped (QuestGiver.Tap): one step nearer to not needing the hand.</summary>
        public static void Tapped()
        {
            if (Learned) return;
            PlayerPrefs.SetInt(LearnedKey, PlayerPrefs.GetInt(LearnedKey, 0) + 1);
            PlayerPrefs.Save();
        }

        void Update()
        {
            if (Time.unscaledTime < _nextLook) return;
            _nextLook = Time.unscaledTime + 0.5f;
            if (_pointer == null) _pointer = FindFirstObjectByType<TutorialPointer>();
            if (_pointer == null) return;

            var want = Guidance.Off || Visit.Active ? null : Candidate();
            if (want == _pointing) return;

            // Let go of ours, but never of a hand someone else has taken since (the cloud's guide).
            if (_pointing != null && Pointing(_pointing)) _pointer.Hide();
            _pointing = null;
            if (want == null || _pointer.Target.HasValue) return;   // the hand is busy elsewhere
            _pointer.PointAt(want);
            _pointing = want;
        }

        bool Pointing(Transform target) =>
            target != null && _pointer.Target.HasValue && Vector2.Distance(_pointer.Target.Value, target.position) < 0.05f;

        /// <summary>A newcomer's bubble, or someone with news, on screen, while nothing else needs the player.</summary>
        static Transform Candidate()
        {
            var tutorial = TutorialDirector.Instance;
            if (tutorial != null && !tutorial.Finished) return null;   // the opening has the hand
            if (Attention.Busy) return null;
            var view = Wildlife.PerchView();
            foreach (var b in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
                if (!b.Taken && b.Contents != null && b.Contents.TryGetComponent<QuestGiver>(out var newcomer)
                    && newcomer.GiverKind != QuestGiver.Kind.Ghost && view.Contains(b.transform.position)) return b.transform;
            if (Learned) return null;
            foreach (var giver in FindObjectsByType<QuestGiver>(FindObjectsSortMode.None))
                if (giver.HasNews && view.Contains(giver.transform.position)) return giver.transform;
            return null;
        }
    }
}
