using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The quest engine shared by Miso, Captain Wasabi and the spirits. Data: <see cref="QuestData"/>;
    /// words: <see cref="StoryText"/>.
    ///
    ///   * Coriander and Peach give their missions strictly in order, one at a time; after a reward
    ///     they wait 15 seconds, then the next is ready.
    ///   * A ghost appears near the king when its trigger mote fires — unless its own objective is
    ///     already done, in which case it never shows.
    ///   * Every objective completes when its mote fires, even before the quest has been started.
    ///     Objectives that are states rather than events (no deer left, five tame monsters…) are
    ///     checked every 5 seconds for whichever mission is active.
    ///   * Reading the intro runs the quest's set-up; reading the completion text pays out 70 units
    ///     south of the giver: the reward item in a free bubble, wood, gold and gems as chests.
    /// </summary>
    public class QuestSystem : MonoBehaviour
    {
        public enum Stage { NotStarted, IntroRead, TaskComplete, GiftReceived }

        public class Progress
        {
            public QuestDef Def;
            public Stage Stage;
            public bool Complete;
        }

        public static QuestSystem Instance { get; private set; }

        readonly List<Progress> _coriander = new();
        readonly List<Progress> _peach = new();
        readonly List<Progress> _ghosts = new();
        readonly HashSet<int> _ghostsSpawned = new();

        int _corianderIndex, _peachIndex;
        float _corianderReadyAt, _peachReadyAt;
        float _nextCheck;

        /// <summary>Set the first time you talk to any quest giver; stops the "go talk to them" hints.</summary>
        public static bool AnyoneTalkedTo { get; set; }

        void Awake()
        {
            Instance = this;
            foreach (var q in QuestData.Coriander) _coriander.Add(new Progress { Def = q });
            foreach (var q in QuestData.Peach) _peach.Add(new Progress { Def = q });
            foreach (var q in QuestData.Ghosts) _ghosts.Add(new Progress { Def = q });
        }

        void OnEnable() => Motes.Triggered += OnMote;
        void OnDisable() => Motes.Triggered -= OnMote;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------- queries

        /// <summary>The current mission of Coriander or Peach, or null once they have none left (or are between missions).</summary>
        public Progress Current(QuestGiver.Kind kind)
        {
            var list = kind == QuestGiver.Kind.Peach ? _peach : _coriander;
            int index = kind == QuestGiver.Kind.Peach ? _peachIndex : _corianderIndex;
            float readyAt = kind == QuestGiver.Kind.Peach ? _peachReadyAt : _corianderReadyAt;
            if (index >= list.Count || Time.time < readyAt) return null;
            return list[index];
        }

        public Progress Ghost(int id) => _ghosts.Find(g => g.Def.Id == id);

        // ---------------------------------------------------------------- motes

        void OnMote(MoteDef mote)
        {
            foreach (var list in new[] { _coriander, _peach, _ghosts })
                foreach (var q in list)
                    if (q.Def.CompleteMote == mote.Name) q.Complete = true;

            foreach (var g in _ghosts)
            {
                if (g.Def.Trigger != mote.Name || _ghostsSpawned.Contains(g.Def.Id)) continue;
                if (Motes.Has(g.Def.CompleteMote)) continue;   // done before the ghost could ask
                _ghostsSpawned.Add(g.Def.Id);
                SpawnGhost(g);
            }
        }

        void SpawnGhost(Progress ghost)
        {
            var catalog = ItemCatalog.Instance;
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (catalog == null || king == null) return;

            // A random spot within 800 units of the king where a radius-30 circle is on clear land.
            Vector2 around = king.transform.position;
            Vector2 spot = around + BunniSpace.ToUnity(new Vector2(0f, 100f));
            for (int i = 0; i < 200; i++)
            {
                var p = BunniSpace.RandomPointInCircle(around, 800f);
                if (BunniSpace.CircleOnMap(p, 30f)) { spot = p; break; }
            }

            var go = catalog.Create("Ghost");
            if (go == null) return;
            go.transform.position = new Vector3(spot.x, spot.y, 0f);
            var giver = go.GetComponent<QuestGiver>();
            if (giver != null) giver.AssignGhost(ghost.Def.Id);
            go.SetActive(true);
        }

        // ---------------------------------------------------------------- timed checks

        void Update()
        {
            if (Visit.Active) return;   // a friend's island never moves your quests on
            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + 5f;

            var c = Current(QuestGiver.Kind.Coriander);
            if (c != null && c.Stage == Stage.IntroRead && c.Def.Id == 5 && FindObjectsByType<DeerAnimal>(FindObjectsSortMode.None).Length == 0)
                Motes.Trigger("girl_quest_8");

            var p = Current(QuestGiver.Kind.Peach);
            if (p == null || p.Stage != Stage.IntroRead) return;
            switch (p.Def.Id)
            {
                case 1:
                    foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
                        if (w.SpeciesName == "Panda" && w.Workplace != null && w.Workplace.Accepts == ResourceKind.Wood)
                        { Motes.Trigger("panda_sawmill"); break; }
                    break;
                case 3:
                    int tame = 0;
                    foreach (var m in FindObjectsByType<MonsterAnimal>(FindObjectsSortMode.None)) if (m.Tame) tame++;
                    if (tame >= 5) Motes.Trigger("monster_tame");
                    break;
                case 2:
                    if (StoreData.CountByName("Legendary Plum Tree") >= 6) Motes.Trigger("5_plum");
                    break;
            }
        }

        // ---------------------------------------------------------------- talking

        /// <summary>
        /// Miso's small talk: her usual chatter; once her grandmother is back, that (and no more
        /// talk of "Grandma's ghost"); once everyone is home, that too.
        /// </summary>
        static string[] MisoComments()
        {
            if (!YuzuHeart.Alive) return StoryText.MisoChat;
            var list = new System.Collections.Generic.List<string>();
            foreach (var line in StoryText.MisoChat) if (!line.Contains("ghost")) list.Add(line);
            list.AddRange(StoryText.MisoGrandmaBack);
            if (YuzuHeart.FinaleSeen) list.AddRange(StoryText.MisoHome);
            return list.ToArray();
        }

        /// <summary>What the giver says now: the pages for its current stage.</summary>
        public string[] PagesFor(Progress q, QuestGiver.Kind kind)
        {
            if (q == null)
            {
                var comments = kind == QuestGiver.Kind.Peach ? StoryText.WasabiChat : MisoComments();
                if (comments.Length == 0) return new[] { "..." };
                return new[] { comments[Random.Range(0, comments.Length)] };
            }

            switch (q.Stage)
            {
                case Stage.NotStarted:
                    return QuestDef.Pages(q.Complete ? q.Def.Complete : q.Def.Intro);
                case Stage.IntroRead:
                    return QuestDef.Pages(q.Complete ? q.Def.Complete : q.Def.During);
                default:
                    return QuestDef.Pages(q.Def.Complete);
            }
        }

        /// <summary>
        /// The dialog closed (QuestItem.Close → AdvanceState). Intro read: set up and wait, or
        /// straight to complete. Complete read: pay out.
        /// </summary>
        public void Advance(Progress q, QuestGiver giver)
        {
            if (q == null) return;
            AnyoneTalkedTo = true;

            if (q.Stage == Stage.NotStarted && !q.Complete)
            {
                q.Stage = Stage.IntroRead;
                MissionSetUps.Run(q.Def.SetUp, giver);
                return;
            }

            if (q.Complete && q.Stage != Stage.GiftReceived)
            {
                q.Stage = Stage.GiftReceived;
                PayOut(q.Def, giver.transform.position);

                if (giver.GiverKind == QuestGiver.Kind.Ghost) { giver.Depart(); return; }
                if (giver.GiverKind == QuestGiver.Kind.Coriander) { _corianderIndex++; _corianderReadyAt = Time.time + 15f; }
                else { _peachIndex++; _peachReadyAt = Time.time + 15f; }
            }
        }

        /// <summary>Rewards 70 units south of the giver: the item in a bubble, resources in chests.</summary>
        public static void PayOut(QuestDef q, Vector2 giverAt)
        {
            var spot = giverAt + BunniSpace.ToUnity(new Vector2(0f, -70f));
            var catalog = ItemCatalog.Instance;
            var parent = catalog != null ? catalog.ItemParent : null;
            if (!string.IsNullOrEmpty(q.RewardItem) && catalog != null) catalog.MakeStore(q.RewardItem, spot);
            float x = -1.2f;
            if (q.Wood > 0) { TreasureChest.SpawnValue(spot + new Vector2(x, -0.3f), ResourceKind.Wood, q.Wood, parent); x += 1.2f; }
            if (q.Gold > 0) { TreasureChest.SpawnValue(spot + new Vector2(x, -0.3f), ResourceKind.Gold, q.Gold, parent); x += 1.2f; }
            if (q.Gems > 0) TreasureChest.SpawnValue(spot + new Vector2(x, -0.3f), ResourceKind.Gem, q.Gems, parent);
        }

        // ---------------------------------------------------------------- saving

        public (int coriander, int peach, List<(int giver, int id, int stage, bool complete)> quests, List<int> ghosts) Snapshot()
        {
            var quests = new List<(int, int, int, bool)>();
            foreach (var q in _coriander) quests.Add((1, q.Def.Id, (int)q.Stage, q.Complete));
            foreach (var q in _peach) quests.Add((2, q.Def.Id, (int)q.Stage, q.Complete));
            foreach (var q in _ghosts) quests.Add((0, q.Def.Id, (int)q.Stage, q.Complete));
            return (_corianderIndex, _peachIndex, quests, new List<int>(_ghostsSpawned));
        }

        public void Restore(int coriander, int peach, List<(int giver, int id, int stage, bool complete)> quests, List<int> ghosts)
        {
            _corianderIndex = coriander;
            _peachIndex = peach;
            foreach (var (giver, id, stage, complete) in quests)
            {
                var list = giver == 1 ? _coriander : giver == 2 ? _peach : _ghosts;
                var q = list.Find(x => x.Def.Id == id);
                if (q == null) continue;
                q.Stage = (Stage)stage;
                q.Complete = complete;
            }
            _ghostsSpawned.Clear();
            foreach (var g in ghosts) _ghostsSpawned.Add(g);
        }
    }
}
