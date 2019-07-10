#include "$ENGINE$/BasePass.bslinc"
#include "$ENGINE$/GBufferOutput.bslinc"
#include "$ENGINE$/PerCameraData.bslinc"

shader Background {

    mixin BasePass;
    mixin GBufferOutput;
    mixin PerCameraData;

    code {
    
        [alias(gTexLayer0)]
        SamplerState gTexLayer0Samp;
        Texture2D gTexLayer0;

        [alias(gTexLayer1)]
        SamplerState gTexLayer1Samp;
        Texture2D gTexLayer1;

        [alias(gTexLayer2)]
        SamplerState gTexLayer2Samp;
        Texture2D gTexLayer2;

        [alias(gTexLayer3)]
        SamplerState gTexLayer3Samp;
        Texture2D gTexLayer3;

        cbuffer BackgroundSpeeds {
            float speed0 = 1100.0f;
            float speed01 = 2200.0f;
            float speed1 = 850.0f;
            float speed11 = 1700.0f;
            float speed2 = 600.0f;
            float speed21 = 1200.0f;
            float speed3 = 350.0f;
            float speed31 = 700.0f;
        }
    
        void fsmain(
            in VStoFS input, 
            out float4 OutSceneColor : SV_Target0,
            out float4 OutGBufferA : SV_Target1,
            out float4 OutGBufferB : SV_Target2,
            out float2 OutGBufferC : SV_Target3,
            out float OutGBufferD : SV_Target4)
        {
            SurfaceData surfaceData;
            surfaceData.worldNormal.xyz = float3(0, 1, 0);
            surfaceData.roughness = 1.0f;
            surfaceData.metalness = 0.0f;
            surfaceData.mask = gLayer;

            // Calculate layers. TODO: it would be perhaps more
            // efficient to somehow calculate the UVs per vertex
            float2 UV0 = float2((input.uv0.x + gViewOrigin.x / speed01)*1.0f,
                (input.uv0.y + gViewOrigin.z / speed0)*1.0f);
            
            // Offsets are added to make it look less trippy around (0, 0, 0)
            // And these are multiplied to make the textures bigger and blurry
        
            float2 UV1 = float2((0.12 + input.uv0.x + gViewOrigin.x / speed11)*2.0f,
                (0.12 + input.uv0.y + gViewOrigin.z / speed1)*2.0f);
        
            float2 UV2 = float2((0.512 + input.uv0.x + gViewOrigin.x / speed21)*2.0f,
                (0.512 + input.uv0.y + gViewOrigin.z / speed2)*2.0f);
        
            float2 UV3 = float2((0.05 + input.uv0.x + gViewOrigin.x / speed31)*2.0f,
                (0.05 + input.uv0.y + gViewOrigin.z / speed3)*2.0f);

            float4 layer0 = gTexLayer0.Sample(gTexLayer0Samp, UV0);
            float4 layer1 = gTexLayer1.Sample(gTexLayer1Samp, UV1);
            float4 layer2 = gTexLayer2.Sample(gTexLayer2Samp, UV2);
            float4 layer3 = gTexLayer3.Sample(gTexLayer3Samp, UV3);

            // Just set the emissive colour to not have lighting affect this
            OutSceneColor.rgb =
                  layer0.rgb 
                + layer1.rgb * layer1.a * 0.7f
                + layer2.rgb * layer2.a * 0.7f
                + layer3.rgb * layer3.a * 0.7f; 

            OutSceneColor.a = 1.0;
            surfaceData.albedo = 0.0f;
            
            encodeGBuffer(surfaceData, OutGBufferA, OutGBufferB, OutGBufferC, OutGBufferD);
        }
    };
};