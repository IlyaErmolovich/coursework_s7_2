Shader "Winter Duel/Evening Sky"
{
    Properties
    {
        [Header(Vertical sky gradient)]
        _TopColor ("Upper evening blue", Color) = (0.34, 0.40, 0.52, 1)
        _MiddleColor ("Middle blue gray", Color) = (0.55, 0.59, 0.67, 1)
        _HorizonColor ("Horizon gray lavender", Color) = (0.72, 0.70, 0.76, 1)
        _HorizonHeight ("Horizon height (world Y)", Float) = -1
        _MiddleHeight ("Middle height (world Y)", Float) = 2.5
        _TopHeight ("Top height (world Y)", Float) = 6

        [Header(Static haze behind the forest)]
        _HazeColor ("Haze color", Color) = (0.79, 0.77, 0.83, 1)
        _HazeOpacity ("Haze opacity", Range(0, 0.3)) = 0.065
        _HazeHeight ("Main haze height (world Y)", Float) = 0.25
        _HazeSoftness ("Haze edge softness", Range(0.1, 2)) = 0.45
        _HazeSpacing ("Haze band spacing", Range(0.4, 3)) = 1.2
        _HazeIrregularity ("Haze edge irregularity", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Blend One Zero
        Cull Off
        ZWrite Off
        Pass
        {
            Name "EveningSky"
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor, _MiddleColor, _HorizonColor, _HazeColor;
                float _HorizonHeight, _MiddleHeight, _TopHeight;
                float _HazeOpacity, _HazeHeight, _HazeSoftness, _HazeSpacing, _HazeIrregularity;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 positionWS : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(world);
                output.positionWS = world.xy;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS;
                float lower = smoothstep(_HorizonHeight, max(_HorizonHeight + 0.01, _MiddleHeight), p.y);
                float upper = smoothstep(_MiddleHeight, max(_MiddleHeight + 0.01, _TopHeight), p.y);
                half3 sky = lerp(_HorizonColor.rgb, _MiddleColor.rgb, lower);
                sky = lerp(sky, _TopColor.rgb, upper);

                // Fixed long waves: soft, uneven bands with no time-dependent animation.
                float bend = _HazeIrregularity * (0.65 * sin(p.x * 0.29 + 1.1) + 0.35 * sin(p.x * 0.61));
                float softness = max(0.05, _HazeSoftness);
                float d0 = (p.y - _HazeHeight - bend) / softness;
                float d1 = (p.y - _HazeHeight - _HazeSpacing - bend * 0.6 - 0.12 * sin(p.x * 0.19)) / (softness * 0.78);
                float d2 = (p.y - _HazeHeight - _HazeSpacing * 2.15 + bend * 0.4) / (softness * 0.55);
                float bands = exp(-d0*d0) + 0.48 * exp(-d1*d1) + 0.23 * exp(-d2*d2);
                float patches = 0.74 + 0.26 * sin(p.x * 0.14 + 0.6);
                float haze = saturate(bands * patches * _HazeOpacity);
                return half4(lerp(sky, _HazeColor.rgb, haze), 1);
            }
            ENDHLSL
        }
    }
}
