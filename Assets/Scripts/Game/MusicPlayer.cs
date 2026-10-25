using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The background music, composed in Capy Music Studio (Tools/MusicStudio): a morning tune, an
    /// afternoon tune and a lullaby for the night. All three loop together, and the island's clock
    /// slowly crossfades from one to the next — morning into afternoon around midday, afternoon
    /// into night through dusk, night back into morning at dawn — so a change is never sudden.
    /// Fades in when the island loads, and follows the Music slider in Settings.
    ///
    /// A holiday skin may bring its own tune (<see cref="Skins.Music"/>): it takes the morning's and
    /// the afternoon's place, crossfading in when the skin goes on and out when it comes off; the
    /// night keeps its lullaby.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        const float Volume = 0.3f;

        AudioSource _morning, _afternoon, _night, _holiday;
        float _fadeIn, _duck = 1f, _holidayMix;

        void Start()
        {
            _morning = Track("Music/music_day");
            _afternoon = Track("Music/music_afternoon");
            _night = Track("Music/music_night");
        }

        AudioSource Track(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null) return null;
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.volume = 0f;
            s.playOnAwake = false;
            s.Play();
            return s;
        }

        /// <summary>0 by day … 1 at night, easing through dusk and dawn.</summary>
        static float Nightness()
        {
            float h = GameClock.Hour;
            if (h >= 19.5f || h < 4.6f) return 1f;
            if (h >= 17f) return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(17f, 19.5f, h));
            if (h < 7f) return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(7f, 4.6f, h));
            return 0f;
        }

        /// <summary>How much of the daytime music is the afternoon's: 0 in the morning, 1 after midday.</summary>
        static float Afternoonness()
        {
            float h = GameClock.Hour;
            if (h < 7f) return 0f;   // dawn: the night hands over to the morning
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(11.5f, 13.5f, h));
        }

        void Update()
        {
            _fadeIn = Mathf.MoveTowards(_fadeIn, 1f, Time.unscaledDeltaTime / 4f);   // a few seconds to swell in
            // The opening has its own score: the island's music waits quietly until it ends.
            _duck = Mathf.MoveTowards(_duck, UI.IntroAudio.Active ? 0f : 1f, Time.unscaledDeltaTime / 2f);
            float n = Nightness(), a = Afternoonness(), master = Volume * AudioMix.Music * _fadeIn * _duck;
            // without an afternoon tune, the morning one plays all day
            if (_afternoon == null) a = 0f;
            float h = Holiday();
            if (_morning != null) _morning.volume = master * (1f - n) * (1f - a) * (1f - h);
            if (_afternoon != null) _afternoon.volume = master * (1f - n) * a * (1f - h);
            if (_holiday != null) _holiday.volume = master * (1f - n) * h;
            if (_night != null) _night.volume = master * n * 0.85f;
        }

        /// <summary>How much of the daytime music is the holiday skin's tune, fading over a few seconds.</summary>
        float Holiday()
        {
            var clip = Skins.Music;
            if (clip != null && (_holiday == null || _holiday.clip != clip) && _holidayMix <= 0f)
            {
                if (_holiday == null)
                {
                    _holiday = gameObject.AddComponent<AudioSource>();
                    _holiday.loop = true;
                    _holiday.playOnAwake = false;
                }
                _holiday.clip = clip;
                _holiday.volume = 0f;
                _holiday.Play();
            }
            // Out first (the skin came off, or another tune is on its way), then in.
            bool on = clip != null && _holiday != null && _holiday.clip == clip;
            _holidayMix = Mathf.MoveTowards(_holidayMix, on ? 1f : 0f, Time.unscaledDeltaTime / 3f);
            if (_holidayMix <= 0f && !on && _holiday != null && _holiday.clip != null) { _holiday.Stop(); _holiday.clip = null; }
            return _holidayMix;
        }
    }
}
