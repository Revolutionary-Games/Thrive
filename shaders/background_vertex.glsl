#version 330

uniform vec3 cameraPos;
uniform float speed0;
uniform float speed01;
uniform float speed1;
uniform float speed11;
uniform float speed2;
uniform float speed21;
uniform float speed3;
uniform float speed31;

in vec4 vertex;
in vec4 uv0;
in vec4 uv1;
in vec4 uv2;
in vec4 uv3;

out vec2 UV0;
out vec2 UV1;
out vec2 UV2;
out vec2 UV3;

void main()
{
    // Our vertices are already in screenspace
    gl_Position = vec4(vertex.xy, 0.0, 1.0);

    UV0.x = (uv0.x + cameraPos.x / speed01)*1.0f;
    UV0.y = (uv0.y + cameraPos.z / speed0)*0.5f;
    // Offsets are added to make it look less trippy around (0, 0, 0)
    // And these are multiplied to make the textures bigger and blurry

    UV1.x = (0.12 + uv0.x + cameraPos.x / speed11)*2.0f;
    UV1.y = (0.12 + uv0.y + cameraPos.z / speed1)*1.0f;

    UV2.x = (0.512 + uv0.x + cameraPos.x / speed21)*2.0f;
    UV2.y = (0.512 + uv0.y + cameraPos.z / speed2)*1.0f;

    UV3.x = (0.05 + uv0.x + cameraPos.x / speed31)*2.0f;
    UV3.y = (0.05 + uv0.y + cameraPos.z / speed3)*1.0f;
}
