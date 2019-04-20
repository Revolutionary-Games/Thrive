#version 330

uniform mat4 worldViewProj;
uniform float time;
uniform float size;
uniform float maxRot;

in vec4 vertex;
in vec4 uv0;

out vec2 UV;

void main()
{
    float pi = 3.1415927;

    vec4 tempVertex = vertex;
    tempVertex.x = vertex.x + size*3*sin(3*time);
    tempVertex.y = vertex.y + size*3*sin(2*time);
    
    float angle = cos(time)*pi*maxRot/360;
    mat4 rotation = mat4(
        vec4(cos(angle), -sin(angle), 0, 0),
        vec4(sin(angle),  cos(angle), 0, 0),
        vec4(0,0,1,0),
        vec4(0,0,0,1));

    gl_Position = worldViewProj * (rotation * tempVertex);
    UV = uv0.xy;
}
