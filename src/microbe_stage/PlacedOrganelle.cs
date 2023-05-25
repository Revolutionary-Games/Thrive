using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   An organelle that has been placed in a microbe.
/// </summary>
public class PlacedOrganelle : Spatial, IPositionedOrganelle, ISaveLoadedTracked
{
    [JsonIgnore]
    private readonly List<uint> shapes = new();

    private bool needsColourUpdate = true;
    private bool needsDissolveEffectUpdate = true;

    [JsonProperty]
    private Color colour = Colors.White;

    [JsonProperty]
    private float dissolveEffectValue;

    private bool growthValueDirty = true;
    private float growthValue;

    private Microbe? currentShapesParent;

#pragma warning disable CA2213

    /// <summary>
    ///   Used to update the tint
    /// </summary>
    private ShaderMaterial? organelleMaterial;

    private Spatial? organelleSceneInstance;
#pragma warning restore CA2213

    /// <summary>
    ///   The compounds still needed to divide. Initialized from Definition.InitialComposition
    /// </summary>
    [JsonProperty]
    private Dictionary<Compound, float> compoundsLeft = new();

    private List<IOrganelleComponent>? components;

    public PlacedOrganelle(OrganelleDefinition definition, Hex position, int orientation)
    {
        Definition = definition;
        Position = position;
        Orientation = orientation;
    }

    public OrganelleDefinition Definition { get; set; }

    public Hex Position { get; set; }

    public int Orientation { get; set; }

    [JsonProperty]
    public Microbe? ParentMicrobe { get; private set; }

    /// <summary>
    ///   The graphics child node of this organelle
    /// </summary>
    [JsonIgnore]
    public Spatial? OrganelleGraphics { get; private set; }

    /// <summary>
    ///   Animation player this organelle has
    /// </summary>
    [JsonIgnore]
    public AnimationPlayer? OrganelleAnimation { get; private set; }

    /// <summary>
    ///   The tint colour of this organelle.
    /// </summary>
    public Color Colour
    {
        get => colour;
        set
        {
            colour = value;
            needsColourUpdate = true;
        }
    }

    /// <summary>
    ///   Value between 0 and 1 on how far along to splitting this organelle is
    /// </summary>
    [JsonIgnore]
    public float GrowthValue
    {
        get
        {
            if (growthValueDirty)
                RecalculateGrowthValue();
            return growthValue;
        }
    }

    [JsonIgnore]
    public float DissolveEffectValue
    {
        get => dissolveEffectValue;
        set
        {
            dissolveEffectValue = value;
            needsDissolveEffectUpdate = true;
        }
    }

    /// <summary>
    ///   True when organelle was split in preparation for reproducing
    /// </summary>
    public bool WasSplit { get; set; }

    /// <summary>
    ///   True in the organelle that was created as a result of a split
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     In the original organelle WasSplit is true and in the
    ///     created duplicate IsDuplicate is true. SisterOrganelle is
    ///     set in the original organelle.
    ///   </para>
    /// </remarks>
    public bool IsDuplicate { get; set; }

    public PlacedOrganelle? SisterOrganelle { get; set; }

    /// <summary>
    ///   The components instantiated for this placed organelle. Throws if not currently in a microbe
    /// </summary>
    [JsonIgnore]
    public List<IOrganelleComponent> Components => components ??
        throw new InvalidOperationException("This must be placed in a microbe before accessing components");

    /// <summary>
    ///   The upgrades that this organelle has which affect how the components function
    /// </summary>
    [JsonProperty]
    public OrganelleUpgrades? Upgrades { get; set; }

    /// <summary>
    ///   Computes the total storage capacity of this organelle. Works
    ///   only after being added to a microbe and before being
    ///   removed.
    /// </summary>
    [JsonIgnore]
    public float StorageCapacity
    {
        get
        {
            float value = 0.0f;

            foreach (var component in Components)
            {
                if (component is StorageComponent storage)
                {
                    value += storage.Capacity;
                }
            }

            return value;
        }
    }

    [JsonProperty]
    public Dictionary<Enzyme, int> StoredEnzymes { get; private set; } = new();

    /// <summary>
    ///   True if this is an agent vacuole. Number of agent vacuoles
    ///   determine how often a cell can shoot toxins.
    /// </summary>
    [JsonIgnore]
    public bool IsAgentVacuole => HasComponent<AgentVacuoleComponent>();

    [JsonIgnore]
    public bool IsSlimeJet => HasComponent<SlimeJetComponent>();

    [JsonIgnore]
    public bool IsBindingAgent => HasComponent<BindingAgentComponent>();

    public bool IsLoadedFromSave { get; set; }

    /// <summary>
    ///   Guards against adding this to the scene not through OnAddedToMicrobe
    /// </summary>
    public override void _Ready()
    {
        if (Definition == null)
            throw new InvalidOperationException($"{nameof(Definition)} of {nameof(PlacedOrganelle)} is null");

        if (ParentMicrobe == null)
        {
            throw new InvalidOperationException(
                $"{nameof(PlacedOrganelle)} not added to scene through {nameof(OnAddedToMicrobe)}");
        }

        if (IsLoadedFromSave)
            FinishAttachToMicrobe();

        ApplyScale();
    }

    public bool HasShape(uint searchShape)
    {
        return shapes.Contains(searchShape);
    }

    /// <summary>
    ///   Checks if this organelle has the specified component type
    /// </summary>
    public bool HasComponent<T>()
        where T : class
    {
        foreach (var component in Components)
        {
            // TODO: determine if is T or as T is better
            if (component is T)
                return true;
        }

        return false;
    }

    /// <summary>
    ///   Called by a microbe when this organelle has been added to it
    /// </summary>
    public void OnAddedToMicrobe(Microbe microbe)
    {
        if (Definition == null)
            throw new InvalidOperationException("PlacedOrganelle has no definition set");

        if (ParentMicrobe != null)
            throw new InvalidOperationException("PlacedOrganelle is already in a microbe");

        // Store parameters
        ParentMicrobe = microbe;

        // Grab the species colour for us
        Colour = microbe.CellTypeProperties.Colour;

        ParentMicrobe.OrganelleParent.AddChild(this);

        FinishAttachToMicrobe();

        ResetGrowth();
    }

    /// <summary>
    ///   Called by a microbe when this organelle has been removed from it
    /// </summary>
    public void OnRemovedFromMicrobe()
    {
        if (ParentMicrobe == null)
            throw new InvalidOperationException("This organelle is not in a microbe");

        ParentMicrobe.OrganelleParent.RemoveChild(this);

        // Remove physics
        ParentMicrobe.Mass -= Definition.Mass;

        // Remove our sub collisions
        foreach (var shape in shapes)
        {
            currentShapesParent!.RemoveShapeOwner(shape);
        }

        currentShapesParent = null;
        shapes.Clear();

        // Remove components
        foreach (var component in Components)
        {
            component.OnDetachFromCell(this);
        }

        components = null;

        ParentMicrobe = null;
    }

    /// <summary>
    ///   Called by Microbe.Update
    /// </summary>
    /// <param name="delta">Time since last call</param>
    public void UpdateAsync(float delta)
    {
        foreach (var component in Components)
        {
            component.UpdateAsync(delta);
        }
    }

    /// <summary>
    ///   The part of update that is allowed to modify Godot resources
    /// </summary>
    public void UpdateSync()
    {
        // Update each OrganelleComponent
        foreach (var component in Components)
        {
            component.UpdateSync();
        }

        // If the organelle is supposed to be another color.
        if (needsColourUpdate)
        {
            UpdateColour();
        }

        if (needsDissolveEffectUpdate)
            UpdateDissolveEffect();
    }

    /// <summary>
    ///   Gives organelles more compounds to grow (or takes free compounds).
    ///   If <see cref="allowedCompoundUse"/> goes to 0 stops early and doesn't use any more compounds.
    /// </summary>
    public void GrowOrganelle(CompoundBag compounds, ref float allowedCompoundUse, ref float freeCompoundsLeft,
        bool reverseCompoundsLeftOrder)
    {
        float totalTaken = 0;

        // TODO: should we just check a single type per update (and remove once done) so we can skip creating a bunch
        // of extra lists
        foreach (var key in reverseCompoundsLeftOrder ?
                     compoundsLeft.Keys.Reverse().ToArray() :
                     compoundsLeft.Keys.ToArray())
        {
            var amountNeeded = compoundsLeft[key];

            if (amountNeeded <= 0.0f)
                continue;

            if (allowedCompoundUse <= 0)
                break;

            float usedAmount = 0;

            float allowedUseAmount = Math.Min(amountNeeded, allowedCompoundUse);

            if (freeCompoundsLeft > 0)
            {
                var usedFreeCompounds = Math.Min(allowedUseAmount, freeCompoundsLeft);
                usedAmount += usedFreeCompounds;
                allowedUseAmount -= usedFreeCompounds;
                freeCompoundsLeft -= usedFreeCompounds;
            }

            // Take compounds if the cell has what we need
            // ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST controls how much of a certain compound must exist before we
            // take some
            var amountAvailable =
                compounds.GetCompoundAmount(key) - Constants.ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST;

            if (amountAvailable > MathUtils.EPSILON)
            {
                // We can take some
                var amountToTake = Mathf.Min(allowedUseAmount, amountAvailable);

                usedAmount += compounds.TakeCompound(key, amountToTake);
            }

            if (usedAmount < MathUtils.EPSILON)
                continue;

            allowedCompoundUse -= usedAmount;

            var left = amountNeeded - usedAmount;

            if (left < 0.0001f)
                left = 0;

            compoundsLeft[key] = left;

            totalTaken += usedAmount;
        }

        if (totalTaken > 0)
        {
            growthValueDirty = true;

            ApplyScale();
        }
    }

    /// <summary>
    ///   Calculates total number of compounds left until this organelle can divide
    /// </summary>
    public float CalculateCompoundsLeft()
    {
        float totalLeft = 0;

        foreach (var entry in compoundsLeft)
        {
            totalLeft += entry.Value;
        }

        return totalLeft;
    }

    /// <summary>
    ///   Calculates how much compounds this organelle has absorbed already, adds to the dictionary
    /// </summary>
    public float CalculateAbsorbedCompounds(Dictionary<Compound, float> result)
    {
        float totalAbsorbed = 0;

        foreach (var entry in compoundsLeft)
        {
            var amountLeft = entry.Value;

            var amountTotal = Definition.InitialComposition[entry.Key];

            var absorbed = amountTotal - amountLeft;

            result.TryGetValue(entry.Key, out var alreadyInResult);

            result[entry.Key] = alreadyInResult + absorbed;

            totalAbsorbed += absorbed;
        }

        return totalAbsorbed;
    }

    /// <summary>
    ///   Resets the state. Used after dividing
    /// </summary>
    public void ResetGrowth()
    {
        // Return the compound bin to its original state
        growthValue = 0.0f;
        growthValueDirty = true;

        // Deep copy
        compoundsLeft.Clear();

        foreach (var entry in Definition.InitialComposition)
        {
            compoundsLeft.Add(entry.Key, entry.Value);
        }

        ApplyScale();

        // If it was split from a primary organelle, destroy it.
        if (IsDuplicate)
        {
            GD.PrintErr("ResetGrowth called on a duplicate organelle, " +
                "this is currently unsupported");

            // parentMicrobe.RemoveOrganelle(this);
        }
        else
        {
            WasSplit = false;
            SisterOrganelle = null;
        }
    }

    public void UpdateRenderPriority(int priority)
    {
        if (organelleMaterial == null)
            return;

        organelleMaterial.RenderPriority = priority;
    }

    /// <summary>
    ///   Returns the rotated position, as it should be in the colony.
    ///   Used for re-parenting shapes to other microbes
    /// </summary>
    public Vector3 RotatedPositionInsideColony(Vector3 shapePosition)
    {
        var rotation = Quat.Identity;
        if (ParentMicrobe?.Colony != null)
        {
            var parent = ParentMicrobe;

            // Get the rotation of all colony ancestors up to master
            while (parent != ParentMicrobe.Colony.Master)
            {
                if (parent == null)
                    throw new Exception("Reached a null parent microbe without finding the colony leader");

                rotation *= new Quat(parent.Transform.basis);
                parent = parent.ColonyParent;
            }
        }
        else
        {
            return shapePosition;
        }

        rotation = rotation.Normalized();

        // Transform the vector with the rotation quaternion
        shapePosition = rotation.Xform(shapePosition);
        return shapePosition;
    }

    /// <summary>
    ///   Re-parents the organelle shape to the "to" microbe.
    /// </summary>
    public void ReParentShapes(Microbe to, Vector3 offset)
    {
        if (to == currentShapesParent)
            return;

        if (ParentMicrobe == null || currentShapesParent == null)
            throw new InvalidOperationException("This organelle needs to be placed in a microbe first");

        // TODO: we are in trouble if ever the hex count mismatches with the shapes. It's fine if this can never happen
        // but a more bulletproof way would be to add code to at least detect and try to recover if there is no
        // matching hex for a shape
        // https://github.com/Revolutionary-Games/Thrive/issues/2504
        var hexes = Definition.GetRotatedHexes(Orientation).ToArray();

        for (int i = 0; i < shapes.Count; i++)
        {
            Vector3 shapePosition = ShapeTruePosition(hexes[i]);

            // Rotate the position of the organelle to its true position relative to the master
            shapePosition = RotatedPositionInsideColony(shapePosition);

            // Scale for bacteria physics.
            if (ParentMicrobe.CellTypeProperties.IsBacteria)
                shapePosition *= 0.4f;

            shapePosition += offset;

            var ownerId = shapes[i];
            var transform = new Transform(Quat.Identity, shapePosition);

            // Create a new owner id and apply the new position to it
            shapes[i] = currentShapesParent.CreateNewOwnerId(to, transform, ownerId);
            currentShapesParent.RemoveShapeOwner(ownerId);
        }

        foreach (var component in Components)
        {
            component.OnShapeParentChanged(to, offset);
        }

        currentShapesParent = to;
    }

    private static Color CalculateHSVForOrganelle(Color rawColour)
    {
        // Get hue saturation and brightness for the colour

        // According to stack overflow HSV and HSB are the same thing
        rawColour.ToHsv(out var hue, out var saturation, out var brightness);

        return Color.FromHsv(hue, saturation * 2, brightness);
    }

    private void FinishAttachToMicrobe()
    {
        // Graphical display
        if (Definition.LoadedScene != null)
        {
            SetupOrganelleGraphics();
        }

        // Physics
        ParentMicrobe!.Mass += Definition.Mass;

        // TODO: if organelles can grow while cells are in a colony this will be needed
        // Add the mass of the organelles to the colony master
        // if (ParentMicrobe.Colony != null && ParentMicrobe != ParentMicrobe.Colony.Master &&
        //     !IsLoadedFromSave)
        //     ParentMicrobe.Colony.Master.Mass += Definition.Mass;

        // We don't need preview cells to be collidable (as it can lag the editor if the cell is massive).
        if (!ParentMicrobe.IsForPreviewOnly)
            MakeCollisionShapes(ParentMicrobe.Colony?.Master ?? ParentMicrobe);

        if (Definition.Enzymes != null)
        {
            foreach (var entry in Definition.Enzymes)
            {
                var enzyme = SimulationParameters.Instance.GetEnzyme(entry.Key);

                StoredEnzymes[enzyme] = entry.Value;
            }
        }

        // Components
        components = new List<IOrganelleComponent>();

        foreach (var factory in Definition.ComponentFactories)
        {
            var component = factory.Create();

            if (component == null)
                throw new Exception("PlacedOrganelle component factory returned null");

            component.OnAttachToCell(this);

            components.Add(component);
        }

        growthValueDirty = true;
    }

    private Vector3 ShapeTruePosition(Hex parentOffset)
    {
        return Hex.AxialToCartesian(parentOffset) + Hex.AxialToCartesian(Position);
    }

    /// <summary>
    ///   Creates the collision shape(s) necessary for this organelle
    /// </summary>
    /// <param name="to">The microbe to add the shapes to</param>
    /// <remarks>
    ///   <para>
    ///     TODO: make this take into initial colony membership into account so that calling ReParentShapes twice
    ///     when loading a game is not necessary
    ///   </para>
    /// </remarks>
    private void MakeCollisionShapes(Microbe to)
    {
        currentShapesParent = to;

        float hexSize = Constants.DEFAULT_HEX_SIZE;

        // Scale the physics hex size down for bacteria
        if (ParentMicrobe!.CellTypeProperties.IsBacteria)
            hexSize *= 0.4f;

        // Add hex collision shapes
        foreach (Hex hex in Definition.GetRotatedHexes(Orientation))
        {
            var shape = new SphereShape();
            shape.Radius = hexSize * 2.0f;

            // The shape is in our parent so the final position is our
            // offset plus the hex offset
            Vector3 shapePosition = ShapeTruePosition(hex);

            // Scale for bacteria physics.
            if (ParentMicrobe.CellTypeProperties.IsBacteria)
                shapePosition *= 0.4f;

            // Create a transform for a shape position
            var transform = new Transform(Quat.Identity, shapePosition);
            var ownerId = to.CreateShapeOwnerWithTransform(transform, shape);
            shapes.Add(ownerId);
        }
    }

    private void RecalculateGrowthValue()
    {
        growthValueDirty = false;

        growthValue = 1.0f - CalculateCompoundsLeft() / Definition.OrganelleCost;
    }

    private void ApplyScale()
    {
        if (!Definition.ShouldScale)
            return;

        if (OrganelleGraphics != null)
            OrganelleGraphics.Scale = new Vector3(1 + GrowthValue, 1 + GrowthValue, 1 + GrowthValue);
    }

    private void UpdateColour()
    {
        var color = CalculateHSVForOrganelle(Colour);
        if (organelleSceneInstance is OrganelleMeshWithChildren organelleMeshWithChildren)
        {
            organelleMeshWithChildren.SetTintOfChildren(color);
        }

        organelleMaterial?.SetShaderParam("tint", color);

        needsColourUpdate = false;
    }

    private void UpdateDissolveEffect()
    {
        if (organelleSceneInstance is OrganelleMeshWithChildren organelleMeshWithChildren)
        {
            organelleMeshWithChildren.SetDissolveEffectOfChildren(dissolveEffectValue);
        }

        organelleMaterial?.SetShaderParam("dissolveValue", dissolveEffectValue);

        needsDissolveEffectUpdate = false;
    }

    private void SetupOrganelleGraphics()
    {
        organelleSceneInstance = (Spatial)Definition.LoadedScene!.Instance();

        // Store animation player for later use
        if (!string.IsNullOrEmpty(Definition.DisplaySceneAnimation))
        {
            OrganelleAnimation = organelleSceneInstance.GetNode<AnimationPlayer>(Definition.DisplaySceneAnimation);
        }

        // Store the material of the organelle to be updated
        organelleMaterial = organelleSceneInstance.GetMaterial(Definition.DisplaySceneModelPath);
        UpdateRenderPriority(Hex.GetRenderPriority(Position));

        // There is an intermediate node so that the organelle scene root rotation and scale work
        OrganelleGraphics = new Spatial();
        OrganelleGraphics.AddChild(organelleSceneInstance);

        AddChild(OrganelleGraphics);

        OrganelleGraphics.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
            Constants.DEFAULT_HEX_SIZE);

        // Position the intermediate node relative to origin of cell
        var transform = new Transform(Quat.Identity,
            Hex.AxialToCartesian(Position) + Definition.CalculateModelOffset());

        OrganelleGraphics.Transform = transform;

        // For some reason MathUtils.CreateRotationForOrganelle(Orientation) in the above transform doesn't work
        OrganelleGraphics.RotateY(Orientation * -60 * MathUtils.DEGREES_TO_RADIANS);
    }
}
