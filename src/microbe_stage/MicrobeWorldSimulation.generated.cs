﻿// Automatically generated file. DO NOT EDIT!
// Run GenerateThreadedSystems to generate this file
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Godot;

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
                // Catch for extra system run safety (for debugging why higher level catches don't get errors)
                try
                {
                    // Timeslot 1 on thread 2
                    simpleShapeCreatorSystem.Update(delta);
                    countLimitedDespawnSystem.Update(delta);
                    damageCooldownSystem.Update(delta);
                    irradiationSystem.Update(delta);
                    entitySignalingSystem.Update(delta);
                    barrier1.SignalAndWait();

                    // Timeslot 2 on thread 2
                    microbePhysicsCreationAndSizeSystem.Update(delta);
                    colonyCompoundDistributionSystem.Update(delta);
                    compoundAbsorptionSystem.Update(delta);
                    barrier1.SignalAndWait();
                    barrier1.SignalAndWait();

                    // Timeslot 4 on thread 2
                    ProcessSystem.Update(delta);
                    barrier1.SignalAndWait();

                    // Timeslot 5 on thread 2
                    microbeTemporaryEffectsSystem.Update(delta);
                    barrier1.SignalAndWait();

                    // Timeslot 6 on thread 2
                    multicellularGrowthSystem.Update(delta);
                    osmoregulationAndHealingSystem.Update(delta);
                    radiationDamageSystem.Update(delta);
                    barrier1.SignalAndWait();
                    barrier1.SignalAndWait();

                    // Timeslot 8 on thread 2
                    unneededCompoundVentingSystem.Update(delta);
                    barrier1.SignalAndWait();
                    barrier1.SignalAndWait();

                    // Timeslot 10 on thread 2
                    engulfedDigestionSystem.Update(delta);
                    if (RunAI)
                    {
                        microbeAI.ReportPotentialPlayerPosition(reportedPlayerPosition);
                        microbeAI.Update(delta);
                    }

                    microbeEmissionSystem.Update(delta);
                    microbeMovementSystem.Update(delta);
                    physicsBodyControlSystem.Update(delta);
                    colonyBindingSystem.Update(delta);
                    delayedColonyOperationSystem.Update(delta);
                    microbeMovementSoundSystem.Update(delta);
                    microbeFlashingSystem.Update(delta);
                    barrier1.SignalAndWait();

                    // Timeslot 11 on thread 2
                    SpawnSystem.Update(delta);
                    colonyStatsUpdateSystem.Update(delta);
                    barrier1.SignalAndWait();

                    // Timeslot 12 on thread 2
                    microbeDeathSystem.Update(delta);
                    microbeEventCallbackSystem.Update(delta);
                    barrier1.SignalAndWait();

                    // Timeslot 13 on thread 2
                    damageSoundSystem.Update(delta);
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
            mucocystSystem.Update(delta);
            animationControlSystem.Update(delta);
            endosymbiontOrganelleSystem.Update(delta);
            microbeVisualsSystem.Update(delta);
            barrier1.SignalAndWait();

            // Timeslot 2 on thread 1
            predefinedVisualLoaderSystem.Update(delta);
            strainSystem.Update(delta);
            barrier1.SignalAndWait();

            // Timeslot 3 on thread 1
            microbeHeatAccumulationSystem.Update(delta);
            barrier1.SignalAndWait();

            // Timeslot 4 on thread 1
            physicsBodyCreationSystem.Update(delta);
            physicsBodyDisablingSystem.Update(delta);
            physicsUpdateAndPositionSystem.Update(delta);
            attachedEntityPositionSystem.Update(delta);
            physicsCollisionManagementSystem.Update(delta);
            toxinCollisionSystem.Update(delta);
            damageOnTouchSystem.Update(delta);
            pathBasedSceneLoader.Update(delta);
            siderophoreSystem.Update(delta);
            entityMaterialFetchSystem.Update(delta);
            pilusDamageSystem.Update(delta);
            microbeRenderPrioritySystem.Update(delta);
            CameraFollowSystem.Update(delta);
            soundListenerSystem.Update(delta);
            disallowPlayerBodySleepSystem.Update(delta);
            collisionShapeLoaderSystem.Update(delta);
            microbeCollisionSoundSystem.Update(delta);
            cellBurstEffectSystem.Update(delta);
            barrier1.SignalAndWait();

            // Timeslot 5 on thread 1
            allCompoundsVentingSystem.Update(delta);
            barrier1.SignalAndWait();

            // Timeslot 6 on thread 1
            TimedLifeSystem.Update(delta);
            fluidCurrentsSystem.Update(delta);
            barrier1.SignalAndWait();

            // Timeslot 7 on thread 1
            microbeReproductionSystem.Update(delta);
            barrier1.SignalAndWait();

            // Timeslot 8 on thread 1
            organelleComponentFetchSystem.Update(delta);
            slimeSlowdownSystem.Update(delta);
            barrier1.SignalAndWait();

            // Timeslot 9 on thread 1
            engulfingSystem.Update(delta);
            barrier1.SignalAndWait();

            // Timeslot 10 on thread 1
            spatialAttachSystem.Update(delta);
            spatialPositionSystem.Update(delta);
            barrier1.SignalAndWait();

            // Timeslot 11 on thread 1
            engulfedHandlingSystem.Update(delta);
            organelleTickSystem.Update(delta);
            barrier1.SignalAndWait();

            // Timeslot 12 on thread 1
            physicsSensorSystem.Update(delta);
            barrier1.SignalAndWait();

            // Timeslot 13 on thread 1
            fadeOutActionSystem.Update(delta);
            barrier1.SignalAndWait();

            // Timeslot 14 on thread 1
            soundEffectSystem.Update(delta);

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
            cellCountingEntitySet.Complete();
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
            pathBasedSceneLoader.Update(delta);
            predefinedVisualLoaderSystem.Update(delta);
            endosymbiontOrganelleSystem.Update(delta);
            microbeVisualsSystem.Update(delta);
            animationControlSystem.Update(delta);
            entityMaterialFetchSystem.Update(delta);
            collisionShapeLoaderSystem.Update(delta);
            simpleShapeCreatorSystem.Update(delta);
            microbePhysicsCreationAndSizeSystem.Update(delta);
            physicsBodyCreationSystem.Update(delta);
            damageCooldownSystem.Update(delta);
            physicsCollisionManagementSystem.Update(delta);
            physicsUpdateAndPositionSystem.Update(delta);
            colonyCompoundDistributionSystem.Update(delta);
            attachedEntityPositionSystem.Update(delta);
            microbeHeatAccumulationSystem.Update(delta);
            toxinCollisionSystem.Update(delta);
            siderophoreSystem.Update(delta);
            microbeTemporaryEffectsSystem.Update(delta);
            damageOnTouchSystem.Update(delta);
            pilusDamageSystem.Update(delta);
            engulfingSystem.Update(delta);
            spatialAttachSystem.Update(delta);
            spatialPositionSystem.Update(delta);
            mucocystSystem.Update(delta);
            irradiationSystem.Update(delta);
            compoundAbsorptionSystem.Update(delta);
            ProcessSystem.Update(delta);
            multicellularGrowthSystem.Update(delta);
            engulfedDigestionSystem.Update(delta);
            engulfedHandlingSystem.Update(delta);
            osmoregulationAndHealingSystem.Update(delta);
            microbeReproductionSystem.Update(delta);
            countLimitedDespawnSystem.Update(delta);
            SpawnSystem.Update(delta);
            colonyStatsUpdateSystem.Update(delta);
            entitySignalingSystem.Update(delta);
            organelleComponentFetchSystem.Update(delta);
            strainSystem.Update(delta);
            radiationDamageSystem.Update(delta);
            unneededCompoundVentingSystem.Update(delta);
            allCompoundsVentingSystem.Update(delta);
            if (RunAI)
            {
                microbeAI.ReportPotentialPlayerPosition(reportedPlayerPosition);
                microbeAI.Update(delta);
            }

            microbeEmissionSystem.Update(delta);
            microbeDeathSystem.Update(delta);
            fadeOutActionSystem.Update(delta);
            physicsBodyDisablingSystem.Update(delta);
            slimeSlowdownSystem.Update(delta);
            microbeMovementSystem.Update(delta);
            physicsBodyControlSystem.Update(delta);
            colonyBindingSystem.Update(delta);
            delayedColonyOperationSystem.Update(delta);
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
            fluidCurrentsSystem.Update(delta);
            microbeRenderPrioritySystem.Update(delta);
            CameraFollowSystem.Update(delta);
            TimedLifeSystem.Update(delta);
            disallowPlayerBodySleepSystem.Update(delta);
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
            cellCountingEntitySet.Complete();
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
            colourAnimationSystem.Update(delta);
            microbeShaderSystem.Update(delta);
            tintColourApplyingSystem.Update(delta);
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

    private void DisposeGenerated()
    {
    }
}
