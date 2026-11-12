Shader "VelvetVows/Academy Tailored Fabric"
{
    Properties
    {
        _BaseMap ("Original garment detail", 2D) = "white" {}
        _BaseColor ("Cloth dye", Color) = (.25,.20,.22,1)
        _Smoothness ("Fabric sheen", Range(0,1)) = .12
        _Metallic ("Metallic", Range(0,1)) = 0
        _Cutoff ("Cutout", Range(0,1)) = .3
        _PlainCloth ("Plain garment without printed graphics", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; half fog : TEXCOORD3; };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness, _Metallic, _Cutoff, _PlainCloth;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                half4 detail = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                clip(detail.a - _Cutoff);
                half luma = dot(detail.rgb, half3(.2126,.7152,.0722));
                half3 dyed = _BaseColor.rgb * (.32h + .68h * sqrt(saturate(luma)));
                // White collar, cuffs and piping keep their original texture colour.
                half3 albedo = lerp(dyed, detail.rgb * .85h, smoothstep(.32h,.78h,luma));
                albedo = lerp(albedo, _BaseColor.rgb * .52h, _PlainCloth);
                InputData lighting = (InputData)0;
                lighting.positionWS = i.positionWS;
                lighting.normalWS = normalize(i.normalWS) * (front ? 1 : -1);
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                lighting.bakedGI = SampleSH(lighting.normalWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                lighting.shadowMask = half4(1,1,1,1);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo; surface.alpha = 1;
                surface.smoothness = _Smoothness; surface.metallic = _Metallic;
                surface.occlusion = 1; surface.normalTS = half3(0,0,1);
                half4 colour = UniversalFragmentPBR(lighting, surface);
                colour.rgb = MixFog(colour.rgb, i.fog);
                return colour;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Universal Render Pipeline/Lit"
}
