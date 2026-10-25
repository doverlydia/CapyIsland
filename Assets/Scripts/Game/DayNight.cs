using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CapyIsland.Game
{
    /// <summary>
    /// How the island looks at each hour of <see cref="GameClock"/>: the scene's global 2D light
    /// takes the colour of the hour (pink-gold dawn, clear noon, amber dusk, cool blue night) and
    /// a light colour grade follows it — a touch warmer and softer at the ends of the day, a little
    /// punchier at noon, and a gentle vignette after dark.
    ///
    /// Only the world is tinted: the HUD draws on an overlay canvas after post-processing, so text
    /// stays exactly as readable at midnight as at noon. Cheap effects only (colour adjustments,
    /// white balance, vignette), for phones and WebGL.
    /// </summary>
    public class DayNight : MonoBehaviour
    {
        struct Key
        {
            public float Hour;
            public Color Light;
            public float Exposure, Contrast, Saturation, Temperature, Vignette;
            public Key(float hour, Color light, float exposure, float contrast, float saturation, float temperature, float vignette)
            {
                Hour = hour; Light = light; Exposure = exposure; Contrast = contrast;
                Saturation = saturation; Temperature = temperature; Vignette = vignette;
            }
        }

        // Night is moonlit, not black: the island stays easy to read.
        static readonly Color NightLight = new(0.46f, 0.52f, 0.80f);

        static readonly Key[] Keys =
        {
            new(0f,    NightLight,                      -0.10f,  8f, -12f, -14f, 0.30f),
            new(4.6f,  NightLight,                      -0.10f,  8f, -12f, -14f, 0.30f),
            new(6f,    new Color(1.00f, 0.84f, 0.80f),   0.00f,  5f,   2f,   8f, 0.14f),
            new(8f,    Color.white,                      0.02f,  5f,   3f,   2f, 0.08f),
            new(12.5f, Color.white,                      0.03f,  6f,   4f,   0f, 0.06f),
            new(16f,   Color.white,                      0.02f,  5f,   3f,   3f, 0.08f),
            new(18f,   new Color(1.00f, 0.86f, 0.74f),   0.00f,  5f,   2f,  10f, 0.16f),
            new(19.6f, NightLight,                      -0.10f,  8f, -12f, -14f, 0.30f),
            new(24f,   NightLight,                      -0.10f,  8f, -12f, -14f, 0.30f),
        };

        Light2D _light;
        Volume _volume;
        ColorAdjustments _colour;
        WhiteBalance _balance;
        Vignette _vignette;
        float _nextFind;

        void Start() => BuildVolume();

        void OnDestroy()
        {
            if (_volume != null)
            {
                Destroy(_volume.profile);
                Destroy(_volume.gameObject);
            }
        }

        void BuildVolume()
        {
            var go = new GameObject("DayNightGrade");
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _colour = profile.Add<ColorAdjustments>(true);
            _balance = profile.Add<WhiteBalance>(true);
            _vignette = profile.Add<Vignette>(true);
            _vignette.color.Override(new Color(0.08f, 0.10f, 0.22f));
            _vignette.smoothness.Override(0.5f);
            _volume.profile = profile;
        }

        void LateUpdate()
        {
            // The camera and the scene light come and go with island rebuilds and visits.
            if (Time.unscaledTime >= _nextFind)
            {
                _nextFind = Time.unscaledTime + 2f;
                if (_light == null)
                    foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                        if (l.lightType == Light2D.LightType.Global) { _light = l; break; }
                var cam = Camera.main;
                if (cam != null)
                {
                    var data = cam.GetUniversalAdditionalCameraData();
                    if (data != null && !data.renderPostProcessing) data.renderPostProcessing = true;
                }
            }

            var k = Sample(GameClock.Hour);
            // Rain: a cooler, greyer, softer light, like a real wet afternoon.
            float rain = Weather.Rain;
            var light = Color.Lerp(k.Light, k.Light * new Color(0.74f, 0.78f, 0.86f), rain);
            light *= Skins.Tint;   // a holiday skin's own light (white when none is on)
            if (_light != null) _light.color = light;
            if (_colour == null) return;
            _colour.postExposure.Override(k.Exposure - 0.06f * rain);
            _colour.contrast.Override(k.Contrast - 3f * rain);
            _colour.saturation.Override(k.Saturation - 16f * rain);
            _balance.temperature.Override(k.Temperature - 6f * rain);
            _vignette.intensity.Override(k.Vignette + 0.06f * rain);
        }

        static Key Sample(float hour)
        {
            for (int i = 0; i < Keys.Length - 1; i++)
            {
                var a = Keys[i];
                var b = Keys[i + 1];
                if (hour < a.Hour || hour > b.Hour) continue;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a.Hour, b.Hour, hour));
                return new Key(hour,
                    Color.Lerp(a.Light, b.Light, t),
                    Mathf.Lerp(a.Exposure, b.Exposure, t),
                    Mathf.Lerp(a.Contrast, b.Contrast, t),
                    Mathf.Lerp(a.Saturation, b.Saturation, t),
                    Mathf.Lerp(a.Temperature, b.Temperature, t),
                    Mathf.Lerp(a.Vignette, b.Vignette, t));
            }
            return Keys[0];
        }
    }
}
