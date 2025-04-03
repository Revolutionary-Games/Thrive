shader_type canvas_item;

/*
This Vertex shader requires the size property values to be passed again through shader parameters
If you want to avoid this, you may use the other spinner shader: SpinnerPixel
*/

// Use negative numbers to rotate counterclockwise
uniform float rotationSpeed = 1.0;

// This is done to ensure this shader will work with assets that have been scaled down in engine
uniform float sizeX = 0;
uniform float sizeY = 0;

// Matrix to calculate the rotation
vec2 rotate (vec2 uv, vec2 pivot, float angle) {
    mat2 rotationMatrix = mat2(vec2(cos(angle), -sin(angle)),
        vec2(sin(angle), cos(angle)));

    uv -= pivot;
    uv = uv * rotationMatrix;
    uv += pivot;
    return uv;
}

void vertex() {
    // Make a vector2 with the passed size values
    vec2 size = vec2(sizeX, sizeY);

    // Pass rotation speed
    float time = TIME * rotationSpeed;

    VERTEX = rotate(VERTEX, vec2(size * 0.5), time);
}