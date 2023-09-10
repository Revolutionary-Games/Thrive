using System;
using System.Collections.Generic;
using System.ComponentModel;
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
public class MulticellularCreature : RigidBody, ISpawned, IProcessable, ISaveLoadedTracked, ICharacterInventory,
    IStructureSelectionReceiver<StructureDefinition>, IActionProgressSource
{
    private static readonly Vector3 SwimUpForce = new(0, 20, 0);

    [JsonProperty]
    private readonly CompoundBag compounds = new(0.0f);

    [JsonProperty]
    private readonly List<IInteractableEntity> carriedObjects = new();

    private Compound atp = null!;
    private Compound glucose = null!;

    private StructureDefinition? buildingTypeToPlace;

    [JsonProperty]
    private CreatureAI? ai;

    [JsonProperty]
    private ISpawnSystem? spawnSystem;

#pragma warning disable CA2213
    private MulticellularMetaballDisplayer metaballDisplayer = null!;

    private Spatial? buildingToPlaceGhost;
#pragma warning restore CA2213

    // TODO: a real system for determining the hand and equipment slots
    // TODO: hand count based on body plan
    [JsonProperty]
    private InventorySlotData handSlot = new(1, EquipmentSlotType.Hand, new Vector2(0.82f, 0.43f));

    // TODO: increase inventory slots based on equipment
    [JsonProperty]
    private InventorySlotData[] inventorySlots =
    {
        new(2),
        new(3),
    };

    [JsonProperty]
    private EntityReference<IInteractableEntity>? actionTarget;

    [JsonProperty]
    private float performedActionTime;

    [JsonProperty]
    private float totalActionRequiredTime;

    /// <summary>
    ///   Where an action was started, used to detect if the creature moves too much and the action should be canceled
    /// </summary>
    [JsonProperty]
    private Vector3 startedActionPosition;

    [JsonProperty]
    private float targetSwimLevel;

    [JsonProperty]
    private float upDownSwimSpeed = 3;

    private bool actionHasSucceeded;

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

    [JsonProperty]
    public bool ActionInProgress { get; private set; }

    [JsonIgnore]
    public float ActionProgress => totalActionRequiredTime != 0 ? performedActionTime / totalActionRequiredTime : 0;

    // TODO: make this creature height dependent
    [JsonIgnore]
    public Vector3? ExtraProgressBarWorldOffset => null;

    [JsonIgnore]
    public bool IsPlacingStructure => buildingTypeToPlace != null;

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

        UpdateActionStatus(delta);
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

        // This is in physics process as this follows the player physics entity
        if (IsPlacingStructure && buildingToPlaceGhost != null)
        {
            // Position the preview
            buildingToPlaceGhost.GlobalTransform = GetStructurePlacementLocation();
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
        compounds.NominalCapacity = 100;

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
        // TODO: kill method needs to call DropAll() and CancelStructurePlacing
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
    ///     TODO: Somehow make sure when the AI can use this to that the text overrides don't need to be generated
    ///     as those will waste performance for no reason. Maybe we just need two variants of the method?
    ///     Also the player when checking if a selected action is still allowed, will result in extra text lookups.
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

        var harvesting = target.GetHarvestingInfo();

        if (harvesting != null)
        {
            var availableTools = this.GetAllCategoriesOfEquippedItems();

            // Do we have the required tools
            var missingTool = harvesting.CheckRequiredTool(availableTools);
            if (missingTool == null)
            {
                yield return (InteractionType.Harvest, true, null);
            }
            else
            {
                var message = TranslationServer.Translate("INTERACTION_HARVEST_CANNOT_MISSING_TOOL").FormatSafe(
                    TranslationServer.Translate(missingTool.GetAttribute<DescriptionAttribute>().Description));

                yield return (InteractionType.Harvest, false, message);
            }
        }

        if (target is IAcceptsResourceDeposit { DepositActionAllowed: true } deposit)
        {
            bool takesItems = deposit.GetWantedItems(this) != null;

            yield return (InteractionType.DepositResources, takesItems,
                takesItems ?
                    null :
                    TranslationServer.Translate("INTERACTION_DEPOSIT_RESOURCES_NO_SUITABLE_RESOURCES"));
        }

        if (target is IConstructable { Completed: false } constructable)
        {
            bool canBeBuilt = constructable.HasRequiredResourcesToConstruct;

            yield return (InteractionType.Construct, canBeBuilt,
                canBeBuilt ?
                    null :
                    TranslationServer.Translate("INTERACTION_CONSTRUCT_MISSING_DEPOSITED_MATERIALS"));
        }

        // Add the extra interactions the entity provides
        var extraInteractions = target.GetExtraAvailableActions();

        if (extraInteractions != null)
        {
            foreach (var (interaction, disabledText) in extraInteractions)
            {
                yield return (interaction, disabledText == null, disabledText);
            }
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
            case InteractionType.Harvest:
                return this.HarvestEntity(target);
            case InteractionType.Craft:
            {
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
            }

            case InteractionType.DepositResources:
            {
                // TODO: instead of closing, just update the interaction popup to allow finishing construction
                // immediately
                if (target is IAcceptsResourceDeposit deposit)
                {
                    if (!deposit.AutoTakesResources)
                    {
                        // TODO: allow selecting items
                        GD.Print("TODO: selecting items to deposit interface is not done");
                    }

                    var itemsToDeposit = deposit.GetWantedItems(this);

                    if (itemsToDeposit != null)
                    {
                        var slots = itemsToDeposit.ToList();
                        deposit.DepositItems(slots.Select(i => i.ContainedItem).WhereNotNull());

                        foreach (var slot in slots)
                        {
                            if (!DeleteItem(slot))
                                GD.PrintErr("Failed to delete deposited item");
                        }

                        return true;
                    }
                }

                GD.PrintErr("Deposit action failed due to bad target or currently held items");
                return false;
            }

            case InteractionType.Construct:
            {
                if (target is IConstructable
                        { Completed: false, HasRequiredResourcesToConstruct: true } constructable)
                {
                    // Start action for constructing, the action when finished will pick what it does based on the
                    // target entity
                    StartAction(target, constructable.TimedActionDuration);
                    return true;
                }

                return false;
            }

            default:
            {
                // This might be an extra interaction
                var extraInteractions = target.GetExtraAvailableActions();

                if (extraInteractions != null)
                {
                    foreach (var (extraInteraction, _) in extraInteractions)
                    {
                        if (extraInteraction == interactionType)
                            return target.PerformExtraAction(extraInteraction);
                    }
                }

                // Unknown action type and not an extra action provided by the target
                GD.PrintErr($"Unimplemented action handling for {interactionType}");
                return false;
            }
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

        HandleEntityDrop(item, entityNode);

        slot.ContainedItem = null;

        return true;
    }

    public bool DeleteItem(int slotId)
    {
        var slot = this.SlotWithId(slotId);

        if (slot == null)
        {
            GD.Print("Trying to delete item in non-existent slot");
            return false;
        }

        return DeleteItem(slot);
    }

    public bool DeleteItem(InventorySlotData slot)
    {
        if (slot.ContainedItem == null)
            return false;

        slot.ContainedItem.DestroyAndQueueFree();

        slot.ContainedItem = null;
        return true;
    }

    public bool DeleteWorldEntity(IInteractableEntity entity)
    {
        // TODO: could verify the interact distance etc. here
        // If the above TODO is done then probably the crafting action should have test methods to verify that it can
        // consume all of the items first, before attempting the craft to not consume partial resources
        entity.DestroyDetachAndQueueFree();
        return true;
    }

    public void DirectlyDropEntity(IInteractableEntity entity)
    {
        HandleEntityDrop(entity, entity.EntityNode);
    }

    public IEnumerable<InventorySlotData> ListInventoryContents()
    {
        return inventorySlots;
    }

    public IEnumerable<InventorySlotData> ListEquipmentContents()
    {
        yield return handSlot;
    }

    public bool IsItemSlotMoveAllowed(int fromSlotId, int toSlotId)
    {
        // TODO: implement slot type restrictions

        // TODO: non-hand equipment slots should only take equipment of the right type

        return true;
    }

    public void MoveItemSlots(int fromSlotId, int toSlotId)
    {
        var from = this.SlotWithId(fromSlotId) ?? throw new ArgumentException("Invalid from slot");
        var to = this.SlotWithId(toSlotId) ?? throw new ArgumentException("Invalid to slot");

        (from.ContainedItem, to.ContainedItem) = (to.ContainedItem, from.ContainedItem);

        if (from.ContainedItem != null)
            SetItemPositionInSlot(from, from.ContainedItem.EntityNode);

        if (to.ContainedItem != null)
            SetItemPositionInSlot(to, to.ContainedItem.EntityNode);
    }

    public void CancelCurrentAction()
    {
        if (!ActionInProgress)
            return;

        totalActionRequiredTime = 0;

        // Reset the shown progress
        var target = actionTarget?.Value;
        if (target != null)
        {
            performedActionTime = 0;
            UpdateActionTargetProgress(target);
        }

        ActionInProgress = false;
        actionTarget = null;
    }

    public void OnStructureTypeSelected(StructureDefinition structureDefinition)
    {
        // Just to be safe, cancel existing placing
        CancelStructurePlacing();

        buildingTypeToPlace = structureDefinition;

        // Show the ghost where it is about to be placed
        buildingToPlaceGhost = buildingTypeToPlace.GhostScene.Instance<Spatial>();

        // TODO: should we add the ghost to our child or keep it in the world?
        GetParent().AddChild(buildingToPlaceGhost);

        buildingToPlaceGhost.GlobalTransform = GetStructurePlacementLocation();

        // TODO: disallow placing when overlaps with physics objects (and show ghost with red tint)
    }

    public void AttemptStructurePlace()
    {
        if (buildingTypeToPlace == null)
            return;

        // TODO: check placement location being valid
        var location = GetStructurePlacementLocation();

        // Take the resources the construction takes
        var usedResources = this.FindRequiredResources(buildingTypeToPlace.ScaffoldingCost);

        if (usedResources == null)
        {
            GD.Print("Not enough resources to start structure after all");

            // TODO: play invalid placement sound
            return;
        }

        foreach (var usedResource in usedResources)
        {
            if (!DeleteItem(usedResource))
            {
                GD.PrintErr("Resource for placing structure consuming failed");
                return;
            }
        }

        // Create the structure entity
        var structureScene = SpawnHelpers.LoadStructureScene();

        SpawnHelpers.SpawnStructure(buildingTypeToPlace, location, GetParent(), structureScene);

        // Stop showing the ghost
        CancelStructurePlacing();
    }

    public void CancelStructurePlacing()
    {
        if (!IsPlacingStructure)
            return;

        buildingToPlaceGhost?.QueueFree();
        buildingToPlaceGhost = null;

        buildingTypeToPlace = null;
    }

    public bool GetAndConsumeActionSuccess()
    {
        if (actionHasSucceeded)
        {
            actionHasSucceeded = false;
            return true;
        }

        return false;
    }

    public Dictionary<WorldResource, int> CalculateWholeAvailableResources()
    {
        return this.CalculateAvailableResources();
    }

    private bool PickupToSlot(IInteractableEntity item, InventorySlotData slot)
    {
        if (slot.ContainedItem != null)
            return false;

        slot.ContainedItem = item;

        var targetNode = item.EntityNode;

        if (targetNode.GetParent() != null)
        {
            // Remove the object from the world
            targetNode.ReParent(this);
        }
        else
        {
            // We are picking up a crafting result or another entity that is not in the world
            AddChild(targetNode);
        }

        SetItemPositionInSlot(slot, targetNode);

        // Add the object to be carried
        carriedObjects.Add(item);

        // Would be very annoying to keep getting the prompt to interact with the object
        item.InteractionDisabled = true;

        // Surprise surprise, the physics detach bug can also hit here
        if (targetNode is RigidBody entityPhysics)
            entityPhysics.Mode = ModeEnum.Kinematic;

        return true;
    }

    private void HandleEntityDrop(IInteractableEntity item, Spatial entityNode)
    {
        // TODO: drop position based on creature size, and also confirm the drop point is free from other physics
        // objects

        var offset = new Vector3(0, 1.5f, 3.6f);

        // Assume our parent is the world
        var world = GetParent() ?? throw new Exception("Creature has no parent to place dropped entity in");

        var ourTransform = GlobalTransform;

        // Handle directly dropped entities that haven't been anywhere yet
        if (entityNode.GetParent() == null)
        {
            world.AddChild(entityNode);
        }
        else
        {
            entityNode.ReParent(world);
        }

        entityNode.GlobalTranslation = ourTransform.origin + ourTransform.basis.Quat().Xform(offset);

        // Allow others to interact with the object again
        item.InteractionDisabled = false;

        if (entityNode is RigidBody entityPhysics)
            entityPhysics.Mode = ModeEnum.Rigid;
    }

    private void SetItemPositionInSlot(InventorySlotData slot, Spatial node)
    {
        // TODO: inventory carried items should not be shown in the world

        // TODO: better positioning and actually attaching it to the place the object is carried in
        var offset = new Vector3(-0.5f, 2.7f, 1.5f + 2.5f * slot.Id);

        node.Translation = offset;
    }

    private void StartAction(IInteractableEntity target, float totalDuration)
    {
        if (ActionInProgress)
            CancelCurrentAction();

        ActionInProgress = true;
        actionTarget = new EntityReference<IInteractableEntity>(target);
        performedActionTime = 0;
        totalActionRequiredTime = totalDuration;
        startedActionPosition = GlobalTranslation;
    }

    private void UpdateActionStatus(float delta)
    {
        if (!ActionInProgress)
            return;

        // If moved too much, cancel
        if (GlobalTranslation.DistanceSquaredTo(startedActionPosition) > Constants.ACTION_CANCEL_DISTANCE)
        {
            // TODO: play an action cancel sound
            CancelCurrentAction();
            return;
        }

        // If target is gone, cancel the action
        var target = actionTarget?.Value;
        if (target == null)
        {
            // TODO: play an action cancel sound
            CancelCurrentAction();
            return;
        }

        // Update the time to update the progress value
        performedActionTime += delta;

        if (performedActionTime >= totalActionRequiredTime)
        {
            // Action is now complete
            SetActionTargetAsCompleted(target);
            ActionInProgress = false;
            actionTarget = null;
        }
        else
        {
            UpdateActionTargetProgress(target);
        }
    }

    private void UpdateActionTargetProgress(IInteractableEntity target)
    {
        if (target is IProgressReportableActionSource progressReportable)
        {
            progressReportable.ReportActionProgress(ActionProgress);
        }
    }

    private void SetActionTargetAsCompleted(IInteractableEntity target)
    {
        if (target is ITimedActionSource actionSource)
        {
            actionSource.OnFinishTimeTakingAction();
        }
        else
        {
            GD.PrintErr("Cannot report finished action to unknown entity type");
        }
    }

    private Transform GetStructurePlacementLocation()
    {
        if (buildingTypeToPlace == null)
            throw new InvalidOperationException("No structure type selected");

        var relative = new Vector3(0, 0, 1) * buildingTypeToPlace.WorldSize.z * 1.3f;

        // TODO: a raycast to get the structure on the ground
        // Also for player creature, taking the camera direction into account instead of the creature rotation would
        // be better
        var transform = GlobalTransform;
        var rotation = transform.basis.Quat();

        var worldTransform = new Transform(new Basis(rotation), transform.origin + rotation.Xform(relative));
        return worldTransform;
    }
}
