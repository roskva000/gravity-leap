Shader "Universal Render Pipeline/Custom/RustAndNeon"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (0.5, 0.5, 0.5, 1)
        _MainTex("Base Map", 2D) = "white" {}
        _RustTex("Rust Texture", 2D) = "white" {}
        _NeonColor("Neon Glow Color", Color) = (0, 1, 0, 1)
        _NeonPower("Neon Power", Range(0, 10)) = 1.0
        _RustAmount("Rust Amount", Range(0, 1)) = 0.5
        _BumpMap("Normal Map", 2D) = "bump" {}
        _PulseSpeed("Pulse Speed", Float) = 2.0
        _PulseIntensity("Pulse Intensity", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_RustTex);
            SAMPLER(sampler_RustTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _NeonColor;
                float _NeonPower;
                float _RustAmount;
                float _PulseSpeed;
                float _PulseIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 rustTex = SAMPLE_TEXTURE2D(_RustTex, sampler_RustTex, input.uv);

                // Pas karıştırma mantığı
                half3 finalColor = lerp(baseTex.rgb * _BaseColor.rgb, rustTex.rgb, _RustAmount);

                // Basit Neon parlaması (Kenar aydınlatması / Fresnel benzeri)
                float fresnel = 1.0 - saturate(dot(normalize(input.normalWS), float3(0, 0, 1)));
                
                // Nabız (Pulse) hesaplaması
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseIntensity + (1.0 - _PulseIntensity);
                half3 glow = _NeonColor.rgb * pow(fresnel, 3.0) * _NeonPower * pulse;

                return half4(finalColor + glow, 1.0);
            }
            ENDHLSL
        }
    }
}
