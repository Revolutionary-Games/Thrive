#version 330

uniform mat4 worldViewProj;

in vec4 vertex;
in vec2 uv0;

out vec2 oUV0;

void main()
{
    gl_Position = worldViewProj * vertex;

    oUV0 = uv0;
}
