Shader "LightingBlocks/Spectral Lit"
{
 Properties {
 _BaseMap("Color",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1)
 _NormalMap("Normal",2D)="bump"{} _NormalStrength("Normal strength",Float)=.25
 _MaskMap("AO (G), Smoothness (A)",2D)="white"{}
 _SoilMap("Soil",2D)="white"{} _SoilNormal("Soil normal",2D)="bump"{} _SoilMask("Soil mask",2D)="white"{}
 _MossMap("Moss",2D)="white"{} _MossNormal("Growth normal",2D)="bump"{} _GrassMap("Grass ground",2D)="white"{} _HabitatMap("Habitat weights",2D)="white"{}
 _Terrain("Terrain blend",Float)=0 _Triplanar("Cliff triplanar",Float)=0 _VertexTint("Vertex tint",Float)=0
 _SoftFill("Soft vegetation fill",Float)=0
 _Smoothness("Smoothness",Float)=.3 _AO("Occlusion",Range(0,1))=.4 _Cutoff("Alpha cutoff",Range(0,1))=0 _Cull("Culling",Float)=2
 [HDR]_EmissionColor("Emission",Color)=(0,0,0,1) [HDR]_RimColor("Spectral edge",Color)=(0,0,0,1)
 _RimPower("Edge power",Float)=3 _Wind("Wind",Float)=0 _Charge("Charge",Float)=1 _EmissionShape("Shaped emission",Range(0,1))=0 [HideInInspector] _LightingWisdomVisible("Wisdom visibility",Float)=0
 }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
 Cull [_Cull]
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);TEXTURE2D(_MaskMap);SAMPLER(sampler_MaskMap);
 TEXTURE2D(_SoilMap);SAMPLER(sampler_SoilMap);TEXTURE2D(_SoilNormal);SAMPLER(sampler_SoilNormal);TEXTURE2D(_SoilMask);SAMPLER(sampler_SoilMask);
 TEXTURE2D(_MossMap);SAMPLER(sampler_MossMap);TEXTURE2D(_GrassMap);SAMPLER(sampler_GrassMap);TEXTURE2D(_HabitatMap);SAMPLER(sampler_HabitatMap);
 TEXTURE2D(_MossNormal);SAMPLER(sampler_MossNormal);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseMap_ST,_BaseColor,_EmissionColor,_RimColor;
 float _SoftFill,_NormalStrength,_Terrain,_Triplanar,_Smoothness,_AO,_Cutoff,_Cull,_RimPower,_Wind,_Charge,_EmissionShape,_VertexTint,_LightingWisdomVisible;
 CBUFFER_END
 float3 _ForestPlayer,_ForestWind;float _ForestTime;
 struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half4 tangentWS:TEXCOORD2;float2 uv:TEXCOORD3;half fog:TEXCOORD4;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 V Vert(A a){V o=(V)0;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);
 float3 p=a.positionOS.xyz;float3 world=TransformObjectToWorld(p);float height=saturate(p.y*.8);
 float gust=sin(_ForestTime*.55+world.x*.14+world.z*.11)+.3*sin(_ForestTime*1.1+world.x*.5);
 float2 away=world.xz-_ForestPlayer.xz;float dist=length(away);float2 bend=away/max(dist,.01)*saturate(1-dist/1.1)*height;
 float2 windDir=_ForestWind.xz;windDir=dot(windDir,windDir)>1e-6?normalize(windDir):float2(.7,.4);
 p.xz+=(windDir*gust*height+bend*1.5)*_Wind;
 VertexPositionInputs vp=GetVertexPositionInputs(p);VertexNormalInputs vn=GetVertexNormalInputs(a.normalOS,a.tangentOS);
 o.positionCS=vp.positionCS;o.positionWS=vp.positionWS;o.normalWS=vn.normalWS;o.tangentWS=half4(vn.tangentWS,a.tangentOS.w*GetOddNegativeScale());o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.fog=ComputeFogFactor(vp.positionCS.z);o.color=a.color;return o;}
 void Cut(V i){clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a-_Cutoff);}
 half4 Frag(V i):SV_Target{
 UNITY_SETUP_INSTANCE_ID(i);float2 uv=_Terrain>.5?i.positionWS.xz/3:i.uv;half3 n=normalize(i.normalWS);
 half4 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv),mask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,uv);
 half3 normal=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,uv),_NormalStrength);
 if(_Terrain>.5){float4 weights=SAMPLE_TEXTURE2D(_HabitatMap,sampler_HabitatMap,(i.positionWS.xz+float2(64,64))/float2(128,256));
 half4 soil=SAMPLE_TEXTURE2D(_SoilMap,sampler_SoilMap,uv),moss=SAMPLE_TEXTURE2D(_MossMap,sampler_MossMap,uv*.8),grass=SAMPLE_TEXTURE2D(_GrassMap,sampler_GrassMap,uv);
 color=color*weights.a+soil*weights.b+moss*weights.r+grass*weights.g;
 mask=mask*weights.a+SAMPLE_TEXTURE2D(_SoilMask,sampler_SoilMask,uv)*weights.b+half4(1,.85,0,.18)*(weights.r+weights.g);normal=normalize(normal*weights.a+UnpackNormalScale(SAMPLE_TEXTURE2D(_SoilNormal,sampler_SoilNormal,uv),_NormalStrength)*weights.b+UnpackNormalScale(SAMPLE_TEXTURE2D(_MossNormal,sampler_MossNormal,uv),_NormalStrength)*(weights.r+weights.g));}
 if(_Triplanar>.5){float3 w=pow(abs(n),4);w/=max(dot(w,1),.001);color=SAMPLE_TEXTURE2D(_SoilMap,sampler_SoilMap,i.positionWS.zy/3)*w.x+SAMPLE_TEXTURE2D(_SoilMap,sampler_SoilMap,i.positionWS.xz/3)*w.y+SAMPLE_TEXTURE2D(_SoilMap,sampler_SoilMap,i.positionWS.xy/3)*w.z;normal=half3(0,0,1);}
 clip(color.a-_Cutoff);color.rgb*=lerp(half3(1,1,1),i.color.rgb,_VertexTint);
 half3 tangent=normalize(i.tangentWS.xyz+half3(.00001,0,0));half3 bitangent=cross(n,tangent)*i.tangentWS.w;n=normalize(mul(normal,half3x3(tangent,bitangent,n)));
 half3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);half rim=pow(1-saturate(dot(n,view)),_RimPower);
 InputData input=(InputData)0;input.positionWS=i.positionWS;input.normalWS=n;input.viewDirectionWS=view;input.shadowCoord=TransformWorldToShadowCoord(i.positionWS);input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);input.shadowMask=half4(1,1,1,1);
 SurfaceData surface=(SurfaceData)0;surface.albedo=color.rgb*_BaseColor.rgb;surface.alpha=1;surface.smoothness=mask.a*_Smoothness;surface.occlusion=lerp(1,mask.g,_AO);surface.normalTS=normal;
 surface.emission=surface.albedo*_SoftFill+(_EmissionColor.rgb*lerp(1,.25+.75*pow(saturate(dot(n,view)),1.5),_EmissionShape)+_RimColor.rgb*rim)*_Charge;surface.emission+=_LightingWisdomVisible*half3(.55,.42,1.05);
 half4 result=UniversalFragmentPBR(input,surface);result.rgb=MixFog(result.rgb,i.fog);return result;}
 half4 Depth(V i):SV_Target{Cut(i);return 0;}
 half4 Normals(V i):SV_Target{Cut(i);return half4(normalize(i.normalWS),0);}
 ENDHLSL
 Pass {Name "ForwardLit" Tags {"LightMode"="UniversalForwardOnly"}
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma target 3.5
 #pragma multi_compile_fog
 #pragma multi_compile_instancing
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
 #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fragment _ _LIGHT_LAYERS
 #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
 ENDHLSL }
 Pass {Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Depth
 #pragma multi_compile_instancing
 ENDHLSL }
 Pass {Name "DepthNormals" Tags {"LightMode"="DepthNormalsOnly"} ZWrite On
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Normals
 #pragma multi_compile_instancing
 ENDHLSL }
 Pass {Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Depth
 #pragma multi_compile_instancing
 ENDHLSL }
 Pass {Name "LightingFogMask" Tags {"LightMode"="LightingFogMask"} ZWrite Off ZTest Equal ColorMask 0 Stencil {Ref 1 ReadMask 1 WriteMask 1 Comp Always Pass Replace}
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Depth
 #pragma multi_compile_instancing
 ENDHLSL }
 }
}
