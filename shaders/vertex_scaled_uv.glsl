#version 330

uniform mat4 worldViewProj;
uniform vec2 scale;

in vec4 vertex;
in vec4 uv0;

out vec2 UV;

void main()
{
    gl_Position = worldViewProj * vertex;
    UV.x = uv0.x / scale.x;
    UV.y = uv0.y / scale.y;
}
