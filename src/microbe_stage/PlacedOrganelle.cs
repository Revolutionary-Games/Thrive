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
    private List<uint> shapes = new List<uint>();

    private bool needsColourUpdate = true;

    [JsonProperty]
    private Color colour = new Color(1, 1, 1, 1);

    private bool growthValueDirty = true;
    private float growthValue;

    private Microbe currentShapesParent;

    /// <summary>
    ///   Used to update the tint
    /// </summary>
    private ShaderMaterial organelleMaterial;

    /// <summary>
    ///   The compounds still needed to divide. Initialized from Definition.InitialComposition
    /// </summary>
    [JsonProperty]
    private Dictionary<Compound, float> compoundsLeft;

    public PlacedOrganelle(OrganelleDefinition definition, Hex position, int orientation)
    {
        Definition = definition;
        Position = position;
        Orientation = orientation;
    }

    public PlacedOrganelle()
    {
    }

    public OrganelleDefinition Definition { get; set; }

    public Hex Position { get; set; }

    public int Orientation { get; set; }

    [JsonProperty]
    public Microbe ParentMicrobe { get; private set; }

    /// <summary>
    ///   The graphics child node of this organelle
    /// </summary>
    [JsonIgnore]
    public Spatial OrganelleGraphics { get; private set; }

    /// <summary>
    ///   Animation player this organelle has or null
    /// </summary>
    [JsonIgnore]
    public AnimationPlayer OrganelleAnimation { get; private set; }

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

    public PlacedOrganelle SisterOrganelle { get; set; }

    /// <summary>
    ///   The components instantiated for this placed organelle
    /// </summary>
    [JsonIgnore]
    public List<IOrganelleComponent> Components { get; private set; }

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

    /// <summary>
    ///   True if this is an agent vacuole. Number of agent vacuoles
    ///   determine how often a cell can shoot toxins.
    /// </summary>
    [JsonIgnore]
    public bool IsAgentVacuole => HasComponent<AgentVacuoleComponent>();

    [JsonIgnore]
    public bool IsBindingAgent => HasComponent<BindingAgentComponent>();

    public bool IsLoadedFromSave { get; set; }

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
    ///   Guards against adding this to the scene not through OnAddedToMicrobe
    /// </summary>
    public override void _Ready()
    {
        if (Definition == null)
            GD.PrintErr("Definition of PlacedOrganelle is null");

        if (ParentMicrobe == null)
            GD.PrintErr("PlacedOrganelle not added to scene through OnAddedToMicrobe");

        if (IsLoadedFromSave)
            FinishAttachToMicrobe();

        ApplyScale();
    }

    /// <summary>
    ///   Called by a microbe when this organelle has been added to it
    /// </summary>
    public void OnAddedToMicrobe(Microbe microbe)
    {
        if (Definition == null)
            throw new Exception("PlacedOrganelle has no definition set");

        if (ParentMicrobe != null)
            throw new InvalidOperationException("PlacedOrganelle is already in a microbe");

        // Store parameters
        ParentMicrobe = microbe;

        // Grab the species colour for us
        Colour = microbe.Species.Colour;

        ParentMicrobe.OrganelleParent.AddChild(this);

        FinishAttachToMicrobe();

        ResetGrowth();
    }

    /// <summary>
    ///   Called by a microbe when this organelle has been removed from it
    /// </summary>
    public void OnRemovedFromMicrobe()
    {
        ParentMicrobe.OrganelleParent.RemoveChild(this);

        // Remove physics
        ParentMicrobe.Mass -= Definition.Mass;

        // Remove our sub collisions
        foreach (var shape in shapes)
        {
            currentShapesParent.RemoveShapeOwner(shape);
        }

        currentShapesParent = null;
        shapes.Clear();

        // Remove components
        foreach (var component in Components)
        {
            component.OnDetachFromCell(this);
        }

        Components = null;

        ParentMicrobe = null;
    }

    /// <summary>
    ///   Called by Microbe.Update
    /// </summary>
    public void Update(float delta)
    {
        // Update each OrganelleComponent
        foreach (var component in Components)
        {
            component.Update(delta);
        }

        // If the organelle is supposed to be another color.
        if (needsColourUpdate)
        {
            UpdateColour();
        }
    }

    /// <summary>
    ///   Gives organelles more compounds to grow
    /// </summary>
    public void GrowOrganelle(CompoundBag compounds)
    {
        float totalTaken = 0;
        var keys = new List<Compound>(compoundsLeft.Keys);

        foreach (var key in keys)
        {
            var amountNeeded = compoundsLeft[key];

            if (amountNeeded <= 0.0f)
                continue;

            // Take compounds if the cell has what we need
            // ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST controls how
            // much of a certain compound must exist before we take
            // some
            var amountAvailable = compounds.GetCompoundAmount(key)
                - Constants.ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST;

            if (amountAvailable <= 0.0f)
                continue;

            // We can take some
            var amountToTake = Mathf.Min(amountNeeded, amountAvailable);

            var amount = compounds.TakeCompound(key, amountToTake);
            var left = amountNeeded - amount;

            if (left < 0.0001)
                left = 0;

            compoundsLeft[key] = left;

            totalTaken += amount;
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
    ///   Calculates how much compounds this organelle has absorbed
    ///   already, adds to the dictionary
    /// </summary>
    public float CalculateAbsorbedCompounds(Dictionary<Compound, float> result)
    {
        float totalAbsorbed = 0;

        foreach (var entry in compoundsLeft)
        {
            var amountLeft = entry.Value;

            var amountTotal = Definition.InitialComposition[entry.Key];

            var absorbed = amountTotal - amountLeft;

            float alreadyInResult = 0;

            if (result.ContainsKey(entry.Key))
                alreadyInResult = result[entry.Key];

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
        compoundsLeft = new Dictionary<Compound, float>();

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

    /// <summary>
    ///  Re-parents the organelle shape to the "to" microbe.
    /// </summary>
    public void ReParentShapes(Microbe to, Vector3 offset)
    {
        if (to == currentShapesParent)
            return;

        // TODO: we are in trouble if ever the hex count mismatches with the shapes. It's fine if this can never happen
        // but a more bulletproof way would be to add code to at least detect and try to recover if there is no
        // matching hex for a shape
        // https://github.com/Revolutionary-Games/Thrive/issues/2504
        var hexes = Definition.GetRotatedHexes(Orientation).ToArray();

        for (int i = 0; i < shapes.Count; i++)
        {
            var rotation = Quat.Identity;
            Vector3 shapePosition = ShapeTruePosition(hexes[i]);
            if (ParentMicrobe.Colony != null)
            {
                var parent = ParentMicrobe;

                // Get the rotation of all colony ancestors up to master
                while (parent != ParentMicrobe.Colony.Master)
                {
                    rotation *= new Quat(parent.Transform.basis);
                    parent = parent.ColonyParent;
                }
            }

            rotation = rotation.Normalized();

            // Transform the vector with the rotation quaternion
            shapePosition = rotation.Xform(shapePosition);

            // Scale for bacteria physics.
            if (ParentMicrobe.Species.IsBacteria)
                shapePosition *= 0.5f;

            shapePosition += offset;

            var transform = new Transform(Quat.Identity, shapePosition);

            var ownerId = shapes[i];

            var shape = currentShapesParent.ShapeOwnerGetShape(ownerId, 0);
            var newOwnerId = to.CreateShapeOwner(shape);
            to.ShapeOwnerAddShape(newOwnerId, shape);
            to.ShapeOwnerSetTransform(newOwnerId, transform);

            shapes[i] = newOwnerId;

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
        // TODO: shouldn't we also add the mass to the colony master?
        ParentMicrobe.Mass += Definition.Mass;

        MakeCollisionShapes(ParentMicrobe.Colony?.Master ?? ParentMicrobe);

        // Components
        Components = new List<IOrganelleComponent>();

        foreach (var factory in Definition.ComponentFactories)
        {
            var component = factory.Create();

            if (component == null)
                throw new Exception("PlacedOrganelle component factory returned null");

            component.OnAttachToCell(this);

            Components.Add(component);
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
        if (ParentMicrobe.Species.IsBacteria)
            hexSize *= 0.5f;

        // Add hex collision shapes
        foreach (Hex hex in Definition.GetRotatedHexes(Orientation))
        {
            var shape = new SphereShape();
            shape.Radius = hexSize * 2.0f;

            var ownerId = to.CreateShapeOwner(shape);

            // This is needed to actually add the shape
            to.ShapeOwnerAddShape(ownerId, shape);

            // TODO: merge this common logic with ReParentShapes to a helper method
            // https://github.com/Revolutionary-Games/Thrive/issues/2504

            // The shape is in our parent so the final position is our
            // offset plus the hex offset
            Vector3 shapePosition = ShapeTruePosition(hex);

            // Scale for bacteria physics.
            if (ParentMicrobe.Species.IsBacteria)
                shapePosition *= 0.5f;

            var transform = new Transform(Quat.Identity, shapePosition);
            to.ShapeOwnerSetTransform(ownerId, transform);

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
        organelleMaterial?.SetShaderParam("tint", CalculateHSVForOrganelle(Colour));

        needsColourUpdate = false;
    }

    private void SetupOrganelleGraphics()
    {
        var organelleSceneInstance = (Spatial)Definition.LoadedScene.Instance();

        // Store animation player for later use
        if (!string.IsNullOrEmpty(Definition.DisplaySceneAnimation))
        {
            OrganelleAnimation = organelleSceneInstance.GetNode<AnimationPlayer>(Definition.DisplaySceneAnimation);
        }

        // Store the material of the organelle to be updated
        organelleMaterial = organelleSceneInstance.GetMaterial(Definition.DisplaySceneAnimation);

        // There is an intermediate node so that the organelle scene root rotation and scale work
        OrganelleGraphics = new Spatial();
        OrganelleGraphics.AddChild(organelleSceneInstance);

        AddChild(OrganelleGraphics);

        OrganelleGraphics.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
            Constants.DEFAULT_HEX_SIZE);

        // Position the intermediate node relative to origin of cell
        var transform = new Transform(Quat.Identity,
            Hex.AxialToCartesian(Position) + Definition.CalculateModelOffset());
        if (organelleMaterial != null)
        {
            organelleMaterial.RenderPriority = Hex.GetRenderPriority(Position);
        }

        OrganelleGraphics.Transform = transform;

        // For some reason MathUtils.CreateRotationForOrganelle(Orientation) in the above transform doesn't work
        OrganelleGraphics.RotateY(Orientation * -60 * MathUtils.DEGREES_TO_RADIANS);
    }
}
