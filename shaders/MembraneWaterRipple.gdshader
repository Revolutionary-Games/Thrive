shader_type spatial;
render_mode blend_mix, depth_prepass_alpha, cull_back, diffuse_lambert;

// Almost completely transparent background
uniform vec4 WaterColor : source_color = vec4(0.0, 0.0, 0.0, 0.02);
// Intensity of ripple displacement
uniform float RippleStrength = 0.8;
// Time offset for animation
uniform float TimeOffset = 0.0;
// Speed of movement
uniform float MovementSpeed = 0.0;
// Direction of movement
uniform vec2 MovementDirection = vec2(0.0, 0.0);
// Wave phase
uniform float Phase = 0.2;
// Regulates viscosity - Higher values = less viscous and longer lasting ripples
uniform float Attenuation = 0.998;

// Wave height calculation with improved long-distance propagation
float WaveHeight(vec2 position, vec2 center, float time, float elapsed) {
    // Distance from wave origin
    float dist = length(position - center);
    
    // Primary wave 
    // Higher speed = less viscous appearance
    float speed = 2.0;
    float frequency = 1.5;
    // Lower exp decay factor = less viscous appearance
    float amplitude = 0.6 * exp(-0.05 * dist);
    float primaryWave = amplitude * sin(dist * frequency - time * speed);
    
    // Add subtle harmonic overtones for more natural water movement
    float harmonic1 = 0.12 * sin(dist * frequency * 1.5 - time * speed * 1.3) * exp(-0.1 * dist);
    float harmonic2 = 0.08 * sin(dist * frequency * 0.7 - time * speed * 0.7) * exp(-0.07 * dist);
    
    // Combine waves with logarithmic decay for more distant propagation
    float combined = primaryWave + harmonic1 + harmonic2;
    
    // Use a gentler time decay for longer lasting effect
    // Lower elapsed decay (0.25) = less viscous water with longer-lasting ripples
    // Lower distance decay (0.08) = ripples travel further like less viscous fluid
    return combined * exp(-elapsed * 0.25) * exp(-0.08 * dist);
}

// Physics-based water ripple simulation with enhanced trail and propagation
float SimulateRipples(vec2 position, vec2 direction, float speed, float time) {
    // Generate minimal ripples even when barely moving
    if (speed < 0.03) speed = 0.03;
    
    // We'll simulate ripples generated at different positions along the movement path
    // Ripple origins follow the movement direction
    float totalRipple = 0.0;
    
    // Current position of the membrane (origin for new ripples)
    vec2 currentPos = vec2(0.0, 0.0);
    
    // Simulate multiple ripple sources from object's recent past positions (longer trail)
    // More iterations = longer trail, less viscous appearance
    for (int i = 0; i < 14; i++) {
        // Previous positions along movement path (farther back in time for larger i)
        // Lower factor = ripples closer together, more viscous
        float timeAgo = float(i) * 0.15;
        
        // Position of the membrane at time (time - timeAgo)
        // Higher multiplier = longer trail
        vec2 pastPos = currentPos - direction * speed * 5.0 * timeAgo;
        
        // Calculate wave height from this past position
        // More recent positions (smaller i) have stronger effect
        float wave = WaveHeight(position, pastPos, time, timeAgo);
        
        // If very close to object, create depression (water pushed down by object)
        if (i < 1 && length(position - currentPos) < 0.8) {
            // Water gets pushed down where object is
            float depression = 0.8 * (1.0 - length(position - currentPos) / 0.8);
            wave -= depression * 0.5;
        }
        
        if (i > 1 && i < 8) {
            // Calculate position relative to trail
            vec2 toPos = position - pastPos;
            float alongTrail = dot(toPos, direction);
            vec2 projected = pastPos + alongTrail * direction;
            float perpDist = length(position - projected);
            
            if (alongTrail < 0.0 && perpDist < 3.5 * (1.0 + float(i) * 0.25)) {
                float vAngle = 0.2 + float(i) * 0.04;
                float vWidth = abs(alongTrail) * vAngle;
                float vLineDist = abs(perpDist - vWidth);
                
                // Add wake trail ripples with more pronounced pattern
                if (vLineDist < 1.2) {
                    float wakeIntensity = (1.0 - vLineDist / 1.2) * 0.25 * exp(-float(i) * 0.18);
                    wave += wakeIntensity * sin(alongTrail * 2.0 - time * 4.0);
                }
            }
        }
        
        // These create additional subtle ripples that propagate further
        if (i > 2) {
            // Secondary ripples - smaller, higher frequency waves that propagate further
            float secondaryDist = length(position - pastPos);
            if (secondaryDist > 2.0 && secondaryDist < 18.0) {
                float secondaryWave = 0.18 * sin(secondaryDist * 2.3 - time * 3.0 + float(i)) * 
                                     exp(-0.06 * secondaryDist) * exp(-timeAgo * 0.2);
                wave += secondaryWave;
            }
            
            // Tertiary ripples adds very subtles low frequency waves for distant propagation
            if (secondaryDist > 4.0 && secondaryDist < 30.0) {
                float tertiaryWave = 0.1 * sin(secondaryDist * 0.9 - time * 1.5 + float(i) * 0.5) * 
                                     exp(-0.025 * secondaryDist) * exp(-timeAgo * 0.12);
                wave += tertiaryWave;
            }
        }
        
        // This creates the elongated appearance of ripples behind a moving object
        vec2 toPos = position - pastPos;
        float alongDir = dot(toPos, direction);
        if (alongDir < 0.0 && alongDir > -12.0) {  
            // Stretch factor increases with distance for more pronounced trail
            float stretchFactor = min(1.0, -alongDir * 0.1) * 0.3;
            
            // Create stretched ripples that extend behind the movement
            float stretchedWave = 0.15 * sin(alongDir * 1.5 - time * 2.0) * 
                                 exp(alongDir * 0.1) * exp(-timeAgo * 0.3);
            
            // Add elongated wake pattern for more natural water trail
            wave += stretchedWave * stretchFactor;
        }
        
        // Lower time_ago decay factor (0.5) = longer lasting trail, less viscous appearance
        totalRipple += wave * (1.0 - timeAgo * 0.5);  
    }
    
    // Add edge fade to prevent hard cutoffs at mesh edges
    float edgeDist = min(8.0 - abs(position.x), 8.0 - abs(position.y));
    float edgeFade = smoothstep(0.0, 2.0, edgeDist);
    
    // Scale by movement speed and strength with edge fade
    return totalRipple * speed * edgeFade;
}

void vertex() {
    // Get vertex position in model space
    vec2 pos = VERTEX.xz;
    
    // Apply water simulation with enhanced trail
    float displacement = SimulateRipples(pos, MovementDirection, MovementSpeed, TimeOffset);
    
    // Apply height displacement
    VERTEX.y += displacement * RippleStrength;
    
    // Calculate normals for proper lighting with finer sampling for detailed ripples
    vec3 normal = NORMAL;
    if (abs(displacement) > 0.001) {

        float stepSize = 0.08;
        float dx = (SimulateRipples(pos + vec2(stepSize, 0.0), MovementDirection, MovementSpeed, TimeOffset) - 
                   SimulateRipples(pos - vec2(stepSize, 0.0), MovementDirection, MovementSpeed, TimeOffset)) / (2.0 * stepSize);
        float dz = (SimulateRipples(pos + vec2(0.0, stepSize), MovementDirection, MovementSpeed, TimeOffset) - 
                   SimulateRipples(pos - vec2(0.0, stepSize), MovementDirection, MovementSpeed, TimeOffset)) / (2.0 * stepSize);
        
        // Create normal from gradient
        vec3 gradientNormal = normalize(vec3(-dx * RippleStrength, 1.0, -dz * RippleStrength));
        
        // Blend normal for smooth transitions
        float blendFactor = min(1.0, abs(displacement) * 5.0);
        normal = normalize(mix(NORMAL, gradientNormal, blendFactor));
    }
    
    NORMAL = normal;
}

void fragment() {
    // Simple water coloring
    float depthFactor = clamp(1.0 - (-VERTEX.y * 0.5), 0.0, 1.0);
    
    // Keep color fully transparent, only showing ripple distortion
    vec3 finalColor = vec3(0.0, 0.0, 0.0);
    
    // Basic lighting
    float nDotL = dot(NORMAL, normalize(vec3(0.4, 0.8, 0.3)));
    float viewDotNormal = max(0.0, dot(normalize(VIEW), NORMAL));
    
    // Final coloring with reduced highlight intensity for less pronounced contrast
    float enhancedHighlight = pow(max(0.0, nDotL), 8.0) * 0.25; 
    float enhancedFresnel = pow(1.0 - viewDotNormal, 2.0) * 0.4; 
    ALBEDO = finalColor + vec3(enhancedHighlight) + vec3(0.8, 0.9, 1.0) * enhancedFresnel;
    
    // Reduced specular to make wave tops less pronounced
    SPECULAR = 0.5;
    // Slightly increased roughness for softer reflections
    ROUGHNESS = 0.2;
    ALPHA = WaterColor.a;
}
