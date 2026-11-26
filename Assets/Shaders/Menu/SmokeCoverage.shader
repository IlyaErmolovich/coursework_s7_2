Shader "Winter Duel/Menu Smoke Coverage"
{
 Properties { [PerRendererData] _MainTex ("Rounded puff mask", 2D) = "white" {} }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
  Cull Off ZWrite Off ZTest Always
  Blend One OneMinusSrcAlpha
  ColorMask A
  Pass {
   Tags { "LightMode"="Universal2D" }
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   struct Attributes {float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR;};
   struct Varyings {float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR;};
   Varyings Vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color;return o;}
   half4 Frag(Varyings i):SV_Target {return half4(0,0,0,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a*i.color.a);}
   ENDHLSL
  }
 }
}
