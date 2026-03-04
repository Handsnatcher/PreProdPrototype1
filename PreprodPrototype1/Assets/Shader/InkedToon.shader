Shader "Custom/InkedToon"
{
    Properties
    {
        _MainTex      ("Texture",         2D)           = "white" {}
        _Color        ("Tint",            Color)         = (1,1,1,1)
        _ShadowColor  ("Shadow Color",    Color)         = (0.2,0.2,0.3,1)
        _ShadowSteps  ("Shadow Steps",    Range(1,8))    = 3
        _ShadowSmooth ("Step Smoothness", Range(0,0.3))  = 0.05
        _RimColor     ("Rim Color",       Color)         = (1,1,1,1)
        _RimPower     ("Rim Power",       Range(0.1,8))  = 3
        _OutlineColor ("Outline Color",   Color)         = (0,0,0,1)
        _OutlineWidth ("Outline Width",   Range(0,0.1))  = 0.02
        _InkTex       ("Ink Texture",     2D)            = "white" {}
        _InkStrength  ("Ink Strength",    Range(0,1))    = 0.3
        _InkScale     ("Ink Scale",       Range(0.1,10)) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // Outline pass
        Pass
        {
            Name "OUTLINE"
            Cull Front

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT;
                IN.positionOS.xyz += normalize(IN.normalOS) * _OutlineWidth;
                OUT.positionHCS    = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // Toon pass
        Pass
        {
            Name "TOON"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex ToonVert
            #pragma fragment ToonFrag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_InkTex);  SAMPLER(sampler_InkTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ShadowColor;
                float  _ShadowSteps;
                float  _ShadowSmooth;
                float4 _RimColor;
                float  _RimPower;
                float  _InkStrength;
                float  _InkScale;
                float4 _OutlineColor;
                float  _OutlineWidth;
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
                float3 positionWS  : TEXCOORD2;
            };

            Varyings ToonVert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 ToonFrag(Varyings IN) : SV_Target
            {
                float3 normal   = normalize(IN.normalWS);
                float3 viewDir  = normalize(GetWorldSpaceViewDir(IN.positionWS));

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float  atten    = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                // Toon ramp
                float NdotL    = max(0, dot(normal, lightDir)) * atten;
                float bandSize = 1.0 / _ShadowSteps;
                float toon     = floor(NdotL / bandSize) * bandSize;
                toon           = smoothstep(toon, toon + _ShadowSmooth, NdotL);

                // Ink hatching
                float2 inkUV  = IN.positionWS.xy * _InkScale;
                float  ink    = SAMPLE_TEXTURE2D(_InkTex, sampler_InkTex, inkUV).r;
                float  inkMask = (1.0 - toon) * _InkStrength * ink;

                // Rim
                float rim = pow(1.0 - saturate(dot(viewDir, normal)), _RimPower);

                // Final color - preserves model texture
                half4 texColor   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;
                half4 finalColor = lerp(texColor * _ShadowColor, texColor, toon);
                finalColor.rgb  -= inkMask;
                finalColor.rgb  += _RimColor.rgb * rim * toon;
                finalColor.a     = texColor.a;

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}