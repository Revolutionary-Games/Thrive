// Automatically generated file. DO NOT EDIT!
// Run GenerateThreadedSystems to generate this file
public partial class MicrobeWorldSimulation
{
    protected override void OnProcessFixedLogic(float delta)
    {
        // Execution group 1
        animationControlSystem.Update(delta);

        // Execution group 2
        entityMaterialFetchSystem.Update(delta);

        // Execution group 3
        simpleShapeCreatorSystem.Update(delta);
        physicsBodyCreationSystem.Update(delta);

        // Execution group 4
        microbeReproductionSystem.Update(delta);
        organelleComponentFetchSystem.Update(delta);
        microbeMovementSystem.Update(delta);
        physicsBodyDisablingSystem.Update(delta);
        physicsSensorSystem.Update(delta);

        // Execution group 5
        predefinedVisualLoaderSystem.Update(delta);

        // Execution group 6
        osmoregulationAndHealingSystem.Update(delta);
        microbeFlashingSystem.Update(delta);
        damageSoundSystem.Update(delta);
        colonyBindingSystem.Update(delta);
        soundEffectSystem.Update(delta);

        // Execution group 7
        attachedEntityPositionSystem.Update(delta);
        physicsUpdateAndPositionSystem.Update(delta);
        spatialPositionSystem.Update(delta);

        // Execution group 8
        cellBurstEffectSystem.Update(delta);

        // Execution group 9
        pilusDamageSystem.Update(delta);
        microbeDeathSystem.Update(delta);
        colonyCompoundDistributionSystem.Update(delta);
        engulfingSystem.Update(delta);

        // Execution group 10
        if (RunAI)
        {
            microbeAI.ReportPotentialPlayerPosition(reportedPlayerPosition);
            microbeAI.Update(delta);
        }

        microbeEventCallbackSystem.Update(delta);

        // Execution group 11
        microbeVisualsSystem.Update(delta);

        // Execution group 12
        organelleTickSystem.Update(delta);

        // Execution group 13
        microbeRenderPrioritySystem.Update(delta);
        countLimitedDespawnSystem.Update(delta);
        damageCooldownSystem.Update(delta);
        damageOnTouchSystem.Update(delta);
        disallowPlayerBodySleepSystem.Update(delta);
        fadeOutActionSystem.Update(delta);
        pathBasedSceneLoader.Update(delta);
        physicsBodyControlSystem.Update(delta);
        physicsCollisionManagementSystem.Update(delta);
        collisionShapeLoaderSystem.Update(delta);
        soundListenerSystem.Update(delta);
        spatialAttachSystem.Update(delta);
        allCompoundsVentingSystem.Update(delta);
        colonyStatsUpdateSystem.Update(delta);
        compoundAbsorptionSystem.Update(delta);
        engulfedDigestionSystem.Update(delta);
        engulfedHandlingSystem.Update(delta);
        entitySignalingSystem.Update(delta);
        fluidCurrentsSystem.Update(delta);
        microbeCollisionSoundSystem.Update(delta);
        microbeEmissionSystem.Update(delta);
        microbeMovementSoundSystem.Update(delta);
        slimeSlowdownSystem.Update(delta);
        microbePhysicsCreationAndSizeSystem.Update(delta);
        toxinCollisionSystem.Update(delta);
        unneededCompoundVentingSystem.Update(delta);
        delayedColonyOperationSystem.Update(delta);
        multicellularGrowthSystem.Update(delta);
        CameraFollowSystem.Update(delta);
        SpawnSystem.Update(delta);
        ProcessSystem.Update(delta);
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
