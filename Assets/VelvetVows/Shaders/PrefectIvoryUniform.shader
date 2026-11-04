Shader "VelvetVows/Prefect Ivory Uniform"
{
    Properties
    {
        _BaseMap ("Original uniform detail", 2D) = "white" {}
        _IvoryColor ("Ivory cloth", Color) = (0.98, 0.93, 0.82, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; };
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _IvoryColor;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 original = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half detail = dot(original.rgb, half3(.2126, .7152, .0722));
                half clothShade = saturate(.52h + detail * 22.0h);
                Light mainLight = GetMainLight();
                half diffuse = saturate(dot(normalize(input.normalWS), mainLight.direction));
                half lightShade = .72h + .28h * diffuse;
                return half4(_IvoryColor.rgb * clothShade * lightShade, original.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
