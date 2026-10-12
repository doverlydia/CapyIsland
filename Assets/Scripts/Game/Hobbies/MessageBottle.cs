using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A message bottle of your own. Bought from Skipper Nori, it joins your trail like anything
    /// else you buy; drag it to the beach and drop it on the sand by the water, and the page opens
    /// to write what goes inside. It stays where you put it: tap it to change the note, drag it to
    /// move it along the shore. Cancel the page and it goes back into your trail.
    ///
    /// On a friend's island, a bottle written on their shore is a note left for them (the sea
    /// library keeps those), and this one is used up.
    /// </summary>
    public class MessageBottle : MonoBehaviour, IDropSpot
    {
        const string Key = "CapyIsland.MyBottles";

        [Serializable] class Saved { public string id, title, text; public float x, y; public bool placed; }
        [Serializable] class SavedList { public List<Saved> list = new(); }

        Saved _data;
        Follower _follower;
        Vector3 _restAt;
        bool _writing;

        // ---------------------------------------------------------------- the bottles you own

        static SavedList Load()
        {
            try { return JsonUtility.FromJson<SavedList>(PlayerPrefs.GetString(Key, "")) ?? new SavedList(); }
            catch { return new SavedList(); }
        }

        static void Store(SavedList all)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(all));
            PlayerPrefs.Save();
        }

        void Persist()
        {
            var all = Load();
            int i = all.list.FindIndex(s => s.id == _data.id);
            if (i >= 0) all.list[i] = _data; else all.list.Add(_data);
            Store(all);
        }

        void Forget()
        {
            var all = Load();
            all.list.RemoveAll(s => s.id == _data.id);
            Store(all);
        }

        /// <summary>Is this one of yours (so the sea library doesn't lay it out a second time)?</summary>
        public static bool IsMine(string title, string text) => Load().list.Exists(s => s.placed && s.title == title && s.text == text);

        /// <summary>Puts your bottles back: the ones on your beach (at home), and any still in your trail.</summary>
        public static void RestoreAll()
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            Vector2 behind = king != null ? (Vector2)king.transform.position + new Vector2(-0.9f, -0.4f) : Vector2.zero;
            foreach (var s in Load().list)
            {
                if (s.placed) { if (!Visit.Active) Create(s, new Vector2(s.x, s.y), carried: false); }
                else Create(s, behind, carried: true);
            }
        }

        /// <summary>A new, empty bottle, into your trail.</summary>
        public static void BuyNew()
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            Vector2 at = king != null ? (Vector2)king.transform.position + new Vector2(-0.9f, -0.4f) : Vector2.zero;
            var bottle = Create(new Saved { id = Guid.NewGuid().ToString("N") }, at, carried: true);
            bottle.Persist();
            bottle.transform.localScale *= 0.2f;
            bottle.transform.DOScale(bottle.transform.localScale * 5f, 0.35f).SetEase(Ease.OutBack);
            Hints.Show("Drag your bottle to the beach and drop it on the sand by the water.");
        }

        static MessageBottle Create(Saved data, Vector2 at, bool carried)
        {
            var art = HobbyData.Art("Bottle");
            var go = new GameObject("MessageBottle");
            go.transform.position = new Vector3(at.x, at.y, 0f);
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            body.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            if (art != null) go.transform.localScale = Vector3.one * (0.6f / Mathf.Max(0.01f, art.bounds.size.x));
            var drag = go.AddComponent<DraggableActor>();
            drag.Configure(1.4f, 0.5f);
            var follower = go.AddComponent<Follower>();
            go.AddComponent<YSorter>();
            var bottle = go.AddComponent<MessageBottle>();
            bottle._data = data;
            bottle._follower = follower;
            if (!carried) follower.PlaceInWorld();
            return bottle;
        }

        // ---------------------------------------------------------------- picking up and putting down

        void OnPickedUp() => _restAt = transform.position;

        void OnDropped()
        {
            bool tap = CameraRig.CameraInputController.ReleaseWasTap;
            if (_data.placed)
            {
                if (tap) { transform.position = _restAt; Edit(); return; }
                if (OnShore(transform.position))
                {
                    _data.x = transform.position.x; _data.y = transform.position.y;
                    Persist();
                    Settle();
                }
                else
                {
                    transform.position = _restAt;
                    Hints.Show("Bottles go on the sand, right by the water.");
                }
                return;
            }

            // Still in the trail: only the beach will do.
            if (tap || !OnShore(transform.position))
            {
                if (!tap) Hints.Show("Drag your bottle to the beach and drop it on the sand by the water.");
                _follower.PickUpIntoInventory();
                return;
            }
            // Planted where you dropped it, like a building: it stays there whether you write your
            // note now or later (tap it to write).
            _follower.PlaceInWorld();
            if (!Visit.Active)   // on a friend's shore it is only a note for them, never planted
            {
                _data.placed = true;
                _data.x = transform.position.x; _data.y = transform.position.y;
                Persist();
            }
            Settle();
            Write();
        }

        void Settle()
        {
            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Twinkle((Vector2)transform.position + Vector2.up * 0.3f, 6);
            Sfx.Play(SfxKind.Plop, 0.5f);
        }

        // The placing ring: green on the sand by the water, red anywhere else.
        public Vector2 DropSize => new(1.1f, 0.55f);
        public bool CanDropAt(Vector2 at) => OnShore(at);
        public string DropName => "Message in a Bottle";

        /// <summary>The sand at the water's edge: on land, with the sea close by.</summary>
        static bool OnShore(Vector2 p)
        {
            if (!BunniSpace.PointOnLand(p)) return false;
            for (float r = 0.5f; r <= 1.6f; r += 0.55f)
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI / 6f;
                    if (!BunniSpace.PointOnLand(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f) * r)) return true;
                }
            return false;
        }

        // ---------------------------------------------------------------- the note inside

        void Write()
        {
            _writing = true;
            UI.LibraryScreen.WriteBottle(Written, null, null, Cancelled);
        }

        void Written(string title, string text)
        {
            _writing = false;
            if (Visit.Active)
            {
                // On a friend's shore: a note for them; this bottle is used up.
                SeaLibrary.LeaveNoteForHost(title, text);
                Forget();
                transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject));
                return;
            }
            _data.title = title; _data.text = text;
            _data.x = transform.position.x; _data.y = transform.position.y;
            _data.placed = true;
            Persist();
            HobbyLog.AddBook(title, text);
            HobbyLog.AddXp(Hobby.Writing, Mathf.Clamp(text.Length / 20, 10, 40));
            Motes.Trigger("hobby_book");
            Settle();
            Hints.Show("Your note is tucked inside. Tap the bottle to change it.");
        }

        void Cancelled()
        {
            _writing = false;
            if (Visit.Active && !_data.placed) { _follower.PickUpIntoInventory(); return; }   // still yours to carry home
            if (string.IsNullOrEmpty(_data.text)) Hints.Show("Tap your bottle any time to write your note.");
        }

        void Edit()
        {
            _writing = true;
            // Still empty (the page was closed before): writing it now is its first note.
            if (string.IsNullOrEmpty(_data.text)) { UI.LibraryScreen.WriteBottle(Written, null, null, Cancelled); return; }
            UI.LibraryScreen.WriteBottle(Edited, _data.title, _data.text, () => _writing = false);
        }

        void Edited(string title, string text)
        {
            _writing = false;
            // Keep your shelf in step with the bottle.
            foreach (var book in HobbyLog.Books)
                if (book.title == _data.title && book.text == _data.text) { book.title = title; book.text = text; HobbyLog.Save(); break; }
            _data.title = title; _data.text = text;
            Persist();
            Settle();
            Hints.Show("Your note is updated.");
        }

        void Update()
        {
            // The page was closed some other way (the back button): same as Cancel.
            if (!_writing) return;
            var page = UI.LibraryScreen.Instance;
            if (page == null || !page.IsOpen) Cancelled();
        }
    }
}
