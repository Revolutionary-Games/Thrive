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
// Program Name: SGXLib_Lighting
// Program Desc: Per pixel lighting functions.
// Program Type: Vertex/Pixel shader
// Language: HLSL
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
void SGX_TransformNormal(in float4x4 m, 
				   in float3 v, 
				   out float3 vOut)
{
	vOut = mul((float3x3)m, v);
}

//-----------------------------------------------------------------------------
void SGX_TransformPosition(in float4x4 mWorldView, 
				   in float4 vPos, 
				   out float3 vOut)
{
	vOut = mul(mWorldView, vPos).xyz;
}

//-----------------------------------------------------------------------------
void SGX_Light_Directional_Diffuse(
				   in float3 vNormal,
				   in float3 vNegLightDirView,
				   in float3 vDiffuseColour, 
				   in float3 vBaseColour, 
				   out float3 vOut)
{
	float3 vNormalView = normalize(vNormal);
	float nDotL = dot(vNormalView, vNegLightDirView);
	
	vOut = vBaseColour + vDiffuseColour * saturate(nDotL);
}

//-----------------------------------------------------------------------------
void SGX_Light_Directional_DiffuseSpecular(
					in float3 vNormal,
					in float3 vViewPos,					
					in float3 vNegLightDirView,
					in float3 vDiffuseColour, 
					in float3 vSpecularColour, 
					in float fSpecularPower, 
					in float3 vBaseDiffuseColour,
					in float3 vBaseSpecularColour,					
					out float3 vOutDiffuse,
					out float3 vOutSpecular)
{
	vOutDiffuse  = vBaseDiffuseColour;
	vOutSpecular = vBaseSpecularColour;
	
	float3 vNormalView = normalize(vNormal);		
	float nDotL		   = dot(vNormalView, vNegLightDirView);			
	float3 vView       = -normalize(vViewPos);
	float3 vHalfWay    = normalize(vView + vNegLightDirView);
	float nDotH        = dot(vNormalView, vHalfWay);
	
	if (nDotL > 0)
	{
		vOutDiffuse  += vDiffuseColour * nDotL;		
		vOutSpecular += vSpecularColour * pow(saturate(nDotH), fSpecularPower);						
	}
}

//-----------------------------------------------------------------------------
void SGX_Light_Point_Diffuse(
				    in float3 vNormal,
				    in float3 vViewPos,
				    in float3 vLightPosView,
				    in float4 vAttParams,
				    in float3 vDiffuseColour, 
				    in float3 vBaseColour, 
				    out float3 vOut)
{
	vOut = vBaseColour;		
	
	float3 vLightView  = vLightPosView - vViewPos;
	float fLightD      = length(vLightView);
	float3 vNormalView = normalize(vNormal);
	float nDotL        = dot(vNormalView, normalize(vLightView));
	
	if (nDotL > 0 && fLightD <= vAttParams.x)
	{
		float fAtten	   = 1 / (vAttParams.y + vAttParams.z*fLightD + vAttParams.w*fLightD*fLightD);
			
		vOut += vDiffuseColour * nDotL * fAtten;
	}		
}



//-----------------------------------------------------------------------------
void SGX_Light_Point_DiffuseSpecular(
				    in float3 vNormal,
				    in float3 vViewPos,
				    in float3 vLightPosView,
				    in float4 vAttParams,
				    in float3 vDiffuseColour, 
				    in float3 vSpecularColour, 
					in float fSpecularPower, 
				    in float3 vBaseDiffuseColour,
					in float3 vBaseSpecularColour,					
					out float3 vOutDiffuse,
					out float3 vOutSpecular)
{
	vOutDiffuse  = vBaseDiffuseColour;
	vOutSpecular = vBaseSpecularColour;

	float3 vLightView  = vLightPosView - vViewPos;
	float fLightD      = length(vLightView);
	vLightView		   = normalize(vLightView);	
	float3 vNormalView = normalize(vNormal);
	float nDotL        = dot(vNormalView, vLightView);	
		
	if (nDotL > 0 && fLightD <= vAttParams.x)
	{					
		float3 vView       = -normalize(vViewPos);			
		float3 vHalfWay    = normalize(vView + vLightView);		
		float nDotH        = dot(vNormalView, vHalfWay);
		float fAtten	   = 1 / (vAttParams.y + vAttParams.z*fLightD + vAttParams.w*fLightD*fLightD);					
		
		vOutDiffuse  += vDiffuseColour * nDotL * fAtten;
		vOutSpecular += vSpecularColour * pow(saturate(nDotH), fSpecularPower) * fAtten;					
	}		
}

//-----------------------------------------------------------------------------
void SGX_Light_Spot_Diffuse(
				    in float3 vNormal,
				    in float3 vViewPos,
				    in float3 vLightPosView,
				    in float3 vNegLightDirView,
				    in float4 vAttParams,
				    in float3 vSpotParams,
				    in float3 vDiffuseColour, 
				    in float3 vBaseColour, 
				    out float3 vOut)
{
	vOut = vBaseColour;		
	
	float3 vLightView  = vLightPosView - vViewPos;
	float fLightD      = length(vLightView);
	vLightView		   = normalize(vLightView);
	float3 vNormalView = normalize(vNormal);
	float nDotL        = dot(vNormalView, vLightView);
	
	if (nDotL > 0 && fLightD <= vAttParams.x)
	{
		float fAtten	= 1 / (vAttParams.y + vAttParams.z*fLightD + vAttParams.w*fLightD*fLightD);
		float rho		= dot(vNegLightDirView, vLightView);						
		float fSpotE	= saturate((rho - vSpotParams.y) / (vSpotParams.x - vSpotParams.y));
		float fSpotT	= pow(fSpotE, vSpotParams.z);	
						
		vOut += vDiffuseColour * nDotL * fAtten * fSpotT;
	}		
}

//-----------------------------------------------------------------------------
void SGX_Light_Spot_DiffuseSpecular(
				    in float3 vNormal,
				    in float3 vViewPos,
				    in float3 vLightPosView,
				    in float3 vNegLightDirView,
				    in float4 vAttParams,
				    in float3 vSpotParams,
				    in float3 vDiffuseColour, 
				    in float3 vSpecularColour, 
					in float fSpecularPower, 
				    in float3 vBaseDiffuseColour,
					in float3 vBaseSpecularColour,					
					out float3 vOutDiffuse,
					out float3 vOutSpecular)
{
	vOutDiffuse  = vBaseDiffuseColour;		
	vOutSpecular = vBaseSpecularColour;
	
	float3 vLightView  = vLightPosView - vViewPos;
	float fLightD      = length(vLightView);
	vLightView		   = normalize(vLightView);
	float3 vNormalView = normalize(vNormal);
	float nDotL        = dot(vNormalView, vLightView);
	
	
	if (nDotL > 0 && fLightD <= vAttParams.x)
	{
		float3 vView       = -normalize(vViewPos);	
		float3 vHalfWay    = normalize(vView + vLightView);				
		float nDotH        = dot(vNormalView, vHalfWay);
		float fAtten	= 1 / (vAttParams.y + vAttParams.z*fLightD + vAttParams.w*fLightD*fLightD);
		float rho		= dot(vNegLightDirView, vLightView);						
		float fSpotE	= saturate((rho - vSpotParams.y) / (vSpotParams.x - vSpotParams.y));
		float fSpotT	= pow(fSpotE, vSpotParams.z);	
						
		vOutDiffuse  += vDiffuseColour * nDotL * fAtten * fSpotT;
		vOutSpecular += vSpecularColour * pow(saturate(nDotH), fSpecularPower) * fAtten * fSpotT;
	}		
}
