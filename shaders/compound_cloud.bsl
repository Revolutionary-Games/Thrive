#include "$ENGINE$/BasePass.bslinc"
#include "$ENGINE$/ForwardLighting.bslinc"

options
{
    transparent = true;
    // Make render before cells
    priority = 10;
};

shader CompoundCloud {

    mixin BasePass;
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

    code {
    
        [alias(gDensityTex)]
        SamplerState gDensitySamp;
        Texture2D gDensityTex;

        [alias(gNoiseTex)]
        SamplerState gNoiseSamp;
        Texture2D gNoiseTex = white;

        cbuffer CloudColours {
            float4 gCloudColour1;
            float4 gCloudColour2;
            float4 gCloudColour3;
            float4 gCloudColour4;
        }
    
        float4 fsmain(in VStoFS input) : SV_Target0 {
            // Setting this too high makes the clouds invisible
            float CLOUD_DISSIPATION = 2.0;
        
            float4 concentrations = gDensityTex.Sample(gDensitySamp, input.uv0);
            
            float cloud1 = concentrations.r * pow(gNoiseTex.Sample(gNoiseSamp, input.uv0).r, CLOUD_DISSIPATION);
            float cloud2 = concentrations.g * pow(gNoiseTex.Sample(gNoiseSamp, input.uv0 + 0.2f).r, CLOUD_DISSIPATION);
            float cloud3 = concentrations.b * pow(gNoiseTex.Sample(gNoiseSamp, input.uv0 + 0.4f).r, CLOUD_DISSIPATION);
            float cloud4 = concentrations.a * pow(gNoiseTex.Sample(gNoiseSamp, input.uv0 + 0.6f).r, CLOUD_DISSIPATION);

            float4 colour =
                  // first
                gCloudColour1 * cloud1
                + // second
                gCloudColour2 * cloud2
                + // third
                gCloudColour3 * cloud3
                + // fourth
                gCloudColour4 * cloud4;
            
            colour.a = min(cloud1 + cloud2 + cloud3 + cloud4, 0.9f);            

            return colour;
        }
    };
};