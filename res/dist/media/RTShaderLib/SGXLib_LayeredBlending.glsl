#version 120

/*
** layered blending & misc math
** Blending modes, RGB/HSL/Contrast/Desaturate, levels control
**
** The shaders below are base on the shaders created by:
** Romain Dura | Romz
** Blog: http://blog.mouaif.org
** Post: http://blog.mouaif.org/?p=94
*/


/*
** Desaturation
*/

vec4 Desaturate(in vec3 color, in float Desaturation)
{
	vec3 grayXfer = vec3(0.3, 0.59, 0.11);
	vec3 gray = vec3(dot(grayXfer, color));
	return vec4(mix(color, gray, Desaturation), 1.0);
}


/*
** Hue, saturation, luminance
*/

vec3 RGBToHSL(in vec3 color)
{
	vec3 hsl; // init to 0 to avoid warnings ? (and reverse if + remove first part)
	
	float fmin = min(min(color.r, color.g), color.b);    //Min. value of RGB
	float fmax = max(max(color.r, color.g), color.b);    //Max. value of RGB
	float delta = fmax - fmin;             //Delta RGB value

	hsl.z = (fmax + fmin) / 2.0; // Luminance

	if (delta == 0.0)		//This is a gray, no chroma...
	{
		hsl.x = 0.0;	// Hue
		hsl.y = 0.0;	// Saturation
	}
	else                                    //Chromatic data...
	{
		if (hsl.z < 0.5)
			hsl.y = delta / (fmax + fmin); // Saturation
		else
			hsl.y = delta / (2.0 - fmax - fmin); // Saturation
		
		float deltaR = (((fmax - color.r) / 6.0) + (delta / 2.0)) / delta;
		float deltaG = (((fmax - color.g) / 6.0) + (delta / 2.0)) / delta;
		float deltaB = (((fmax - color.b) / 6.0) + (delta / 2.0)) / delta;

		if (color.r == fmax )
			hsl.x = deltaB - deltaG; // Hue
		else if (color.g == fmax)
			hsl.x = (1.0 / 3.0) + deltaR - deltaB; // Hue
		else if (color.b == fmax)
			hsl.x = (2.0 / 3.0) + deltaG - deltaR; // Hue

		if (hsl.x < 0.0)
			hsl.x += 1.0; // Hue
		else if (hsl.x > 1.0)
			hsl.x -= 1.0; // Hue
	}

	return hsl;
}

float HueToRGB(in float f1, in float f2, in float hue)
{
	if (hue < 0.0)
		hue += 1.0;
	else if (hue > 1.0)
		hue -= 1.0;
	float res;
	if ((6.0 * hue) < 1.0)
		res = f1 + (f2 - f1) * 6.0 * hue;
	else if ((2.0 * hue) < 1.0)
		res = f2;
	else if ((3.0 * hue) < 2.0)
		res = f1 + (f2 - f1) * ((2.0 / 3.0) - hue) * 6.0;
	else
		res = f1;
	return res;
}

vec3 HSLToRGB(in vec3 hsl)
{
	vec3 rgb;
	
	if (hsl.y == 0.0)
		rgb = vec3(hsl.z); // Luminance
	else
	{
		float f2;
		
		if (hsl.z < 0.5)
			f2 = hsl.z * (1.0 + hsl.y);
		else
			f2 = (hsl.z + hsl.y) - (hsl.y * hsl.z);
			
		float f1 = 2.0 * hsl.z - f2;
		
		rgb.r = HueToRGB(f1, f2, hsl.x + (1.0/3.0));
		rgb.g = HueToRGB(f1, f2, hsl.x);
		rgb.b = HueToRGB(f1, f2, hsl.x - (1.0/3.0));
	}
	
	return rgb;
}


/*
** Contrast, saturation, brightness
** Code of this function is from TGM's shader pack
** http://irrlicht.sourceforge.net/phpBB2/viewtopic.php?t=21057
*/

// For all settings: 1.0 = 100% 0.5=50% 1.5 = 150%
vec3 ContrastSaturationBrightness(in vec3 color, in float brt, in float sat, in float con)
{
	// Increase or decrease these values to adjust r, g and b color channels separately
	const float AvgLumR = 0.5;
	const float AvgLumG = 0.5;
	const float AvgLumB = 0.5;
	
	const vec3 LumCoeff = vec3(0.2125, 0.7154, 0.0721);
	
	vec3 AvgLumin = vec3(AvgLumR, AvgLumG, AvgLumB);
	vec3 brtColor = color * brt;
	vec3 intensity = vec3(dot(brtColor, LumCoeff));
	vec3 satColor = mix(intensity, brtColor, sat);
	vec3 conColor = mix(AvgLumin, satColor, con);
	return conColor;
}

/*
** Float blending modes
** Adapted from here: http://www.nathanm.com/photoshop-blending-math/
** But I modified the HardMix (wrong condition), Overlay, SoftLight, ColorDodge, ColorBurn, VividLight, PinLight (inverted layers) ones to have correct results
*/

#define BlendLinearDodgef 				BlendAddf
#define BlendLinearBurnf 				BlendSubtractf
#define BlendAddf(base, blend) 			min(base + blend, 1.0)
#define BlendSubtractf(base, blend) 	max(base + blend - 1.0, 0.0)
#define BlendLightenf(base, blend) 		max(blend, base)
#define BlendDarkenf(base, blend) 		min(blend, base)
#define BlendScreenf(base, blend) 		(1.0 - ((1.0 - base) * (1.0 - blend)))
#define BlendOverlayf(base, blend) 		(base < 0.5 ? (2.0 * base * blend) : (1.0 - 2.0 * (1.0 - base) * (1.0 - blend)))
#define BlendSoftLightf(base, blend) 	((blend < 0.5) ? (2.0 * base * blend + base * base * (1.0 - 2.0 * blend)) : (sqrt(base) * (2.0 * blend - 1.0) + 2.0 * base * (1.0 - blend)))
#define BlendColorDodgef(base, blend) 	((blend == 1.0) ? blend : min(base / (1.0 - blend), 1.0))
#define BlendColorBurnf(base, blend) 	((blend == 0.0) ? blend : max((1.0 - ((1.0 - base) / blend)), 0.0))
#define BlendHardMixf(base, blend) 		((BlendVividLightf(base, blend) < 0.5) ? 0.0 : 1.0)



/*
** Vector3 blending modes
*/

// Component wise blending
#define Blend1(base, blend, funcf) 		funcf(base, blend)
#define Blend3(base, blend, funcf) 		vec3(funcf(base.r, blend.r), funcf(base.g, blend.g), funcf(base.b, blend.b))
#define Blend4(base, blend, funcf) 		vec4(funcf(base.r, blend.r), funcf(base.g, blend.g), funcf(base.b, blend.b), funcf(base.a, blend.a))

#define BlendNormal(base, blend) 		(base)
#define BlendMultiply(base, blend) 		(base * blend)
#define BlendAverage(base, blend) 		((base + blend) / 2.0)
#define BlendAdd(base, blend) 		min(base + blend, 1.0)
#define BlendSubtract(base, blend) 	max(base + blend - 1.0, 0.0)
#define BlendDifference(base, blend) 	abs(base - blend)
#define BlendNegation(base, blend) 	(1.0 - abs(1.0 - base - blend))
#define BlendExclusion(base, blend) 	(base + blend - 2.0 * base * blend)
#define BlendPhoenix(base, blend) 		(min(base, blend) - max(base, blend) + 1.0)
#define BlendOpacity(base, blend, F, O) 	(F(base, blend) * O + blend * (1.0 - O))

// Hue Blend mode creates the result color by combining the luminance and saturation of the base color with the hue of the blend color.
float BlendHue1(in float base, in float blend)
{
	return base;
}

vec3 BlendHue3(in vec3 base, in vec3 blend)
{
	vec3 baseHSL = RGBToHSL(base);
	return HSLToRGB(vec3(RGBToHSL(blend).r, baseHSL.g, baseHSL.b));
}

vec4 BlendHue4(in vec4 base, in vec4 blend)
{
	vec3 hue = BlendHue3(base.xyz, blend.xyz);
	return vec4(hue.x, hue.y, hue.z, BlendHue1(base.w, blend.w));
}

// Saturation Blend mode creates the result color by combining the luminance and hue of the base color with the saturation of the blend color.
float BlendSaturation1(in float base, in float blend)
{
	return base;
}

vec3 BlendSaturation3(in vec3 base, in vec3 blend)
{
	vec3 baseHSL = RGBToHSL(base);
	return HSLToRGB(vec3(baseHSL.r, RGBToHSL(blend).g, baseHSL.b));
}

vec4 BlendSaturation4(in vec4 base, in vec4 blend)
{
	vec3 hue = BlendSaturation3(base.xyz, blend.xyz);
	return vec4(hue.x, hue.y, hue.z, BlendSaturation1(base.w, blend.w));
}

// Color Mode keeps the brightness of the base color and applies both the hue and saturation of the blend color.
float BlendColor1(in float base, in float blend)
{
	return base;
}

vec3 BlendColor3(in vec3 base, in vec3 blend)
{
	vec3 blendHSL = RGBToHSL(blend);
	return HSLToRGB(vec3(blendHSL.r, blendHSL.g, RGBToHSL(base).b));
}

vec4 BlendColor4(in vec4 base, in vec4 blend)
{
	vec3 hue = BlendColor3(base.xyz, blend.xyz);
	return vec4(hue.x, hue.y, hue.z, BlendColor1(base.w, blend.w));
}


// Luminosity Blend mode creates the result color by combining the hue and saturation of the base color with the luminance of the blend color.
float BlendLuminosity1(in float base, in float blend)
{
	return base;
}

vec3 BlendLuminosity3(in vec3 base, in vec3 blend)
{
	vec3 baseHSL = RGBToHSL(base);
	return HSLToRGB(vec3(baseHSL.r, baseHSL.g, RGBToHSL(blend).b));
}

vec4 BlendLuminosity4(in vec4 base, in vec4 blend)
{
	vec3 hue = BlendLuminosity3(base.xyz, blend.xyz);
	return vec4(hue.x, hue.y, hue.z, BlendLuminosity1(base.w, blend.w));
}

float BlendLinearLightf(in float s1, in float s2)
{
	float oColor;
	
	if (s2 < 0.5)
	{
		float s2x = (2.0 * s2);
		oColor = BlendSubtractf(s1, s2x);
	}
	else
	{	 
		float s2x = (2.0 * (s2 - 0.5));
		oColor = BlendAddf(s1, s2x);
	}
	
	return oColor;
}

float BlendVividLightf(in float s1, in float s2)
{
	float oColor;
	
	if (s2 < 0.5)
	{
		float s2x = (2.0 * s2);
		oColor = BlendColorBurnf(s1, s2x);
	}
	else
	{	 
		float s2x = (2.0 * (s2 - 0.5));
		oColor = BlendColorDodgef(s1, s2x);
	}
	
	return oColor;
}

float BlendPinLightf(in float s1, in float s2)
{
	float oColor;
	
	if (s2 < 0.5)
	{
		float s2x = (2.0 * s2);
		oColor = BlendDarkenf(s1, s2x);
	}
	else
	{	 
		float s2x = (2.0 * (s2 - 0.5));
		oColor = BlendLightenf(s1, s2x);
	}
	
	return oColor;
}

float BlendReflectf(in float s1, in float s2)
{
	float oColor;
	
	if (s2 == 1.0)
	{
		oColor = s2;
	}
	else
	{	 
		float s1x = (s1 * s1) / (1.0 - s2);
		
		oColor = min(s1x, 1.0);
	}
	
	return oColor;
}

//------------------------------------
// Interface for RTShader
//------------------------------------


void SGX_blend_normal(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendNormal(basePixel, blendPixel);
}

void SGX_blend_normal(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendNormal(basePixel, blendPixel);
}

void SGX_blend_normal(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendNormal(basePixel, blendPixel);
}


void SGX_blend_lighten(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendLightenf(basePixel, blendPixel);
}

void SGX_blend_lighten(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendLightenf(basePixel, blendPixel);
}

void SGX_blend_lighten(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendLightenf(basePixel, blendPixel);
}


void SGX_blend_darken(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendDarkenf(basePixel, blendPixel);
}

void SGX_blend_darken(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendDarkenf(basePixel, blendPixel);
}

void SGX_blend_darken(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendDarkenf(basePixel, blendPixel);
}


void SGX_blend_multiply(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendMultiply(basePixel, blendPixel);
}

void SGX_blend_multiply(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendMultiply(basePixel, blendPixel);
}

void SGX_blend_multiply(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendMultiply(basePixel, blendPixel);
}


void SGX_blend_average(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendAverage(basePixel, blendPixel);
}

void SGX_blend_average(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendAverage(basePixel, blendPixel);
}

void SGX_blend_average(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendAverage(basePixel, blendPixel);
}


void SGX_blend_add(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendAdd(basePixel, blendPixel);
}

void SGX_blend_add(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendAdd(basePixel, blendPixel);
}

void SGX_blend_add(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendAdd(basePixel, blendPixel);
}


void SGX_blend_subtract(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendSubtract(basePixel, blendPixel);
}

void SGX_blend_subtract(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendSubtract(basePixel, blendPixel);
}

void SGX_blend_subtract(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendSubtract(basePixel, blendPixel);
}


void SGX_blend_difference(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendDifference(basePixel, blendPixel);
}
void SGX_blend_difference(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendDifference(basePixel, blendPixel);
}
void SGX_blend_difference(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendDifference(basePixel, blendPixel);
}


void SGX_blend_negation(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendNegation(basePixel, blendPixel);
}
void SGX_blend_negation(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendNegation(basePixel, blendPixel);
}
void SGX_blend_negation(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendNegation(basePixel, blendPixel);
}


void SGX_blend_exclusion(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendExclusion(basePixel, blendPixel);
}
void SGX_blend_exclusion(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendExclusion(basePixel, blendPixel);
}
void SGX_blend_exclusion(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendExclusion(basePixel, blendPixel);
}


void SGX_blend_screen(in vec4 s1, in vec4 s2, out vec4 oColor)
{	
	oColor = vec4(BlendScreenf(s1.r, s2.r), 
		BlendScreenf(s1.g, s2.g), 
		BlendScreenf(s1.b, s2.b), 
		BlendScreenf(s1.a, s2.a));
}
void SGX_blend_screen(in vec3 s1, in vec3 s2, out vec3 oColor)
{
	oColor = vec3(BlendScreenf(s1.r, s2.r), 
		BlendScreenf(s1.g, s2.g), 
		BlendScreenf(s1.b, s2.b));
}
void SGX_blend_screen(in float s1, in float s2, out float oColor)
{
	oColor = BlendScreenf(s1, s2);
}


void SGX_blend_overlay(in vec4 s1, in vec4 s2, out vec4 oColor)
{
	oColor = vec4(BlendOverlayf(s1.r, s2.r), 
		BlendOverlayf(s1.g, s2.g), 
		BlendOverlayf(s1.b, s2.b), 
		BlendOverlayf(s1.a, s2.a));
}
void SGX_blend_overlay(in vec3 s1, in vec3 s2, out vec3 oColor)
{
	oColor = vec3(BlendOverlayf(s1.r, s2.r), 
		BlendOverlayf(s1.g, s2.g), 
		BlendOverlayf(s1.b, s2.b));
}
void SGX_blend_overlay(in float s1, in float s2, out float oColor)
{
	oColor = BlendOverlayf(s1, s2);
}


void SGX_blend_softLight(in vec4 s1, in vec4 s2, out vec4 oColor)
{
	oColor = vec4(BlendSoftLightf(s1.r, s2.r), 
		BlendSoftLightf(s1.g, s2.g), 
		BlendSoftLightf(s1.b, s2.b), 
		BlendSoftLightf(s1.a, s2.a));
}
void SGX_blend_softLight(in vec3 s1, in vec3 s2, out vec3 oColor)
{
	oColor = vec3(BlendSoftLightf(s1.r, s2.r), 
		BlendSoftLightf(s1.g, s2.g), 
		BlendSoftLightf(s1.b, s2.b));
}
void SGX_blend_softLight(in float s1, in float s2, out float oColor)
{
	oColor = BlendSoftLightf(s1, s2);
}


void SGX_blend_hardLight(in vec4 s1, in vec4 s2, out vec4 oColor)
{
	oColor = vec4(BlendOverlayf(s1.r, s2.r), 
		BlendOverlayf(s1.g, s2.g), 
		BlendOverlayf(s1.b, s2.b), 
		BlendOverlayf(s1.a, s2.a));
}
void SGX_blend_hardLight(in vec3 s1, in vec3 s2, out vec3 oColor)
{
	oColor = vec3(BlendOverlayf(s1.r, s2.r), 
		BlendOverlayf(s1.g, s2.g), 
		BlendOverlayf(s1.b, s2.b));
}
void SGX_blend_hardLight(in float s1, in float s2, out float oColor)
{
	oColor = BlendOverlayf(s1, s2);
}


void SGX_blend_colorDodge(in vec4 s1, in vec4 s2, out vec4 oColor)
{
	oColor = vec4(BlendColorDodgef(s1.r, s2.r), 
		BlendColorDodgef(s1.g, s2.g), 
		BlendColorDodgef(s1.b, s2.b), 
		BlendColorDodgef(s1.a, s2.a));
}
void SGX_blend_colorDodge(in vec3 s1, in vec3 s2, out vec3 oColor)
{
	oColor = vec3(BlendColorDodgef(s1.r, s2.r), 
		BlendColorDodgef(s1.g, s2.g), 
		BlendColorDodgef(s1.b, s2.b));
}
void SGX_blend_colorDodge(in float s1, in float s2, out float oColor)
{
	oColor = BlendColorDodgef(s1, s2);
}


void SGX_blend_colorBurn(in vec4 s1, in vec4 s2, out vec4 oColor)
{
	oColor = vec4(BlendColorBurnf(s1.r, s2.r), 
		BlendColorBurnf(s1.g, s2.g), 
		BlendColorBurnf(s1.b, s2.b), 
		BlendColorBurnf(s1.a, s2.a));
}
void SGX_blend_colorBurn(in vec3 s1, in vec3 s2, out vec3 oColor)
{
	oColor = vec3(BlendColorBurnf(s1.r, s2.r), 
		BlendColorBurnf(s1.g, s2.g), 
		BlendColorBurnf(s1.b, s2.b));
}
void SGX_blend_colorBurn(in float s1, in float s2, out float oColor)
{
	oColor = BlendColorBurnf(s1, s2);
}


void SGX_blend_linearDodge(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendAddf(basePixel, blendPixel);
}
void SGX_blend_linearDodge(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendAddf(basePixel, blendPixel);
}
void SGX_blend_linearDodge(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendAddf(basePixel, blendPixel);
}


void SGX_blend_linearBurn(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendSubtractf(basePixel, blendPixel);
}
void SGX_blend_linearBurn(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendSubtractf(basePixel, blendPixel);
}
void SGX_blend_linearBurn(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendSubtractf(basePixel, blendPixel);
}


void SGX_blend_linearLight(in vec4 s1, in vec4 s2, out vec4 oColor)
{
	oColor = vec4(BlendLinearLightf(s1.r, s2.r), 
		BlendLinearLightf(s1.g, s2.g), 
		BlendLinearLightf(s1.b, s2.b), 
		BlendLinearLightf(s1.a, s2.a));
}
void SGX_blend_linearLight(in vec3 s1, in vec3 s2, out vec3 oColor)
{
	oColor = vec3(BlendLinearLightf(s1.r, s2.r), 
		BlendLinearLightf(s1.g, s2.g), 
		BlendLinearLightf(s1.b, s2.b));
}
void SGX_blend_linearLight(in float s1, in float s2, out float oColor)
{
	oColor = BlendLinearLightf(s1, s2);
}


void SGX_blend_vividLight(in vec4 s1, in vec4 s2, out vec4 oColor)
{
	oColor = vec4(BlendVividLightf(s1.r, s2.r), 
		BlendVividLightf(s1.g, s2.g), 
		BlendVividLightf(s1.b, s2.b), 
		BlendVividLightf(s1.a, s2.a));
}
void SGX_blend_vividLight(in vec3 s1, in vec3 s2, out vec3 oColor)
{
	oColor = vec3(BlendVividLightf(s1.r, s2.r), 
		BlendVividLightf(s1.g, s2.g), 
		BlendVividLightf(s1.b, s2.b));
}
void SGX_blend_vividLight(in float s1, in float s2, out float oColor)
{
	oColor = BlendVividLightf(s1, s2);
}


void SGX_blend_pinLight(in vec4 s1, in vec4 s2, out vec4 oColor)
{
	oColor = vec4(BlendPinLightf(s1.r, s2.r), 
		BlendPinLightf(s1.g, s2.g), 
		BlendPinLightf(s1.b, s2.b), 
		BlendPinLightf(s1.a, s2.a));
}
void SGX_blend_pinLight(in vec3 s1, in vec3 s2, out vec3 oColor)
{
	oColor = vec3(BlendPinLightf(s1.r, s2.r), 
		BlendPinLightf(s1.g, s2.g), 
		BlendPinLightf(s1.b, s2.b));
}
void SGX_blend_pinLight(in float s1, in float s2, out float oColor)
{
	oColor = BlendPinLightf(s1, s2);
}


void SGX_blend_hardMix(in vec4 s1, in vec4 s2, out vec4 oColor)
{
	oColor = vec4(BlendHardMixf(s1.r, s2.r), 
		BlendHardMixf(s1.g, s2.g), 
		BlendHardMixf(s1.b, s2.b), 
		BlendHardMixf(s1.a, s2.a));
}
void SGX_blend_hardMix(in vec3 s1, in vec3 s2, out vec3 oColor)
{
	oColor = vec3(BlendHardMixf(s1.r, s2.r), 
		BlendHardMixf(s1.g, s2.g), 
		BlendHardMixf(s1.b, s2.b));
}
void SGX_blend_hardMix(in float s1, in float s2, out float oColor)
{
	oColor = BlendHardMixf(s1, s2);
}

void SGX_blend_reflect(in vec4 s1, in vec4 s2, out vec4 oColor)
{
	oColor = vec4(BlendReflectf(s1.r, s2.r), 
		BlendReflectf(s1.g, s2.g), 
		BlendReflectf(s1.b, s2.b), 
		BlendReflectf(s1.a, s2.a));
}
void SGX_blend_reflect(in vec3 s1, in vec3 s2, out vec3 oColor)
{
	oColor = vec3(BlendReflectf(s1.r, s2.r), 
		BlendReflectf(s1.g, s2.g), 
		BlendReflectf(s1.b, s2.b));
}
void SGX_blend_reflect(in float s1, in float s2, out float oColor)
{
	oColor = BlendReflectf(s1, s2);
}


void SGX_blend_glow(in vec4 s1, in vec4 s2, out vec4 oColor)
{
	oColor = vec4(BlendReflectf(s1.r, s2.r), 
		BlendReflectf(s1.g, s2.g), 
		BlendReflectf(s1.b, s2.b), 
		BlendReflectf(s1.a, s2.a));
}
void SGX_blend_glow(in vec3 s1, in vec3 s2, out vec3 oColor)
{
	oColor = vec3(BlendReflectf(s1.r, s2.r), 
		BlendReflectf(s1.g, s2.g), 
		BlendReflectf(s1.b, s2.b));
}
void SGX_blend_glow(in float s1, in float s2, out float oColor)
{
	oColor = BlendReflectf(s1, s2);
}


void SGX_blend_phoenix(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendPhoenix(basePixel, blendPixel);
}
void SGX_blend_phoenix(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendPhoenix(basePixel, blendPixel);
}
void SGX_blend_phoenix(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendPhoenix(basePixel, blendPixel);
}


void SGX_blend_saturation(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendSaturation4(basePixel, blendPixel);
}
void SGX_blend_saturation(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendSaturation3(basePixel, blendPixel);
}
void SGX_blend_saturation(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendSaturation1(basePixel, blendPixel);
}


void SGX_blend_color(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendColor4(basePixel, blendPixel);
}
void SGX_blend_color(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendColor3(basePixel, blendPixel);
}
void SGX_blend_color(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendColor1(basePixel, blendPixel);
}


void SGX_blend_luminosity(in vec4 basePixel, in vec4 blendPixel, out vec4 oColor)
{
	oColor = BlendLuminosity4(basePixel, blendPixel);
}
void SGX_blend_luminosity(in vec3 basePixel, in vec3 blendPixel, out vec3 oColor)
{
	oColor = BlendLuminosity3(basePixel, blendPixel);
}
void SGX_blend_luminosity(in float basePixel, in float blendPixel, out float oColor)
{
	oColor = BlendLuminosity1(basePixel, blendPixel);
}


////////////////////////////////////////////////////////////////////////////////////
/// Source modification functions
////////////////////////////////////////////////////////////////////////////////////


void SGX_src_mod_modulate(in vec4 iColor, in vec4 controlVal, out vec4 oColor)
{
	oColor = iColor * controlVal;
}
void SGX_src_mod_modulate(in vec3 iColor, in vec3 controlVal, out vec3 oColor)
{
	oColor = iColor * controlVal;
}
void SGX_src_mod_modulate(in float iColor, in float controlVal, out float oColor)
{
	oColor = iColor * controlVal;
}

void SGX_src_mod_inv_modulate(in vec4 iColor, in vec4 controlVal, out vec4 oColor)
{
	oColor = mix(iColor, vec4(1.0,1.0,1.0,1.0), controlVal);
}
void SGX_src_mod_inv_modulate(in vec3 iColor, in vec3 controlVal, out vec3 oColor)
{
	oColor = mix(iColor, vec3(1.0,1.0,1.0), controlVal);
}
void SGX_src_mod_inv_modulate(in float iColor, in float controlVal, out float oColor)
{
	oColor = mix(iColor, 1, controlVal);
}