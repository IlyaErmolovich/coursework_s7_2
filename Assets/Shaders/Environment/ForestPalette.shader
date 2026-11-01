Shader "Winter Duel/Forest Evening Palette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Original forest PNG", 2D) = "white" {}
        _ShadowColor ("Dark forest row", Color) = (0.23, 0.27, 0.35, 1)
        _LightColor ("Light forest row", Color) = (0.53, 0.56, 0.65, 1)
        _LuminanceMin ("Source shadow level (linear)", Range(0, 0.3)) = 0.055
        _LuminanceMax ("Source light level (linear)", Range(0.1, 1)) = 0.32
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Name "ForestPalette"
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _ShadowColor, _LightColor;
                float _LuminanceMin, _LuminanceMax;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float luminance = dot(source.rgb, float3(0.2126, 0.7152, 0.0722));
                float tone = saturate((luminance - _LuminanceMin) / max(0.001, _LuminanceMax - _LuminanceMin));
                half3 color = lerp(_ShadowColor.rgb, _LightColor.rgb, tone);
                return half4(color * input.color.rgb, source.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
