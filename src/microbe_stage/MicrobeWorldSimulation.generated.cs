// Automatically generated file. DO NOT EDIT!
// Run GenerateThreadedSystems to generate this file
public partial class MicrobeWorldSimulation
{
    protected override void OnProcessFixedLogic(float delta)
    {
        // Execution group 1
        animationControlSystem.Update(delta);
        predefinedVisualLoaderSystem.Update(delta);
        microbeVisualsSystem.Update(delta);
        pathBasedSceneLoader.Update(delta);

        // Execution group 2
        entityMaterialFetchSystem.Update(delta);
        simpleShapeCreatorSystem.Update(delta);
        microbePhysicsCreationAndSizeSystem.Update(delta);

        // Execution group 3
        physicsBodyCreationSystem.Update(delta);

        // Execution group 4
        physicsUpdateAndPositionSystem.Update(delta);

        // Execution group 5
        attachedEntityPositionSystem.Update(delta);
        physicsBodyDisablingSystem.Update(delta);
        damageCooldownSystem.Update(delta);
        physicsCollisionManagementSystem.Update(delta);

        // Execution group 6
        damageOnTouchSystem.Update(delta);
        disallowPlayerBodySleepSystem.Update(delta);
        colonyCompoundDistributionSystem.Update(delta);
        pilusDamageSystem.Update(delta);
        allCompoundsVentingSystem.Update(delta);

        // Execution group 7
        unneededCompoundVentingSystem.Update(delta);

        // Execution group 8
        compoundAbsorptionSystem.Update(delta);

        // Execution group 9
        ProcessSystem.Update(delta);

        // Execution group 10
        multicellularGrowthSystem.Update(delta);
        entitySignalingSystem.Update(delta);
        toxinCollisionSystem.Update(delta);
        microbeCollisionSoundSystem.Update(delta);
        osmoregulationAndHealingSystem.Update(delta);

        // Execution group 11
        microbeReproductionSystem.Update(delta);

        // Execution group 12
        organelleComponentFetchSystem.Update(delta);

        // Execution group 13
        if (RunAI)
        {
            microbeAI.ReportPotentialPlayerPosition(reportedPlayerPosition);
            microbeAI.Update(delta);
        }

        // Execution group 14
        microbeEmissionSystem.Update(delta);
        slimeSlowdownSystem.Update(delta);

        // Execution group 15
        microbeMovementSystem.Update(delta);

        // Execution group 16
        physicsBodyControlSystem.Update(delta);
        microbeMovementSoundSystem.Update(delta);
        organelleTickSystem.Update(delta);

        // Execution group 17
        physicsSensorSystem.Update(delta);
        microbeRenderPrioritySystem.Update(delta);
        engulfingSystem.Update(delta);
        countLimitedDespawnSystem.Update(delta);
        spatialAttachSystem.Update(delta);

        // Execution group 18
        SpawnSystem.Update(delta);

        // Execution group 19
        microbeEventCallbackSystem.Update(delta);
        colonyBindingSystem.Update(delta);

        // Execution group 20
        microbeFlashingSystem.Update(delta);

        // Execution group 21
        damageSoundSystem.Update(delta);

        // Execution group 22
        soundEffectSystem.Update(delta);
        soundListenerSystem.Update(delta);
        spatialPositionSystem.Update(delta);
        cellBurstEffectSystem.Update(delta);
        CameraFollowSystem.Update(delta);
        colonyStatsUpdateSystem.Update(delta);
        engulfedDigestionSystem.Update(delta);
        microbeDeathSystem.Update(delta);

        // Execution group 23
        fadeOutActionSystem.Update(delta);
        collisionShapeLoaderSystem.Update(delta);
        engulfedHandlingSystem.Update(delta);
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
}
