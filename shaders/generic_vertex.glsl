#version 330

uniform mat4 worldViewProj;

in vec4 vertex;
in vec4 uv0;

out vec2 UV;

void main()
{
    gl_Position = worldViewProj * vertex;
    UV = uv0.xy;
}
