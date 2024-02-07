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
        var background1 = new Task(() =>
            {
                // Timeslot 1 on thread 2
                simpleShapeCreatorSystem.Update(delta);
                countLimitedDespawnSystem.Update(delta);
                damageCooldownSystem.Update(delta);
                colonyCompoundDistributionSystem.Update(delta);
                entitySignalingSystem.Update(delta);
                collisionShapeLoaderSystem.Update(delta);
                barrier1.SignalAndWait();

                // Timeslot 3 on thread 2
                physicsCollisionManagementSystem.Update(delta);
                damageOnTouchSystem.Update(delta);
                toxinCollisionSystem.Update(delta);
                pilusDamageSystem.Update(delta);
                disallowPlayerBodySleepSystem.Update(delta);
                microbeCollisionSoundSystem.Update(delta);
                barrier1.SignalAndWait();
                barrier1.SignalAndWait();

                // Timeslot 5 on thread 2
                osmoregulationAndHealingSystem.Update(delta);
                microbeReproductionSystem.Update(delta);
                organelleComponentFetchSystem.Update(delta);
                barrier1.SignalAndWait();

                // Timeslot 6 on thread 2
                slimeSlowdownSystem.Update(delta);
                barrier1.SignalAndWait();

                // Timeslot 8 on thread 2
                colonyBindingSystem.Update(delta);
                microbeFlashingSystem.Update(delta);
                barrier1.SignalAndWait();

                // Timeslot 9 on thread 2
                delayedColonyOperationSystem.Update(delta);
                barrier1.SignalAndWait();

                // Timeslot 10 on thread 2
                microbeMovementSoundSystem.Update(delta);
                microbeEventCallbackSystem.Update(delta);
                barrier1.SignalAndWait();

                barrier1.SignalAndWait();
            });

        TaskExecutor.Instance.AddTask(background1);

        // Timeslot 1 on thread 1
        pathBasedSceneLoader.Update(delta);
        predefinedVisualLoaderSystem.Update(delta);
        microbeVisualsSystem.Update(delta);
        animationControlSystem.Update(delta);
        entityMaterialFetchSystem.Update(delta);
        fluidCurrentsSystem.Update(delta);
        TimedLifeSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 2 on thread 1
        microbePhysicsCreationAndSizeSystem.Update(delta);
        physicsBodyCreationSystem.Update(delta);
        physicsBodyDisablingSystem.Update(delta);
        physicsUpdateAndPositionSystem.Update(delta);

        // Timeslot 3 on thread 1
        attachedEntityPositionSystem.Update(delta);
        allCompoundsVentingSystem.Update(delta);
        unneededCompoundVentingSystem.Update(delta);
        compoundAbsorptionSystem.Update(delta);
        ProcessSystem.Update(delta);
        multicellularGrowthSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 4 on thread 1
        engulfingSystem.Update(delta);
        spatialAttachSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 5 on thread 1
        spatialPositionSystem.Update(delta);
        SpawnSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 6 on thread 1
        engulfedDigestionSystem.Update(delta);
        colonyStatsUpdateSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 7 on thread 1
        engulfedHandlingSystem.Update(delta);
        if (RunAI)
        {
            microbeAI.ReportPotentialPlayerPosition(reportedPlayerPosition);
            microbeAI.Update(delta);
        }

        microbeEmissionSystem.Update(delta);
        microbeMovementSystem.Update(delta);

        // Timeslot 8 on thread 1
        physicsBodyControlSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 9 on thread 1
        microbeDeathSystem.Update(delta);
        fadeOutActionSystem.Update(delta);
        organelleTickSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 10 on thread 1
        physicsSensorSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 11 on thread 1
        damageSoundSystem.Update(delta);
        soundEffectSystem.Update(delta);
        soundListenerSystem.Update(delta);
        cellBurstEffectSystem.Update(delta);

        // Timeslot 12 on thread 1
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
