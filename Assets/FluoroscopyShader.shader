Shader "Custom/Fluoroscopy"
{
    Properties
    {
        _VesselColor ("Vessel Color", Color) = (0.05, 0.05, 0.07, 1.0)
        _Brightness ("Brightness", Range(0.01, 1.0)) = 0.12
        _EdgeDark ("Edge Darkening", Range(0.0, 1.0)) = 0.7
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _VesselColor;
                float  _Brightness;
                float  _EdgeDark;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                VertexPositionInputs posInputs  = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(IN.normalOS);
                OUT.normalWS  = normInputs.normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal  = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);

                // Fresnel — vessel edges appear DARKER (like X-ray absorption)
                float fresnel = 1.0 - saturate(dot(normal, viewDir));
                fresnel = pow(fresnel, 1.5);

                float darkness = _Brightness + fresnel * _EdgeDark;
                float3 color = _VesselColor.rgb * darkness;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
