Shader "Universal Render Pipeline/Custom/HologramUI"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _GlowColor("Glow Color", Color) = (0, 1, 1, 1)
        _ScanlineSpeed("Scanline Speed", Float) = 1.0
        _FlickerSpeed("Flicker Speed", Float) = 0.5
        _Alpha("Alpha", Range(0, 1)) = 0.8
        _GlitchIntensity("Glitch Intensity", Range(0, 1)) = 0.1
        _GlitchFrequency("Glitch Frequency", Float) = 1.0
        _RGBSplit("RGB Split", Range(0, 0.1)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float _ScanlineSpeed;
                float _FlickerSpeed;
                float _Alpha;
                float _GlitchIntensity;
                float _GlitchFrequency;
                float _RGBSplit;
            CBUFFER_END

            float rand(float2 seed)
            {
                return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                
                // Spazmodik Glitch Hesaplaması
                float glitchTime = floor(_Time.y * _GlitchFrequency);
                float glitchChance = rand(float2(glitchTime, 1.0));
                float horizontalOffset = 0;
                
                if (glitchChance < 0.3) // %30 ihtimalle glitch tetiklenir
                {
                    // Katmanlı (slice) yırtılma etkisi
                    float slice = floor(uv.y * 15.0);
                    horizontalOffset = (rand(float2(glitchTime, slice)) - 0.5) * _GlitchIntensity;
                }
                
                // Chromatic Aberration (RGB Split) ve Glitch birleşimi
                half r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(horizontalOffset + _RGBSplit, 0)).r;
                half g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(horizontalOffset, 0)).g;
                half b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(horizontalOffset - _RGBSplit, 0)).b;
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(horizontalOffset, 0)).a;

                // Tarama çizgisi efekti (Scanlines)
                float scanline = sin(uv.y * 100.0 + _Time.y * _ScanlineSpeed) * 0.1 + 0.9;
                
                // Titreme efekti (Flicker)
                float flicker = sin(_Time.y * _FlickerSpeed) * 0.05 + 0.95;
                
                half3 finalColor = half3(r, g, b) * _GlowColor.rgb * scanline * flicker;
                float finalAlpha = a * _Alpha * flicker;

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
