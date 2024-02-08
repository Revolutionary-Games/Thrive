// Automatically generated file. DO NOT EDIT!
// Run GenerateThreadedSystems to generate this file
using System.Threading;
using System.Threading.Tasks;

public partial class MicrobeWorldSimulation
{
    private readonly SimpleBarrier barrier1 = new(2);

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
                compoundAbsorptionSystem.Update(delta);
                ProcessSystem.Update(delta);
                barrier1.SignalAndWait();

                // Timeslot 2 on thread 2
                unneededCompoundVentingSystem.Update(delta);
                barrier1.SignalAndWait();
                barrier1.SignalAndWait();

                // Timeslot 4 on thread 2
                multicellularGrowthSystem.Update(delta);

                // Timeslot 5 on thread 2
                pilusDamageSystem.Update(delta);
                microbeCollisionSoundSystem.Update(delta);
                barrier1.SignalAndWait();

                // Timeslot 7 on thread 2
                toxinCollisionSystem.Update(delta);
                SpawnSystem.Update(delta);
                colonyStatsUpdateSystem.Update(delta);
                barrier1.SignalAndWait();

                // Timeslot 14 on thread 2
                microbeMovementSoundSystem.Update(delta);
                microbeFlashingSystem.Update(delta);
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
        entitySignalingSystem.Update(delta);
        fluidCurrentsSystem.Update(delta);
        TimedLifeSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 2 on thread 1
        collisionShapeLoaderSystem.Update(delta);

        // Timeslot 3 on thread 1
        microbePhysicsCreationAndSizeSystem.Update(delta);
        physicsBodyCreationSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 4 on thread 1
        physicsBodyDisablingSystem.Update(delta);
        physicsCollisionManagementSystem.Update(delta);
        damageOnTouchSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 5 on thread 1
        physicsUpdateAndPositionSystem.Update(delta);
        attachedEntityPositionSystem.Update(delta);
        allCompoundsVentingSystem.Update(delta);
        disallowPlayerBodySleepSystem.Update(delta);

        // Timeslot 6 on thread 1
        engulfingSystem.Update(delta);
        spatialAttachSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 7 on thread 1
        spatialPositionSystem.Update(delta);
        engulfedDigestionSystem.Update(delta);

        // Timeslot 8 on thread 1
        osmoregulationAndHealingSystem.Update(delta);
        microbeReproductionSystem.Update(delta);

        // Timeslot 9 on thread 1
        organelleComponentFetchSystem.Update(delta);
        engulfedHandlingSystem.Update(delta);

        // Timeslot 10 on thread 1
        if (RunAI)
        {
            microbeAI.ReportPotentialPlayerPosition(reportedPlayerPosition);
            microbeAI.Update(delta);
        }

        microbeEmissionSystem.Update(delta);

        // Timeslot 11 on thread 1
        slimeSlowdownSystem.Update(delta);
        microbeMovementSystem.Update(delta);

        // Timeslot 12 on thread 1
        physicsBodyControlSystem.Update(delta);
        colonyBindingSystem.Update(delta);

        // Timeslot 13 on thread 1
        delayedColonyOperationSystem.Update(delta);
        microbeDeathSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 14 on thread 1
        fadeOutActionSystem.Update(delta);
        organelleTickSystem.Update(delta);
        physicsSensorSystem.Update(delta);
        barrier1.SignalAndWait();

        // Timeslot 15 on thread 1
        microbeEventCallbackSystem.Update(delta);
        damageSoundSystem.Update(delta);

        // Timeslot 16 on thread 1
        soundEffectSystem.Update(delta);
        soundListenerSystem.Update(delta);

        // Timeslot 17 on thread 1
        cellBurstEffectSystem.Update(delta);
        microbeRenderPrioritySystem.Update(delta);

        // Timeslot 18 on thread 1
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
        colonyCompoundDistributionSystem.Update(delta);
        toxinCollisionSystem.Update(delta);
        damageOnTouchSystem.Update(delta);
        pilusDamageSystem.Update(delta);
        engulfingSystem.Update(delta);
        spatialAttachSystem.Update(delta);
        spatialPositionSystem.Update(delta);
        countLimitedDespawnSystem.Update(delta);
        compoundAbsorptionSystem.Update(delta);
        ProcessSystem.Update(delta);
        multicellularGrowthSystem.Update(delta);
        SpawnSystem.Update(delta);
        colonyStatsUpdateSystem.Update(delta);
        entitySignalingSystem.Update(delta);
        osmoregulationAndHealingSystem.Update(delta);
        microbeReproductionSystem.Update(delta);
        organelleComponentFetchSystem.Update(delta);
        unneededCompoundVentingSystem.Update(delta);
        allCompoundsVentingSystem.Update(delta);
        engulfedDigestionSystem.Update(delta);
        engulfedHandlingSystem.Update(delta);
        if (RunAI)
        {
            microbeAI.ReportPotentialPlayerPosition(reportedPlayerPosition);
            microbeAI.Update(delta);
        }

        microbeEmissionSystem.Update(delta);
        microbeDeathSystem.Update(delta);
        slimeSlowdownSystem.Update(delta);
        microbeMovementSystem.Update(delta);
        physicsBodyControlSystem.Update(delta);
        colonyBindingSystem.Update(delta);
        delayedColonyOperationSystem.Update(delta);
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
    }
}
