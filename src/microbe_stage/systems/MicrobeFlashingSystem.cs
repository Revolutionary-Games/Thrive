namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Handles flashing microbes different colour based on the mode they are in or if they are taking damage. Needs
///   to run before the damage events are cleared.
/// </summary>
[ReadsComponent(typeof(MicrobeControl))]
[ReadsComponent(typeof(Selectable))]
[RunsAfter(typeof(OsmoregulationAndHealingSystem))]
[RunsBefore(typeof(DamageSoundSystem))]
public partial class MicrobeFlashingSystem : BaseSystem<World, float>
{
    public MicrobeFlashingSystem(World world) : base(world)
    {
    }

    [Query]
    [All<Selectable>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref MicrobeControl control, ref ColourAnimation animation, ref Health health, in Entity entity)
    {
        if (HasReceivedDamage(ref health))
        {
            // Flash the microbe red
            animation.Flash(new Color(1, 0, 0, 0.5f), Constants.MICROBE_FLASH_DURATION, 1);
            return;
        }

        // Flash based on the current state of the microbe
        switch (control.State)
        {
            default:
            case MicrobeState.Normal:
                break;
            case MicrobeState.Binding:
                animation.Flash(new Color(0.2f, 0.5f, 0.0f, 0.5f), Constants.MICROBE_FLASH_DURATION);
                break;
            case MicrobeState.Unbinding:
            {
                if (entity.Get<Selectable>().Selected)
                {
                    animation.Flash(new Color(1.0f, 0.0f, 0.0f, 0.5f), Constants.MICROBE_FLASH_DURATION);
                }
                else
                {
                    animation.Flash(new Color(1.0f, 0.5f, 0.2f, 0.5f), Constants.MICROBE_FLASH_DURATION);
                }

                break;
            }
        }
    }

    private bool HasReceivedDamage(ref Health health)
    {
        var damageEvents = health.RecentDamageReceived;

        if (damageEvents == null)
            return false;

        lock (damageEvents)
        {
            return damageEvents.Count > 0;
        }
    }
}
