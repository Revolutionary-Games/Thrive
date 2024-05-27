namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;
using Godot;
using World = DefaultEcs.World;

/// <summary>
///   Handles things related to <see cref="MicrobeShaderParameters"/>. This should run each frame and pause when
///   the game is paused.
/// </summary>
/// <remarks>
///   <para>
///     This is marked as just reading the entity materials as this just does a single shader parameter write to it
///   </para>
/// </remarks>
[With(typeof(MicrobeShaderParameters))]
[With(typeof(EntityMaterial))]
[ReadsComponent(typeof(EntityMaterial))]
[ReadsComponent(typeof(CellProperties))]
[RuntimeCost(8)]
[RunsOnFrame]
[RunsOnMainThread]
public sealed class MicrobeShaderSystem : AEntitySetSystem<float>
{
    // private readonly Lazy<Texture> noiseTexture = GD.Load<Texture>("res://assets/textures/dissolve_noise.tres");

    private readonly StringName dissolveValueName = new("dissolveValue");

    public MicrobeShaderSystem(World world) : base(world, null)
    {
    }

    public override void Dispose()
    {
        Dispose(true);
        base.Dispose();
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var shaderParameters = ref entity.Get<MicrobeShaderParameters>();

        if (shaderParameters.ParametersApplied && !shaderParameters.PlayAnimations)
            return;

        if (shaderParameters.PlayAnimations)
        {
            if (shaderParameters.DissolveAnimationSpeed != 0)
            {
                if (float.IsNaN(shaderParameters.DissolveValue))
                {
                    GD.PrintErr("Correcting NaN as dissolve shader parameter");
                    shaderParameters.DissolveValue = 0;
                }
                else if (shaderParameters.DissolveValue < 1)
                {
                    shaderParameters.DissolveValue += shaderParameters.DissolveAnimationSpeed * delta;

                    if (entity.Has<CellProperties>())
                    {
                        ref var cellProperties = ref entity.Get<CellProperties>();

                        // Makes the engulf animation fade out during dissolve
                        cellProperties.CreatedMembrane?.HandleEngulfAnimation(false, delta);
                    }

                    if (shaderParameters.DissolveValue > 1)
                    {
                        // Animation finished
                        shaderParameters.DissolveValue = 1;

                        // TODO: only set this to false if all animations are finished (if in the future more
                        // animations are added)
                        shaderParameters.PlayAnimations = false;
                    }
                }
            }
            else
            {
                GD.PrintErr("Entity has incorrectly enabled animations playing but no animation flag " +
                    "is turned on");
                shaderParameters.PlayAnimations = false;
            }
        }

        ref var entityMaterial = ref entity.Get<EntityMaterial>();

        // Wait for the material to be defined
        if (entityMaterial.Materials == null)
            return;

        foreach (var material in entityMaterial.Materials)
        {
            material.SetShaderParameter(dissolveValueName, shaderParameters.DissolveValue);

            // Dissolve texture must be set in the material set on the object otherwise the dissolve animation
            // won't play correctly. It used to be the case that the old C# code set the noise texture here but
            // now it is much simpler to just require it to be set in the scenes.
        }

        shaderParameters.ParametersApplied = true;
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            dissolveValueName.Dispose();
        }
    }
}
