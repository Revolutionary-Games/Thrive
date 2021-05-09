shader_type spatial;
render_mode unshaded;

// If false the shader will use generated hex patterns for the texture which
// generaly looks better and uniform but probably slightly slower
uniform bool useTexture;

uniform sampler2D gridTexture;
uniform sampler2D maskTexture;
uniform vec4 color : hint_color;

uniform vec2 gridSize;

varying vec3 worldPos;

const vec2 s = vec2(1.7320508, 1);

// Hexagonal grid pattern code from https://www.shadertoy.com/view/wtdSzX

float hex(in vec2 p){
    p = abs(p);
    return max(dot(p, s * .5), p.y);
}

vec4 getHex(vec2 p){
    vec4 hC = floor(vec4(p, p - vec2(1, .5)) / s.xyxy) + .5;
    vec4 h = vec4(p - hC.xy * s, p - (hC.zw + .5) * s);

    return dot(h.xy, h.xy) < dot(h.zw, h.zw) 
        ? vec4(h.xy, hC.xy) 
        : vec4(h.zw, hC.zw + .5);
}

void vertex(){
    worldPos = (WORLD_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment(){
    vec4 gridTex = texture(gridTexture, (worldPos.xz / gridSize) *
        vec2(2.778, 4.811));
    vec4 mask = texture(maskTexture, UV);

    vec4 final;

    if (useTexture){
        final.rgb = gridTex.rgb * color.rgb;
        final.a = gridTex.a * mask.a;
    }
    else
    {
        vec4 h = getHex((worldPos.xz / gridSize) * vec2(3.849, 3.85) * 5. +
            s.yx + vec2(-0.12, -0.23));
        float eDist = hex(h.xy);
        vec3 col = mix(vec3(0.), color.rgb, smoothstep(0., .03, eDist - .5 + .03));

        final.rgb = col.xyz;
        final.a = col.x * mask.a;
    }

    ALBEDO = final.rgb;
    ALPHA = final.a;
}
