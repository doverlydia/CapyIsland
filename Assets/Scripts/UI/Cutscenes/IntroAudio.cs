using DG.Tweening;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// The opening's own soundtrack: a music cue per scene (Resources/Music/Intro), crossfading
    /// from one to the next, and its sound effects (Resources/Sfx/Intro_*). Runs on real time, so
    /// it plays while the game is paused behind the movie. While it is active the island's own
    /// music steps aside (MusicPlayer listens).
    /// </summary>
    public static class IntroAudio
    {
        const float MusicVolume = 0.42f;

        static GameObject _host;
        static AudioSource _a, _b;   // two sources, so one fades out as the next fades in
        static readonly System.Collections.Generic.Dictionary<string, AudioSource> _airs = new();   // looping beds under the music (wind, fire)
        static bool _useA;

        public static bool Active { get; private set; }

        static void Ensure()
        {
            if (_host != null) return;
            _host = new GameObject("[IntroAudio]");
            Object.DontDestroyOnLoad(_host);
            _a = _host.AddComponent<AudioSource>();
            _b = _host.AddComponent<AudioSource>();
            foreach (var s in new[] { _a, _b }) { s.loop = true; s.playOnAwake = false; s.volume = 0f; s.ignoreListenerPause = true; }
        }

        /// <summary>Crossfades to the music cue <paramref name="cue"/> (Resources/Music/Intro/&lt;cue&gt;).</summary>
        public static void Play(string cue, float fade = 1.5f)
        {
            var clip = Resources.Load<AudioClip>("Music/Intro/" + cue);
            Ensure();
            Active = true;
            var from = _useA ? _a : _b;
            var to = _useA ? _b : _a;
            _useA = !_useA;
            from.DOKill();
            from.DOFade(0f, fade).SetUpdate(true).OnComplete(from.Stop);
            if (clip == null) return;
            to.DOKill();
            to.clip = clip;
            to.volume = 0f;
            to.Play();
            to.DOFade(MusicVolume * Game.AudioMix.Music, fade).SetUpdate(true);
        }

        /// <summary>Fades the intro's music away and hands the island its own music back.</summary>
        public static void Stop(float fade = 1.5f)
        {
            if (_host == null) { Active = false; return; }
            foreach (var s in new System.Collections.Generic.List<AudioSource>(_airs.Values) { _a, _b })
            {
                s.DOKill();
                s.DOFade(0f, fade).SetUpdate(true).OnComplete(s.Stop);
            }
            DOVirtual.DelayedCall(fade * 0.5f, () => Active = false, ignoreTimeScale: true);
        }

        /// <summary>
        /// The music falls silent but the opening keeps the island's own music away: a held
        /// breath, for the moment that should land in quiet.
        /// </summary>
        public static void Hush(float fade = 1.5f)
        {
            if (_host == null) return;
            foreach (var s in new[] { _a, _b })
            {
                s.DOKill();
                s.DOFade(0f, fade).SetUpdate(true).OnComplete(s.Stop);
            }
        }

        /// <summary>
        /// A looping bed of weather (Resources/Sfx/&lt;name&gt;, e.g. Amb_Wind) under the music, faded
        /// to <paramref name="volume"/>; 0 fades it out.
        /// </summary>
        public static void Air(string name, float volume, float fade = 2f)
        {
            Ensure();
            if (!_airs.TryGetValue(name, out var air) || air == null)
            {
                if (volume <= 0f) return;
                var clip = Resources.Load<AudioClip>("Sfx/" + name);
                if (clip == null) return;
                air = _airs[name] = _host.AddComponent<AudioSource>();
                air.clip = clip; air.loop = true; air.playOnAwake = false; air.volume = 0f; air.ignoreListenerPause = true;
            }
            air.DOKill();
            if (volume <= 0f) { air.DOFade(0f, fade).SetUpdate(true).OnComplete(air.Stop); return; }
            if (!air.isPlaying) { air.volume = 0f; air.Play(); }
            air.DOFade(volume * Game.AudioMix.Effects, fade).SetUpdate(true);
        }

        /// <summary>One of the opening's sound effects (Resources/Sfx/Intro_&lt;name&gt;).</summary>
        public static void Sfx(string name, float volume = 1f)
        {
            var clip = Resources.Load<AudioClip>("Sfx/Intro_" + name);
            if (clip == null) return;
            Ensure();
            _a.PlayOneShot(clip, volume * Game.AudioMix.Effects);
        }
    }
}
