#version 120
/*
-----------------------------------------------------------------------------
This source file is part of OGRE
(Object-oriented Graphics Rendering Engine)
For the latest info, see http://www.ogre3d.org

Copyright (c) 2000-2012 Torus Knot Software Ltd
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
-----------------------------------------------------------------------------
*/

//-----------------------------------------------------------------------------
// Program Name: FFPLib_Fog
// Program Desc: Fog functions of the FFP.
// Program Type: Vertex/Pixel shader
// Language: GLSL
// Notes: Implements core functions needed by FFPFog class.
// Based on fog engine. 
// See http://msdn.microsoft.com/en-us/library/bb173398.aspx
// Vertex based fog: the w component of the out position is used
// as the distance parameter to fog formulas. This is basically the z coordinate
// in world space. See pixel fog under D3D docs. The fog factor is computed according 
// to each formula, then clamped and output to the pixel shader.
// Pixel based fog: the w component of the out position is passed to pixel shader
// that computes the fog factor based on it.
// Both techniques use the fog factor in the end of the pixel shader to blend
// the output color with the fog color.
//-----------------------------------------------------------------------------



//-----------------------------------------------------------------------------
void FFP_VertexFog_Linear(in mat4 mWorldViewProj, 
				  in vec4 pos, 				   
				  in vec4 fogParams,				   
				  out float oFogFactor)
{
	vec4 vOutPos  = mWorldViewProj * pos;
	float distance  = abs(vOutPos.w);	
	float fogFactor = (fogParams.z - distance) * fogParams.w;
	
	oFogFactor  = clamp(fogFactor, 0.0, 1.0);	
}

//-----------------------------------------------------------------------------
void FFP_VertexFog_Exp(in mat4 mWorldViewProj, 
			     in vec4 pos, 				   
			     in vec4 fogParams,				   
			     out float oFogFactor)
{
	vec4 vOutPos  = mWorldViewProj * pos;
	float distance  = abs(vOutPos.w);	
	float exp       = distance*fogParams.x;
	float fogFactor = 1.0 / pow(2.71828, exp);
	
	oFogFactor  = clamp(fogFactor, 0.0, 1.0);	
}

//-----------------------------------------------------------------------------
void FFP_VertexFog_Exp2(in mat4 mWorldViewProj, 
				   in vec4 pos, 				   
				   in vec4 fogParams,				   
				   out float oFogFactor)
{
	vec4 vOutPos  = mWorldViewProj * pos;
	float distance  = abs(vOutPos.w);	
	float exp       = (distance*fogParams.x*distance*fogParams.x);
	float fogFactor = 1.0 / pow(2.71828, exp);
	
	oFogFactor  = clamp(fogFactor, 0.0, 1.0);	
}


//-----------------------------------------------------------------------------
void FFP_PixelFog_Depth(in mat4 mWorldViewProj, 
				   in vec4 pos, 				   				   				   
				   out float oDepth)
{
	vec4 vOutPos  = mWorldViewProj * pos;
	oDepth			= vOutPos.w;	
}

//-----------------------------------------------------------------------------
void FFP_PixelFog_Linear(in float depth,		   
				   in vec4 fogParams,				   
				   in vec4 fogColor,
				   in vec4 baseColor,
				   out vec4 oColor)
{
	float distance = abs(depth);
	float fogFactor = clamp((fogParams.z - distance) * fogParams.w, 0.0, 1.0);
	
	oColor = mix(fogColor, baseColor, fogFactor);
}

//-----------------------------------------------------------------------------
void FFP_PixelFog_Exp(in float depth,		   
				   in vec4 fogParams,				   
				   in vec4 fogColor,
				   in vec4 baseColor,
				   out vec4 oColor)
{
	float distance  = abs(depth);	
	float exp       = (distance*fogParams.x);
	float fogFactor = clamp(1.0 / pow(2.71828, exp), 0.0, 1.0);
	
	oColor = mix(fogColor, baseColor, fogFactor);
}

//-----------------------------------------------------------------------------
void FFP_PixelFog_Exp2(in float depth,		   
				   in vec4 fogParams,				   
				   in vec4 fogColor,
				   in vec4 baseColor,
				   out vec4 oColor)
{
	float distance  = abs(depth);	
	float exp       = (distance*fogParams.x*distance*fogParams.x);
	float fogFactor = clamp(1.0 / pow(2.71828, exp), 0.0, 1.0);
	
	oColor = mix(fogColor, baseColor, fogFactor);		
}
