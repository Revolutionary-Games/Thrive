shader_type spatial;
render_mode unshaded;

// Procedural hex grid tiling for a better looking and more uniform hexes.
// This is heavily inspired by Andrew Hung's tutorial.
// See https://andrewhungblog.wordpress.com/2018/07/28/shader-art-tutorial-hexagonal-grids

uniform sampler2D maskTexture;
uniform vec4 color : hint_color;
uniform float lineWidth = 0.02;
uniform float edgeLength = 1.3;

const vec2 hexSize = vec2(1.7320508, 1.0); // 1.7320508 = sqrt(3)
const vec2 hexSizeHalf = hexSize * 0.5;

varying vec3 worldPos;
varying float minLineWidth;

// Returns normalized distance to the nearest hex center.
float hexDist(vec2 coord)
{
    vec4 dists = mod(vec4(coord, coord - hexSizeHalf), hexSize.xyxy) - hexSizeHalf.xyxy;    
    vec2 dist = dot(dists.xy, dists.xy) < dot(dists.zw, dists.zw) ? abs(dists.xy) : abs(dists.zw);
    return max(dot(dist, hexSizeHalf), dist.y) + 0.5;
}

void vertex(){
    worldPos = (CAMERA_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;

    // min line width to not look unsteady
    minLineWidth = (worldPos.y / VIEWPORT_SIZE.y);
}

void fragment(){
    vec4 mask = texture(maskTexture, UV);

    if (mask.a == 0.0){
        ALPHA = 0.0;
        return;
    }

    // Current coordinate
    vec2 coord = (VERTEX.xy + vec2(worldPos.x, -worldPos.z)) / edgeLength;

    // Distance to the nearest Hex center.
    float dist = hexDist(coord);

    ALPHA = smoothstep(1.0 - max(minLineWidth, lineWidth), 1.0, dist) * mask.a;

    ALBEDO = color.rgb;
}
