Shader "LightingBlocks/Spectral Motes"
{
 Properties{[HDR]_BaseColor("Color",Color)=(1,0.8,0.4,1) _BaseMap("Texture",2D)="white"{}}
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Blend SrcAlpha One ZWrite Off Cull Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor,_BaseMap_ST;
   CBUFFER_END
   struct A{float4 p:POSITION;float4 c:COLOR;float2 uv:TEXCOORD0;};
   struct V{float4 p:SV_POSITION;float4 c:COLOR;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
   V vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.c=a.c*_BaseColor;o.uv=a.uv;return o;}
   half4 frag(V i):SV_Target{
    float radius=length(i.uv-.5)*2;
    i.c.a*=pow(saturate(1-radius),2);return i.c;
   }
   ENDHLSL
  }
 }
}
