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

shader Organelle
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
        // TODO: the old organelle shader had modulation here
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

    code
    {
        cbuffer MaterialParams
        {
            float gOpacity = 1.0f;
            [color]
            float3 gEmissiveColor = { 1.0f, 1.0f, 1.0f };
            float2 gUVOffset = { 0.0f, 0.0f };
            float2 gUVTile = { 1.0f, 1.0f };

            float4 gTint = { 1.0f, 1.0f, 1.0f, 1.0f };
            float gJiggleAmount = 0.05f;
            float gJiggleMaxAngle = 15.f;
        }

        // animation
        VStoFS vsmain(VertexInput input)
        {
            VStoFS output;

            VertexIntermediate intermediate = getVertexIntermediate(input);

            // Animation
            const float pi = 3.1415927f;
            input.position.x += gJiggleAmount * (3.f * sin(3.f * gTime));
            input.position.y += gJiggleAmount * (3.f * sin(2.f * gTime));

             float angle = cos(gTime) * pi * gJiggleMaxAngle / 360.f;
             const float4x4 rotation = float4x4(
                float4(cos(angle), -sin(angle), 0.f, 0.f),
                float4(sin(angle),  cos(angle), 0.f, 0.f),
                float4(0.f, 0.f, 1.f, 0.f),
                float4(0.f, 0.f, 0.f, 1.f));

            input.position = mul(rotation, input.position);

            float4 worldPosition = getVertexWorldPosition(input, intermediate);

            output.position = mul(gMatViewProj, worldPosition);

            output.worldPosition = worldPosition.xyz;
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

        float4 fsmain(in VStoFS input) : SV_Target0
        {
            float2 uv = input.uv0 * gUVTile + gUVOffset;

            float3 normal = normalize(gNormalTex.Sample(gNormalSamp, uv).xyz * 2.0f - float3(1, 1, 1));
            float3 worldNormal = calcWorldNormal(input, normal);

            SurfaceData surfaceData;
            surfaceData.albedo = gAlbedoTex.Sample(gAlbedoSamp, uv) * gTint;
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