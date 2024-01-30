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

        // Execution group 2
        pathBasedSceneLoader.Update(delta);
        entityMaterialFetchSystem.Update(delta);
        simpleShapeCreatorSystem.Update(delta);
        microbePhysicsCreationAndSizeSystem.Update(delta);
        physicsBodyCreationSystem.Update(delta);

        // Execution group 3
        physicsBodyDisablingSystem.Update(delta);

        // Execution group 4
        physicsUpdateAndPositionSystem.Update(delta);
        damageCooldownSystem.Update(delta);
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
        organelleTickSystem.Update(delta);

        // Execution group 5
        physicsSensorSystem.Update(delta);
        microbeRenderPrioritySystem.Update(delta);
        engulfingSystem.Update(delta);

        // Execution group 6
        countLimitedDespawnSystem.Update(delta);
        spatialAttachSystem.Update(delta);
        SpawnSystem.Update(delta);
        microbeEventCallbackSystem.Update(delta);

        // Execution group 7
        microbeCollisionSoundSystem.Update(delta);
        attachedEntityPositionSystem.Update(delta);
        colonyBindingSystem.Update(delta);
        microbeFlashingSystem.Update(delta);
        damageSoundSystem.Update(delta);
        microbeMovementSoundSystem.Update(delta);
        soundEffectSystem.Update(delta);
        soundListenerSystem.Update(delta);
        spatialPositionSystem.Update(delta);
        cellBurstEffectSystem.Update(delta);
        CameraFollowSystem.Update(delta);
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
