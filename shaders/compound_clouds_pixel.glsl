#version 330

uniform sampler2D cloudTexture;
uniform sampler2D perlinNoise;

uniform vec4 cloudColour1;
uniform vec4 cloudColour2;
uniform vec4 cloudColour3;
uniform vec4 cloudColour4;

// Must match the names in compoundClouds.vs
in vec2 oUV0;

out vec4 color;

void main()
{
    float CLOUD_DISSIPATION = 6.0;

    vec4 concentrations = texture2D(cloudTexture, oUV0);
    
    float cloud1 = concentrations.r * pow(texture2D(perlinNoise, oUV0).r, CLOUD_DISSIPATION);
    float cloud2 = concentrations.g * pow(texture2D(perlinNoise, oUV0 + 0.2f).r, CLOUD_DISSIPATION);
    float cloud3 = concentrations.b * pow(texture2D(perlinNoise, oUV0 + 0.4f).r, CLOUD_DISSIPATION);
    float cloud4 = concentrations.a * pow(texture2D(perlinNoise, oUV0 + 0.6f).r, CLOUD_DISSIPATION);

    color =
          // first
        cloudColour1 * cloud1
        + // second
        cloudColour2 * cloud2
        + // third
        cloudColour3 * cloud3
        + // fourth
        cloudColour4 * cloud4;
    
    color.a = min(cloud1 + cloud2 + cloud3 + cloud4, 0.9f);
}
