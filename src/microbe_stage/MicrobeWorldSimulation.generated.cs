// Automatically generated file. DO NOT EDIT!
// Run GenerateThreadedSystems to generate this file
using System.Threading;
using System.Threading.Tasks;

public partial class MicrobeWorldSimulation
{
    private readonly Barrier barrier1 = new(2);

    private void InitGenerated()
    {
    }

    private void OnProcessFixedWithThreads(float delta)
    {
        var background0 = new Task(() =>
            {
                // Execution group 1 on thread 2
                simpleShapeCreatorSystem.Update(delta);
                microbePhysicsCreationAndSizeSystem.Update(delta);

                // Execution group 2 on thread 2
                damageCooldownSystem.Update(delta);
                barrier1.SignalAndWait();
                barrier1.SignalAndWait();
                physicsCollisionManagementSystem.Update(delta);
                colonyCompoundDistributionSystem.Update(delta);
                pilusDamageSystem.Update(delta);

                // Execution group 4 on thread 2
                physicsUpdateAndPositionSystem.Update(delta);
                attachedEntityPositionSystem.Update(delta);
                countLimitedDespawnSystem.Update(delta);
                damageOnTouchSystem.Update(delta);
                allCompoundsVentingSystem.Update(delta);
                unneededCompoundVentingSystem.Update(delta);
                compoundAbsorptionSystem.Update(delta);
                entitySignalingSystem.Update(delta);
                toxinCollisionSystem.Update(delta);
                ProcessSystem.Update(delta);
                multicellularGrowthSystem.Update(delta);
                osmoregulationAndHealingSystem.Update(delta);
                microbeReproductionSystem.Update(delta);
                organelleComponentFetchSystem.Update(delta);
                if (RunAI)
                {
                    microbeAI.ReportPotentialPlayerPosition(reportedPlayerPosition);
                    microbeAI.Update(delta);
                }

                microbeEmissionSystem.Update(delta);
                barrier1.SignalAndWait();
                barrier1.SignalAndWait();
                SpawnSystem.Update(delta);
                colonyStatsUpdateSystem.Update(delta);
                microbeDeathSystem.Update(delta);
                slimeSlowdownSystem.Update(delta);
                microbeMovementSystem.Update(delta);

                // Execution group 5 on thread 2
                colonyBindingSystem.Update(delta);
                physicsBodyControlSystem.Update(delta);
                microbeMovementSoundSystem.Update(delta);
                barrier1.SignalAndWait();
                microbeEventCallbackSystem.Update(delta);
                microbeFlashingSystem.Update(delta);
                barrier1.SignalAndWait();
                barrier1.SignalAndWait();
                damageSoundSystem.Update(delta);
                microbeCollisionSoundSystem.Update(delta);
                engulfedDigestionSystem.Update(delta);
                engulfedHandlingSystem.Update(delta);
                disallowPlayerBodySleepSystem.Update(delta);
                collisionShapeLoaderSystem.Update(delta);
                fluidCurrentsSystem.Update(delta);
                delayedColonyOperationSystem.Update(delta);
                barrier1.SignalAndWait();
                TimedLifeSystem.Update(delta);
                barrier1.SignalAndWait();

                barrier1.SignalAndWait();
            });

        TaskExecutor.Instance.AddTask(background0);

        // Execution group 1 (on main)
        pathBasedSceneLoader.Update(delta);
        predefinedVisualLoaderSystem.Update(delta);
        barrier1.SignalAndWait();
        microbeVisualsSystem.Update(delta);
        animationControlSystem.Update(delta);
        entityMaterialFetchSystem.Update(delta);
        physicsBodyCreationSystem.Update(delta);

        // Execution group 2 (on main)
        physicsBodyDisablingSystem.Update(delta);
        barrier1.SignalAndWait();
        barrier1.SignalAndWait();
        engulfingSystem.Update(delta);

        // Execution group 3 (on main)
        spatialAttachSystem.Update(delta);

        // Execution group 4 (on main)
        spatialPositionSystem.Update(delta);
        barrier1.SignalAndWait();
        barrier1.SignalAndWait();
        fadeOutActionSystem.Update(delta);
        barrier1.SignalAndWait();
        organelleTickSystem.Update(delta);

        // Execution group 5 (on main)
        physicsSensorSystem.Update(delta);
        barrier1.SignalAndWait();
        barrier1.SignalAndWait();
        soundEffectSystem.Update(delta);
        soundListenerSystem.Update(delta);
        barrier1.SignalAndWait();
        cellBurstEffectSystem.Update(delta);
        microbeRenderPrioritySystem.Update(delta);
        CameraFollowSystem.Update(delta);

        barrier1.SignalAndWait();

        cellCountingEntitySet.Complete();
        reportedPlayerPosition = null;
    }

    private void OnProcessFixedWithoutThreads(float delta)
    {
        // This variant doesn't use threading, use when not enough threads are available
        // or threaded run would be slower (or just for debugging)
        pathBasedSceneLoader.Update(delta);
        predefinedVisualLoaderSystem.Update(delta);
        microbeVisualsSystem.Update(delta);
        animationControlSystem.Update(delta);
        entityMaterialFetchSystem.Update(delta);
        collisionShapeLoaderSystem.Update(delta);
        simpleShapeCreatorSystem.Update(delta);
        microbePhysicsCreationAndSizeSystem.Update(delta);
        physicsBodyCreationSystem.Update(delta);
        physicsUpdateAndPositionSystem.Update(delta);
        attachedEntityPositionSystem.Update(delta);
        physicsBodyDisablingSystem.Update(delta);
        damageCooldownSystem.Update(delta);
        physicsCollisionManagementSystem.Update(delta);
        damageOnTouchSystem.Update(delta);
        colonyCompoundDistributionSystem.Update(delta);
        toxinCollisionSystem.Update(delta);
        pilusDamageSystem.Update(delta);
        engulfingSystem.Update(delta);
        spatialAttachSystem.Update(delta);
        spatialPositionSystem.Update(delta);
        countLimitedDespawnSystem.Update(delta);
        allCompoundsVentingSystem.Update(delta);
        unneededCompoundVentingSystem.Update(delta);
        compoundAbsorptionSystem.Update(delta);
        entitySignalingSystem.Update(delta);
        ProcessSystem.Update(delta);
        multicellularGrowthSystem.Update(delta);
        osmoregulationAndHealingSystem.Update(delta);
        microbeReproductionSystem.Update(delta);
        organelleComponentFetchSystem.Update(delta);
        engulfedDigestionSystem.Update(delta);
        engulfedHandlingSystem.Update(delta);
        if (RunAI)
        {
            microbeAI.ReportPotentialPlayerPosition(reportedPlayerPosition);
            microbeAI.Update(delta);
        }

        microbeEmissionSystem.Update(delta);
        slimeSlowdownSystem.Update(delta);
        microbeMovementSystem.Update(delta);
        physicsBodyControlSystem.Update(delta);
        colonyBindingSystem.Update(delta);
        SpawnSystem.Update(delta);
        colonyStatsUpdateSystem.Update(delta);
        microbeDeathSystem.Update(delta);
        fadeOutActionSystem.Update(delta);
        organelleTickSystem.Update(delta);
        physicsSensorSystem.Update(delta);
        microbeMovementSoundSystem.Update(delta);
        microbeEventCallbackSystem.Update(delta);
        microbeFlashingSystem.Update(delta);
        damageSoundSystem.Update(delta);
        microbeCollisionSoundSystem.Update(delta);
        soundEffectSystem.Update(delta);
        soundListenerSystem.Update(delta);
        cellBurstEffectSystem.Update(delta);
        microbeRenderPrioritySystem.Update(delta);
        CameraFollowSystem.Update(delta);
        disallowPlayerBodySleepSystem.Update(delta);
        fluidCurrentsSystem.Update(delta);
        delayedColonyOperationSystem.Update(delta);
        TimedLifeSystem.Update(delta);

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
