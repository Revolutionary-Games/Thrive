// #include "$ENGINE$/BasePass.bslinc"
#include "$ENGINE$/ForwardLighting.bslinc"
#include "$ENGINE$/PerFrameData.bslinc"
#include "$ENGINE$/VertexInput.bslinc"
#include "$ENGINE$/PerCameraData.bslinc"
#include "$ENGINE$/PerObjectData.bslinc"
#include "$ENGINE$/GBufferOutput.bslinc"
#include "$ENGINE$/PerObjectData.bslinc"

options
{
    transparent = true;
};

shader Membrane
{
    // mixin BasePass;
    mixin PerFrameData;
    mixin PerCameraData;
    mixin PerObjectData;    
    mixin PerObjectData;
    mixin VertexInput;
    mixin GBufferOutput;

    mixin ForwardLighting;

    blend
    {
        target  
        {
            enabled = true;
            color = { srcA, srcIA, add };
        };
    };  
    
    depth
    {
        write = false;
    };

    variations
    {
        WIGGLY = { true, false };
    };

    code
    {
        // animation
        VStoFS vsmain(VertexInput input)
        {
            VStoFS output;
            
            VertexIntermediate intermediate = getVertexIntermediate(input);
            float4 worldPosition = getVertexWorldPosition(input, intermediate);
            
            // Animation
#if WIGGLY
            // NOTE: using input.position would make this not depend on world position.
            // How this currently is done, moving through the world causes wiggling
            worldPosition.x += sin(worldPosition.z + sign(worldPosition.x) * gTime) / 10.f;
            worldPosition.z += sin(worldPosition.x - sign(worldPosition.z) * gTime) / 10.f;
#endif

            output.worldPosition = worldPosition.xyz;
            output.position = mul(gMatViewProj, worldPosition);
            populateVertexOutput(input, intermediate, output);
            
            return output;
        }

        [alias(gAlbedoTex)]
        SamplerState gAlbedoSamp;
        
        [alias(gNormalTex)]
        SamplerState gNormalSamp;
        
        [alias(gRoughnessTex)]
        SamplerState gRoughnessSamp;
        
        [alias(gMetalnessTex)]
        SamplerState gMetalnessSamp;
    
        [alias(gEmissiveMaskTex)]
        SamplerState gEmissiveMaskSamp;
        
        Texture2D gAlbedoTex = white;
        Texture2D gNormalTex = normal;
        Texture2D gRoughnessTex = white;
        Texture2D gMetalnessTex = black;
        Texture2D gEmissiveMaskTex = black;

        [alias(gDamagedTex)]
        SamplerState gDamagedSamp;
        Texture2D gDamagedTex = white;        
        
        cbuffer MaterialParams
        {
            float gOpacity = 1.0f;
            [color]
            float3 gEmissiveColor = { 1.0f, 1.0f, 1.0f };
            float2 gUVOffset = { 0.0f, 0.0f };
            float2 gUVTile = { 1.0f, 1.0f };

            float4 gTint = { 1.0f, 1.0f, 1.0f, 1.0f };
            float gHealthFraction = { 0.f };
        }
        
        float4 fsmain(in VStoFS input) : SV_Target0
        {
            float2 uv = input.uv0 * gUVTile + gUVOffset;
        
            float3 normal = normalize(gNormalTex.Sample(gNormalSamp, uv).xyz * 2.0f - float3(1, 1, 1));
            float3 worldNormal = calcWorldNormal(input, normal);
        
            SurfaceData surfaceData;
            surfaceData.albedo = (gAlbedoTex.Sample(gAlbedoSamp, uv) * gHealthFraction +
                        gDamagedTex.Sample(gDamagedSamp, uv) * (1.f - gHealthFraction)) * gTint;
            surfaceData.worldNormal.xyz = worldNormal;
            surfaceData.worldNormal.w = 1.0f;
            surfaceData.roughness = gRoughnessTex.Sample(gRoughnessSamp, uv).x;
            surfaceData.metalness = gMetalnessTex.Sample(gMetalnessSamp, uv).x;
            
            float3 lighting = calcLighting(input.worldPosition.xyz, input.position, uv, surfaceData);
            float3 emissive = gEmissiveColor * gEmissiveMaskTex.Sample(gEmissiveMaskSamp, uv).x;
            return float4(emissive + lighting, surfaceData.albedo.a * gOpacity);
        }   
    };
};