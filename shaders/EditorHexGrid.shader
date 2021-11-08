shader_type spatial;
render_mode unshaded;

// Implemented our own version of procedural hex grid tiling for a better
// looking and more uniform hexes.
// See andrewhungblog.wordpress.com/2018/07/28/shader-art-tutorial-hexagonal-grids/

uniform sampler2D maskTexture;
uniform vec4 color : hint_color;
uniform float lineWidth = 0.02;
uniform float edgeLength = 1.3;

const vec2 hexSize = vec2(1.7320508, 1); // 1.7320508 = sqrt(3)

varying vec3 worldPos;
varying float minLineWidth;

// Returns normalized distance to the nearest hex center.
float calcHexCenterDistance(vec2 coord)
{
    vec4 hexCenter = round(vec4(coord, coord - vec2(1., .5)) / hexSize.xyxy);
    vec4 offset = vec4(coord - hexCenter.xy * hexSize, coord - (hexCenter.zw + .5) * hexSize);
    vec2 position = dot(offset.xy, offset.xy) < dot(offset.zw, offset.zw) ? abs(offset.xy) : abs(offset.zw);
    return max(dot(position, hexSize * .5), position.y) + .5;
}

void vertex(){
    worldPos = (CAMERA_MATRIX * vec4(0., 0., 0., 1.)).xyz;

    // min line width to not look unsteady
    minLineWidth = (worldPos.y / VIEWPORT_SIZE.y);
}

void fragment(){
    // Current coordinate
    vec2 coord = (VERTEX.xy + vec2(worldPos.x, -worldPos.z)) / edgeLength;

    // Distance to the nearest Hex center.
    float dist = calcHexCenterDistance(coord);

    float lineWidthInverse = 1. - max(minLineWidth, lineWidth);

    vec4 mask = texture(maskTexture, UV);

    ALPHA = dist < lineWidthInverse ? 0. : smoothstep(lineWidthInverse, 1., dist) * mask.a;

    ALBEDO = color.rgb;
}
