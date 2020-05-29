shader_type spatial;
render_mode unshaded, blend_add;

uniform sampler2D densities;
uniform sampler2D noise;

uniform vec4 colour1 : hint_color = vec4(0, 0, 0, 0);
uniform vec4 colour2 : hint_color  = vec4(0, 0, 0, 0);
uniform vec4 colour3 : hint_color  = vec4(0, 0, 0, 0);
uniform vec4 colour4 : hint_color  = vec4(0, 0, 0, 0);

uniform vec2 UVoffset = vec2(0, 0);

// Setting this too high makes the clouds invisible
const float CLOUD_DISSIPATION = 5.0;

// Should be the same as its counterpart in CompoundCloudPlane.cs
const float CLOUD_MAX_INTENSITY_SHOWN = 1000f;

float getIntensity(float value){
	return min(0.8 * atan(0.003f * CLOUD_MAX_INTENSITY_SHOWN * value), 1.0f);
}

void fragment(){
    vec4 concentrations = texture(densities, UV + UVoffset);
    
    float cloud1 = getIntensity(concentrations.r) * pow(texture(noise, UV + UVoffset).r, 
        CLOUD_DISSIPATION);
        
    float cloud2 = getIntensity(concentrations.g) * pow(texture(noise, UV + UVoffset + 0.2f).r, 
        CLOUD_DISSIPATION);
        
    float cloud3 = getIntensity(concentrations.b) * pow(texture(noise, UV + UVoffset + 0.4f).r, 
        CLOUD_DISSIPATION);
        
    float cloud4 = getIntensity(concentrations.a) * pow(texture(noise, UV + UVoffset + 0.6f).r, 
        CLOUD_DISSIPATION);
    
    vec4 colour =
        // first
        colour1 * cloud1
        + // second
        colour2 * cloud2
        + // third
        colour3 * cloud3
        + // fourth
        colour4 * cloud4;
    
    ALPHA = min(cloud1 + cloud2 + cloud3 + cloud4, 0.9f);
    
    ALBEDO = colour.rgb;
}
