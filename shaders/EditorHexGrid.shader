shader_type spatial;
render_mode unshaded;

// TODO: implement our own version of procedural hex grid tiling for a better
// looking and more uniform hexes.
// See andrewhungblog.wordpress.com/2018/07/28/shader-art-tutorial-hexagonal-grids/

uniform sampler2D gridTexture;
uniform sampler2D maskTexture;
uniform vec4 color : hint_color;

uniform vec2 gridSize;

varying vec3 worldPos;

void vertex(){
    worldPos = (WORLD_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment(){
    vec4 gridTex = texture(gridTexture, (worldPos.xz / gridSize) *
        vec2(2.778, 4.811));
    vec4 mask = texture(maskTexture, UV);

    ALBEDO = gridTex.rgb * color.rgb;
    ALPHA = gridTex.a * mask.a;
}
