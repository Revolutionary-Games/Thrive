shader_type spatial;
render_mode unshaded;

// this is 1 by default to keep the dark BGs looking nice
uniform float lightLevel = 1.0f;

uniform vec2 repeats = vec2(1.0f, 1.0f);
uniform sampler2D layer0 : hint_albedo;
uniform sampler2D layer1 : hint_albedo;
uniform sampler2D layer2 : hint_albedo;
uniform sampler2D layer3 : hint_albedo;

const vec2 speed0 = vec2(3300.0f);
const vec2 speed1 = vec2(2550.0f);
const vec2 speed2 = vec2(1800.0f);
const vec2 speed3 = vec2(1050.0f);

varying vec2 UV3;
varying vec2 UV4;

vec3 LightInfluence(float amount)
{
    vec3 influence = vec3(amount, amount, amount);
    int time = 0;

    if(amount > 0.5f)
        time = 2;
    else if(amount > 0.25f)
        time = 1;
    else
        time = 0;

    switch (time)
    {
        // Night
        case 0 :
        {
           influence = mix(vec3(0.052f, 0.05f, 0.17f), vec3(0.25f, 0.25f, 0.25f),
                       4.0f * amount); 

            return influence;
        }
        // Dawn and Dusk
        case 1 :
        {
            influence = mix(vec3(0.25f, 0.25f, 0.25f), vec3(0.75f, 0.5f, 0.5f),
                        4.0f * amount - 1.0f);
            
            return influence;
        }
        // Day
        case 2 :
        {
            influence = mix(vec3(0.75f, 0.5f, 0.5f), vec3(1.0f, 1.0f, 1.0f),
                        2.0f * amount - 1.0f);

            return influence;
        }
    }
}

void vertex(){
    vec2 offset = (repeats - 1.0f) / 2.0f;
    vec2 worldPos = (CAMERA_MATRIX * vec4(0.0f, 0.0f, 0.0f, 1.0f)).xz;

    UV = (UV + worldPos / (speed0 * repeats)) * repeats - offset;
    UV2 = (0.12f + UV + worldPos / (speed1));
    UV3 = (0.512f + UV + worldPos / (speed2));
    UV4 = (0.05f + UV + worldPos / (speed3));
}

void fragment(){
    vec4 colour0 = texture(layer0, UV);
    vec4 colour1 = texture(layer1, UV2);
    vec4 colour2 = texture(layer2, UV3);
    vec4 colour3 = texture(layer3, UV4);

    vec3 mixture0 = mix(colour1.rgb, colour2.rgb * colour2.a, 1.0f);
    vec3 mixture1 = mix(colour2.rgb, colour3.rgb * colour3.a, 1.0f);
    vec3 mixture2 = mix(mixture0.rgb, mixture1.rgb, 0.5f);
    vec3 composition = mix(colour0.rgb, mixture2.rgb, 0.5f);

    ALBEDO.rgb = composition.rgb * LightInfluence(lightLevel);

    ALPHA = 1.0f;
}
