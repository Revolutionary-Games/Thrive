shader_type canvas_item;

/*
Vertex is used for better performance, however it requires the size property values to be passed again through shader parameters.
Fragment is less performant but can be used to avoid passing the size in case the assets using this shader become too many.
*/

//Use negative numbers to rotate counterclockwise
uniform float rotationSpeed = 1.0;

//This is done to ensure this shader will work with assets that have been scaled down in engine. Comment to use fragment
uniform float sizeX = 0;
uniform float sizeY = 0;

//Matrix to calculate the rotation
vec2 rotate (vec2 uv, vec2 pivot, float angle) {
    mat2 rotationMatrix = mat2(vec2(cos(angle),-sin(angle)),
                                vec2(sin(angle),cos(angle)));
    
    uv -= pivot;
    uv = uv * rotationMatrix;
    uv += pivot;
    return uv;

}

//Comment to use fragment
void vertex() {
    //Mke a vector2 with the passed size values
    vec2 size = vec2(sizeX, sizeY);
    
    //Pass rotation speed
    float time = TIME * rotationSpeed;
    
    VERTEX = rotate(VERTEX, vec2(size*0.5),time);
}

/*
void fragment() {
    //Pass rotation speed
    float time = -TIME * rotation_speed;
    
    vec2 uv_rotated = rotate(UV, vec2(0.5), time);
    COLOR = texture(TEXTURE, uv_rotated);
}
*/