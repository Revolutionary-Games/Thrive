//---------------------------------------------------------------------------
//These materials/shaders are part of the NEW InstanceManager implementation
//Written by Matias N. Goldberg ("dark_sylinc")
//---------------------------------------------------------------------------

uniform sampler2D diffuseMap;

uniform vec4	lightPosition;
uniform vec3	cameraPosition;
uniform vec3 	lightAmbient;
uniform vec3	lightDiffuse;
uniform vec3	lightSpecular;
uniform vec4	lightAttenuation;
uniform float	lightGloss;

#if DEPTH_SHADOWRECEIVER
uniform float invShadowMapSize;
uniform sampler2DShadow shadowMap;

//declare external function
//vec4 calcDepthShadow(in vec4 inColour, in float lum);
float calcDepthShadow(sampler2DShadow shadowMap, vec4 uv, float invShadowMapSize);
#endif

varying vec2 _uv0;
varying vec3 oNormal;
varying vec3 oVPos;
#if DEPTH_SHADOWRECEIVER
	varying vec4 oLightSpacePos;
#endif

//---------------------------------------------
//Main Pixel Shader
//---------------------------------------------
void main(void)
{
	vec4 color = texture2D( diffuseMap, _uv0 );

	float fShadow = 1.0;
#if DEPTH_SHADOWRECEIVER
	fShadow = calcDepthShadow( shadowMap, oLightSpacePos, invShadowMapSize );
#endif

	vec4 baseColour = texture2D( diffuseMap, _uv0 );

	//Blinn-Phong lighting
	vec3 normal	= normalize( oNormal );
	vec3 lightDir		= lightPosition.xyz - oVPos * lightPosition.w;
	vec3 eyeDir			= normalize( cameraPosition - oVPos );

	float fLength	= length( lightDir );
	lightDir			= normalize( lightDir );

	float NdotL	= max( 0.0, dot( normal, lightDir ) );
	vec3 halfVector		= normalize(lightDir + eyeDir);
	float HdotN	= max( 0.0, dot( halfVector, normal ) );
	
	vec3 ambient  = lightAmbient * baseColour.xyz;
	vec3 diffuse  = lightDiffuse * NdotL * baseColour.xyz;
	vec3 specular = lightSpecular * pow( HdotN, lightGloss );
	
	vec3 directLighting = (diffuse + specular) * fShadow;
	
	gl_FragColor = vec4( directLighting + ambient, baseColour.a );
	//gl_FragColor = baseColour;
}
