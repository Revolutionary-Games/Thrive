using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main script on each multicellular creature in the game
/// </summary>
[JsonObject(IsReference = true)]
[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/late_multicellular_stage/MulticellularCreature.tscn", UsesEarlyResolve = false)]
[DeserializedCallbackTarget]
public class MulticellularCreature : RigidBody, ISpawned, IProcessable, ISaveLoadedTracked, ICharacterInventory
{
    private static readonly Vector3 SwimUpForce = new(0, 20, 0);

    [JsonProperty]
    private readonly CompoundBag compounds = new(0.0f);

    [JsonProperty]
    private readonly List<IInteractableEntity> carriedObjects = new();

    private Compound atp = null!;
    private Compound glucose = null!;

    [JsonProperty]
    private CreatureAI? ai;

    [JsonProperty]
    private ISpawnSystem? spawnSystem;

#pragma warning disable CA2213
    private MulticellularMetaballDisplayer metaballDisplayer = null!;
#pragma warning restore CA2213

    // TODO: a real system for determining the hand and equipment slots
    // TODO: hand count based on body plan
    [JsonProperty]
    private InventorySlotData handSlot = new(1, EquipmentSlotType.Hand, new Vector2(0.8f, 0.5f));

    // TODO: increase inventory slots based on equipment
    [JsonProperty]
    private InventorySlotData[] inventorySlots =
    {
        new(2),
        new(3),
    };

    [JsonProperty]
    private float targetSwimLevel;

    [JsonProperty]
    private float upDownSwimSpeed = 3;

    // TODO: implement
    [JsonIgnore]
    public List<TweakedProcess> ActiveProcesses => new();

    // TODO: implement
    [JsonIgnore]
    public CompoundBag ProcessCompoundStorage => compounds;

    // TODO: implement multicellular process statistics
    [JsonIgnore]
    public ProcessStatistics? ProcessStatistics => null;

    [JsonProperty]
    public bool Dead { get; private set; }

    [JsonProperty]
    public Action<MulticellularCreature>? OnDeath { get; set; }

    [JsonProperty]
    public Action<MulticellularCreature, bool>? OnReproductionStatus { get; set; }

    [JsonProperty]
    public Action<MulticellularCreature, IInteractableEntity>? RequestCraftingInterfaceFor { get; set; }

    /// <summary>
    ///   The species of this creature. It's mandatory to initialize this with <see cref="ApplySpecies"/> otherwise
    ///   random stuff in this instance won't work
    /// </summary>
    [JsonProperty]
    public LateMulticellularSpecies Species { get; private set; } = null!;

    /// <summary>
    ///   True when this is the player's creature
    /// </summary>
    [JsonProperty]
    public bool IsPlayerCreature { get; private set; }

    /// <summary>
    ///   For checking if the player is in freebuild mode or not
    /// </summary>
    [JsonProperty]
    public GameProperties CurrentGame { get; private set; } = null!;

    /// <summary>
    ///   Needs access to the world for population changes
    /// </summary>
    [JsonIgnore]
    public GameWorld GameWorld => CurrentGame.GameWorld;

    /// <summary>
    ///   The direction the creature wants to move. Doesn't need to be normalized
    /// </summary>
    public Vector3 MovementDirection { get; set; } = Vector3.Zero;

    [JsonProperty]
    public MovementMode MovementMode { get; set; }

    [JsonProperty]
    public float TimeUntilNextAIUpdate { get; set; }

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Spatial EntityNode => this;

    public int DespawnRadiusSquared { get; set; }

    /// <summary>
    ///   TODO: adjust entity weight once fleshed out
    /// </summary>
    [JsonIgnore]
    public float EntityWeight => 1.0f;

    [JsonIgnore]
    public bool IsLoadedFromSave { get; set; }

    public override void _Ready()
    {
        base._Ready();

        atp = SimulationParameters.Instance.GetCompound("atp");
        glucose = SimulationParameters.Instance.GetCompound("glucose");

        metaballDisplayer = GetNode<MulticellularMetaballDisplayer>("MetaballDisplayer");
    }

    /// <summary>
    ///   Must be called when spawned to provide access to the needed systems
    /// </summary>
    public void Init(ISpawnSystem spawnSystem, GameProperties currentGame, bool isPlayer)
    {
        this.spawnSystem = spawnSystem;
        CurrentGame = currentGame;
        IsPlayerCreature = isPlayer;

        if (!isPlayer)
            ai = new CreatureAI(this);

        // Needed for immediately applying the species
        _Ready();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        // TODO: implement growth
        OnReproductionStatus?.Invoke(this, true);
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);

        if (MovementMode == MovementMode.Swimming)
        {
            // TODO: apply buoyancy (if this is underwater)

            if (Translation.y < targetSwimLevel)
                ApplyCentralImpulse(Mass * SwimUpForce * delta);

            if (MovementDirection != Vector3.Zero)
            {
                // TODO: movement force calculation
                ApplyCentralImpulse(Mass * MovementDirection * delta);
            }
        }
        else
        {
            if (MovementDirection != Vector3.Zero)
            {
                // TODO: movement force calculation
                ApplyCentralImpulse(Mass * MovementDirection * delta * 50);
            }
        }
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    public void ApplySpecies(Species species)
    {
        if (species is not LateMulticellularSpecies lateSpecies)
            throw new ArgumentException("Only late multicellular species can be used on creatures");

        Species = lateSpecies;

        // TODO: set from species
        compounds.Capacity = 100;

        // TODO: better mass calculation
        Mass = lateSpecies.BodyLayout.Sum(m => m.Size * m.CellType.TotalMass);

        // Setup graphics
        // TODO: handle lateSpecies.Scale
        metaballDisplayer.DisplayFromList(lateSpecies.BodyLayout);
    }

    /// <summary>
    ///   Applies the default movement mode this species has when spawned.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: we probably need to allow spawning in different modes for example amphibian creatures
    ///   </para>
    /// </remarks>
    public void ApplyMovementModeFromSpecies()
    {
        if (Species.ReproductionLocation != ReproductionLocation.Water)
        {
            MovementMode = MovementMode.Walking;
        }
    }

    public void SetInitialCompounds()
    {
        compounds.AddCompound(atp, 50);
        compounds.AddCompound(glucose, 50);
    }

    public MulticellularCreature SpawnOffspring()
    {
        var currentPosition = GlobalTransform.origin;

        // TODO: calculate size somehow
        var separation = new Vector3(10, 0, 0);

        // Create the offspring
        var copyEntity = SpawnHelpers.SpawnCreature(Species, currentPosition + separation,
            GetParent(), SpawnHelpers.LoadMulticellularScene(), true, spawnSystem!, CurrentGame);

        // Make it despawn like normal
        spawnSystem!.AddEntityToTrack(copyEntity);

        // TODO: some kind of resource splitting for the offspring?

        PlaySoundEffect("res://assets/sounds/soundeffects/reproduction.ogg");

        return copyEntity;
    }

    public void BecomeFullyGrown()
    {
        // TODO: implement growth
        // Once growth is added check spawnSystem.IsUnderEntityLimitForReproducing before calling SpawnOffspring
    }

    public void ResetGrowth()
    {
        // TODO: implement growth
    }

    public void Damage(float amount, string source)
    {
        if (IsPlayerCreature && CheatManager.GodMode)
            return;

        if (amount == 0 || Dead)
            return;

        if (string.IsNullOrEmpty(source))
            throw new ArgumentException("damage type is empty");

        // if (amount < 0)
        // {
        //     GD.PrintErr("Trying to deal negative damage");
        //     return;
        // }

        // TODO: sound

        // TODO: show damage visually
        // Flash(1.0f, new Color(1, 0, 0, 0.5f), 1);

        // TODO: hitpoints and death
        // if (Hitpoints <= 0.0f)
        // {
        //     Hitpoints = 0.0f;
        //     Kill();
        // TODO: kill method needs to call DropAll()
        // }
    }

    public void PlaySoundEffect(string effect, float volume = 1.0f)
    {
        // TODO: make these sound objects only be loaded once
        // var sound = GD.Load<AudioStream>(effect);

        // TODO: implement sound playing, should probably create a helper method to share with Microbe

        /*// Find a player not in use or create a new one if none are available.
        var player = otherAudioPlayers.Find(nextPlayer => !nextPlayer.Playing);

        if (player == null)
        {
            // If we hit the player limit just return and ignore the sound.
            if (otherAudioPlayers.Count >= Constants.MAX_CONCURRENT_SOUNDS_PER_ENTITY)
                return;

            player = new AudioStreamPlayer3D();
            player.MaxDistance = 100.0f;
            player.Bus = "SFX";

            AddChild(player);
            otherAudioPlayers.Add(player);
        }

        player.UnitDb = GD.Linear2Db(volume);
        player.Stream = sound;
        player.Play();*/
    }

    public void SwimUpOrJump(float delta)
    {
        if (MovementMode == MovementMode.Swimming)
        {
            targetSwimLevel += upDownSwimSpeed * delta;
        }
        else
        {
            // TODO: only allow jumping when touching the ground
            // TODO: suppress jump when the user just interacted with a dialog to confirm something, maybe jump should
            // use the on press key thing to only trigger jumping once?
            ApplyCentralImpulse(new Vector3(0, 1, 0) * delta * 1000);
        }
    }

    public void SwimDownOrCrouch(float delta)
    {
        // TODO: crouching
        targetSwimLevel -= upDownSwimSpeed * delta;
    }

    /// <summary>
    ///   Calculates the actions this creature can do on a target object
    /// </summary>
    /// <param name="target">The object to potentially do something for</param>
    /// <returns>Enumerator of the possible actions</returns>
    /// <remarks>
    ///   <para>
    ///     Somehow make sure when the AI can use this to that the text overrides don't need to be generated as those
    ///     will waste performance for no reason. Maybe we just need two variants of the method?
    ///   </para>
    /// </remarks>
    public IEnumerable<(InteractionType Interaction, bool Enabled, string? TextOverride)> CalculatePossibleActions(
        IInteractableEntity target)
    {
        if (target.CanBeCarried)
        {
            bool full = !FitsInCarryingCapacity(target);
            yield return (InteractionType.Pickup, !full,
                full ? TranslationServer.Translate("INTERACTION_PICK_UP_CANNOT_FULL") : null);
        }

        if (target is ResourceEntity)
        {
            // Assume all resources can be used in some kind of crafting
            yield return (InteractionType.Craft, true, null);
        }
    }

    public bool AttemptInteraction(IInteractableEntity target, InteractionType interactionType)
    {
        // Make sure action is allowed first
        if (!CalculatePossibleActions(target).Any(t => t.Enabled && t.Interaction == interactionType))
            return false;

        // Then perform it
        switch (interactionType)
        {
            case InteractionType.Pickup:
                return PickupItem(target);
            case InteractionType.Craft:
                if (RequestCraftingInterfaceFor == null)
                {
                    // AI should directly use the crafting methods to create the crafter products
                    GD.PrintErr(
                        $"Only player creature can open crafting ({nameof(RequestCraftingInterfaceFor)} is unset)");
                    return false;
                }

                // Request the crafting interface to be opened with the target pre-selected
                RequestCraftingInterfaceFor.Invoke(this, target);
                return true;
            default:
                GD.PrintErr($"Unimplemented action handling for {interactionType}");
                return false;
        }
    }

    public bool FitsInCarryingCapacity(IInteractableEntity interactableEntity)
    {
        return this.HasEmptySlot();
    }

    /// <summary>
    ///   Pickup item to the first available slot
    /// </summary>
    public bool PickupItem(IInteractableEntity item)
    {
        // Find an empty slot to put the thing in
        // Prefer hand slots
        foreach (var slot in inventorySlots.Prepend(handSlot))
        {
            if (slot.ContainedItem == null)
            {
                return PickUpItem(item, slot.Id);
            }
        }

        // No empty slots
        return false;
    }

    public bool PickUpItem(IInteractableEntity item, int slotId)
    {
        // Find the slot to put the item in
        if (handSlot.Id == slotId)
            return PickupToSlot(item, handSlot);

        foreach (var slot in inventorySlots)
        {
            if (slot.Id == slotId)
                return PickupToSlot(item, slot);
        }

        return false;
    }

    public void DropAll()
    {
        var thingsToDrop = this.ListAllItems().Select(s => s.ContainedItem).WhereNotNull().ToList();

        foreach (var entity in thingsToDrop)
        {
            // TODO: the missing check that the dropped position is free of other physics objects is really going to be
            // a problem here
            DropItem(entity);
        }
    }

    public bool DropItem(IInteractableEntity item)
    {
        var slot = this.SlotWithItem(item);

        if (slot == null)
        {
            GD.PrintErr("Trying to drop item we can't find in our inventory slots");
            return false;
        }

        if (!carriedObjects.Remove(item))
        {
            // We weren't carrying that
            GD.PrintErr("Can't drop something creature isn't carrying");
            return false;
        }

        var entityNode = item.EntityNode;

        // TODO: drop position based on creature size, and also confirm the drop point is free from other physics
        // objects

        var offset = new Vector3(0, 1.5f, 3.6f);

        // Assume our parent is the world
        var world = GetParent() ?? throw new Exception("Creature has no parent to place dropped entity in");

        var ourTransform = GlobalTransform;

        entityNode.ReParent(world);
        entityNode.GlobalTranslation = ourTransform.origin + ourTransform.basis.Quat().Xform(offset);

        // Allow others to interact with the object again
        item.InteractionDisabled = false;

        if (entityNode is RigidBody entityPhysics)
            entityPhysics.Mode = ModeEnum.Rigid;

        slot.ContainedItem = null;

        return true;
    }

    public IEnumerable<InventorySlotData> ListInventoryContents()
    {
        return inventorySlots;
    }

    public IEnumerable<InventorySlotData> ListHandContents()
    {
        yield return handSlot;
    }

    private bool PickupToSlot(IInteractableEntity item, InventorySlotData slot)
    {
        if (slot.ContainedItem != null)
            return false;

        slot.ContainedItem = item;

        var targetNode = item.EntityNode;

        // Remove the object from the world
        targetNode.ReParent(this);

        // TODO: inventory carried items should not be shown in the world

        // TODO: better positioning and actually attaching it to the place the object is carried in
        // TODO: this also has a problem when items are removed and added back in random order (gaps and conflicting
        // positions)
        var offset = new Vector3(-0.5f, 3, 4) * (carriedObjects.Count + 1);

        targetNode.Translation = offset;

        // Add the object to be carried
        carriedObjects.Add(item);

        // Would be very annoying to keep getting the prompt to interact with the object
        item.InteractionDisabled = true;

        // Surprise surprise, the physics detach bug can also hit here
        if (targetNode is RigidBody entityPhysics)
            entityPhysics.Mode = ModeEnum.Kinematic;

        return true;
    }
}
