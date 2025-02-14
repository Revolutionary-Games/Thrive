// NOTE: Shader automatically converted from Godot Engine 4.3.stable.mono's ParticleProcessMaterial.

shader_type particles;
render_mode disable_velocity;

uniform vec3 direction;
uniform float spread;
uniform float flatness;
uniform float inherit_emitter_velocity_ratio = 0.0;
uniform vec3 emission_shape_offset = vec3(0.0);
uniform vec3 emission_shape_scale = vec3(1.0);
uniform vec3 velocity_pivot = vec3(0.0);
uniform vec3 emission_box_extents;
uniform vec4 color_value : source_color;
uniform sampler2D alpha_curve : repeat_disable;

uniform float game_time;

uniform sampler3D noiseDisturbancesX;
uniform sampler3D noiseDisturbancesY;

uniform sampler3D noiseCurrentsX;
uniform sampler3D noiseCurrentsY;

float rand_from_seed(inout uint seed) {
	int k;
	int s = int(seed);
	if (s == 0) {
		s = 305420679;
	}
	k = s / 127773;
	s = 16807 * (s - k * 127773) - 2836 * k;
	if (s < 0) {
		s += 2147483647;
	}
	seed = uint(s);
	return float(seed % uint(65536)) / 65535.0;
}

float rand_from_seed_m1_p1(inout uint seed) {
	return rand_from_seed(seed) * 2.0 - 1.0;
}

uint hash(uint x) {
	x = ((x >> uint(16)) ^ x) * uint(73244475);
	x = ((x >> uint(16)) ^ x) * uint(73244475);
	x = (x >> uint(16)) ^ x;
	return x;
}

struct DisplayParameters {
	vec3 scale;
	float hue_rotation;
	float animation_speed;
	float animation_offset;
	float lifetime;
	vec4 color;
};

struct DynamicsParameters {
	float angle;
	float angular_velocity;
	float initial_velocity_multiplier;
	float directional_velocity;
	float radial_velocity;
	float orbit_velocity;
};

struct PhysicalParameters {
	float linear_accel;
	float radial_accel;
	float tangent_accel;
	float damping;
};

void calculate_initial_physical_params(inout PhysicalParameters params, inout uint alt_seed) {
	params.linear_accel = 0.0f;
	params.radial_accel = 0.0f;
	params.tangent_accel = 0.0f;
	params.damping = 1.0f;
}

void calculate_initial_dynamics_params(inout DynamicsParameters params, inout uint alt_seed) {
	// -------------------- DO NOT REORDER OPERATIONS, IT BREAKS VISUAL COMPATIBILITY
	// -------------------- ADD NEW OPERATIONS AT THE BOTTOM
	params.angle = 0.0f;
	params.angular_velocity = 0.0f;
	params.initial_velocity_multiplier = 0.0f;
	params.directional_velocity = 0.0f;
	params.radial_velocity = 0.0f;
	params.orbit_velocity = 0.0f;
}

void calculate_initial_display_params(inout DisplayParameters params, inout uint alt_seed) {
	// -------------------- DO NOT REORDER OPERATIONS, IT BREAKS VISUAL COMPATIBILITY
	// -------------------- ADD NEW OPERATIONS AT THE BOTTOM
	float pi = 3.14159;
	params.scale = vec3(1.0f);
	params.scale = sign(params.scale) * max(abs(params.scale), 0.001);
	params.hue_rotation = 1.0f;
	params.animation_speed = 1.0f;
	params.animation_offset = 0.0f;
	params.lifetime = 1.0f;
	params.color = color_value;
}

vec3 calculate_initial_position(inout uint alt_seed) {
	float pi = 3.14159;
	vec3 pos = vec3(0.0);
	{ // Emission shape.
		pos = vec3(rand_from_seed(alt_seed) * 2.0 - 1.0, rand_from_seed(alt_seed) * 2.0 - 1.0, rand_from_seed(alt_seed) * 2.0 - 1.0) * emission_box_extents;
	}
	return pos * emission_shape_scale + emission_shape_offset;
}

vec3 get_random_direction_from_spread(inout uint alt_seed, float spread_angle) {
	float pi = 3.14159;
	float degree_to_rad = pi / 180.0;
	float spread_rad = spread_angle * degree_to_rad;
	float angle1_rad = rand_from_seed_m1_p1(alt_seed) * spread_rad;
	float angle2_rad = rand_from_seed_m1_p1(alt_seed) * spread_rad * (1.0 - flatness);
	vec3 direction_xz = vec3(sin(angle1_rad), 0.0, cos(angle1_rad));
	vec3 direction_yz = vec3(0.0, sin(angle2_rad), cos(angle2_rad));
	direction_yz.z = direction_yz.z / max(0.0001, sqrt(abs(direction_yz.z))); // Better uniform distribution.
	vec3 spread_direction = vec3(direction_xz.x * direction_yz.z, direction_yz.y, direction_xz.z * direction_yz.z);
	vec3 direction_nrm = length(direction) > 0.0 ? normalize(direction) : vec3(0.0, 0.0, 1.0);
	// Rotate spread to direction.
	vec3 binormal = cross(vec3(0.0, 1.0, 0.0), direction_nrm);
	if (length(binormal) < 0.0001) {
		// Direction is parallel to Y. Choose Z as the binormal.
		binormal = vec3(0.0, 0.0, 1.0);
	}
	binormal = normalize(binormal);
	vec3 normal = cross(binormal, direction_nrm);
	spread_direction = binormal * spread_direction.x + normal * spread_direction.y + direction_nrm * spread_direction.z;
	return normalize(spread_direction);
}

vec3 process_radial_displacement(DynamicsParameters param, float lifetime, inout uint alt_seed, mat4 transform, mat4 emission_transform, float delta) {
	vec3 radial_displacement = vec3(0.0);
	if (delta < 0.001) {
		return radial_displacement;
	}
	float radial_displacement_multiplier = 1.0;
	vec3 global_pivot = (emission_transform * vec4(velocity_pivot, 1.0)).xyz;
	if (length(transform[3].xyz - global_pivot) > 0.01) {
		radial_displacement = normalize(transform[3].xyz - global_pivot) * radial_displacement_multiplier * param.radial_velocity;
	} else {
		radial_displacement = get_random_direction_from_spread(alt_seed, 360.0) * param.radial_velocity;
	}
	if (radial_displacement_multiplier * param.radial_velocity < 0.0) {
		// Prevent inwards velocity to flicker once the point is reached.
		radial_displacement = normalize(radial_displacement) * min(abs(radial_displacement_multiplier * param.radial_velocity), length(transform[3].xyz - global_pivot) / delta);
	}
	return radial_displacement;
}

void process_physical_parameters(inout PhysicalParameters params, float lifetime_percent) {
}

void start() {
	uint base_number = NUMBER;
	uint alt_seed = hash(base_number + uint(1) + RANDOM_SEED);
	DisplayParameters params;
	calculate_initial_display_params(params, alt_seed);
	DynamicsParameters dynamic_params;
	calculate_initial_dynamics_params(dynamic_params, alt_seed);
	PhysicalParameters physics_params;
	calculate_initial_physical_params(physics_params, alt_seed);
	if (rand_from_seed(alt_seed) > AMOUNT_RATIO) {
		ACTIVE = false;
	}

	if (RESTART_CUSTOM) {
		CUSTOM = vec4(0.0);
		CUSTOM.w = params.lifetime;
		CUSTOM.x = dynamic_params.angle;
	}
	if (RESTART_COLOR) {
		COLOR = params.color;
	}
	if (RESTART_ROT_SCALE) {
		TRANSFORM[0].xyz = vec3(1.0, 0.0, 0.0);
		TRANSFORM[1].xyz = vec3(0.0, 1.0, 0.0);
		TRANSFORM[2].xyz = vec3(0.0, 0.0, 1.0);
	}
	if (RESTART_POSITION) {
		TRANSFORM[3].xyz = calculate_initial_position(alt_seed);
		TRANSFORM = EMISSION_TRANSFORM * TRANSFORM;
	}
	if (RESTART_VELOCITY) {
		VELOCITY = get_random_direction_from_spread(alt_seed, spread) * dynamic_params.initial_velocity_multiplier;
	}

	VELOCITY = (EMISSION_TRANSFORM * vec4(VELOCITY, 0.0)).xyz;
	VELOCITY += EMITTER_VELOCITY * inherit_emitter_velocity_ratio;
}

void process() {
	uint base_number = NUMBER;
	//if (repeatable) {
	//	base_number = INDEX;
	//}
	uint alt_seed = hash(base_number + uint(1) + RANDOM_SEED);
	DisplayParameters params;
	calculate_initial_display_params(params, alt_seed);
	DynamicsParameters dynamic_params;
	calculate_initial_dynamics_params(dynamic_params, alt_seed);
	PhysicalParameters physics_params;
	calculate_initial_physical_params(physics_params, alt_seed);

	float pi = 3.14159;
	float degree_to_rad = pi / 180.0;

	CUSTOM.y += DELTA / LIFETIME;
	CUSTOM.y = mix(CUSTOM.y, 1.0, INTERPOLATE_TO_END);
	float lifetime_percent = CUSTOM.y / params.lifetime;
	if (CUSTOM.y > CUSTOM.w) {
		ACTIVE = false;
	}
	
	process_physical_parameters(physics_params, lifetime_percent);
	vec2 current;
	{
		vec2 position = TRANSFORM[3].xz;
		
		// TODO: move constants to uniforms
		float timescale = 1.0f / 500.0f;
		float ratio = 0.15f;
		
		float disturbanceX = texture(noiseDisturbancesX, vec3(position, game_time) / 64.0f).r;
		float disturbanceY = texture(noiseDisturbancesY, vec3(position, game_time) / 64.0f).r;
		
		float currentX = texture(noiseCurrentsX, vec3(position.x * 0.1f, position.y, game_time * timescale) / 64.0f).r; 
		float currentY = texture(noiseCurrentsY, vec3(position.x * 0.1f, position.y, game_time * timescale) / 64.0f).r; 
		
		vec2 disturbances = vec2(disturbanceX, disturbanceY);
		vec2 currents = vec2(currentX, currentY);
		
		if (abs(currents.x) < 0.4)
		{
			currents.x = 0.0f;
		}
		
		if (abs(currents.y) < 0.4)
		{
			currents.y = 0.0f;
		}
		
		current = mix(currents, disturbances, ratio) * 2.0f - 1.0f;
		current *= 5.0f;
	}
	
	VELOCITY = vec3(current.x, 0.0f, current.y);

	TRANSFORM[3].xyz += VELOCITY * DELTA;

	vec4 finalColor = color_value;
	finalColor.a *= texture(alpha_curve, vec2(lifetime_percent)).r;
	
	COLOR = finalColor;

	TRANSFORM[0].xyz = normalize(TRANSFORM[0].xyz);
	TRANSFORM[1].xyz = normalize(TRANSFORM[1].xyz);
	TRANSFORM[2].xyz = normalize(TRANSFORM[2].xyz);
	TRANSFORM[0].xyz *= sign(params.scale.x) * max(abs(params.scale.x), 0.001);
	TRANSFORM[1].xyz *= sign(params.scale.y) * max(abs(params.scale.y), 0.001);
	TRANSFORM[2].xyz *= sign(params.scale.z) * max(abs(params.scale.z), 0.001);

	CUSTOM.z = params.animation_offset + lifetime_percent * params.animation_speed;

	if (CUSTOM.y > CUSTOM.w) {
		ACTIVE = false;
	}
}
