// The sea around the islands: one big lit sprite, so the day/night light tints it like
// everything else. On top of the flat blue it adds three soft things, all in world space so
// they stay put as the camera moves:
//   * colour drifting between a touch lighter and a touch deeper (two slow layers of noise)
//   * pale streaks of light on small waves, fading in and out
//   * foam at the shore: a soft rim, and thin lines rolling in toward the beach
// The shore comes from _ShoreTex, a small map of distance to the nearest island baked at
// start (Sea.cs). Kept gentle on purpose: a cozy painted sea, not a realistic ocean.
Shader "CapyIsland/Sea"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _ShoreTex("Shore distance", 2D) = "white" {}
        _ShoreRect("Shore rect (x, y, w, h)", Vector) = (0, 0, 1, 1)
        _Depth("Deeper blue offshore", Range(0, 1)) = 0.8
        _Tone("Light and deep drift", Range(0, 0.3)) = 0.08
        _Glints("Wave highlights", Range(0, 1)) = 0.6
        _Foam("Shore foam", Range(0, 1)) = 0.75
        _Speed("Speed", Range(0, 3)) = 1

        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex SeaVertex
            #pragma fragment SeaFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color        : COLOR;
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color        : COLOR;
                float2 sea         : TEXCOORD6;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            TEXTURE2D(_ShoreTex);
            SAMPLER(sampler_ShoreTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _ShoreRect;
                half _Depth, _Tone, _Glints, _Foam, _Speed;
            CBUFFER_END

            // Value noise: smooth, cheap, no textures.
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            float Fbm(float2 p)
            {
                return Noise(p) * 0.6 + Noise(p * 2.03 + 17.1) * 0.3 + Noise(p * 4.1 + 31.7) * 0.1;
            }

            Varyings SeaVertex(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                o.sea = TransformObjectToWorld(input.positionOS).xy;
                return o;
            }

            half4 SeaFragment(Varyings input) : SV_Target
            {
                float2 p = input.sea;
                float t = _Time.y * _Speed;
                half3 col = input.color.rgb;

                // How far from the nearest island: r for the foam (a few units), g for depth (further).
                float2 suv = (p - _ShoreRect.xy) / _ShoreRect.zw;
                float2 shore = SAMPLE_TEXTURE2D(_ShoreTex, sampler_ShoreTex, suv).rg;
                float d = shore.r;

                // Open water is a deeper blue than the shallows by the beach, so the light on it shows.
                float deep = smoothstep(0.05, 0.9, shore.g);
                col = lerp(col, col * half3(0.72, 0.84, 0.95), deep * _Depth);

                // Lighter and deeper patches, drifting two ways at once.
                float a = Fbm(p * 0.07 + float2(t * 0.018, t * 0.011));
                float b = Fbm(p * 0.11 + float2(-t * 0.014, t * 0.019) + 5.3);
                col *= 1.0 + (a - b) * _Tone * 2.0;

                // Light on small waves: soft streaks, stretched sideways, that come and go, each with
                // a faint darker trough just below it.
                float2 q = float2(p.x * 0.45 + t * 0.12, p.y * 1.7 + sin(p.x * 0.25 + t * 0.4) * 0.5);
                float n = Noise(q);
                float crest = smoothstep(0.66, 0.84, n) * smoothstep(0.94, 0.82, n);
                float trough = smoothstep(0.66, 0.84, Noise(q + float2(0.0, 0.35))) * (1.0 - crest);
                float comeAndGo = smoothstep(0.38, 0.7, Noise(p * 0.05 + float2(t * 0.03, -t * 0.02) + 11.0));
                float open = 0.35 + 0.65 * deep;   // calmer in the shallows
                col *= 1.0 - trough * comeAndGo * open * _Glints * 0.12;
                col = lerp(col, half3(1, 1, 1), crest * comeAndGo * open * _Glints * 0.75);

                // Foam (d: 0 at the shore, 1 a few units out).
                float breakup = smoothstep(0.25, 0.65, Noise(p * 0.9 + float2(t * 0.15, 0)));
                float rim = (1.0 - smoothstep(0.02, 0.12, d)) * (0.55 + 0.45 * breakup);
                float roll = frac(d * 2.6 + t * 0.22);                       // lines rolling in
                float line_ = smoothstep(0.0, 0.05, roll) * (1.0 - smoothstep(0.05, 0.14, roll));
                line_ *= (1.0 - smoothstep(0.08, 0.55, d)) * breakup;        // fading as they come
                col = lerp(col, half3(1, 1, 1), saturate(rim * 0.8 + line_ * 0.6) * _Foam);

                return CommonLitFragment(input, half4(col, input.color.a));
            }
            ENDHLSL
        }

        Pass
        {
            // Without the 2D renderer (scene view, previews): the same sea, unlit.
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex V
            #pragma fragment F

            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _ShoreRect;
                half _Depth, _Tone, _Glints, _Foam, _Speed;
            CBUFFER_END

            Varyings V(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.color = input.color * _Color;
                return o;
            }

            half4 F(Varyings input) : SV_Target { return input.color; }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}
