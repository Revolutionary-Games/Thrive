shader_type spatial;
render_mode blend_mix, depth_prepass_alpha, cull_back, diffuse_lambert;

uniform vec4 water_color : source_color = vec4(0.0, 0.0, 0.0, 0.02); // Almost completely transparent background
uniform float ripple_strength = 0.8; // Increased for more pronounced ripples
uniform float time_offset = 0.0;
uniform float movement_speed = 0.0;
uniform vec2 movement_direction = vec2(0.0, 0.0);
uniform float phase = 0.2;
uniform float attenuation = 0.998; // regulates viscositi - Higher values = less viscous, longer lasting ripples

// Wave height calculation with improved long-distance propagation
float wave_height(vec2 position, vec2 center, float time, float elapsed) {
    // Distance from wave origin
    float dist = length(position - center);
    
    // Primary wave 
    float speed = 2.0; // VISCOSITY: Higher speed = less viscous appearance
    float frequency = 1.5;
    float amplitude = 0.6 * exp(-0.05 * dist); // VISCOSITY: Lower exp decay factor = less viscous appearance
    float primary_wave = amplitude * sin(dist * frequency - time * speed);
    
    // Add subtle harmonic overtones for more natural water movement
    float harmonic1 = 0.12 * sin(dist * frequency * 1.5 - time * speed * 1.3) * exp(-0.1 * dist);
    float harmonic2 = 0.08 * sin(dist * frequency * 0.7 - time * speed * 0.7) * exp(-0.07 * dist);
    
    // Combine waves with logarithmic decay for more distant propagation
    float combined = primary_wave + harmonic1 + harmonic2;
    
    // Use a gentler time decay for much longer lasting effect
    // VISCOSITY: Lower elapsed decay (0.25) = less viscous water with longer-lasting ripples
    // VISCOSITY: Lower distance decay (0.08) = ripples travel further like less viscous fluid
    return combined * exp(-elapsed * 0.25) * exp(-0.08 * dist);
}

// Physics-based water ripple simulation with enhanced trail and propagation
float simulate_ripples(vec2 position, vec2 direction, float speed, float time) {
    // Generate minimal ripples even when barely moving
    if (speed < 0.03) speed = 0.03;
    
    // We'll simulate ripples generated at different positions along the movement path
    // Ripple origins follow the movement direction
    float total_ripple = 0.0;
    
    // Current position of the membrane (origin for new ripples)
    vec2 current_pos = vec2(0.0, 0.0);
    
    // Simulate multiple ripple sources from object's recent past positions (longer trail)
    for (int i = 0; i < 14; i++) {  // VISCOSITY: More iterations = longer trail, less viscous appearance
        // Previous positions along movement path (farther back in time for larger i)
        float time_ago = float(i) * 0.15; // VISCOSITY: Lower factor = ripples closer together, more viscous
        
        // Position of the membrane at time (time - time_ago)
        vec2 past_pos = current_pos - direction * speed * 5.0 * time_ago; // VISCOSITY: Higher multiplier = longer trail
        
        // Calculate wave height from this past position
        // More recent positions (smaller i) have stronger effect
        float wave = wave_height(position, past_pos, time, time_ago);
        
        // If very close to object, create depression (water pushed down by object)
        if (i < 1 && length(position - current_pos) < 0.8) {
            // Water gets pushed down where object is
            float depression = 0.8 * (1.0 - length(position - current_pos) / 0.8);
            wave -= depression * 0.5;
        }
        
        if (i > 1 && i < 8) {
            // Calculate position relative to trail
            vec2 to_pos = position - past_pos;
            float along_trail = dot(to_pos, direction);
            vec2 projected = past_pos + along_trail * direction;
            float perp_dist = length(position - projected);
            
            if (along_trail < 0.0 && perp_dist < 3.5 * (1.0 + float(i) * 0.25)) {
                float v_angle = 0.2 + float(i) * 0.04;
                float v_width = abs(along_trail) * v_angle;
                float v_line_dist = abs(perp_dist - v_width);
                
                // Add wake trail ripples with more pronounced pattern
                if (v_line_dist < 1.2) {
                    float wake_intensity = (1.0 - v_line_dist / 1.2) * 0.25 * exp(-float(i) * 0.18);
                    wave += wake_intensity * sin(along_trail * 2.0 - time * 4.0);
                }
            }
        }
        
        // These create additional subtle ripples that propagate further
        if (i > 2) {
            // Secondary ripples - smaller, higher frequency waves that propagate further
            float secondary_dist = length(position - past_pos);
            if (secondary_dist > 2.0 && secondary_dist < 18.0) {
                float secondary_wave = 0.18 * sin(secondary_dist * 2.3 - time * 3.0 + float(i)) * 
                                     exp(-0.06 * secondary_dist) * exp(-time_ago * 0.2);
                wave += secondary_wave;
            }
            
            // Tertiary ripples - very subtle, low frequency waves for distant propagation
            if (secondary_dist > 4.0 && secondary_dist < 30.0) {  // Extended range for longer propagation
                float tertiary_wave = 0.1 * sin(secondary_dist * 0.9 - time * 1.5 + float(i) * 0.5) * 
                                     exp(-0.025 * secondary_dist) * exp(-time_ago * 0.12);
                wave += tertiary_wave;
            }
        }
        
        // This creates the elongated appearance of ripples behind a moving object
        vec2 to_pos = position - past_pos;
        float along_dir = dot(to_pos, direction);
        if (along_dir < 0.0 && along_dir > -12.0) {  
            // Stretch factor increases with distance for more pronounced trail
            float stretch_factor = min(1.0, -along_dir * 0.1) * 0.3;
            
            // Create stretched ripples that extend behind the movement
            float stretched_wave = 0.15 * sin(along_dir * 1.5 - time * 2.0) * 
                                 exp(along_dir * 0.1) * exp(-time_ago * 0.3);
            
            // Add elongated wake pattern for more natural water trail
            wave += stretched_wave * stretch_factor;
        }
        
        // VISCOSITY: Lower time_ago decay factor (0.5) = longer lasting trail, less viscous appearance
        total_ripple += wave * (1.0 - time_ago * 0.5);  
    }
    
    // Add edge fade to prevent hard cutoffs at mesh edges
    float edge_dist = min(8.0 - abs(position.x), 8.0 - abs(position.y));
    float edge_fade = smoothstep(0.0, 2.0, edge_dist);
    
    // Scale by movement speed and strength with edge fade
    return total_ripple * speed * edge_fade;
}

void vertex() {
    // Get vertex position in model space
    vec2 pos = VERTEX.xz;
    
    // Apply water simulation with enhanced trail
    float displacement = simulate_ripples(pos, movement_direction, movement_speed, time_offset);
    
    // Apply height displacement
    VERTEX.y += displacement * ripple_strength;
    
    // Calculate normals for proper lighting with finer sampling for detailed ripples
    vec3 normal = NORMAL;
    if (abs(displacement) > 0.001) {
        // Smaller step size for more detailed normals
        float step_size = 0.08;
        float dx = (simulate_ripples(pos + vec2(step_size, 0.0), movement_direction, movement_speed, time_offset) - 
                   simulate_ripples(pos - vec2(step_size, 0.0), movement_direction, movement_speed, time_offset)) / (2.0 * step_size);
        float dz = (simulate_ripples(pos + vec2(0.0, step_size), movement_direction, movement_speed, time_offset) - 
                   simulate_ripples(pos - vec2(0.0, step_size), movement_direction, movement_speed, time_offset)) / (2.0 * step_size);
        
        // Create normal from gradient
        vec3 gradient_normal = normalize(vec3(-dx * ripple_strength, 1.0, -dz * ripple_strength));
        
        // Blend normal for smooth transitions
        float blend_factor = min(1.0, abs(displacement) * 5.0);
        normal = normalize(mix(NORMAL, gradient_normal, blend_factor));
    }
    
    NORMAL = normal;
}

void fragment() {
    // Simple water coloring
    float depth_factor = clamp(1.0 - (-VERTEX.y * 0.5), 0.0, 1.0);
    
    // Keep color fully transparent, only showing ripple distortion
    vec3 final_color = vec3(0.0, 0.0, 0.0); // Invisible base color
    
    // Basic lighting
    float n_dot_l = dot(NORMAL, normalize(vec3(0.4, 0.8, 0.3)));
    float view_dot_normal = max(0.0, dot(normalize(VIEW), NORMAL));
    
    // Final coloring - enhance this values to make ripples more visible
    float enhanced_highlight = pow(max(0.0, n_dot_l), 8.0) * 0.4; 
    float enhanced_fresnel = pow(1.0 - view_dot_normal, 2.0) * 0.5; 
    ALBEDO = final_color + vec3(enhanced_highlight) + vec3(0.8, 0.9, 1.0) * enhanced_fresnel;
    
    SPECULAR = 0.7; // Increased specular to highlight ripples
    ROUGHNESS = 0.1; // Decreased roughness for more pronounced reflections
    ALPHA = water_color.a;
}