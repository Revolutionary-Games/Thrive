shader_type canvas_item;

/*
Fragment is less performant but doesn't require to pass the size in the parameters.
If you need the more performant variant, you may use the other shader: SpinnerVertex
*/

//Use negative numbers to rotate counterclockwise
uniform float rotationSpeed = 1.0;



//Matrix to calculate the rotation
vec2 rotate (vec2 uv, vec2 pivot, float angle) {
    mat2 rotationMatrix = mat2(vec2(cos(angle),-sin(angle)),
                                vec2(sin(angle),cos(angle)));
    
    uv -= pivot;
    uv = uv * rotationMatrix;
    uv += pivot;
    return uv;

}


void fragment() {
    //Pass rotation speed
    float time = -TIME * rotationSpeed;
    
    vec2 uvRotated = rotate(UV, vec2(0.5), time);
    COLOR = texture(TEXTURE, uvRotated);
}