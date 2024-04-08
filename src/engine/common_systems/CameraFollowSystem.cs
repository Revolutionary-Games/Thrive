namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;
using Godot;
using Newtonsoft.Json;
using World = DefaultEcs.World;

/// <summary>
///   Handles moving a camera to follow entity with <see cref="CameraFollowTarget"/> component
/// </summary>
/// <remarks>
///   <para>
///     This is not a frame run system to not cause massive jitter. Related:
///     https://github.com/Revolutionary-Games/Thrive/issues/4695
///   </para>
/// </remarks>
[With(typeof(CameraFollowTarget))]
[With(typeof(WorldPosition))]
[ReadsComponent(typeof(WorldPosition))]
[RunsAfter(typeof(PhysicsUpdateAndPositionSystem))]
[RunsAfter(typeof(AttachedEntityPositionSystem))]
[RunsOnMainThread]
public sealed class CameraFollowSystem : AEntitySetSystem<float>
{
    private bool cameraUsed;

    private bool errorPrinted;
    private bool warnedAboutMissingCamera;

    public CameraFollowSystem(World world) : base(world, null)
    {
    }

    /// <summary>
    ///   Needs to be set by the game stage using this system to the camera that needs updating, otherwise this
    ///   system does nothing
    /// </summary>
    [JsonIgnore]
    public IGameCamera? Camera { get; set; }

    protected override void PreUpdate(float delta)
    {
        base.PreUpdate(delta);

        cameraUsed = false;
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var followTarget = ref entity.Get<CameraFollowTarget>();

        if (followTarget.Disabled)
            return;

        if (cameraUsed)
        {
            if (!errorPrinted)
            {
                errorPrinted = true;
                GD.PrintErr(
                    "Multiple entities have active CameraFollowTarget components, camera will follow a random one");
            }

            return;
        }

        cameraUsed = true;

        if (Camera != null)
        {
            ref var position = ref entity.Get<WorldPosition>();

            Camera.UpdateCameraPosition(delta, position.Position);
        }
        else if (!warnedAboutMissingCamera)
        {
            warnedAboutMissingCamera = true;
            GD.PrintErr("CameraFollowSystem doesn't have camera set, can't follow an entity");
        }
    }

    protected override void PostUpdate(float delta)
    {
        base.PostUpdate(delta);

        if (!cameraUsed)
        {
            // Update camera without a target
            Camera?.UpdateCameraPosition(delta, null);
        }
    }
}
