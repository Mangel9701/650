Shader "Custom/RealisticFire"
{
    Properties
    {
        [Header(Colors)]
        [HDR] _BaseColor("Base Color (Dark Red)", Color) = (0.5, 0.1, 0.0, 1.0)
        [HDR] _MidColor("Mid Color (Orange)", Color) = (1.0, 0.5, 0.0, 1.0)
        [HDR] _HighColor("High Color (Yellow/White)", Color) = (1.0, 1.0, 0.5, 1.0)
        
        [Header(Animation)]
        _Speed("Fire Speed (X: Horizontal, Y: Vertical)", Vector) = (0.5, 2.0, 0.0, 0.0)
        _DistortionScale("Distortion Scale", Range(1, 20)) = 10.0
        _DistortionStrength("Distortion Strength", Range(0, 1)) = 0.5
        
        [Header(Shape)]
        _HeightFade("Height Fade", Range(0.1, 10)) = 1.0
        _WidthFade("Width Fade", Range(0.1, 10)) = 1.0
        _NoiseScale("Noise Scale", Range(1, 20)) = 5.0
        
        _EmissiveIntensity("Emissive Intensity", Range(0, 20)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector"="True"}
        LOD 100
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _BaseColor;
            float4 _MidColor;
            float4 _HighColor;
            float4 _Speed;
            float _DistortionScale;
            float _DistortionStrength;
            float _HeightFade;
            float _WidthFade;
            float _NoiseScale;
            float _EmissiveIntensity;

            float2 hash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(dot(hash(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)),
                                 dot(hash(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
                            lerp(dot(hash(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
                                 dot(hash(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                float2 shift = float2(100.0, 100.0);
                for (int i = 0; i < 3; ++i) {
                    v += a * noise(p);
                    p = p * 2.0 + shift;
                    a *= 0.5;
                }
                return v;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;

                // 1. Distortion
                float2 distUV = uv * _DistortionScale + float2(time * _Speed.x * 0.2, -time * _Speed.y * 0.3);
                float d = fbm(distUV) * _DistortionStrength;

                // 2. Main Noise
                float2 fireUV = uv * _NoiseScale + float2(d, -time * _Speed.y);
                float n = fbm(fireUV);
                n = n * 0.5 + 0.5; // Rescale to roughly 0-1

                // 3. Masks for elongated fireplace
                // Vertical fade: flame disappears as it goes up
                float verticalMask = saturate(1.0 - pow(uv.y, _HeightFade));
                // Horizontal fade: flame contained in the center (elongated horizontally if uv.x range is large)
                float horizontalMask = saturate(1.0 - pow(abs(uv.x - 0.5) * 2.0, _WidthFade));
                
                float combinedMask = verticalMask * horizontalMask;
                float intensity = saturate(n * combinedMask * 1.5);

                // 4. Colorization
                float3 color = lerp(_BaseColor.rgb, _MidColor.rgb, saturate(intensity * 1.5));
                color = lerp(color, _HighColor.rgb, saturate((intensity - 0.5) * 2.0));
                
                // Emissive boost
                float3 finalColor = color * intensity * _EmissiveIntensity;

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
