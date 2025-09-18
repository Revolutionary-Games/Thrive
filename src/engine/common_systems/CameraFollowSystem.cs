namespace Systems;

using System.Runtime.CompilerServices;
using Arch.System;
using Components;
using Godot;
using Newtonsoft.Json;
using World = Arch.Core.World;

/// <summary>
///   Handles moving a camera to follow entity with <see cref="CameraFollowTarget"/> component
/// </summary>
/// <remarks>
///   <para>
///     This is not a frame run system to not cause massive jitter. Related:
///     https://github.com/Revolutionary-Games/Thrive/issues/4695
///   </para>
/// </remarks>
[ReadsComponent(typeof(WorldPosition))]
[RunsAfter(typeof(PhysicsUpdateAndPositionSystem))]
[RunsAfter(typeof(AttachedEntityPositionSystem))]
[RunsOnMainThread]
public partial class CameraFollowSystem : BaseSystem<World, float>
{
    private bool cameraUsed;

    private bool errorPrinted;
    private bool warnedAboutMissingCamera;

    public CameraFollowSystem(World world) : base(world)
    {
    }

    /// <summary>
    ///   Needs to be set by the game stage using this system to the camera that needs updating, otherwise this
    ///   system does nothing
    /// </summary>
    [JsonIgnore]
    public IGameCamera? Camera { get; set; }

    public override void BeforeUpdate(in float delta)
    {
        cameraUsed = false;
    }

    public override void AfterUpdate(in float delta)
    {
        if (!cameraUsed)
        {
            // Update camera without a target
            Camera?.UpdateCameraPosition(delta, null);
        }
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref CameraFollowTarget followTarget, ref WorldPosition position)
    {
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
            Camera.UpdateCameraPosition(delta, position.Position);
        }
        else if (!warnedAboutMissingCamera)
        {
            warnedAboutMissingCamera = true;
            GD.PrintErr("CameraFollowSystem doesn't have camera set, can't follow an entity");
        }
    }
}
