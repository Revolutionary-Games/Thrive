using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   An organelle that has been placed in a simulated microbe. Very different from <see cref="OrganelleTemplate"/> and
///   <see cref="OrganelleDefinition"/>.
/// </summary>
public class PlacedOrganelle : IPositionedOrganelle
{
    private bool growthValueDirty = true;
    private float growthValue;

    /// <summary>
    ///   The compounds still needed to divide. Initialized from Definition.InitialComposition
    /// </summary>
    [JsonProperty]
    private Dictionary<Compound, float> compoundsLeft;

    private Quat cachedExternalOrientation = Quat.Identity;
    private Vector3 cachedExternalPosition = Vector3.Zero;

    public PlacedOrganelle(OrganelleDefinition definition, Hex position, int orientation, OrganelleUpgrades? upgrades)
    {
        Definition = definition;
        Position = position;
        Orientation = orientation;

        // Upgrades must be applied before initializing the components
        Upgrades = upgrades;

        InitializeComponents();

        compoundsLeft ??= new Dictionary<Compound, float>();
        ResetGrowth();
    }

    /// <summary>
    ///   JSON constructor that avoid re-running some core logic
    /// </summary>
    [JsonConstructor]
    public PlacedOrganelle(OrganelleDefinition definition, Hex position, int orientation,
        Dictionary<Compound, float> compoundsLeft, OrganelleUpgrades? upgrades)
    {
        Definition = definition;
        Position = position;
        Orientation = orientation;
        this.compoundsLeft = compoundsLeft;
        Upgrades = upgrades;

        // TODO: figure out if re-creating components on loading a save is the right approach
        InitializeComponents();
    }

    public OrganelleDefinition Definition { get; }

    public Hex Position { get; set; }

    public int Orientation { get; set; }

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

    public PlacedOrganelle? SisterOrganelle { get; set; }

    /// <summary>
    ///   The components instantiated for this placed organelle. Not saved as components are re-created on save load.
    ///   See the <see cref="Components.OrganelleContainer"/> comments about saving.
    /// </summary>
    [JsonIgnore]
    public List<IOrganelleComponent> Components { get; } = new();

    /// <summary>
    ///   The upgrades that this organelle has which affect how the components function
    /// </summary>
    [JsonProperty]
    public OrganelleUpgrades? Upgrades { get; private set; }

    /// <summary>
    ///   Computes the total storage capacity of this organelle
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
    ///   Can be set by organelle components to override the enzymes returned by <see cref="GetEnzymes"/>. This is
    ///   not saved right now as this is only used by <see cref="LysosomeComponent"/> which will re-add when the
    ///   component is re-initialized.
    /// </summary>
    [JsonIgnore]
    public Dictionary<Enzyme, int>? OverriddenEnzymes { get; set; }

    public static Color CalculateHSVForOrganelle(Color rawColour)
    {
        // Get hue saturation and brightness for the colour

        // According to stack overflow HSV and HSB are the same thing
        rawColour.ToHsv(out var hue, out var saturation, out var brightness);

        return Color.FromHsv(hue, saturation * 2, brightness);
    }

    /// <summary>
    ///   Gets the effective enzymes provided by this organelle. TODO: allow this to change over time, right now only
    ///   when organelles are attached this is effective
    /// </summary>
    /// <returns>Effective enzyme data for this organelle</returns>
    public IReadOnlyDictionary<Enzyme, int> GetEnzymes()
    {
        if (OverriddenEnzymes != null)
            return OverriddenEnzymes;

        return Definition.Enzymes;
    }

    // TODO: remove if this stays unused
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
    ///   Gives organelles more compounds to grow (or takes free compounds).
    ///   If <see cref="allowedCompoundUse"/> goes to 0 stops early and doesn't use any more compounds.
    /// </summary>
    /// <returns>True when this has grown a bit and visuals transform needs to be re-applied</returns>
    public bool GrowOrganelle(CompoundBag compounds, ref float allowedCompoundUse, ref float freeCompoundsLeft,
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

            return true;
        }

        return false;
    }

    /// <summary>
    ///   Called by <see cref="MicrobeVisualsSystem"/> when graphics have been created for this organelle
    /// </summary>
    /// <param name="visualsInstance">The graphics initialized from this organelle's type's specified scene</param>
    public void ReportCreatedGraphics(Spatial visualsInstance)
    {
        if (OrganelleGraphics != null)
            throw new InvalidOperationException("Can't set organelle graphics multiple times");

        OrganelleGraphics = visualsInstance;

        // Store animation player for later use
        if (!string.IsNullOrEmpty(Definition.DisplaySceneAnimation))
        {
            OrganelleAnimation = visualsInstance.GetNode<AnimationPlayer>(Definition.DisplaySceneAnimation);
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
    ///   Resets the state. Used after dividing. Note that the organelle container visuals need to be marked dirty for
    ///   the sizing to apply
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

        if (IsDuplicate)
        {
            GD.PrintErr("ResetGrowth called on a duplicate organelle, this is not allowed");
        }
        else
        {
            WasSplit = false;
            SisterOrganelle = null;
        }
    }

    public Transform CalculateVisualsTransform()
    {
        var scale = CalculateTransformScale();

        return new Transform(
            new Basis(MathUtils.CreateRotationForOrganelle(1 * Orientation)).Scaled(new Vector3(scale, scale, scale)),
            Hex.AxialToCartesian(Position) + Definition.ModelOffset);

        // TODO: check is this still needed
        // For some reason MathUtils.CreateRotationForOrganelle(Orientation) in the above transform doesn't work
        // OrganelleGraphics.RotateY(Orientation * -60 * MathUtils.DEGREES_TO_RADIANS);
    }

    public Transform CalculateVisualsTransformExternal(Vector3 externalPosition, Quat orientation)
    {
        var scale = CalculateTransformScale();

        cachedExternalOrientation = orientation;
        cachedExternalPosition = externalPosition;

        // TODO: check that the rotation of ModelOffset works correctly here (also in
        // CalculateVisualsTransformExternalCached)
        return new Transform(new Basis(orientation).Scaled(new Vector3(scale, scale, scale)),
            externalPosition + orientation.Xform(Definition.ModelOffset));
    }

    /// <summary>
    ///   Variant of <see cref="CalculateVisualsTransformExternal"/> that uses cached external position values. This is
    ///   required as the <see cref="MicrobeReproductionSystem"/> must re-apply scale (and it would massively
    ///   complicate things there if it needed to re-calculate this information)
    /// </summary>
    /// <returns>The organelle transform</returns>
    public Transform CalculateVisualsTransformExternalCached()
    {
        var scale = CalculateTransformScale();

        // TODO: check that the rotation of ModelOffset works correctly here
        return new Transform(new Basis(cachedExternalOrientation).Scaled(new Vector3(scale, scale, scale)),
            cachedExternalPosition + cachedExternalOrientation.Xform(Definition.ModelOffset));
    }

    public (Vector3 Position, Quat Rotation) CalculatePhysicsExternalTransform(Vector3 externalPosition,
        Quat orientation, bool isBacteria)
    {
        // The shape needs to be rotated 90 degrees to point forward for (so that the pilus is not a vertical column
        // but is instead a stabby thing)
        var extraRotation = new Quat(new Vector3(1, 0, 0), Mathf.Pi * 0.5f);

        // Maybe should have a variable for physics shape offset if different organelles need different things
        var offset = new Vector3(0, 0, -1.0f);

        // Need to adjust physics position for bacteria scale
        if (isBacteria)
        {
            // TODO: find the root cause and fix properly why this kind of very specific tweak is needed
            var length = externalPosition.Length() * 0.575f;

            offset.z += length;
        }

        return (externalPosition + orientation.Xform(offset), orientation * extraRotation);
    }

    private void InitializeComponents()
    {
        foreach (var factory in Definition.ComponentFactories)
        {
            var component = factory.Create();

            if (component == null)
                throw new Exception("PlacedOrganelle component factory returned null");

            component.OnAttachToCell(this);

            Components.Add(component);
        }
    }

    private float CalculateTransformScale()
    {
        float growth;
        if (Definition.ShouldScale)
        {
            growth = GrowthValue;
        }
        else
        {
            growth = 0;
        }

        // TODO: organelle scale used to be 1 + GrowthValue before the refactor, and now this is probably *more*
        // intended way, but might be worse looking than before
        return Constants.DEFAULT_HEX_SIZE + growth;
    }

    private void RecalculateGrowthValue()
    {
        growthValueDirty = false;

        growthValue = 1.0f - CalculateCompoundsLeft() / Definition.OrganelleCost;
    }
}
