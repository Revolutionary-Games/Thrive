using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   An organelle that has been placed in a microbe.
/// </summary>
[JsonConverter(typeof(PlacedOrganelleConverter))]
public class PlacedOrganelle : Spatial, IPositionedOrganelle
{
    [JsonIgnore]
    private List<uint> shapes = new List<uint>();

    private bool needsColourUpdate = true;
    private Color colour = new Color(1, 1, 1, 1);

    private bool growthValueDirty = true;
    private float growthValue = 0.0f;

    /// <summary>
    ///   Used to update the tint
    /// </summary>
    private ShaderMaterial organelleMaterial;

    /// <summary>
    ///   The compounds still needed to divide. Initialized from Definition.InitialComposition
    /// </summary>
    private Dictionary<string, float> compoundsLeft;

    public OrganelleDefinition Definition { get; set; }

    public Hex Position { get; set; }

    public int Orientation { get; set; }

    [JsonIgnore]
    public Microbe ParentMicrobe { get; private set; }

    /// <summary>
    ///   The graphics child node of this organelle
    /// </summary>
    public Spatial OrganelleGraphics { get; private set; }

    /// <summary>
    ///   Animation player this organelle has or null
    /// </summary>
    public AnimationPlayer OrganelleAnimation { get; private set; }

    /// <summary>
    ///   The tint colour of this organelle.
    /// </summary>
    public Color Colour
    {
        get
        {
            return colour;
        }
        set
        {
            colour = value;
            needsColourUpdate = true;
        }
    }

    /// <summary>
    ///   Value between 0 and 1 on how far along to splitting this organelle is
    /// </summary>
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
    public bool WasSplit { get; set; } = false;

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
    public bool IsDuplicate { get; set; } = false;

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
    public bool IsAgentVacuole
    {
        get
        {
            return HasComponent<AgentVacuoleComponent>();
        }
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
            if (component as T != null)
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

        // Graphical display
        if (Definition.LoadedScene != null)
        {
            // There is an intermediate node so that the organelle scene root rotation and scale work
            OrganelleGraphics = new Spatial();
            var organelleSceneInstance = (Spatial)Definition.LoadedScene.Instance();

            AddChild(OrganelleGraphics);

            OrganelleGraphics.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
                Constants.DEFAULT_HEX_SIZE);

            var transform = new Transform(MathUtils.CreateRotationForOrganelle(Orientation),
                Definition.CalculateModelOffset());
            OrganelleGraphics.Transform = transform;

            // Store the material of the organelle to be updated
            GeometryInstance geometry;

            // Fetch the actual model from the scene to get at the material we set the tint on
            if (string.IsNullOrEmpty(Definition.DisplaySceneModelPath))
            {
                geometry = (GeometryInstance)organelleSceneInstance;
            }
            else
            {
                geometry = organelleSceneInstance.GetNode<GeometryInstance>(Definition.DisplaySceneModelPath);
            }

            // Store animation player for later use
            if (!string.IsNullOrEmpty(Definition.DisplaySceneAnimation))
            {
                OrganelleAnimation = organelleSceneInstance.GetNode<AnimationPlayer>(Definition.DisplaySceneAnimation);
            }

            organelleMaterial = (ShaderMaterial)geometry.MaterialOverride;

            OrganelleGraphics.AddChild(organelleSceneInstance);
        }

        // Position relative to origin of cell
        RotateY(Orientation * 60);
        Translation = Hex.AxialToCartesian(Position);

        float hexSize = Constants.DEFAULT_HEX_SIZE;

        // Scale the physics hex size down for bacteria
        if (microbe.Species.IsBacteria)
            hexSize *= 0.5f;

        // Physics
        ParentMicrobe.Mass += Definition.Mass;

        // Add hex collision shapes
        foreach (Hex hex in Definition.GetRotatedHexes(Orientation))
        {
            var shape = new SphereShape();
            shape.Radius = hexSize * 2.0f;

            var ownerId = ParentMicrobe.CreateShapeOwner(shape);

            // This is needed to actually add the shape
            ParentMicrobe.ShapeOwnerAddShape(ownerId, shape);

            // The shape is in our parent so the final position is our
            // offset plus the hex offset
            Vector3 shapePosition = Hex.AxialToCartesian(hex) + Translation;

            // Scale for bacteria physics.
            if (microbe.Species.IsBacteria)
                shapePosition *= 0.5f;

            var transform = new Transform(Quat.Identity, shapePosition);
            ParentMicrobe.ShapeOwnerSetTransform(ownerId, transform);

            shapes.Add(ownerId);
        }

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
            ParentMicrobe.RemoveShapeOwner(shape);
        }

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
        var keys = new List<string>(compoundsLeft.Keys);

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
    public float CalculateAbsorbedCompounds(Dictionary<string, float> result)
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
        compoundsLeft = new Dictionary<string, float>();

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

    private static Color CalculateHSLForOrganelle(Color rawColour)
    {
        // Get hue saturation and brightness for the colour
        float saturation = 0;
        float brightness = 0;
        float hue = 0;

        // According to stack overflow HSV and HSB are the same thing
        rawColour.ToHsv(out hue, out saturation, out brightness);

        return Color.FromHsv(hue, saturation * 2, brightness);
    }

    private void RecalculateGrowthValue()
    {
        growthValueDirty = false;

        growthValue = 1.0f - (CalculateCompoundsLeft() / Definition.OrganelleCost);
    }

    private void ApplyScale()
    {
        // Nucleus isn't scaled
        if (HasComponent<NucleusComponent>())
            return;

        Scale = new Vector3(1 + GrowthValue, 1 + GrowthValue, 1 + GrowthValue);
    }

    private void UpdateColour()
    {
        if (organelleMaterial != null)
        {
            organelleMaterial.SetShaderParam("tint", CalculateHSLForOrganelle(Colour));
        }

        needsColourUpdate = false;
    }
}

/// <summary>
///   Custom serializer for PlacedOrganelle and OrganelleTemplate
/// </summary>
internal class PlacedOrganelleConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(PlacedOrganelle) ||
            objectType == typeof(OrganelleTemplate);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
        JsonSerializer serializer)
    {
        // TODO: implement reading
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            serializer.Serialize(writer, null);
            return;
        }

        // Get all properties
        var properties = value.GetType().GetProperties().Where(
            (p) => !p.CustomAttributes.Any(
                (a) => a.AttributeType == typeof(JsonIgnoreAttribute)));

        // And fields
        var fields = value.GetType().GetFields().Where(
            (p) => !p.CustomAttributes.Any(
                (a) => a.AttributeType == typeof(JsonIgnoreAttribute)));

        writer.WriteStartObject();

        foreach (var property in properties)
        {
            writer.WritePropertyName(property.Name);

            // Use default serializer on everything except Definition (definition is a field)
            serializer.Serialize(writer, property.GetValue(value, null));
        }

        foreach (var field in fields)
        {
            writer.WritePropertyName(field.Name);

            // Use default serializer on everything except Definition (definition is a field)
            if (field.Name == "Definition")
            {
                serializer.Serialize(writer,
                    ((OrganelleDefinition)field.GetValue(value)).InternalName);
            }
            else
            {
                serializer.Serialize(writer, field.GetValue(value));
            }
        }

        writer.WriteEndObject();
    }
}
