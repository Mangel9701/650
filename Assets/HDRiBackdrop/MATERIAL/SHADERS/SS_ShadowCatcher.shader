Shader "Custom/ShadowCatcher"
{
    Properties
    {
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.1  // Sensibilidad mínima para considerar sombra
        _ShadowIntensity("Shadow Intensity (Baked Mode)", Range(0,2)) = 1.0  // Multiplicador para intensificar sombras en Baked
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "BakedShadowCatcher"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ MIXED_LIGHTING_SUBTRACTIVE  // Para soporte legacy si usas Subtractive

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                float3 normal : NORMAL;  // Agregado para samplear lightmap correctamente
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 lightmapUV : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;  // Para normal en world space
            };

            float _Cutoff;
            float _ShadowIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.lightmapUV = v.lightmapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                o.worldNormal = TransformObjectToWorldNormal(v.normal);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float attenuation = 1.0;  // Por defecto, full luz (transparente)

                #ifdef LIGHTMAP_ON
                    #if defined(SHADOWS_SHADOWMASK) || defined(LIGHTMAP_SHADOW_MIXING)
                        // Modo Mixed con shadowmask
                        float4 shadowMask = SAMPLE_TEXTURE2D(unity_ShadowMask, sampler_unity_ShadowMask, i.lightmapUV);
                        attenuation = shadowMask.r;  // Asume luz principal en canal R. Ajusta si múltiples luces.
                    #else
                        // Modo Baked: Usa lightmap para estimar sombra
                        half3 bakedGI = SampleLightmap(i.lightmapUV, i.worldNormal);  // Samplea lightmap (incluye direct + indirect)
                        float luminance = dot(bakedGI, float3(0.3, 0.59, 0.11));  // Luminancia aproximada (grayscale)
                        attenuation = saturate(luminance * _ShadowIntensity);  // Ajusta con intensidad
                    #endif
                #endif

                float alpha = 1.0 - attenuation;  // Invierte: sombra = opaco, luz = transparente

                if (alpha < _Cutoff) discard;  // Descarta si sombra es muy débil

                return half4(0, 0, 0, alpha);  // Negro con alpha basado en sombra
            }
            ENDHLSL
        }
    }
}