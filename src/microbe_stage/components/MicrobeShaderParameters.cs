namespace Components;

using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Newtonsoft.Json;

/// <summary>
///   Allows control over the few (animation) shader parameters available in the microbe stage for some entities.
///   Requires <see cref="EntityMaterial"/> to apply.
/// </summary>
[JSONDynamicTypeAllowed]
public struct MicrobeShaderParameters
{
    /// <summary>
    ///   Dissolve effect value, range [0, 1]. 0 is default not dissolved state
    /// </summary>
    public float DissolveValue;

    /// <summary>
    ///   Automatically animate the <see cref="DissolveValue"/> when this is not 0 and <see cref="PlayAnimations"/>
    ///   is true. <c>1</c> is the default speed.
    /// </summary>
    public float DissolveAnimationSpeed;

    /// <summary>
    ///   Set to true to enable playing any of the separate animations. If this is false none of the animations
    ///   play at all.
    /// </summary>
    public bool PlayAnimations;

    /// <summary>
    ///   Always reset this to false after changing something to have the changes apply
    /// </summary>
    [JsonIgnore]
    public bool ParametersApplied;
}

public static class MicrobeShaderParametersHelpers
{
    /// <summary>
    ///   Starts a dissolve animation on an entity. If <see cref="addTimedLifeIfMissing"/> this also adds a timed
    ///   life component (when missing on the entity) to delete the entity once the animation is complete
    /// </summary>
    /// <returns>The time in seconds the animation is expected to take</returns>
    public static float StartDissolveAnimation(this Entity entity, IWorldSimulation newComponentCreator,
        bool useChunkSpeed, bool addTimedLifeIfMissing)
    {
        float speed = 1;

        if (useChunkSpeed)
            speed = Constants.FLOATING_CHUNKS_DISSOLVE_SPEED;

        CommandBuffer? recorder = null;

        if (entity.Has<MicrobeShaderParameters>())
        {
            ref var shaderParameters = ref entity.Get<MicrobeShaderParameters>();

            shaderParameters.DissolveAnimationSpeed = speed;
            shaderParameters.PlayAnimations = true;
        }
        else
        {
            recorder = newComponentCreator.StartRecordingEntityCommands();

            recorder.Set(entity, new MicrobeShaderParameters
            {
                DissolveAnimationSpeed = speed,
                PlayAnimations = true,
            });
        }

        // Add a tiny bit of extra time to ensure the animation is finished by the time is elapsed (for example,
        // despawning-with-a-delay purposes)
        var duration = 1 / speed + 0.0001f;

        if (addTimedLifeIfMissing && !entity.Has<TimedLife>())
        {
            // Add a timed life component as the dissolve animation doesn't despawn the entity

            recorder ??= newComponentCreator.StartRecordingEntityCommands();

            recorder.Set(entity, new TimedLife
            {
                TimeToLiveRemaining = duration,
            });
        }

        if (recorder != null)
            newComponentCreator.FinishRecordingEntityCommands(recorder);

        return duration;
    }
}
