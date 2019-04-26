#version 330

uniform sampler2D tex;

in vec2 UV;

out vec4 color;

void main()
{
	color = texture2D(tex, UV);
}
