namespace Systems;

using System.Runtime.CompilerServices;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Hears the sounds from <see cref="SoundEffectPlayer"/> (this system marks where the player's ears are)
/// </summary>
[ReadsComponent(typeof(WorldPosition))]
[RunsAfter(typeof(PhysicsUpdateAndPositionSystem))]
[RunsAfter(typeof(AttachedEntityPositionSystem))]
[RuntimeCost(2)]
[RunsOnMainThread]
public partial class SoundListenerSystem : BaseSystem<World, float>
{
    private readonly AudioListener3D listener;

    private Transform3D? wantedListenerPosition;

    private bool useTopDownOrientation;

    private bool printedError;

    public SoundListenerSystem(Node listenerParentNode, World world) : base(world)
    {
        listener = new AudioListener3D();
        listener.ClearCurrent();
        listenerParentNode.AddChild(listener);
    }

    public override void Dispose()
    {
        Dispose(true);
        base.Dispose();
    }

    public override void BeforeUpdate(in float delta)
    {
        wantedListenerPosition = null;
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref SoundListener soundListener, ref WorldPosition position)
    {
        if (soundListener.Disabled)
            return;

        if (wantedListenerPosition != null)
        {
            if (!printedError)
            {
                GD.PrintErr("Multiple SoundListener entities are active at once. Only last one will work! " +
                    "This error won't be printed again.");
                printedError = true;
            }
        }

        useTopDownOrientation = soundListener.UseTopDownRotation;
        wantedListenerPosition = position.ToTransform();
    }

    public override void AfterUpdate(in float delta)
    {
        if (wantedListenerPosition == null)
        {
            if (listener.IsCurrent())
                listener.ClearCurrent();
        }
        else
        {
            if (useTopDownOrientation)
            {
                // Listener is directional, so in this case we want to separate the rotation out from the entity
                // transform to not use it
                var transform = wantedListenerPosition.Value;
                transform.Basis = new Basis(Quaternion.FromEuler(new Vector3(0.0f, 0.0f, -1.0f)));
                listener.GlobalTransform = transform;
            }
            else
            {
                listener.GlobalTransform = wantedListenerPosition.Value;
            }

            if (!listener.IsCurrent())
                listener.MakeCurrent();
        }
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            listener.Dispose();
        }
    }
}
