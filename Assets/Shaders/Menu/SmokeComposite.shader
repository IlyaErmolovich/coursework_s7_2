Shader "Winter Duel/Menu Smoke Composite"
{
 Properties { _MainTex("Unified coverage",2D)="black"{} _SmokeColor("Smoke color and opacity",Color)=(.77,.69,.84,.60) _FadeStart("Tip fade start",Range(0,1))=.52 _FadeEnd("Tip fade end",Range(0,1))=.78 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
  Cull Off ZWrite Off
  Blend SrcAlpha OneMinusSrcAlpha
  Pass {
   Tags { "LightMode"="Universal2D" }
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
    half4 _SmokeColor;
    float _FadeStart, _FadeEnd;
   CBUFFER_END
   struct Attributes{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   Varyings Vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;return o;}
   half4 Frag(Varyings i):SV_Target{return half4(_SmokeColor.rgb,_SmokeColor.a*(1-smoothstep(_FadeStart,max(_FadeStart+.001,_FadeEnd),i.uv.y))*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a);}
   ENDHLSL
  }
 }
}
