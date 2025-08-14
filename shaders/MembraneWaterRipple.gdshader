shader_type spatial;
render_mode blend_mix, depth_prepass_alpha, cull_back, diffuse_lambert;

// Almost completely transparent background
uniform vec4 WaterColor : source_color = vec4(0.0, 0.0, 0.0, 0.06);

// Intensity of ripple displacement
uniform float RippleStrength = 0.8;

// Time offset for animation container
uniform float TimeOffset = 0.0;

// Speed of movement container
uniform float MovementSpeed = 0.0;

// Direction of movement container
uniform vec2 MovementDirection = vec2(0.0, 0.0);

// Array of past positions for curved movement path
uniform vec2 PastPositions[14];

// Number of past positions actually available
uniform int PastPositionsCount = 0;

// Wave phase
uniform float Phase = 0.2;

// Regulates viscosity - Higher values = less viscous and longer lasting ripples
uniform float Attenuation = 0.998;

// Stillness factor for smooth effect fadeout
uniform float StillnessFactor = 0.0;

// Membrane radius for scaling
uniform float MembraneRadius = 5.0;

// Global alpha multiplier for fade-in/out transitions
uniform float GlobalAlpha = 1.0;

// Constants for mesh sizing and effect configuration
const float MeshSize = 8.0;
const float EdgeFadeStart = 4.5;
const float EdgeFadeEnd = 0.4;
const int MaxPathSamples = 12;
const int SecondaryStart = 2;
const int SecondaryEnd = 10;

// Constants for storing intensity and displacement
varying float rippleIntensity;
varying float vertexDisplacement;

// Function to get position at arbitrary position in the path with interpolation
vec2 GetPathPosition(float index)
{
    // Handle edge cases
    if (PastPositionsCount == 0)
      return vec2(0.0, 0.0);

    if (PastPositionsCount == 1)
      return PastPositions[0];

    // Ensure index is within bounds
    index = clamp(index, 0.0, float(PastPositionsCount - 1));

    // Get the two closest positions for interpolation
    int lowerIndex = int(floor(index));
    int upperIndex = int(ceil(index));

    // If they're the same, no interpolation needed
    if (lowerIndex == upperIndex) {
        return PastPositions[lowerIndex];
    }

    // Calculate interpolation factor
    float factor = index - float(lowerIndex);
    return mix(PastPositions[lowerIndex], PastPositions[upperIndex], factor);
}

// Calculate wave height at given position relative to a center point
float WaveHeight(vec2 position, vec2 center, float time, float elapsed)
{
    // Distance from wave origin
    float dist = length(position - center);

    // Constants that define the wave
    const float speed = 2.0;
    const float frequency = 2.0;

    // Smaller ripple amplitude for better scaling with small cells
    float amplitude = 0.7 * exp(-0.14 * dist);

    // Main wave component
    float wave = time * speed;
    float primaryWave = amplitude * sin(dist * frequency - wave);

    // Reduced harmonic content for better performance
    float harmonic1 = 0.0;

    // Only calculate harmonics for closer points
    harmonic1 = 0.08 * sin(dist * frequency * 1.8 - time * speed * 1.3) * exp(-0.18 * dist);

    // Combine waves with optimized decay
    float combined = primaryWave + harmonic1;

    // Apply stillness fade factor with smooth transition
    float stillnessInfluence = pow(1.0 - StillnessFactor, 1.5);
    return combined * exp(-elapsed * 0.3) * exp(-0.12 * dist) * stillnessInfluence;
}

// Calculate wake trail effect between two position points
float CalculateWakeTrail(vec2 position, vec2 pos1, vec2 pos2, float timeAgo, float time)
{
    // Skip if positions are too close
    float segmentLength = length(pos2 - pos1);

    // Calculate direction vector
    vec2 trailDir = (pos2 - pos1) / segmentLength;

    // Calculate perpendicular vector
    vec2 perpDir = vec2(-trailDir.y, trailDir.x);

    // Quick projection calculation
    vec2 toPoint = position - pos1;
    float projDist = dot(toPoint, trailDir);

    // Perpendicular distance calculation
    float perpDist = abs(dot(toPoint, perpDir));

    // Wake calculation - reduced width for smaller cells
    float wakeWidth = 0.6;  // Reduced from 0.8 for smaller visual effect
    float intensity = exp(-perpDist * perpDist / wakeWidth);
    float oscillation = sin(projDist * 2.5 - time * 3.0) * 0.2;
    float fade = exp(-timeAgo * 0.4);

    // Apply stillness fade factor
    float stillnessInfluence = pow(1.0 - StillnessFactor, 1.5);
    return intensity * oscillation * fade * 0.25 * stillnessInfluence;
}

// Simulate ripple effects with adaptive sampling
float SimulateRipples(vec2 position, vec2 direction, float speed, float time)
{
    // Skip early if no positions are recorded or water is completely still
    if (PastPositionsCount <= 0 || StillnessFactor >= 0.99)
       return 0.0;

    // Current position is always the most recent one
    vec2 currentPos = PastPositions[0];

    float centerDist = length(position - currentPos);

    // Scale effect with membrane size
    float scaleFactor = max(1.0, MembraneRadius / 5.0);

    // For very large membranes, use size-adaptive ripple calculation
    float rippleDist = centerDist / scaleFactor;

    // Create ripple value based on the current position - reduced effect size
    float radialRipple = 0.3 * sin(rippleDist * 2.5 - time * 3.0) * exp(-0.35 * rippleDist);
    float totalRipple = radialRipple;

    // Path sampling with adaptive quality based on distance
    if (PastPositionsCount >= 2)
    {
        // Determine sample step based on distance
        int sampleStep = 1;
        int maxSamples = PastPositionsCount;

        // Sample the path
        for (int i = 0; i < maxSamples; i += sampleStep) {
            if (i >= PastPositionsCount)
              break;

            // Calculate path time and position
            float pathTime = float(i) / float(MaxPathSamples - 1) * float(PastPositionsCount - 1);
            vec2 pathPos = GetPathPosition(pathTime);
            float timeAgo = pathTime * 0.15;

            // Generates wave
            float wave = WaveHeight(position, pathPos, time, timeAgo);

            // Add water depression at current position
            // Size-adaptive depression radius
            float depressionRadius = 0.6 * scaleFactor;
            if (i == 0 && rippleDist < 0.6) // Size-adaptive comparison
            {
                float depthFactor = rippleDist / 0.6;
                float depression = 0.7 * (1.0 - depthFactor);
                wave -= depression * 0.5;
            }

            float nextPathTime = float(i + 1) / float(MaxPathSamples - 1) * float(PastPositionsCount - 1);
            vec2 nextPathPos = GetPathPosition(nextPathTime);

            // Only calculate wake trails if there's meaningful movement
            // Use a smaller threshold for large membranes
            float moveThreshold = 0.05 / max(1.0, scaleFactor * 0.2);
            if (length(nextPathPos - pathPos) > moveThreshold)
            {
            	wave += CalculateWakeTrail(position, pathPos, nextPathPos, timeAgo, time);
            }

            // Add secondary ripples with reduced frequency
            if (i > SecondaryStart && i < SecondaryEnd && i % (sampleStep * 2) == 0)
            {
                float secondaryDist = length(position - pathPos) / scaleFactor;
                float secondaryPhase = secondaryDist * 2.8 - time * 3.0 + float(i);
                float secondaryWave = 0.12 * sin(secondaryPhase) *
	                exp(-0.18 * secondaryDist) *
                	exp(-timeAgo * 0.3);
                wave += secondaryWave;
            }

            // Add to total with time decay
            totalRipple += wave * exp(-timeAgo * 0.6);
        }
    }

    // Edge fade for more natural falloff - use size-adaptive fade threshold
    float edgeFade = 1.0;
    float edgeFadeThreshold = EdgeFadeStart * scaleFactor;
    if (centerDist > edgeFadeThreshold)
    {
        // Size-adaptive fade rate
        float fadeDistance = edgeFadeThreshold * 1.0;
        edgeFade = 1.0 - (centerDist - edgeFadeThreshold) / fadeDistance;
        edgeFade = clamp(edgeFade, 0.0, 1.0);

        // Apply cubic falloff for smoother edges
        edgeFade = edgeFade * edgeFade * edgeFade;
    }

    // Apply smooth stillness fade
    float stillnessInfluence = pow(1.0 - StillnessFactor, 1.5);

    // Apply speed-based scaling with smooth transition during fade
    float speedFactor = mix(0.15, speed, stillnessInfluence);

    return totalRipple * max(speedFactor, 0.15) * edgeFade * stillnessInfluence;
}

// Calculate the intensity/visibility of ripples at a given position
float CalculateIntensity(vec2 position, float displacement)
{
    // No movement data = no visibility
    if (PastPositionsCount <= 0 || StillnessFactor >= 0.98)
        return 0.0;
    
    vec2 currentPos = PastPositions[0];
    float distFromCenter = length(position - currentPos);
    
    // Get displacement intensity value
    float displacementIntensity = abs(displacement);
    
    // Calculate distance from the movement path
    float minDistToPath = 999999.0;
    for (int i = 0; i < PastPositionsCount - 1; i++) {
        vec2 pathStart = PastPositions[i];
        vec2 pathEnd = PastPositions[i + 1];
        vec2 pathVec = pathEnd - pathStart;
        float pathLength = length(pathVec);
        
        if (pathLength > 0.001) {
            vec2 pathDir = pathVec / pathLength;
            vec2 toPoint = position - pathStart;
            float projDist = clamp(dot(toPoint, pathDir), 0.0, pathLength);
            vec2 closestPoint = pathStart + pathDir * projDist;
            float distToPath = length(position - closestPoint);
            minDistToPath = min(minDistToPath, distToPath);
        }
    }
    
    // Create a visibility mask based on proximity
    float pathProximity = exp(-minDistToPath * 0.5);
    
    // Combine factors with intensity and proximity and stillness
    float intensity = displacementIntensity * 10.0;
    intensity *= pathProximity;
    intensity *= (1.0 - StillnessFactor);
    
    // Apply distance falloff
    float scaleFactor = max(1.0, MembraneRadius / 5.0);
    float maxDist = 8.0 * scaleFactor;
    if (distFromCenter > maxDist * 0.7) {
        float fade = 1.0 - (distFromCenter - maxDist * 0.7) / (maxDist * 0.3);
        intensity *= clamp(fade, 0.0, 1.0);
    }
    
    // Apply global alpha
    intensity *= GlobalAlpha;
    
    return intensity;
}

void vertex()
{
    // Get vertex position in model space
    vec2 pos = VERTEX.xz;

    // Distance based parameter
    float distFromCenter = length(pos);

    // For very large meshes, we need to apply to a larger radius
    // Scale the maximum radius with membrane size
    float scaleFactor = max(1.0, MembraneRadius / 5.0);
    float maxRadius = min(10.0 * scaleFactor, 40.0);

    // Apply effect to vertices within the active radius
    if (distFromCenter <= maxRadius)
    {
        // Apply ripple simulation with movement parameters
        float displacement = SimulateRipples(pos, MovementDirection, MovementSpeed, TimeOffset);

        // Store displacement for fragment shader
        vertexDisplacement = displacement;
        
        // Calculate and store intensity
        rippleIntensity = CalculateIntensity(pos, displacement);

        // Apply height displacement
        VERTEX.y += displacement * RippleStrength * GlobalAlpha;

        // Only calculate normals when there's visible displacement
        if (abs(displacement) > 0.001 && GlobalAlpha > 0.1)
        {
            float stepSize = 0.05;

            // Simplified gradient calculation for normal
            float dx = (SimulateRipples(pos + vec2(stepSize, 0.0), MovementDirection, MovementSpeed, TimeOffset) -
                       displacement) / stepSize;
            float dz = (SimulateRipples(pos + vec2(0.0, stepSize), MovementDirection, MovementSpeed, TimeOffset) -
                       displacement) / stepSize;

            // Create normal from gradient
            vec3 gradientNormal = normalize(vec3(-dx * RippleStrength * GlobalAlpha, 1.0, -dz * RippleStrength * GlobalAlpha));
            NORMAL = gradientNormal;
        }
    }
    else {
        rippleIntensity = 0.0;
        vertexDisplacement = 0.0;
    }
}

void fragment()
{
    // Threshold scales with global alpha to maintain smooth transitions
    float rippleThreshold = 0.01 * max(0.1, GlobalAlpha);
    
    if (rippleIntensity < rippleThreshold) {
        // Completely discard pixels with no ripple activity
        discard;
    }
    
    // Early discard if global alpha is too low
    if (GlobalAlpha < 0.001) {
        discard;
    }
    
    // Calculate lighting parameters
    float nDotL = dot(NORMAL, normalize(vec3(0.4, 0.8, 0.3)));
    float viewDotNormal = max(0.0, dot(normalize(VIEW), NORMAL));

    // Highlights and fresnel calculation
    float enhancedHighlight = pow(max(0.0, nDotL), 8.0) * 0.25 * GlobalAlpha;
    float enhancedFresnel = pow(1.0 - viewDotNormal, 2.0) * 0.4 * GlobalAlpha;

    ALBEDO = vec3(0.0, 0.0, 0.0) + vec3(enhancedHighlight) + vec3(0.8, 0.9, 1.0) * enhancedFresnel;
    SPECULAR = 0.5;
    ROUGHNESS = 0.2;
    
    // Calculate alpha based on ripple intensity
    float fadeMultiplier = pow(1.0 - StillnessFactor, 1.5);
    
    // Use ripple intensity to modulate alpha
    float baseAlpha = WaterColor.a * fadeMultiplier;
    
    // Apply non-linear curve to intensity
    float intensityCurve = pow(clamp(rippleIntensity, 0.0, 1.0), 0.5);
    
    // Final alpha is modulated by both local activity and fade-in/out
    ALPHA = baseAlpha * intensityCurve;
    
    // Additional alpha clipping for very low values
    if (ALPHA < 0.002) {
        discard;
    }
}