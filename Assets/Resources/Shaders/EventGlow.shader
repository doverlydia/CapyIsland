// The light a party or contest gives off on the water round its island (EventGlow.cs).
//
// EventGlow bakes the island's coast once into _MainTex: red is a wide soft glow fading out over
// the sea, green a thin bright line along the beach, blue the stretch of open water off the beach
// where a fishing contest's light plays (zero on the sand and the foam line, so nothing here can
// ever brighten the beach). It is all drawn as light, not paint (premultiplied: Blend One
// OneMinusSrcAlpha), and unlit, so the dark never dims it.
//
// It has two looks:
//  - The ring: the red and green bands as a soft halo round the whole coast, gently rippling,
//    with a broad sweep of light gliding along it now and then. Readable from far away, so it is
//    what the map shows (the map camera sets _EventGlowMap while it draws), and what a party, bugs
//    or birds always show (_Ring), round islands whose land is lit as well.
//  - Up close, for fishing: no band at all, only the water in the blue stretch catching the light,
//    as sun does on a calm sea: little stars that flash on and off at their own spots and paces,
//    thin shifting caustic lines, and the faintest warm lift. It covers nothing (alpha 0), so the
//    water keeps its own colour and only sparkles.
Shader "CapyIsland/EventGlow"
{
    Properties
    {
        _MainTex("Glow band (r wide, g waterline, b open water)", 2D) = "black" {}
        _Core("Waterline colour", Color) = (1, 0.97, 0.86, 1)
        _Gain("Brightness", Range(0, 2)) = 0.85
        _Cover("How much it tints rather than lights", Range(0, 1)) = 0.4
        _Shimmer("Shimmer", Range(0, 1)) = 0.6
        _Ring("Always show the ring", Range(0, 1)) = 1
        _Sea("Sparkle on the water", Range(0, 2)) = 0
        _Grain("Sparkle spacing (world units)", Float) = 0.5

        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex GlowVertex
            #pragma fragment GlowFragment
            #pragma multi_compile_instancing

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float2 world      : TEXCOORD1;
                half4 color       : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _Core;
                half _Gain, _Cover, _Shimmer;
                half _Ring, _Sea;
                float _Grain;
            CBUFFER_END

            // Set by EventGlow for each camera as it draws: 1 for the map's camera, else 0.
            float _EventGlowMap;

            // Value noise: smooth, cheap, no textures.
            float GlowHash(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 23.45);
                return frac(p.x * p.y);
            }

            float GlowNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(GlowHash(i), GlowHash(i + float2(1, 0)), u.x),
                            lerp(GlowHash(i + float2(0, 1)), GlowHash(i + float2(1, 1)), u.x), u.y);
            }

            // One little star per cell of a world-space grid, each at its own spot in its cell
            // (kept off the cell's edges, so its arms never get cut), flashing briefly on its own
            // beat: the sharp top of a slow wave. Its arms grow as it flashes, so it twinkles
            // rather than just blinking.
            float Twinkles(float2 p, float grain, float t, float seed)
            {
                float2 g = p / grain + seed;
                float2 cell = floor(g);
                float2 f = frac(g);
                float h1 = GlowHash(cell);
                float h2 = GlowHash(cell + 19.19);
                float h3 = GlowHash(cell + 47.47);
                float2 d = abs(f - (0.3 + 0.4 * float2(h1, h2)));

                float beat = saturate(sin(t * (0.6 + 1.4 * h3) + h1 * 40.0));
                float flash = beat * beat;
                flash *= flash;
                flash *= flash;   // beat^8: on for a moment, off for a while

                float len = 0.08 + 0.2 * flash;
                float armX = saturate(1.0 - d.x / len) * saturate(1.0 - d.y / 0.05);
                float armY = saturate(1.0 - d.y / len) * saturate(1.0 - d.x / 0.05);
                float heart = saturate(1.0 - length(d) / 0.1);
                float star = max(max(armX, armY), heart * heart * 1.5);
                return flash * star;
            }

            Varyings GlowVertex(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                o.world = TransformObjectToWorld(input.positionOS).xy;
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 GlowFragment(Varyings input) : SV_Target
            {
                half3 band = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                float2 p = input.world;
                float t = _Time.y;
                half a = input.color.a;

                // Light on moving water: two layers of noise drifting different ways.
                float ripple = GlowNoise(p * 0.8 + float2(t * 0.35, -t * 0.22)) * 0.6
                             + GlowNoise(p * 1.9 - float2(t * 0.5, t * 0.3)) * 0.4;
                // A broad sweep of extra light gliding slowly across the island and its coast.
                float sweep = pow(saturate(sin(dot(p, float2(0.11, 0.07)) - t * 0.7)), 6.0);

                // The ring: the far-away look (the map, and every kind but fishing).
                half ring = saturate(max(_Ring, _EventGlowMap));
                half lift = 1.0 + _Shimmer * ((ripple - 0.5) * 0.7 + sweep * 0.8);
                half3 light = (input.color.rgb * band.r + _Core.rgb * band.g * 0.9) * a * _Gain * lift * ring;
                half cover = saturate(band.r * a * _Cover) * ring;

                // Up close: the open water sparkles. Nothing at all where the stretch is empty.
                half sea = band.b * a * _Sea * (1.0 - ring);
                if (sea > 0.002)
                {
                    float stars = Twinkles(p, _Grain, t, 0.0)
                                + Twinkles(p, _Grain * 1.7, t * 0.8, 7.3) * 0.8;
                    // Caustics: where two slowly drifting noises cross the same height, a thin
                    // wavering line of light, like sun through ripples.
                    float n1 = GlowNoise(p * 1.1 + float2(t * 0.21, t * 0.13));
                    float n2 = GlowNoise(p * 1.7 + float2(-t * 0.17, t * 0.19) + 5.2);
                    float caustic = saturate(1.0 - abs(n1 - n2) * 6.0);
                    caustic *= caustic * caustic;
                    light += (_Core.rgb * (stars * 1.1 + caustic * (0.1 + 0.12 * sweep))
                              + input.color.rgb * (0.03 + 0.05 * ripple)) * sea;
                }
                return half4(light, cover);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
