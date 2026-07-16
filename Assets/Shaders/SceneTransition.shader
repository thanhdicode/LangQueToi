// ──────────────────────────────────────────────
// TheSprouty | Shaders/SceneTransition.shader
// Full-screen overlay shader — Snake wipe + Circle wipe.
// OUT: _LinearProgress 0→1 (covers screen)
// IN:  _LinearProgress 1→0 (reveals screen)
// ──────────────────────────────────────────────
Shader "TheSprouty/SceneTransition"
{
    Properties
    {
        _MainTex        ("Texture",          2D)          = "white" {}
        _LinearProgress ("Linear Progress",  Range(0, 1)) = 0
        [HDR] _Color    ("Fill Color",       Color)       = (1, 0.973, 0.933, 1)
        [Enum(Snake,0,Circle,1)]
        _TransitionType ("Transition Type",  Float)       = 0

        [Header(Snake)]
        _StripCount     ("Strip Count",      Float)       = 5
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Overlay+1"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull   Off
        ZTest  Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _LinearProgress;
                float4 _Color;
                float  _TransitionType;
                float  _StripCount;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv     = IN.uv;
                float  filled = 0.0;

                if (_TransitionType < 0.5)
                {
                    // ── Snake wipe ────────────────────────────────────
                    // Strips cascade top-to-bottom using linear time
                    // so every strip takes identical real duration.
                    // Odd strips: left→right. Even strips: right→left.
                    float stripIndex    = floor(uv.y * _StripCount);
                    float totalProgress = _LinearProgress * (_StripCount + 1.0);
                    float localProgress = saturate(totalProgress - stripIndex);
                    float x             = (fmod(stripIndex, 2.0) < 1.0) ? uv.x : (1.0 - uv.x);
                    filled = x < localProgress ? 1.0 : 0.0;
                }
                else
                {
                    // ── Circle wipe ───────────────────────────────────
                    // A circle grows from center (OUT 0→1) or shrinks back (IN 1→0).
                    // Aspect-ratio corrected so it renders as a true circle, not oval.
                    // Normalized so radius = 1 exactly reaches the screen corners.
                    float  aspect   = _ScreenParams.x / _ScreenParams.y;
                    float2 centered = uv - float2(0.5, 0.5);
                    centered.x     *= aspect; // scale x to match physical screen units
                    float  maxR     = 0.5 * sqrt(aspect * aspect + 1.0); // corner distance
                    float  r        = length(centered) / maxR; // 0 (center) → 1 (corners)
                    filled = r < _LinearProgress ? 1.0 : 0.0;
                }

                return half4(_Color.rgb, filled * _Color.a);
            }
            ENDHLSL
        }
    }
}
