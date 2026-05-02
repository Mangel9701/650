#ifndef CUSTOM_TRIPLANAR_INCLUDED
#define CUSTOM_TRIPLANAR_INCLUDED

/**
 * Custom Triplanar Mapping with UDN (Unity Detail Normal) blending.
 * This avoids artifacts and "flatness" in blend zones on curved surfaces.
 */
void CustomTriplanar_float(
    UnityTexture2D Tex, 
    UnitySamplerState Sampler, 
    float3 WorldPos, 
    float3 WorldNormal, 
    float3 Tiling, 
    float3 Offset, 
    float Sharpness, 
    bool IsNormal,
    out float4 Out)
{
    // 1. Setup Weights
    float3 n = normalize(WorldNormal);
    float3 blending = pow(abs(n), max(Sharpness, 0.00001));
    blending /= (blending.x + blending.y + blending.z);

    // 2. Setup UVs
    float2 uvX = WorldPos.zy * Tiling.zy + Offset.zy;
    float2 uvY = WorldPos.xz * Tiling.xz + Offset.xz;
    float2 uvZ = WorldPos.xy * Tiling.xy + Offset.xy;

    // 3. Samples
    float4 colX = SAMPLE_TEXTURE2D(Tex.tex, Sampler.samplerstate, uvX);
    float4 colY = SAMPLE_TEXTURE2D(Tex.tex, Sampler.samplerstate, uvY);
    float4 colZ = SAMPLE_TEXTURE2D(Tex.tex, Sampler.samplerstate, uvZ);

    if (IsNormal)
    {
        // 4. Unpack
        float3 nX = UnpackNormal(colX);
        float3 nY = UnpackNormal(colY);
        float3 nZ = UnpackNormal(colZ);

        // 5. UDN Blending logic
        // We only take the X and Y (tangent/bitangent) from the normal map
        // and orient them to the world axes, then add them to the base normal.
        float3 s = sign(n);
        
        // Plane X (YZ): Map X -> World Z, Map Y -> World Y
        float3 pX = float3(0, nX.y, nX.x * s.x);
        // Plane Y (XZ): Map X -> World X, Map Y -> World Z
        float3 pY = float3(nY.x, 0, nY.y * s.y);
        // Plane Z (XY): Map X -> World X, Map Y -> World Y
        float3 pZ = float3(nZ.x * s.z, nZ.y, 0);

        // Blend perturbations and add to world normal
        float3 finalNormal = normalize(n + (pX * blending.x + pY * blending.y + pZ * blending.z));
        
        Out = float4(finalNormal, 1.0);
    }
    else
    {
        Out = colX * blending.x + colY * blending.y + colZ * blending.z;
    }
}

#endif
