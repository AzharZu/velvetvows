Shader "VelvetVows/Academy Matte Hair"
{
    Properties
    {
        _BaseMap ("Hair detail", 2D) = "white" {}
        _BaseColor ("Natural hair colour", Color) = (.22, .13, .09, 1)
        _Cutoff ("Cutout", Range(0,1)) = .35
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 detail = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(detail.a - _Cutoff);
                half luminance = dot(detail.rgb, half3(.2126, .7152, .0722));
                half3 albedo = _BaseColor.rgb * (.4h + .6h * sqrt(saturate(luminance)));
                half3 normal = normalize(input.normalWS);
                Light key = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 light = max(SampleSH(normal), half3(.2,.2,.2));
                light += key.color * (.25h + .75h * saturate(dot(normal, key.direction))) * key.shadowAttenuation;
                #ifdef _ADDITIONAL_LIGHTS
                for (uint index = 0u; index < GetAdditionalLightsCount(); index++)
                {
                    Light fill = GetAdditionalLight(index, input.positionWS);
                    light += fill.color * saturate(dot(normal, fill.direction)) * fill.distanceAttenuation * .5h;
                }
                #endif
                return half4(albedo * min(light, half3(1.8,1.8,1.8)), 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack "Universal Render Pipeline/Lit"
}
