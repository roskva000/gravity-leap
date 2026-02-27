Shader "Universal Render Pipeline/Custom/HologramUI"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _GlowColor("Glow Color", Color) = (0, 1, 1, 1)
        _ScanlineSpeed("Scanline Speed", Float) = 1.0
        _FlickerSpeed("Flicker Speed", Float) = 0.5
        _Alpha("Alpha", Range(0, 1)) = 0.8
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
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Tarama çizgisi efekti (Scanlines)
                float scanline = sin(input.uv.y * 100.0 + _Time.y * _ScanlineSpeed) * 0.1 + 0.9;
                
                // Titreme efekti (Flicker)
                float flicker = sin(_Time.y * _FlickerSpeed) * 0.05 + 0.95;
                
                half3 finalColor = col.rgb * _GlowColor.rgb * scanline * flicker;
                float finalAlpha = col.a * _Alpha * flicker;

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
