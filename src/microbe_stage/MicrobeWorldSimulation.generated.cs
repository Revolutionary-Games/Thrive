// Automatically generated file. DO NOT EDIT!
// Run GenerateThreadedSystems to generate this file
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Godot;

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
                // Catch for extra system run safety (for debugging why higher level catches don't get errors)
                try
                {
                    // Timeslot 1 on thread 2
                    MicrobeTerrainSystem.BeforeUpdate(delta);
                    MicrobeTerrainSystem.Update(delta);
                    MicrobeTerrainSystem.AfterUpdate(delta);
                    strainSystem.BeforeUpdate(delta);
                    strainSystem.Update(delta);
                    strainSystem.AfterUpdate(delta);
                    entitySignalingSystem.BeforeUpdate(delta);
                    entitySignalingSystem.Update(delta);
                    entitySignalingSystem.AfterUpdate(delta);
                    countLimitedDespawnSystem.BeforeUpdate(delta);
                    countLimitedDespawnSystem.Update(delta);
                    countLimitedDespawnSystem.AfterUpdate(delta);
                    microbeDivisionClippingSystem.BeforeUpdate(delta);
                    microbeDivisionClippingSystem.Update(delta);
                    microbeDivisionClippingSystem.AfterUpdate(delta);
                    barrier1.SignalAndWait();

                    // Timeslot 2 on thread 2
                    colonyCompoundDistributionSystem.BeforeUpdate(delta);
                    colonyCompoundDistributionSystem.Update(delta);
                    colonyCompoundDistributionSystem.AfterUpdate(delta);
                    endosymbiontOrganelleSystem.BeforeUpdate(delta);
                    endosymbiontOrganelleSystem.Update(delta);
                    endosymbiontOrganelleSystem.AfterUpdate(delta);
                    barrier1.SignalAndWait();
                    barrier1.SignalAndWait();

                    // Timeslot 4 on thread 2
                    microbePhysicsCreationAndSizeSystem.BeforeUpdate(delta);
                    microbePhysicsCreationAndSizeSystem.Update(delta);
                    microbePhysicsCreationAndSizeSystem.AfterUpdate(delta);
                    barrier1.SignalAndWait();
                    barrier1.SignalAndWait();

                    // Timeslot 6 on thread 2
                    physicsCollisionManagementSystem.BeforeUpdate(delta);
                    physicsCollisionManagementSystem.Update(delta);
                    physicsCollisionManagementSystem.AfterUpdate(delta);
                    barrier1.SignalAndWait();

                    // Timeslot 7 on thread 2
                    toxinCollisionSystem.BeforeUpdate(delta);
                    toxinCollisionSystem.Update(delta);
                    toxinCollisionSystem.AfterUpdate(delta);
                    damageOnTouchSystem.BeforeUpdate(delta);
                    damageOnTouchSystem.Update(delta);
                    damageOnTouchSystem.AfterUpdate(delta);
                    microbeCollisionSoundSystem.BeforeUpdate(delta);
                    microbeCollisionSoundSystem.Update(delta);
                    microbeCollisionSoundSystem.AfterUpdate(delta);
                    pilusDamageSystem.BeforeUpdate(delta);
                    pilusDamageSystem.Update(delta);
                    pilusDamageSystem.AfterUpdate(delta);
                    physicsUpdateAndPositionSystem.BeforeUpdate(delta);
                    physicsUpdateAndPositionSystem.Update(delta);
                    physicsUpdateAndPositionSystem.AfterUpdate(delta);
                    attachedEntityPositionSystem.BeforeUpdate(delta);
                    attachedEntityPositionSystem.Update(delta);
                    attachedEntityPositionSystem.AfterUpdate(delta);
                    barrier1.SignalAndWait();

                    // Timeslot 8 on thread 2
                    multicellularGrowthSystem.BeforeUpdate(delta);
                    multicellularGrowthSystem.Update(delta);
                    multicellularGrowthSystem.AfterUpdate(delta);
                    osmoregulationAndHealingSystem.BeforeUpdate(delta);
                    osmoregulationAndHealingSystem.Update(delta);
                    osmoregulationAndHealingSystem.AfterUpdate(delta);
                    radiationDamageSystem.BeforeUpdate(delta);
                    radiationDamageSystem.Update(delta);
                    radiationDamageSystem.AfterUpdate(delta);
                    barrier1.SignalAndWait();
                    barrier1.SignalAndWait();

                    // Timeslot 10 on thread 2
                    organelleComponentFetchSystem.BeforeUpdate(delta);
                    organelleComponentFetchSystem.Update(delta);
                    organelleComponentFetchSystem.AfterUpdate(delta);
                    slimeSlowdownSystem.BeforeUpdate(delta);
                    slimeSlowdownSystem.Update(delta);
                    slimeSlowdownSystem.AfterUpdate(delta);
                    barrier1.SignalAndWait();
                    barrier1.SignalAndWait();

                    // Timeslot 12 on thread 2
                    microbeEmissionSystem.BeforeUpdate(delta);
                    microbeEmissionSystem.Update(delta);
                    microbeEmissionSystem.AfterUpdate(delta);
                    siderophoreSystem.BeforeUpdate(delta);
                    siderophoreSystem.Update(delta);
                    siderophoreSystem.AfterUpdate(delta);
                    engulfedDigestionSystem.BeforeUpdate(delta);
                    engulfedDigestionSystem.Update(delta);
                    engulfedDigestionSystem.AfterUpdate(delta);
                    allCompoundsVentingSystem.BeforeUpdate(delta);
                    allCompoundsVentingSystem.Update(delta);
                    allCompoundsVentingSystem.AfterUpdate(delta);
                    barrier1.SignalAndWait();

                    // Timeslot 13 on thread 2
                    SpawnSystem.BeforeUpdate(delta);
                    SpawnSystem.Update(delta);
                    SpawnSystem.AfterUpdate(delta);
                    barrier1.SignalAndWait();

                    // Timeslot 14 on thread 2
                    colonyStatsUpdateSystem.BeforeUpdate(delta);
                    colonyStatsUpdateSystem.Update(delta);
                    colonyStatsUpdateSystem.AfterUpdate(delta);
                    microbeMovementSoundSystem.BeforeUpdate(delta);
                    microbeMovementSoundSystem.Update(delta);
                    microbeMovementSoundSystem.AfterUpdate(delta);
                    barrier1.SignalAndWait();

                    // Timeslot 15 on thread 2
                    physicsBodyControlSystem.BeforeUpdate(delta);
                    physicsBodyControlSystem.Update(delta);
                    physicsBodyControlSystem.AfterUpdate(delta);
                    microbeDeathSystem.BeforeUpdate(delta);
                    microbeDeathSystem.Update(delta);
                    microbeDeathSystem.AfterUpdate(delta);
                    colonyBindingSystem.BeforeUpdate(delta);
                    colonyBindingSystem.Update(delta);
                    colonyBindingSystem.AfterUpdate(delta);
                    delayedColonyOperationSystem.BeforeUpdate(delta);
                    delayedColonyOperationSystem.Update(delta);
                    delayedColonyOperationSystem.AfterUpdate(delta);
                    microbeFlashingSystem.BeforeUpdate(delta);
                    microbeFlashingSystem.Update(delta);
                    microbeFlashingSystem.AfterUpdate(delta);
                    barrier1.SignalAndWait();

                    // Timeslot 16 on thread 2
                    microbeEventCallbackSystem.BeforeUpdate(delta);
                    microbeEventCallbackSystem.Update(delta);
                    microbeEventCallbackSystem.AfterUpdate(delta);
                    damageSoundSystem.BeforeUpdate(delta);
                    damageSoundSystem.Update(delta);
                    damageSoundSystem.AfterUpdate(delta);
                    barrier1.SignalAndWait();
                    barrier1.SignalAndWait();

                    barrier1.SignalAndWait();
                }
                catch (Exception e)
                {
                    GD.PrintErr("Simulation system failure (threaded run for thread 2): " + e);

#if DEBUG
                    if (Debugger.IsAttached)
                        Debugger.Break();
#endif

                    throw;
                }
            });

        TaskExecutor.Instance.AddTask(background1);

        // Catch for extra system run safety (for debugging why higher level catches don't get errors)
        try
        {
            // Timeslot 1 on thread 1
            collisionShapeLoaderSystem.BeforeUpdate(delta);
            collisionShapeLoaderSystem.Update(delta);
            collisionShapeLoaderSystem.AfterUpdate(delta);
            irradiationSystem.BeforeUpdate(delta);
            irradiationSystem.Update(delta);
            irradiationSystem.AfterUpdate(delta);
            damageCooldownSystem.BeforeUpdate(delta);
            damageCooldownSystem.Update(delta);
            damageCooldownSystem.AfterUpdate(delta);
            compoundAbsorptionSystem.BeforeUpdate(delta);
            compoundAbsorptionSystem.Update(delta);
            compoundAbsorptionSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 2 on thread 1
            simpleShapeCreatorSystem.BeforeUpdate(delta);
            simpleShapeCreatorSystem.Update(delta);
            simpleShapeCreatorSystem.AfterUpdate(delta);
            pathBasedSceneLoader.BeforeUpdate(delta);
            pathBasedSceneLoader.Update(delta);
            pathBasedSceneLoader.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 3 on thread 1
            microbeVisualsSystem.BeforeUpdate(delta);
            microbeVisualsSystem.Update(delta);
            microbeVisualsSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 4 on thread 1
            predefinedVisualLoaderSystem.BeforeUpdate(delta);
            predefinedVisualLoaderSystem.Update(delta);
            predefinedVisualLoaderSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 5 on thread 1
            physicsBodyCreationSystem.BeforeUpdate(delta);
            physicsBodyCreationSystem.Update(delta);
            physicsBodyCreationSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 6 on thread 1
            microbeHeatAccumulationSystem.BeforeUpdate(delta);
            microbeHeatAccumulationSystem.Update(delta);
            microbeHeatAccumulationSystem.AfterUpdate(delta);
            mucocystSystem.BeforeUpdate(delta);
            mucocystSystem.Update(delta);
            mucocystSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 7 on thread 1
            ProcessSystem.BeforeUpdate(delta);
            ProcessSystem.Update(delta);
            ProcessSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 8 on thread 1
            physicsBodyDisablingSystem.BeforeUpdate(delta);
            physicsBodyDisablingSystem.Update(delta);
            physicsBodyDisablingSystem.AfterUpdate(delta);
            animationControlSystem.BeforeUpdate(delta);
            animationControlSystem.Update(delta);
            animationControlSystem.AfterUpdate(delta);
            entityMaterialFetchSystem.BeforeUpdate(delta);
            entityMaterialFetchSystem.Update(delta);
            entityMaterialFetchSystem.AfterUpdate(delta);
            microbeTemporaryEffectsSystem.BeforeUpdate(delta);
            microbeTemporaryEffectsSystem.Update(delta);
            microbeTemporaryEffectsSystem.AfterUpdate(delta);
            soundListenerSystem.BeforeUpdate(delta);
            soundListenerSystem.Update(delta);
            soundListenerSystem.AfterUpdate(delta);
            cellBurstEffectSystem.BeforeUpdate(delta);
            cellBurstEffectSystem.Update(delta);
            cellBurstEffectSystem.AfterUpdate(delta);
            intercellularMatrixSystem.BeforeUpdate(delta);
            intercellularMatrixSystem.Update(delta);
            intercellularMatrixSystem.AfterUpdate(delta);
            CameraFollowSystem.BeforeUpdate(delta);
            CameraFollowSystem.Update(delta);
            CameraFollowSystem.AfterUpdate(delta);
            TimedLifeSystem.BeforeUpdate(delta);
            TimedLifeSystem.Update(delta);
            TimedLifeSystem.AfterUpdate(delta);
            disallowPlayerBodySleepSystem.BeforeUpdate(delta);
            disallowPlayerBodySleepSystem.Update(delta);
            disallowPlayerBodySleepSystem.AfterUpdate(delta);
            FluidCurrentsSystem.BeforeUpdate(delta);
            FluidCurrentsSystem.Update(delta);
            FluidCurrentsSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 9 on thread 1
            engulfingSystem.BeforeUpdate(delta);
            engulfingSystem.Update(delta);
            engulfingSystem.AfterUpdate(delta);
            microbeReproductionSystem.BeforeUpdate(delta);
            microbeReproductionSystem.Update(delta);
            microbeReproductionSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 10 on thread 1
            unneededCompoundVentingSystem.BeforeUpdate(delta);
            unneededCompoundVentingSystem.Update(delta);
            unneededCompoundVentingSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 11 on thread 1
            if (RunAI)
            {
                microbeAI.BeforeUpdate(delta);
                microbeAI.ReportPotentialPlayerPosition(reportedPlayerPosition);
                microbeAI.Update(delta);
                microbeAI.AfterUpdate(delta);
            }

            barrier1.SignalAndWait();

            // Timeslot 12 on thread 1
            spatialAttachSystem.BeforeUpdate(delta);
            spatialAttachSystem.Update(delta);
            spatialAttachSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 13 on thread 1
            microbeMovementSystem.BeforeUpdate(delta);
            microbeMovementSystem.Update(delta);
            microbeMovementSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 14 on thread 1
            organelleTickSystem.BeforeUpdate(delta);
            organelleTickSystem.Update(delta);
            organelleTickSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 15 on thread 1
            entityLightSystem.BeforeUpdate(delta);
            entityLightSystem.Update(delta);
            entityLightSystem.AfterUpdate(delta);
            spatialPositionSystem.BeforeUpdate(delta);
            spatialPositionSystem.Update(delta);
            spatialPositionSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 16 on thread 1
            fadeOutActionSystem.BeforeUpdate(delta);
            fadeOutActionSystem.Update(delta);
            fadeOutActionSystem.AfterUpdate(delta);
            physicsSensorSystem.BeforeUpdate(delta);
            physicsSensorSystem.Update(delta);
            physicsSensorSystem.AfterUpdate(delta);
            microbeRenderPrioritySystem.BeforeUpdate(delta);
            microbeRenderPrioritySystem.Update(delta);
            microbeRenderPrioritySystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 17 on thread 1
            soundEffectSystem.BeforeUpdate(delta);
            soundEffectSystem.Update(delta);
            soundEffectSystem.AfterUpdate(delta);
            barrier1.SignalAndWait();

            // Timeslot 18 on thread 1
            engulfedHandlingSystem.BeforeUpdate(delta);
            engulfedHandlingSystem.Update(delta);
            engulfedHandlingSystem.AfterUpdate(delta);

            barrier1.SignalAndWait();
        }
        catch (Exception e)
        {
            GD.PrintErr("Simulation system failure (threaded run for main thread): " + e);

#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif

            throw;
        }

        // Catch for extra system run safety (for debugging why higher level catches don't get errors)
        try
        {

            reportedPlayerPosition = null;
        }
        catch (Exception e)
        {
            GD.PrintErr("Simulation system failure (processing end actions): " + e);

#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif

            throw;
        }
    }

    private void OnProcessFixedWithoutThreads(float delta)
    {
        // This variant doesn't use threading, use when not enough threads are available
        // or threaded run would be slower (or just for debugging)

        // Catch for extra system run safety (for debugging why higher level catches don't get errors)
        try
        {
            collisionShapeLoaderSystem.BeforeUpdate(delta);
            collisionShapeLoaderSystem.Update(delta);
            collisionShapeLoaderSystem.AfterUpdate(delta);
            endosymbiontOrganelleSystem.BeforeUpdate(delta);
            endosymbiontOrganelleSystem.Update(delta);
            endosymbiontOrganelleSystem.AfterUpdate(delta);
            microbeVisualsSystem.BeforeUpdate(delta);
            microbeVisualsSystem.Update(delta);
            microbeVisualsSystem.AfterUpdate(delta);
            simpleShapeCreatorSystem.BeforeUpdate(delta);
            simpleShapeCreatorSystem.Update(delta);
            simpleShapeCreatorSystem.AfterUpdate(delta);
            microbePhysicsCreationAndSizeSystem.BeforeUpdate(delta);
            microbePhysicsCreationAndSizeSystem.Update(delta);
            microbePhysicsCreationAndSizeSystem.AfterUpdate(delta);
            physicsBodyCreationSystem.BeforeUpdate(delta);
            physicsBodyCreationSystem.Update(delta);
            physicsBodyCreationSystem.AfterUpdate(delta);
            physicsUpdateAndPositionSystem.BeforeUpdate(delta);
            physicsUpdateAndPositionSystem.Update(delta);
            physicsUpdateAndPositionSystem.AfterUpdate(delta);
            attachedEntityPositionSystem.BeforeUpdate(delta);
            attachedEntityPositionSystem.Update(delta);
            attachedEntityPositionSystem.AfterUpdate(delta);
            physicsBodyDisablingSystem.BeforeUpdate(delta);
            physicsBodyDisablingSystem.Update(delta);
            physicsBodyDisablingSystem.AfterUpdate(delta);
            microbeHeatAccumulationSystem.BeforeUpdate(delta);
            microbeHeatAccumulationSystem.Update(delta);
            microbeHeatAccumulationSystem.AfterUpdate(delta);
            colonyCompoundDistributionSystem.BeforeUpdate(delta);
            colonyCompoundDistributionSystem.Update(delta);
            colonyCompoundDistributionSystem.AfterUpdate(delta);
            mucocystSystem.BeforeUpdate(delta);
            mucocystSystem.Update(delta);
            mucocystSystem.AfterUpdate(delta);
            allCompoundsVentingSystem.BeforeUpdate(delta);
            allCompoundsVentingSystem.Update(delta);
            allCompoundsVentingSystem.AfterUpdate(delta);
            irradiationSystem.BeforeUpdate(delta);
            irradiationSystem.Update(delta);
            irradiationSystem.AfterUpdate(delta);
            compoundAbsorptionSystem.BeforeUpdate(delta);
            compoundAbsorptionSystem.Update(delta);
            compoundAbsorptionSystem.AfterUpdate(delta);
            damageCooldownSystem.BeforeUpdate(delta);
            damageCooldownSystem.Update(delta);
            damageCooldownSystem.AfterUpdate(delta);
            microbeDivisionClippingSystem.BeforeUpdate(delta);
            microbeDivisionClippingSystem.Update(delta);
            microbeDivisionClippingSystem.AfterUpdate(delta);
            physicsCollisionManagementSystem.BeforeUpdate(delta);
            physicsCollisionManagementSystem.Update(delta);
            physicsCollisionManagementSystem.AfterUpdate(delta);
            toxinCollisionSystem.BeforeUpdate(delta);
            toxinCollisionSystem.Update(delta);
            toxinCollisionSystem.AfterUpdate(delta);
            siderophoreSystem.BeforeUpdate(delta);
            siderophoreSystem.Update(delta);
            siderophoreSystem.AfterUpdate(delta);
            damageOnTouchSystem.BeforeUpdate(delta);
            damageOnTouchSystem.Update(delta);
            damageOnTouchSystem.AfterUpdate(delta);
            pilusDamageSystem.BeforeUpdate(delta);
            pilusDamageSystem.Update(delta);
            pilusDamageSystem.AfterUpdate(delta);
            engulfingSystem.BeforeUpdate(delta);
            engulfingSystem.Update(delta);
            engulfingSystem.AfterUpdate(delta);
            ProcessSystem.BeforeUpdate(delta);
            ProcessSystem.Update(delta);
            ProcessSystem.AfterUpdate(delta);
            multicellularGrowthSystem.BeforeUpdate(delta);
            multicellularGrowthSystem.Update(delta);
            multicellularGrowthSystem.AfterUpdate(delta);
            engulfedDigestionSystem.BeforeUpdate(delta);
            engulfedDigestionSystem.Update(delta);
            engulfedDigestionSystem.AfterUpdate(delta);
            engulfedHandlingSystem.BeforeUpdate(delta);
            engulfedHandlingSystem.Update(delta);
            engulfedHandlingSystem.AfterUpdate(delta);
            osmoregulationAndHealingSystem.BeforeUpdate(delta);
            osmoregulationAndHealingSystem.Update(delta);
            osmoregulationAndHealingSystem.AfterUpdate(delta);
            microbeReproductionSystem.BeforeUpdate(delta);
            microbeReproductionSystem.Update(delta);
            microbeReproductionSystem.AfterUpdate(delta);
            unneededCompoundVentingSystem.BeforeUpdate(delta);
            unneededCompoundVentingSystem.Update(delta);
            unneededCompoundVentingSystem.AfterUpdate(delta);
            organelleComponentFetchSystem.BeforeUpdate(delta);
            organelleComponentFetchSystem.Update(delta);
            organelleComponentFetchSystem.AfterUpdate(delta);
            entitySignalingSystem.BeforeUpdate(delta);
            entitySignalingSystem.Update(delta);
            entitySignalingSystem.AfterUpdate(delta);
            strainSystem.BeforeUpdate(delta);
            strainSystem.Update(delta);
            strainSystem.AfterUpdate(delta);
            radiationDamageSystem.BeforeUpdate(delta);
            radiationDamageSystem.Update(delta);
            radiationDamageSystem.AfterUpdate(delta);
            if (RunAI)
            {
                microbeAI.BeforeUpdate(delta);
                microbeAI.ReportPotentialPlayerPosition(reportedPlayerPosition);
                microbeAI.Update(delta);
                microbeAI.AfterUpdate(delta);
            }

            microbeEmissionSystem.BeforeUpdate(delta);
            microbeEmissionSystem.Update(delta);
            microbeEmissionSystem.AfterUpdate(delta);
            microbeTemporaryEffectsSystem.BeforeUpdate(delta);
            microbeTemporaryEffectsSystem.Update(delta);
            microbeTemporaryEffectsSystem.AfterUpdate(delta);
            slimeSlowdownSystem.BeforeUpdate(delta);
            slimeSlowdownSystem.Update(delta);
            slimeSlowdownSystem.AfterUpdate(delta);
            microbeMovementSystem.BeforeUpdate(delta);
            microbeMovementSystem.Update(delta);
            microbeMovementSystem.AfterUpdate(delta);
            physicsBodyControlSystem.BeforeUpdate(delta);
            physicsBodyControlSystem.Update(delta);
            physicsBodyControlSystem.AfterUpdate(delta);
            colonyBindingSystem.BeforeUpdate(delta);
            colonyBindingSystem.Update(delta);
            colonyBindingSystem.AfterUpdate(delta);
            delayedColonyOperationSystem.BeforeUpdate(delta);
            delayedColonyOperationSystem.Update(delta);
            delayedColonyOperationSystem.AfterUpdate(delta);
            organelleTickSystem.BeforeUpdate(delta);
            organelleTickSystem.Update(delta);
            organelleTickSystem.AfterUpdate(delta);
            entityLightSystem.BeforeUpdate(delta);
            entityLightSystem.Update(delta);
            entityLightSystem.AfterUpdate(delta);
            pathBasedSceneLoader.BeforeUpdate(delta);
            pathBasedSceneLoader.Update(delta);
            pathBasedSceneLoader.AfterUpdate(delta);
            predefinedVisualLoaderSystem.BeforeUpdate(delta);
            predefinedVisualLoaderSystem.Update(delta);
            predefinedVisualLoaderSystem.AfterUpdate(delta);
            animationControlSystem.BeforeUpdate(delta);
            animationControlSystem.Update(delta);
            animationControlSystem.AfterUpdate(delta);
            entityMaterialFetchSystem.BeforeUpdate(delta);
            entityMaterialFetchSystem.Update(delta);
            entityMaterialFetchSystem.AfterUpdate(delta);
            spatialAttachSystem.BeforeUpdate(delta);
            spatialAttachSystem.Update(delta);
            spatialAttachSystem.AfterUpdate(delta);
            spatialPositionSystem.BeforeUpdate(delta);
            spatialPositionSystem.Update(delta);
            spatialPositionSystem.AfterUpdate(delta);
            countLimitedDespawnSystem.BeforeUpdate(delta);
            countLimitedDespawnSystem.Update(delta);
            countLimitedDespawnSystem.AfterUpdate(delta);
            MicrobeTerrainSystem.BeforeUpdate(delta);
            MicrobeTerrainSystem.Update(delta);
            MicrobeTerrainSystem.AfterUpdate(delta);
            SpawnSystem.BeforeUpdate(delta);
            SpawnSystem.Update(delta);
            SpawnSystem.AfterUpdate(delta);
            colonyStatsUpdateSystem.BeforeUpdate(delta);
            colonyStatsUpdateSystem.Update(delta);
            colonyStatsUpdateSystem.AfterUpdate(delta);
            microbeDeathSystem.BeforeUpdate(delta);
            microbeDeathSystem.Update(delta);
            microbeDeathSystem.AfterUpdate(delta);
            fadeOutActionSystem.BeforeUpdate(delta);
            fadeOutActionSystem.Update(delta);
            fadeOutActionSystem.AfterUpdate(delta);
            physicsSensorSystem.BeforeUpdate(delta);
            physicsSensorSystem.Update(delta);
            physicsSensorSystem.AfterUpdate(delta);
            microbeMovementSoundSystem.BeforeUpdate(delta);
            microbeMovementSoundSystem.Update(delta);
            microbeMovementSoundSystem.AfterUpdate(delta);
            microbeEventCallbackSystem.BeforeUpdate(delta);
            microbeEventCallbackSystem.Update(delta);
            microbeEventCallbackSystem.AfterUpdate(delta);
            microbeFlashingSystem.BeforeUpdate(delta);
            microbeFlashingSystem.Update(delta);
            microbeFlashingSystem.AfterUpdate(delta);
            damageSoundSystem.BeforeUpdate(delta);
            damageSoundSystem.Update(delta);
            damageSoundSystem.AfterUpdate(delta);
            microbeCollisionSoundSystem.BeforeUpdate(delta);
            microbeCollisionSoundSystem.Update(delta);
            microbeCollisionSoundSystem.AfterUpdate(delta);
            soundEffectSystem.BeforeUpdate(delta);
            soundEffectSystem.Update(delta);
            soundEffectSystem.AfterUpdate(delta);
            soundListenerSystem.BeforeUpdate(delta);
            soundListenerSystem.Update(delta);
            soundListenerSystem.AfterUpdate(delta);
            cellBurstEffectSystem.BeforeUpdate(delta);
            cellBurstEffectSystem.Update(delta);
            cellBurstEffectSystem.AfterUpdate(delta);
            microbeRenderPrioritySystem.BeforeUpdate(delta);
            microbeRenderPrioritySystem.Update(delta);
            microbeRenderPrioritySystem.AfterUpdate(delta);
            intercellularMatrixSystem.BeforeUpdate(delta);
            intercellularMatrixSystem.Update(delta);
            intercellularMatrixSystem.AfterUpdate(delta);
            CameraFollowSystem.BeforeUpdate(delta);
            CameraFollowSystem.Update(delta);
            CameraFollowSystem.AfterUpdate(delta);
            TimedLifeSystem.BeforeUpdate(delta);
            TimedLifeSystem.Update(delta);
            TimedLifeSystem.AfterUpdate(delta);
            FluidCurrentsSystem.BeforeUpdate(delta);
            FluidCurrentsSystem.Update(delta);
            FluidCurrentsSystem.AfterUpdate(delta);
            disallowPlayerBodySleepSystem.BeforeUpdate(delta);
            disallowPlayerBodySleepSystem.Update(delta);
            disallowPlayerBodySleepSystem.AfterUpdate(delta);
        }
        catch (Exception e)
        {
            GD.PrintErr("Simulation system failure (processing without threads): " + e);

#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif

            throw;
        }

        // Catch for extra system run safety (for debugging why higher level catches don't get errors)
        try
        {

            reportedPlayerPosition = null;
        }
        catch (Exception e)
        {
            GD.PrintErr("Simulation system failure (processing end actions): " + e);

#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif

            throw;
        }
    }

    private void OnProcessFrameLogicGenerated(float delta)
    {
        // NOTE: not currently ran in parallel due to low frame system count
        // Catch for extra system run safety (for debugging why higher level catches don't get errors)
        try
        {
            colourAnimationSystem.BeforeUpdate(delta);
            colourAnimationSystem.Update(delta);
            colourAnimationSystem.AfterUpdate(delta);
            microbeShaderSystem.BeforeUpdate(delta);
            microbeShaderSystem.Update(delta);
            microbeShaderSystem.AfterUpdate(delta);
            tintColourApplyingSystem.BeforeUpdate(delta);
            tintColourApplyingSystem.Update(delta);
            tintColourApplyingSystem.AfterUpdate(delta);
        }
        catch (Exception e)
        {
            GD.PrintErr("Simulation system failure (simple running group method): " + e);

#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif

            throw;
        }
    }

    private void RunSystemInits()
    {
        // Simple init of each system in sequence (most systems don't use this)
        pathBasedSceneLoader.Initialize();
        predefinedVisualLoaderSystem.Initialize();
        animationControlSystem.Initialize();
        collisionShapeLoaderSystem.Initialize();
        simpleShapeCreatorSystem.Initialize();
        endosymbiontOrganelleSystem.Initialize();
        microbeVisualsSystem.Initialize();
        microbePhysicsCreationAndSizeSystem.Initialize();
        physicsBodyCreationSystem.Initialize();
        physicsUpdateAndPositionSystem.Initialize();
        attachedEntityPositionSystem.Initialize();
        countLimitedDespawnSystem.Initialize();
        damageCooldownSystem.Initialize();
        microbeHeatAccumulationSystem.Initialize();
        microbeDivisionClippingSystem.Initialize();
        physicsCollisionManagementSystem.Initialize();
        siderophoreSystem.Initialize();
        damageOnTouchSystem.Initialize();
        colonyCompoundDistributionSystem.Initialize();
        toxinCollisionSystem.Initialize();
        pilusDamageSystem.Initialize();
        engulfingSystem.Initialize();
        spatialAttachSystem.Initialize();
        spatialPositionSystem.Initialize();
        mucocystSystem.Initialize();
        allCompoundsVentingSystem.Initialize();
        engulfedDigestionSystem.Initialize();
        engulfedHandlingSystem.Initialize();
        irradiationSystem.Initialize();
        compoundAbsorptionSystem.Initialize();
        ProcessSystem.Initialize();
        multicellularGrowthSystem.Initialize();
        MicrobeTerrainSystem.Initialize();
        SpawnSystem.Initialize();
        colonyStatsUpdateSystem.Initialize();
        entitySignalingSystem.Initialize();
        osmoregulationAndHealingSystem.Initialize();
        microbeReproductionSystem.Initialize();
        organelleComponentFetchSystem.Initialize();
        strainSystem.Initialize();
        radiationDamageSystem.Initialize();
        unneededCompoundVentingSystem.Initialize();
        microbeAI.Initialize();
        microbeEmissionSystem.Initialize();
        microbeDeathSystem.Initialize();
        fadeOutActionSystem.Initialize();
        physicsBodyDisablingSystem.Initialize();
        microbeTemporaryEffectsSystem.Initialize();
        slimeSlowdownSystem.Initialize();
        microbeMovementSystem.Initialize();
        physicsBodyControlSystem.Initialize();
        colonyBindingSystem.Initialize();
        delayedColonyOperationSystem.Initialize();
        disallowPlayerBodySleepSystem.Initialize();
        organelleTickSystem.Initialize();
        entityLightSystem.Initialize();
        entityMaterialFetchSystem.Initialize();
        physicsSensorSystem.Initialize();
        microbeMovementSoundSystem.Initialize();
        microbeEventCallbackSystem.Initialize();
        microbeFlashingSystem.Initialize();
        damageSoundSystem.Initialize();
        microbeCollisionSoundSystem.Initialize();
        soundEffectSystem.Initialize();
        soundListenerSystem.Initialize();
        cellBurstEffectSystem.Initialize();
        microbeRenderPrioritySystem.Initialize();
        intercellularMatrixSystem.Initialize();
        CameraFollowSystem.Initialize();
        TimedLifeSystem.Initialize();
        FluidCurrentsSystem.Initialize();
        colourAnimationSystem.Initialize();
        microbeShaderSystem.Initialize();
        tintColourApplyingSystem.Initialize();
    }

    private void DisposeGenerated()
    {
    }
}
