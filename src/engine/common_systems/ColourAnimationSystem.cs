﻿namespace Systems;

using System.Runtime.CompilerServices;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Handles updating the state of <see cref="ColourAnimation"/> based on animations triggered elsewhere
/// </summary>
[RuntimeCost(2)]
[RunsOnFrame]
public partial class ColourAnimationSystem : BaseSystem<World, float>
{
    // TODO: Constants.SYSTEM_EXTREME_ENTITIES_PER_THREAD
    public ColourAnimationSystem(World world) : base(world)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref ColourAnimation colourAnimation)
    {
        if (!colourAnimation.Animating)
            return;

        if (colourAnimation.AnimationDuration <= 0)
        {
            GD.PrintErr("Animation duration for ColourAnimation not set properly");
            colourAnimation.AnimationDuration = 0.001f;
        }

        colourAnimation.AnimationElapsed += delta;

        if (colourAnimation.AnimationElapsed >= colourAnimation.AnimationDuration)
        {
            // Finished animation

            if (colourAnimation.AutoReverseAnimation)
            {
                // Play in reverse
                colourAnimation.AutoReverseAnimation = false;

                // Swap direction
                (colourAnimation.AnimationTargetColour, colourAnimation.AnimationStartColour) = (
                    colourAnimation.AnimationStartColour, colourAnimation.AnimationTargetColour);
                colourAnimation.AnimationElapsed -= colourAnimation.AnimationDuration;

                if (colourAnimation.AnimationElapsed < 0)
                    colourAnimation.AnimationElapsed = 0;
            }
            else
            {
                // No new animation to run, stop processing this entity
                colourAnimation.Animating = false;
            }
        }

        colourAnimation.ColourApplied = false;
    }
}
