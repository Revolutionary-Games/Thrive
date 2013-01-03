uniform sampler2D inRTT;
uniform sampler2D inLum;
uniform vec2 texelSize;

varying vec2 uv;
const vec4 BRIGHT_LIMITER = vec4(0.6, 0.6, 0.6, 0.0);

// declare external function
vec4 toneMap(in vec4 inColour, in float lum);

void main(void)
{
    vec4 accum = vec4(0.0, 0.0, 0.0, 0.0);

    accum += texture2D(inRTT, uv + texelSize * vec2(-1.0, -1.0));
    accum += texture2D(inRTT, uv + texelSize * vec2( 0.0, -1.0));
    accum += texture2D(inRTT, uv + texelSize * vec2( 1.0, -1.0));
    accum += texture2D(inRTT, uv + texelSize * vec2(-1.0,  0.0));
    accum += texture2D(inRTT, uv + texelSize * vec2( 0.0,  0.0));
    accum += texture2D(inRTT, uv + texelSize * vec2( 1.0,  0.0));
    accum += texture2D(inRTT, uv + texelSize * vec2(-1.0,  1.0));
    accum += texture2D(inRTT, uv + texelSize * vec2( 0.0,  1.0));
    accum += texture2D(inRTT, uv + texelSize * vec2( 1.0,  1.0));
    
	// take average of 9 samples
	accum *= 0.1111111111111111;

    // Reduce bright and clamp
    accum = max(vec4(0.0, 0.0, 0.0, 1.0), accum - BRIGHT_LIMITER);

	// Sample the luminence texture
	vec4 lum = texture2D(inLum, vec2(0.5, 0.5));
	
	// Tone map result
	gl_FragColor = toneMap(accum, lum.r);

}
