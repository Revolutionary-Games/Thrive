#version 330

uniform sampler2D tex0;
uniform sampler2D tex1;
uniform sampler2D tex2;
uniform sampler2D tex3;

in vec2 UV0;
in vec2 UV1;
in vec2 UV2;
in vec2 UV3;

out vec4 color;

void main()
{
    vec4 layer0 = texture2D(tex0, UV0);
    vec4 layer1 = texture2D(tex1, UV1);
    vec4 layer2 = texture2D(tex2, UV2);
    vec4 layer3 = texture2D(tex3, UV3);
    
    color.rgb =
          layer0.rgb * layer0.a * 1.0f
        + layer1.rgb * layer1.a * 0.7f
        + layer2.rgb * layer2.a * 0.7f
        + layer3.rgb * layer3.a * 0.7f;
    
    color.a = 1.0;
}
