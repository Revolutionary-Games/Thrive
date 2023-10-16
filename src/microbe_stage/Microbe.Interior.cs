using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main script on each cell in the game.
///   Partial class: Compounds, ATP, Reproduction
///   Divide, Organelles, Toxin, Digestion
/// </summary>
public partial class Microbe
{
    [JsonProperty]
    private readonly CompoundBag compounds = new(0.0f);

    [JsonProperty]
    private readonly Dictionary<Compound, float> requiredCompoundsForBaseReproduction = new();

    private Compound atp = null!;
    private Compound glucose = null!;
    private Compound mucilage = null!;

    private Enzyme lipase = null!;

    [JsonProperty]
    private CompoundCloudSystem? cloudSystem;

    [JsonProperty]
    private ISpawnSystem? spawnSystem;

    [JsonProperty]
    private Compound? queuedToxinToEmit;

    /// <summary>
    ///   The organelles in this microbe
    /// </summary>
    [JsonProperty]
    private OrganelleLayout<PlacedOrganelle>? organelles;

    private bool enzymesDirty = true;
    private Dictionary<Enzyme, int> enzymes = new();

    [JsonProperty]
    private float lastCheckedATPDamage;

    [JsonProperty]
    private float lastCheckedOxytoxyDigestionDamage;

    [JsonProperty]
    private float dissolveEffectValue;

    [JsonProperty]
    private float playerEngulfedDeathTimer;

    [JsonProperty]
    private float slimeSecretionCooldown;

    [JsonProperty]
    private float queuedSlimeSecretionTime;

    private float lastCheckedReproduction;

    /// <summary>
    ///   Flips every reproduction update. Used to make compound use for reproduction distribute more evenly between
    ///   the compound types.
    /// </summary>
    private bool consumeReproductionCompoundsReverse;

    /// <summary>
    ///   The microbe stores here the sum of capacity of all the
    ///   current organelles. This is here to prevent anyone from
    ///   messing with this value if we used the the
    ///   CompoundBag for the calculations that use this.
    /// </summary>
    private float organellesCapacity;

    /// <summary>
    ///   Stores additional capacity for compounds outside of organellesCapacity. Currently, this only stores
    ///   additional capacity granted from specialized vacuoles
    /// </summary>
    private Dictionary<Compound, float> additionalCompoundCapacities = new();

    /// <summary>
    ///   True once all organelles are divided to not continuously run code that is triggered
    ///   when a cell is ready to reproduce.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is not saved so that the player cell can enable the editor when loading a save
    ///     where the player is ready to reproduce. If more code is added to be ran just once based
    ///     on this flag, it needs to be made sure that that code re-running after loading a save is
    ///     not a problem.
    ///   </para>
    /// </remarks>
    private bool allOrganellesDivided;

    private float timeUntilChemoreceptionUpdate = Constants.CHEMORECEPTOR_SEARCH_UPDATE_INTERVAL;
    private float timeUntilDigestionUpdate = /*Constants.MICROBE_DIGESTION_UPDATE_INTERVAL*/ 0.2f;

    private bool organelleMaxRenderPriorityDirty = true;
    private int cachedOrganelleMaxRenderPriority;

    /// <summary>
    ///   The stored compounds in this microbe
    /// </summary>
    [JsonIgnore]
    public CompoundBag Compounds => compounds;

    /// <summary>
    ///   True only when this cell has been killed to let know things
    ///   being engulfed by us that we are dead.
    /// </summary>
    [JsonProperty]
    public bool Dead { get; private set; }

    /// <summary>
    ///   The number of agent vacuoles. Determines the time between
    ///   toxin shots.
    /// </summary>
    [JsonProperty]
    public int AgentVacuoleCount { get; private set; }

    /// <summary>
    ///   The slime jets attached to this microbe. JsonIgnore as the components add themselves to this list each load.
    /// </summary>
    [JsonIgnore]
    public List<SlimeJetComponent> SlimeJets { get; private set; } = new();

    /// <summary>
    ///   All organelle nodes need to be added to this node to make scale work
    /// </summary>
    [JsonIgnore]
    public Spatial OrganelleParent { get; private set; } = null!;

    /// <summary>
    ///   The cached highest assigned render priority from all of the organelles.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     A possibly cheaper version of <see cref="OrganelleLayout{T}.MaxRenderPriority"/>.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public int OrganelleMaxRenderPriority
    {
        get
        {
            if (organelleMaxRenderPriorityDirty)
                CountOrganelleMaxRenderPriority();

            return cachedOrganelleMaxRenderPriority;
        }
    }

    [JsonIgnore]
    public CompoundBag ProcessCompoundStorage => Compounds;

    /// <summary>
    ///   For use by the AI to do run and tumble to find compounds. Also used by player cell for tutorials
    /// </summary>
    [JsonProperty]
    public Dictionary<Compound, float> TotalAbsorbedCompounds { get; set; } = new();

    [JsonProperty]
    public float AgentEmissionCooldown { get; private set; }

    [JsonIgnore]
    public Enzyme RequisiteEnzymeToDigest => SimulationParameters.Instance.GetEnzyme(Membrane.Type.DissolverEnzyme);

    [JsonIgnore]
    public float DigestedAmount
    {
        get => dissolveEffectValue;
        set
        {
            dissolveEffectValue = Mathf.Clamp(value, 0.0f, 1.0f);

            throw new NotImplementedException();

            // UpdateDissolveEffect();
        }
    }

    /// <summary>
    ///   Called when the reproduction status of this microbe changes
    /// </summary>
    [JsonProperty]
    public Action<Microbe, bool>? OnReproductionStatus { get; set; }

    /// <summary>
    ///   Called periodically to report the chemoreception settings of the microbe
    /// </summary>
    [JsonProperty]
    public Action<Microbe, IEnumerable<(Compound Compound, float Range, float MinAmount, Color Colour)>,
        IEnumerable<(Species Species, float Range, Color Colour)>>? OnChemoreceptionInfo { get; set; }

    /// <summary>
    ///   Resets the organelles in this microbe to match the species definition
    /// </summary>
    public void ResetOrganelleLayout()
    {
        // TODO: It would be much better if only organelles that need to be removed where removed,
        // instead of everything.
        // When doing that all organelles will need to be re-added anyway if this turned from a prokaryote to eukaryote

        if (organelles == null)
        {
            organelles = new OrganelleLayout<PlacedOrganelle>(OnOrganelleAdded, OnOrganelleRemoved);
        }
        else
        {
            // Just clear the existing ones
            organelles.Clear();
        }
    }

    /// <summary>
    ///   Applies the set species' color to all of this microbe's organelles
    /// </summary>
    public void ApplyPreviewOrganelleColours()
    {
        if (!IsForPreviewOnly)
            throw new InvalidOperationException("Microbe must be a preview-only type");

        if (organelles == null)
            throw new InvalidOperationException("Microbe must be initialized");

        foreach (var entry in organelles.Organelles)
        {
            throw new NotImplementedException();

            // entry.Colour = CellTypeProperties.Colour;
            // entry.UpdateAsync(0);
            //
            // // This applies the colour so UpdateAsync is not technically needed but to avoid weird bugs we just do
            // // it as well
            // entry.UpdateSync();
        }
    }

    /// <summary>
    ///   Report that a pilus shape was added to this microbe. Called by PilusComponent
    /// </summary>
    public void AddPilus(uint shapeOwner, bool injectisome)
    {
        pilusPhysicsShapes.Add(shapeOwner, injectisome);
    }

    public bool RemovePilus(uint shapeOwner)
    {
        return pilusPhysicsShapes.Remove(shapeOwner);
    }

    public bool IsPilus(uint shape)
    {
        return pilusPhysicsShapes.ContainsKey(shape);
    }

    public bool IsInjectisome(uint shape)
    {
        if (!IsPilus(shape))
            return false;

        return pilusPhysicsShapes[shape];
    }

    /// <summary>
    ///   Sets up the hitpoints of this microbe based on the Species membrane
    /// </summary>
    private void SetupMicrobeHitpoints()
    {
        // TODO: Health.RescaleMaxHealth needs to be used
    }

    private void OnPlayerDuplicationCheat(object sender, EventArgs e)
    {
        allOrganellesDivided = true;

        throw new NotImplementedException();

        // Divide();
    }

    /// <summary>
    ///   Removes the player's ability to go to the editor.
    ///   Does nothing when called by the AI.
    /// </summary>
    private void UnreadyToReproduce()
    {
        // Sets this flag to false to make full recomputation on next reproduction readiness check
        // This notably allows to reactivate editor button upon colony unbinding.
        allOrganellesDivided = false;
        OnReproductionStatus?.Invoke(this, false);
    }

    private void HandleMovement(float delta)
    {
        if (PhagocytosisStep != PhagocytosisPhase.None)
        {
            // TODO: is this needed now? movement data is preserved but it doesn't have any effect with the new system
            // Reset movement
            MovementDirection = Vector3.Zero;
            queuedMovementForce = Vector3.Zero;

            return;
        }
    }

    [DeserializedCallbackAllowed]
    private void OnOrganelleAdded(PlacedOrganelle organelle)
    {
        throw new NotImplementedException();

        /*organelle.OnAddedToMicrobe(this);
        processesDirty = true;
        enzymesDirty = true;
        cachedHexCountDirty = true;
        membraneOrganellePositionsAreDirty = true;
        hasSignalingAgent = null;
        cachedRotationSpeed = null;
        organelleMaxRenderPriorityDirty = true;

        if (organelle.IsAgentVacuole)
            AgentVacuoleCount += 1;

        // This is calculated here as it would be a bit difficult to
        // hook up computing this when the StorageBag needs this info.
        UpdateCapacity(organelle, false);
        UpdateCompoundBagCapacities();*/
    }

    [DeserializedCallbackAllowed]
    private void OnOrganelleRemoved(PlacedOrganelle organelle)
    {
        throw new NotImplementedException();

        /*UpdateCapacity(organelle, true);

        if (organelle.IsAgentVacuole)
            AgentVacuoleCount -= 1;

        if (organelle.IsSlimeJet)
            SlimeJets.Remove((SlimeJetComponent)organelle.Components.First(c => c is SlimeJetComponent));

        organelle.OnRemovedFromMicrobe();

        // The organelle only detaches but doesn't delete itself, so we delete it here
        organelle.DetachAndQueueFree();

        processesDirty = true;
        enzymesDirty = true;
        cachedHexCountDirty = true;
        membraneOrganellePositionsAreDirty = true;
        hasSignalingAgent = null;
        cachedRotationSpeed = null;
        organelleMaxRenderPriorityDirty = true;

        UpdateCompoundBagCapacities();*/
    }

    private void CountOrganelleMaxRenderPriority()
    {
        cachedOrganelleMaxRenderPriority = 0;

        if (organelles == null)
            return;

        cachedOrganelleMaxRenderPriority = organelles.MaxRenderPriority;
        organelleMaxRenderPriorityDirty = false;
    }
}
