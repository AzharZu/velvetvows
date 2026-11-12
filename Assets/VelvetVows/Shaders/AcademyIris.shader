Shader "VelvetVows/Academy Iris"
{
    Properties
    {
        _BaseMap ("Original eyes", 2D) = "white" {}
        _BaseColor ("Iris tint", Color) = (.72,.67,.61,1)
        _IrisScale ("Iris size", Range(.6,1)) = .82
        _DualAtlas ("Two eye atlas", Float) = 0
        _Cutoff ("Cutout", Range(0,1)) = .25
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest+1" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct A { float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 n:TEXCOORD1; float2 uv:TEXCOORD2; };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor; half _IrisScale,_DualAtlas,_Cutoff;
            CBUFFER_END
            V Vert(A i) { V o; o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(i.n);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);return o; }
            half4 Frag(V i):SV_Target
            {
                float2 center=float2(_DualAtlas>.5 ? (i.uv.x<.5 ? .25:.75):.5,.5);
                float2 uv=center+(i.uv-center)/_IrisScale;
                half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv); clip(tex.a-_Cutoff);
                half3 albedo=tex.rgb*_BaseColor.rgb;
                Light key=GetMainLight(TransformWorldToShadowCoord(i.world));
                half3 light=max(SampleSH(normalize(i.n)),half3(.25,.25,.25));
                light+=key.color*(.25h+.75h*saturate(dot(normalize(i.n),key.direction)))*key.shadowAttenuation;
                return half4(albedo*min(light,half3(1.6,1.6,1.6)),1);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
