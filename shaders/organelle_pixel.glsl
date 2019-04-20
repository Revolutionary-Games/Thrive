#version 330

uniform sampler2D tex;
uniform vec4 organelleColour = vec4(1.0, 1.0, 1.0, 1.0);

in vec2 UV;

out vec4 color;

void main()
{
	color = texture2D(tex, UV) * 0.8f * organelleColour;
}
