shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_test_disabled, depth_draw_never, cull_disabled, fog_disabled;

uniform sampler3D cloudNoise : filter_linear, repeat_enable;
uniform sampler2D depthTexture : hint_depth_texture, filter_nearest, repeat_disable;

// Prefixed so they can't collide with the locals of the shared modules, which read them through the macros below
uniform vec3 cloudPlanetCenter;
uniform float cloudInnerRadius;
uniform float cloudOuterRadius;
uniform float cloudTileSize;
uniform float cloudDensityMultiplier;
uniform float cloudCoverage;
uniform float cloudMaxDistance;
uniform vec3 cloudSunDirection;
uniform float cloudSunEnergy;

#define CLOUD_NOISE cloudNoise
#define CLOUD_TILE cloudTileSize
#define CLOUD_DENSITY_MULTIPLIER cloudDensityMultiplier
#define CLOUD_COVERAGE cloudCoverage
#define CLOUD_MAX_DISTANCE cloudMaxDistance

// Constant loop bounds, as GLES3 drivers handle those much better than uniform ones
// Note: these are hardcoded to be low because OpenGL is used on low end devices, so they wouldn't allow for more
// march steps anyway for performance reasons. In future, ShaderBuilder may be extended to support constant code
// substitution, which could be useful here to set select slightly lower (or higher) step amounts for multiple quality
// settings for OpenGL mode as well, but it's not strictly necessary.
#define CLOUD_BASE_STEPS 32
#define CLOUD_LIGHT_STEPS 4

// Spends the step budget near the camera instead of evenly along the ray. Comment out for uniform steps.
#define CLOUD_NEAR_WEIGHTED_STEPS

// How strongly steps are pulled towards the camera. This is an exponent, so 2 is quadratic.
#define CLOUD_STEP_EXPONENT 1.5

// Fades out the detail noise where the steps are too long to sample it. Comment out to always use full detail.
#define CLOUD_DETAIL_LOD
