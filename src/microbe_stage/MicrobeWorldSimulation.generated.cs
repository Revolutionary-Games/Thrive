// Automatically generated file. DO NOT EDIT!
// Run GenerateThreadedSystems to generate this file
using System.Threading;
using System.Threading.Tasks;

public partial class MicrobeWorldSimulation
{
    private readonly Barrier barrier1 = new(2);

    protected override void OnProcessFixedLogic(float delta)
    {
        var background1 = new Task(() =>
            {
                // Execution group 2
                simpleShapeCreatorSystem.Update(delta);
                microbePhysicsCreationAndSizeSystem.Update(delta);

                // Execution group 4
                physicsUpdateAndPositionSystem.Update(delta);
                damageCooldownSystem.Update(delta);
                barrier1.SignalAndWait();
                physicsCollisionManagementSystem.Update(delta);
                damageOnTouchSystem.Update(delta);
                colonyCompoundDistributionSystem.Update(delta);
                pilusDamageSystem.Update(delta);
                allCompoundsVentingSystem.Update(delta);
                unneededCompoundVentingSystem.Update(delta);
                compoundAbsorptionSystem.Update(delta);
                ProcessSystem.Update(delta);
                multicellularGrowthSystem.Update(delta);
                entitySignalingSystem.Update(delta);
                toxinCollisionSystem.Update(delta);
                osmoregulationAndHealingSystem.Update(delta);
                microbeReproductionSystem.Update(delta);
                organelleComponentFetchSystem.Update(delta);
                if (RunAI)
                {
                    microbeAI.ReportPotentialPlayerPosition(reportedPlayerPosition);
                    microbeAI.Update(delta);
                }

                microbeEmissionSystem.Update(delta);
                slimeSlowdownSystem.Update(delta);
                microbeMovementSystem.Update(delta);

                // Execution group 6
                countLimitedDespawnSystem.Update(delta);
                barrier1.SignalAndWait();
                spatialAttachSystem.Update(delta);
                SpawnSystem.Update(delta);

                // Execution group 7
                microbeCollisionSoundSystem.Update(delta);
                barrier1.SignalAndWait();
                attachedEntityPositionSystem.Update(delta);
                colonyBindingSystem.Update(delta);
                microbeFlashingSystem.Update(delta);
                damageSoundSystem.Update(delta);
                microbeMovementSoundSystem.Update(delta);
                disallowPlayerBodySleepSystem.Update(delta);
                colonyStatsUpdateSystem.Update(delta);
                engulfedDigestionSystem.Update(delta);
                microbeDeathSystem.Update(delta);
                fadeOutActionSystem.Update(delta);
                physicsBodyControlSystem.Update(delta);
                collisionShapeLoaderSystem.Update(delta);
                engulfedHandlingSystem.Update(delta);
                fluidCurrentsSystem.Update(delta);
                delayedColonyOperationSystem.Update(delta);
                barrier1.SignalAndWait();
                TimedLifeSystem.Update(delta);
                barrier1.SignalAndWait();

                barrier1.SignalAndWait();
            });

        TaskExecutor.Instance.AddTask(background1);

        // Execution group 1 (on main)
        animationControlSystem.Update(delta);
        pathBasedSceneLoader.Update(delta);
        predefinedVisualLoaderSystem.Update(delta);
        barrier1.SignalAndWait();
        microbeVisualsSystem.Update(delta);

        // Execution group 2 (on main)
        entityMaterialFetchSystem.Update(delta);
        physicsBodyCreationSystem.Update(delta);

        // Execution group 3 (on main)
        physicsBodyDisablingSystem.Update(delta);
        barrier1.SignalAndWait();

        // Execution group 4 (on main)
        organelleTickSystem.Update(delta);

        // Execution group 5 (on main)
        physicsSensorSystem.Update(delta);
        microbeRenderPrioritySystem.Update(delta);
        barrier1.SignalAndWait();
        engulfingSystem.Update(delta);

        // Execution group 6 (on main)
        microbeEventCallbackSystem.Update(delta);
        barrier1.SignalAndWait();

        // Execution group 7 (on main)
        soundEffectSystem.Update(delta);
        soundListenerSystem.Update(delta);
        spatialPositionSystem.Update(delta);
        barrier1.SignalAndWait();
        cellBurstEffectSystem.Update(delta);
        CameraFollowSystem.Update(delta);

        barrier1.SignalAndWait();

        cellCountingEntitySet.Complete();

        reportedPlayerPosition = null;
    }

    private void OnProcessFrameLogic(float delta)
    {
        ThrowIfNotInitialized();

        // NOTE: not currently ran in parallel due to low frame system count
        colourAnimationSystem.Update(delta);
        microbeShaderSystem.Update(delta);
        tintColourApplyingSystem.Update(delta);
    }

    private void DisposeGenerated()
    {
        barrier1.Dispose();
    }
}
