using System;
using System.Collections.Generic;
using System.Linq;
using DefaultEcs;
using Godot;
using Newtonsoft.Json;
using Array = Godot.Collections.Array;

/// <summary>
///   Main script on each cell in the game.
///   Partial class: Engulf, Bind/Unbind, Colony,
///   Damage, Kill, Pilus, Membrane
/// </summary>
public partial class Microbe
{
#pragma warning disable CA2213
    private PackedScene endosomeScene = null!;

    private PackedScene cellBurstEffectScene = null!;
#pragma warning restore CA2213

    // private SphereShape pseudopodRangeSphereShape = null!;

    /// <summary>
    ///   Contains the pili this microbe has for collision checking and weather or not
    ///   they have the injectisome upgrade
    /// </summary>
    private Dictionary<uint, bool> pilusPhysicsShapes = new();

    private bool membraneOrganellePositionsAreDirty = true;
    private bool membraneOrganellesWereUpdatedThisFrame;

    private bool destroyed;

    // TODO: with the new engulfing mechanics these stats don't really work the same as before
    [JsonProperty]
    private float escapeInterval;

    [JsonProperty]
    private bool hasEscaped;

    /// <summary>
    ///   Tracks entities this is touching, for beginning engulfing and cell binding.
    /// </summary>
    private HashSet<IEntity> touchedEntities = new();

    /// <summary>
    ///   Tracks entities this is trying to engulf.
    /// </summary>
    [JsonProperty]
    private HashSet<IEngulfable> attemptingToEngulf = new();

    /// <summary>
    ///   Tracks entities this already engulfed.
    /// </summary>
    [JsonProperty]
    private List<object> engulfedObjects = new();

    /// <summary>
    ///   Tracks entities this has previously engulfed.
    /// </summary>
    [JsonProperty]
    private List<object> expelledObjects = new();

    // private HashSet<IEngulfable> engulfablesInPseudopodRange = new();

    // private MeshInstance pseudopodTarget = null!;

    /// <summary>
    ///   Controls for how long the flashColour is held before going
    ///   back to species colour.
    /// </summary>
    [JsonProperty]
    private float flashDuration;

    [JsonProperty]
    private Color flashColour = new(0, 0, 0, 0);

    /// <summary>
    ///   This determines how important the current flashing action is. This allows higher priority flash colours to
    ///   take over.
    /// </summary>
    [JsonProperty]
    private int flashPriority;

    /// <summary>
    ///   This determines how much time is left (in seconds) until this cell can take damage again after becoming
    ///   invulnerable due to a damage source. This was added to balance pili but might extend to more sources.
    /// </summary>
    [JsonProperty]
    private float invulnerabilityDuration;

    [JsonProperty]
    private bool deathParticlesSpawned;

    /// <summary>
    ///   Used to log just once when the touched microbe disposed issue happens to reduce log spam
    /// </summary>
    private bool loggedTouchedDisposeIssue;

    [JsonProperty]
    private MicrobeState state;

    [JsonProperty]
    public Microbe? ColonyParent { get; set; }

    [JsonProperty]
    public List<Microbe>? ColonyChildren { get; set; }

    /// <summary>
    ///   The membrane of this Microbe. Used for grabbing radius / points from this.
    /// </summary>
    [JsonIgnore]
    public Membrane Membrane { get; private set; } = null!;

    [JsonProperty]
    public float Hitpoints { get; private set; }

    [JsonProperty]
    public float MaxHitpoints { get; private set; }

    // Properties for engulfing
    [JsonProperty]
    public PhagocytosisPhase PhagocytosisStep { get; set; }

    /// <summary>
    ///   The amount of space all of the currently engulfed objects occupy in the cytoplasm. This is used to determine
    ///   whether a cell can ingest any more objects or not due to being full.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     In a more technical sense, this is the accumulated <see cref="IEngulfable.EngulfSize"/> from all
    ///     the ingested objects. Maximum should be this cell's own <see cref="EngulfSize"/>.
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public float UsedIngestionCapacity { get; private set; }

    [JsonProperty]
    public EntityReference<Microbe> HostileEngulfer { get; private set; } = new();

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    /// <summary>
    ///   The current state of the microbe. Shared across the colony
    /// </summary>
    [JsonIgnore]
    public MicrobeState State
    {
        get => /*Colony?.State ??*/ state;
        set
        {
            if (state == value)
                return;

            state = value;
            throw new NotImplementedException();

            // if (Colony != null)
            //     Colony.State = value;

            // TODO: reimplement this (probably can be put in the microbe control input class)
            if (value == MicrobeState.Unbinding && IsPlayerMicrobe)
                OnUnbindEnabled?.Invoke(this);
        }
    }

    /// <summary>
    ///   The size this microbe is for engulfing calculations
    /// </summary>
    [JsonIgnore]
    public float EngulfSize
    {
        get
        {
            // Scale with digested progress
            var size = HexCount * (1 - DigestedAmount);

            if (CellTypeProperties.IsBacteria)
                return size * 0.5f;

            return size;
        }
    }

    /// <summary>
    ///   Just like <see cref="ICellProperties.CanEngulf"/> but decoupled from Species and is based on the local
    ///   condition of the microbe instead.
    /// </summary>
    /// <returns>True if this cell fills all the requirements needed to enter engulf mode.</returns>
    [JsonIgnore]
    public bool CanEngulf => !Membrane.Type.CellWall;

    // [JsonIgnore]
    // public bool CanBind => !IsMulticellular && (organelles?.Any(p => p.IsBindingAgent) == true || Colony != null);

    // [JsonIgnore]
    // public bool CanUnbind => !IsMulticellular && Colony != null;

    /// <summary>
    ///   Called when this Microbe dies
    /// </summary>
    [JsonProperty]
    public Action<Microbe>? OnDeath { get; set; }

    [JsonProperty]
    public Action<Microbe>? OnUnbindEnabled { get; set; }

    [JsonProperty]
    public Action<Microbe>? OnUnbound { get; set; }

    [JsonProperty]
    public Action<Microbe, Microbe>? OnIngestedByHostile { get; set; }

    [JsonProperty]
    public Action<Microbe, IEngulfable>? OnSuccessfulEngulfment { get; set; }

    [JsonProperty]
    public Action<Microbe>? OnEngulfmentStorageFull { get; set; }

    [JsonProperty]
    public Action<Microbe, IHUDMessage>? OnNoticeMessage { get; set; }

    /// <summary>
    ///   Give this microbe a specified amount of invulnerability time. Overrides previous value.
    ///   NOTE: Not all damage sources apply invulnerability, check method usages.
    /// </summary>
    public void MakeInvulnerable(float duration)
    {
        invulnerabilityDuration = duration;
    }

    public void ClearEngulfedObjects()
    {
        foreach (var engulfed in engulfedObjects.ToList())
        {
            throw new NotImplementedException();

            // if (engulfed.Object.Value != null)
            // {
            //     engulfedObjects.Remove(engulfed);
            //     engulfed.Object.Value.DestroyDetachAndQueueFree();
            // }
            //
            // engulfed.Phagosome.Value?.DestroyDetachAndQueueFree();
        }

        engulfedObjects.Clear();
    }

    /// <summary>
    ///   Returns true if this microbe is currently in the process of ingesting engulfables.
    /// </summary>
    public bool IsPullingInEngulfables()
    {
        return attemptingToEngulf.Any();
    }

    // TODO: put these somewhere and hook these up
    internal void SuccessfulScavenge()
    {
        GameWorld.AlterSpeciesPopulationInCurrentPatch(Species,
            Constants.CREATURE_SCAVENGE_POPULATION_GAIN,
            TranslationServer.Translate("SUCCESSFUL_SCAVENGE"));
    }

    internal void SuccessfulKill()
    {
        GameWorld.AlterSpeciesPopulationInCurrentPatch(Species,
            Constants.CREATURE_KILL_POPULATION_GAIN,
            TranslationServer.Translate("SUCCESSFUL_KILL"));
    }

    // TODO: this is converted but needs verifying that the flashing looks still good
    /// <summary>
    ///   Flashes the membrane colour when Flash has been called
    /// </summary>
    private void HandleFlashing(float delta)
    {
        // Flash membrane if something happens.
        if (flashDuration > 0 && flashColour != new Color(0, 0, 0, 0))
        {
            flashDuration -= delta;

            // How frequent it flashes, would be nice to update
            // the flash void to have this variable{
            if (flashDuration % 0.6f < 0.3f)
            {
                Membrane.Tint = flashColour;
            }
            else
            {
                // Restore colour
                Membrane.Tint = CellTypeProperties.Colour;
            }

            // Flashing ended
            if (flashDuration <= 0)
            {
                flashDuration = 0;

                // Restore colour
                Membrane.Tint = CellTypeProperties.Colour;
            }
        }
    }
}
