using UnityEngine;

namespace CapyIsland.Game
{
    public enum SfxKind
    {
        Chop, Mine, Nibble, Rustle, Plop, Pop, Chest, Coin, Buy, Heart, Bridge, Boom, Fuse, Fell, Tap,
    }

    /// <summary>
    /// Little sound effects, played through a small pool of sources. Like the original's point
    /// sounds (Sound/Attenuation.PlayPointSound), a sound in the world fades with its distance from
    /// the middle of the screen, so a far-off axe is a whisper and one beside you is crisp. Each
    /// play gets a touch of random pitch so repeats never sound mechanical.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        static Sfx _instance;

        [SerializeField] AudioClip[] clips = new AudioClip[15];   // indexed by SfxKind
        [SerializeField, Range(0f, 1f)] float masterVolume = 0.6f;
        [Tooltip("Unity units from the camera at which a world sound fades to nothing.")]
        [SerializeField] float hearingRange = 22f;

        const int Voices = 10;
        AudioSource[] _sources;
        int _next;
        readonly float[] _lastPlayed = new float[16];

        void Awake()
        {
            _instance = this;
            _sources = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                _sources[i] = s;
            }
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public void SetClips(AudioClip[] list) => clips = list;

        /// <summary>A sound with no place in the world: UI, rewards reaching the HUD.</summary>
        public static void Play(SfxKind kind, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (_instance != null) _instance.PlayInternal(kind, volume, pitchJitter);
        }

        /// <summary>A sound somewhere on the island, quieter the further it is from the view.</summary>
        public static void PlayAt(SfxKind kind, Vector2 at, float volume = 1f, float pitchJitter = 0.08f)
        {
            if (_instance == null) return;
            var cam = Camera.main;
            float fade = 1f;
            if (cam != null)
            {
                float d = Vector2.Distance(cam.transform.position, at);
                fade = Mathf.Clamp01(1f - d / _instance.hearingRange);
                fade *= fade;
            }
            if (fade <= 0.02f) return;
            _instance.PlayInternal(kind, volume * fade, pitchJitter);
        }

        void PlayInternal(SfxKind kind, float volume, float pitchJitter)
        {
            int k = (int)kind;
            if (clips == null || k >= clips.Length || clips[k] == null) return;
            // The same sound twice within 40 ms is one sound: bursts of pings stay pleasant.
            if (Time.unscaledTime - _lastPlayed[k] < 0.04f) return;
            _lastPlayed[k] = Time.unscaledTime;

            var s = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            s.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            s.PlayOneShot(clips[k], Mathf.Clamp01(volume) * masterVolume);
        }
    }
}
