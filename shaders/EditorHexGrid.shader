shader_type spatial;
render_mode unshaded;

// Implemented our own version of procedural hex grid tiling for a better
// looking and more uniform hexes.
// See andrewhungblog.wordpress.com/2018/07/28/shader-art-tutorial-hexagonal-grids/

uniform sampler2D maskTexture;
uniform vec4 color : hint_color;

uniform vec2 gridSize;

const vec2 hexSize = vec2(1.7320508, 1); // 1.7320508 = sqrt(3)
const float edgeLength = 1.3;

varying vec2 worldPos;

// Returns normalized distance to the nearest hex center.
float calcHexCenterDistance(vec2 coord)
{
    vec4 hexCenter = round(vec4(coord, coord - vec2(1., .5)) / hexSize.xyxy);
    vec4 offset = vec4(coord - hexCenter.xy * hexSize, coord - (hexCenter.zw + .5) * hexSize);
    vec2 position = dot(offset.xy, offset.xy) < dot(offset.zw, offset.zw) ? abs(offset.xy) : abs(offset.zw);
    return max(dot(position, hexSize * .5), position.y) + .5;
}

void vertex(){
    worldPos = (CAMERA_MATRIX * vec4(0., 0., 0., 1.)).xz;
}

void fragment(){
    // Antialiasing for small grids.
    float aa = fwidth(VERTEX.xy).x;

    // Current coordinate (flip y)
    vec2 coord = (VERTEX.xy + vec2(worldPos.x, -worldPos.y)) / edgeLength;

    // Distance to the nearest Hex center.
    float dist = calcHexCenterDistance(coord);

    // Half line width
    float halfWidth = min(.02 + aa, .2);

    vec4 mask = texture(maskTexture, UV);

    float halfWidthInverse = 1. - halfWidth;
    ALPHA = dist < halfWidthInverse ? 0. : smoothstep(halfWidthInverse, 1., dist) * mask.a;

    ALBEDO = color.rgb;
}
