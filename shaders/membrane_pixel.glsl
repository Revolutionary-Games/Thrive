#version 330

uniform sampler2D membraneTex;
uniform sampler2D membraneTexDamaged;
uniform vec4 membraneColour;
uniform float healthPercentage;

in vec2 UV;
out vec4 color;
in vec4 vp_color;

void main()
{
    color = (texture2D(membraneTex, UV) * healthPercentage + texture2D(membraneTexDamaged, UV) * (1 - healthPercentage)) * membraneColour;
}
