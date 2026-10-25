using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The photos you take with the game's camera (UI/Photo/PhotoMode): square snaps of the island,
    /// your burrow, your friends. Each is a little JPEG on the device with a tag you can write on
    /// it ("me and Juniper"), and you stick them in your photo album (HomeLog.Album). Kept on this
    /// device only, like your hobbies.
    /// </summary>
    public static class PhotoLog
    {
        const string Key = "CapyIsland.Photos";

        /// <summary>The most kept at once: the camera says so when the roll is full.</summary>
        public const int Max = 60;

        /// <summary>How big a photo is kept, in pixels across (they're square).</summary>
        public const int Size = 720;

        [Serializable] public class Photo { public string id, tag; public long ticks; }
        [Serializable] class Data { public List<Photo> photos = new(); }

        static Data _d;
        static Data D => _d ??= Load();
        static readonly Dictionary<string, Texture2D> _loaded = new();

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            _d = null;
            Changed = null;
            _loaded.Clear();
        }

        static Data Load()
        {
            try { return JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key, "")) ?? new Data(); }
            catch { return new Data(); }
        }

        static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(D));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        static string Folder => Path.Combine(Application.persistentDataPath, "Photos");
        static string FileOf(string id) => Path.Combine(Folder, id + ".jpg");

        /// <summary>Newest first.</summary>
        public static IReadOnlyList<Photo> All
        {
            get
            {
                D.photos.Sort((a, b) => b.ticks.CompareTo(a.ticks));
                return D.photos;
            }
        }

        public static bool Full => D.photos.Count >= Max;

        public static Photo Find(string id) => string.IsNullOrEmpty(id) ? null : D.photos.Find(p => p.id == id);

        /// <summary>Keeps a snap (already square and small). Returns it, or null if it couldn't be saved.</summary>
        public static Photo Add(Texture2D picture)
        {
            if (picture == null || Full) return null;
            var photo = new Photo { id = "p" + DateTime.UtcNow.Ticks.ToString("x"), tag = "", ticks = DateTime.Now.Ticks };
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllBytes(FileOf(photo.id), picture.EncodeToJPG(82));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PhotoLog] Couldn't save a photo: " + e.Message);
                return null;
            }
            _loaded[photo.id] = picture;
            D.photos.Add(photo);
            Save();
            return photo;
        }

        public static void SetTag(Photo photo, string tag)
        {
            if (photo == null) return;
            photo.tag = (tag ?? "").Trim();
            if (photo.tag.Length > 40) photo.tag = photo.tag.Substring(0, 40);
            Save();
        }

        /// <summary>Throws a photo away, and takes it out of the album.</summary>
        public static void Delete(Photo photo)
        {
            if (photo == null) return;
            D.photos.Remove(photo);
            try { if (File.Exists(FileOf(photo.id))) File.Delete(FileOf(photo.id)); } catch { }
            if (_loaded.TryGetValue(photo.id, out var tex) && tex != null) UnityEngine.Object.Destroy(tex);
            _loaded.Remove(photo.id);
            foreach (var page in HomeLog.Album)
            {
                if (page.a == photo.id) page.a = "";
                if (page.b == photo.id) page.b = "";
            }
            HomeLog.Save();
            Save();
        }

        /// <summary>The photo's picture (read from the device the first time), or null if it's gone.</summary>
        public static Texture2D Picture(Photo photo)
        {
            if (photo == null) return null;
            if (_loaded.TryGetValue(photo.id, out var tex) && tex != null) return tex;
            try
            {
                var path = FileOf(photo.id);
                if (!File.Exists(path)) return null;
                tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!tex.LoadImage(File.ReadAllBytes(path))) return null;
                _loaded[photo.id] = tex;
                return tex;
            }
            catch { return null; }
        }

        // ---------------------------------------------------------------- your album, for friends to see

        /// <summary>A friend's album as it came from them: its pages, and each photo in it (small, with its tag).</summary>
        [Serializable] public class Shared
        {
            public List<HomeLog.AlbumPage> pages = new();
            public List<SharedPhoto> photos = new();
        }

        [Serializable] public class SharedPhoto { public string id, tag, jpg; }   // jpg: base64

        /// <summary>The most album photos shown to friends, and how small (pixels across) each goes.</summary>
        const int ShareMax = 30, ShareSize = 320;

        static string _sharedKey, _sharedJson;

        /// <summary>
        /// Your album as friends will see it when they look round your burrow (RealFriends sends it
        /// up with your island): its pages and small copies of the photos stuck in it, with their
        /// tags. Worked out again only when the album changes.
        /// </summary>
        public static string SharedAlbum()
        {
            var pages = HomeLog.Album;
            var key = new System.Text.StringBuilder();
            foreach (var p in pages) key.Append(p.a).Append(',').Append(p.b).Append(';');
            foreach (var p in D.photos) key.Append(p.id).Append(p.tag).Append('|');
            if (_sharedJson != null && _sharedKey == key.ToString()) return _sharedJson;

            var shared = new Shared();
            var used = new HashSet<string>();
            foreach (var page in pages)
            {
                var keep = new HomeLog.AlbumPage { a = "", b = "" };
                foreach (var side in new[] { true, false })
                {
                    var photo = Find(side ? page.a : page.b);
                    if (photo == null) continue;
                    if (!used.Contains(photo.id))
                    {
                        if (used.Count >= ShareMax) continue;
                        var small = Small(Picture(photo));
                        if (small == null) continue;
                        shared.photos.Add(new SharedPhoto { id = photo.id, tag = photo.tag, jpg = Convert.ToBase64String(small.EncodeToJPG(70)) });
                        UnityEngine.Object.Destroy(small);
                        used.Add(photo.id);
                    }
                    if (side) keep.a = photo.id; else keep.b = photo.id;
                }
                shared.pages.Add(keep);
            }
            _sharedKey = key.ToString();
            return _sharedJson = JsonUtility.ToJson(shared);
        }

        static Texture2D Small(Texture2D big)
        {
            if (big == null) return null;
            var rt = RenderTexture.GetTemporary(ShareSize, ShareSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(big, rt);
            var was = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(ShareSize, ShareSize, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, ShareSize, ShareSize), 0, 0);
            tex.Apply();
            RenderTexture.active = was;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        /// <summary>A friend's album (from <see cref="SharedAlbum"/>), or null if it can't be read.</summary>
        public static Shared ReadShared(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<Shared>(json); }
            catch { return null; }
        }

        /// <summary>One of a friend's photos as a UI sprite.</summary>
        public static Sprite SpriteOf(SharedPhoto photo)
        {
            if (photo == null || string.IsNullOrEmpty(photo.jpg)) return null;
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!tex.LoadImage(Convert.FromBase64String(photo.jpg))) return null;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            catch { return null; }
        }

        /// <summary>The photo as a UI sprite.</summary>
        public static Sprite SpriteOf(Photo photo)
        {
            var tex = Picture(photo);
            return tex == null ? null : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
