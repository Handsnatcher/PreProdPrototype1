Shader "Custom/GlassShard"
{
    Properties
    {
        _MainTex      ("Screen Capture",  2D)          = "white" {}
        _FresnelColor ("Fresnel Color",   Color)        = (0.85, 0.95, 1, 1)
        _FresnelPower ("Fresnel Power",   Range(0,8))   = 2
        _Tint         ("Glass Tint",      Color)        = (0.8, 0.92, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent+100"
        }

        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            Name "GLASS"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _FresnelColor;
                float  _FresnelPower;
                float4 _Tint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.uv          = IN.uv;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS   = GetWorldSpaceViewDir(pos.positionWS);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 normal  = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);
                float  fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);

                half4 screen = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                half4 col = screen;
                col.rgb  += _Tint.rgb * 0.08;
                col.rgb  += _FresnelColor.rgb * fresnel * 0.35;
                col.a     = saturate((0.85 + fresnel * 0.15) * _Tint.a);

                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}