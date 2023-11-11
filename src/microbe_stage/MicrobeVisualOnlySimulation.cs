using System;
using Components;
using DefaultEcs;
using DefaultEcs.Threading;
using Godot;
using Systems;

/// <summary>
///   Handles displaying just microbe visuals (as alternative to the full <see cref="MicrobeWorldSimulation"/>)
/// </summary>
public sealed class MicrobeVisualOnlySimulation : WorldSimulation
{
    // Base systems
    private AnimationControlSystem animationControlSystem = null!;
    private AttachedEntityPositionSystem attachedEntityPositionSystem = null!;
    private ColourAnimationSystem colourAnimationSystem = null!;
    private EntityMaterialFetchSystem entityMaterialFetchSystem = null!;
    private FadeOutActionSystem fadeOutActionSystem = null!;
    private PathBasedSceneLoader pathBasedSceneLoader = null!;
    private PredefinedVisualLoaderSystem predefinedVisualLoaderSystem = null!;

    // private RenderOrderSystem renderOrderSystem = null! = null!;

    private SpatialAttachSystem spatialAttachSystem = null!;
    private SpatialPositionSystem spatialPositionSystem = null!;

    // Microbe systems
    private CellBurstEffectSystem cellBurstEffectSystem = null!;

    // private ColonyBindingSystem colonyBindingSystem = null!;
    private MicrobeFlashingSystem microbeFlashingSystem = null!;
    private MicrobeShaderSystem microbeShaderSystem = null!;
    private MicrobeVisualsSystem microbeVisualsSystem = null!;
    private TintColourAnimationSystem tintColourAnimationSystem = null!;

#pragma warning disable CA2213
    private Node visualsParent = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Initialized this visual simulation for use
    /// </summary>
    /// <param name="visualDisplayRoot">Root node to place all visuals under</param>
    public void Init(Node visualDisplayRoot)
    {
        visualsParent = visualDisplayRoot;

        // This is not used for intensive use, and even is used in the background of normal gameplay so this should use
        // just a single thread
        var runner = new DefaultParallelRunner(1);

        animationControlSystem = new AnimationControlSystem(EntitySystem);
        attachedEntityPositionSystem = new AttachedEntityPositionSystem(EntitySystem, runner);
        colourAnimationSystem = new ColourAnimationSystem(EntitySystem, runner);

        entityMaterialFetchSystem = new EntityMaterialFetchSystem(EntitySystem);
        fadeOutActionSystem = new FadeOutActionSystem(this, EntitySystem, runner);
        pathBasedSceneLoader = new PathBasedSceneLoader(EntitySystem, runner);

        predefinedVisualLoaderSystem = new PredefinedVisualLoaderSystem(EntitySystem);

        spatialAttachSystem = new SpatialAttachSystem(visualsParent, EntitySystem);
        spatialPositionSystem = new SpatialPositionSystem(EntitySystem);
        cellBurstEffectSystem = new CellBurstEffectSystem(EntitySystem);

        // For previewing early multicellular some colony operations will be needed
        // colonyBindingSystem = new ColonyBindingSystem(this, EntitySystem, parallelRunner);

        microbeFlashingSystem = new MicrobeFlashingSystem(EntitySystem, runner);
        microbeShaderSystem = new MicrobeShaderSystem(EntitySystem);

        microbeVisualsSystem = new MicrobeVisualsSystem(EntitySystem);

        // organelleComponentFetchSystem = new OrganelleComponentFetchSystem(EntitySystem, runner);

        // TODO: is there a need for the movement system / OrganelleTickSystem to control animations on organelles
        // if those are used then also OrganelleComponentFetchSystem would be needed
        // organelleTickSystem = new OrganelleTickSystem(EntitySystem, runner);

        tintColourAnimationSystem = new TintColourAnimationSystem(EntitySystem);

        OnInitialized();
    }

    public override void ProcessFrameLogic(float delta)
    {
        ThrowIfNotInitialized();

        colourAnimationSystem.Update(delta);
        microbeShaderSystem.Update(delta);
        tintColourAnimationSystem.Update(delta);
    }

    /// <summary>
    ///   Creates a simple visualization microbe in this world at origin that can then be manipulated with the microbe
    ///   visualization methods below
    /// </summary>
    /// <returns>The created entity</returns>
    public Entity CreateVisualisationMicrobe(Species species)
    {
        // TODO: should we have a separate spawn method to just spawn the visual aspects of a microbe?
        // The downside would be duplicated code, but it could skip the component types that don't impact the visuals

        // We pass AI controlled true here to avoid creating player specific data but as we don't have the AI system
        // it is fine to create the AI properties as it won't actually do anything
        SpawnHelpers.SpawnMicrobe(this, species, Vector3.Zero, true);

        ProcessDelaySpawnedEntitiesImmediately();

        // Grab the created entity
        Entity foundEntity = default;

        foreach (var entity in EntitySystem)
        {
            if (!entity.Has<CellProperties>())
                continue;

            // In case there are already multiple microbes, grab the last one
            foundEntity = entity;
        }

        if (foundEntity == default)
            throw new Exception("Could not find microbe entity that should have been created");

        return foundEntity;
    }

    public void ApplyNewVisualisationMicrobeSpecies(Entity microbe, MicrobeSpecies species)
    {
        if (!microbe.Has<CellProperties>())
        {
            GD.PrintErr("Can't apply new species to visualization entity as it is missing a component");
            return;
        }

        // Do a full update apply with the general code method
        ref var cellProperties = ref microbe.Get<CellProperties>();
        cellProperties.ReApplyCellTypeProperties(microbe, species, species);

        // TODO: update species member component if species changed?
    }

    /// <summary>
    ///   Applies just a colour value as the species colour to a microbe
    /// </summary>
    /// <param name="microbe">Microbe entity</param>
    /// <param name="colour">Colour to apply to it (overrides any previously applied species colour)</param>
    public void ApplyMicrobeColour(Entity microbe, Color colour)
    {
        if (!microbe.Has<CellProperties>())
        {
            GD.PrintErr("Can't apply new rigidity to visualization entity as it is missing a component");
            return;
        }

        ref var cellProperties = ref microbe.Get<CellProperties>();

        // Reset the initial used colour
        cellProperties.Colour = colour;

        // Reset the colour used when updating (should be fine to cancel the animation here)
        ref var colourComponent = ref microbe.Get<ColourAnimation>();
        colourComponent.DefaultColour = Membrane.MembraneTintFromSpeciesColour(colour);
        colourComponent.ResetColour();

        // We have to update all organelle visuals to get them to apply the new colour
        ref var organelleContainer = ref microbe.Get<OrganelleContainer>();
        organelleContainer.OrganelleVisualsCreated = false;
    }

    public void ApplyMicrobeRigidity(Entity microbe, float membraneRigidity)
    {
        if (!microbe.Has<CellProperties>())
        {
            GD.PrintErr("Can't apply new rigidity to visualization entity as it is missing a component");
            return;
        }

        ref var cellProperties = ref microbe.Get<CellProperties>();
        cellProperties.MembraneRigidity = membraneRigidity;

        ref var organelleContainer = ref microbe.Get<OrganelleContainer>();

        // Needed to re-apply membrane data
        organelleContainer.OrganelleVisualsCreated = false;
    }

    public void ApplyMicrobeMembraneType(Entity microbe, MembraneType membraneType)
    {
        if (!microbe.Has<CellProperties>())
        {
            GD.PrintErr("Can't apply new membrane type to visualization entity as it is missing a component");
            return;
        }

        ref var cellProperties = ref microbe.Get<CellProperties>();
        cellProperties.MembraneType = membraneType;

        ref var organelleContainer = ref microbe.Get<OrganelleContainer>();

        organelleContainer.OrganelleVisualsCreated = false;
    }

    // This world doesn't use physics
    protected override void WaitForStartedPhysicsRun()
    {
    }

    protected override void OnStartPhysicsRunIfTime(float delta)
    {
    }

    protected override bool RunPhysicsIfBehind()
    {
        return false;
    }

    protected override void OnProcessFixedLogic(float delta)
    {
        microbeVisualsSystem.Update(delta);
        pathBasedSceneLoader.Update(delta);
        predefinedVisualLoaderSystem.Update(delta);
        entityMaterialFetchSystem.Update(delta);
        animationControlSystem.Update(delta);

        attachedEntityPositionSystem.Update(delta);

        // colonyBindingSystem.Update(delta);

        spatialAttachSystem.Update(delta);
        spatialPositionSystem.Update(delta);

        // organelleComponentFetchSystem.Update(delta);
        // organelleTickSystem.Update(delta);

        fadeOutActionSystem.Update(delta);

        // renderOrderSystem.Update(delta);

        cellBurstEffectSystem.Update(delta);

        microbeFlashingSystem.Update(delta);
    }

    protected override void ApplyECSThreadCount(int ecsThreadsToUse)
    {
        // This system doesn't use threading
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            animationControlSystem.Dispose();
            attachedEntityPositionSystem.Dispose();
            colourAnimationSystem.Dispose();
            entityMaterialFetchSystem.Dispose();
            fadeOutActionSystem.Dispose();
            pathBasedSceneLoader.Dispose();
            predefinedVisualLoaderSystem.Dispose();
            spatialAttachSystem.Dispose();
            spatialPositionSystem.Dispose();
            cellBurstEffectSystem.Dispose();
            microbeFlashingSystem.Dispose();
            microbeShaderSystem.Dispose();
            microbeVisualsSystem.Dispose();
            tintColourAnimationSystem.Dispose();
        }

        base.Dispose(disposing);
    }
}
