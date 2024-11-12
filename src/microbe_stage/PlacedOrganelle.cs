﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   An organelle that has been placed in a simulated microbe. Very different from <see cref="OrganelleTemplate"/> and
///   <see cref="OrganelleDefinition"/>.
/// </summary>
public class PlacedOrganelle : IPositionedOrganelle, ICloneable
{
    private readonly List<Compound> tempCompoundsToProcess = new();

    private bool growthValueDirty = true;
    private float growthValue;

    /// <summary>
    ///   The compounds still needed to divide. Initialized from Definition.InitialComposition
    /// </summary>
    [JsonProperty]
    private Dictionary<Compound, float> compoundsLeft;

    private Quaternion cachedExternalOrientation = Quaternion.Identity;
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
    public Node3D? OrganelleGraphics { get; private set; }

    /// <summary>
    ///   Graphics metadata that is set to valid data if <see cref="OrganelleGraphics"/> is not null.
    /// </summary>
    [JsonIgnore]
    public LoadedSceneWithModelInfo LoadedGraphicsSceneInfo { get; private set; }

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

    [JsonProperty]
    public OrganelleUpgrades? Upgrades { get; private set; }

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

    /// <summary>
    ///   Gives organelles more compounds to grow (or takes free compounds).
    ///   If <see cref="allowedCompoundUse"/> goes to 0 stops early and doesn't use any more compounds.
    /// </summary>
    /// <returns>True when this has grown a bit and visuals transform needs to be re-applied</returns>
    public bool GrowOrganelle(CompoundBag compounds, ref float allowedCompoundUse, ref float freeCompoundsLeft,
        bool reverseCompoundsLeftOrder)
    {
        float totalTaken = 0;

        // Find compounds that should be processed. Sadly it seems that this always needs to loop all even if the
        // compound usage limit will cut this short, as otherwise the consume in reverse mode isn't possible to make
        // without allocating extra memory
        foreach (var entry in compoundsLeft)
        {
            if (entry.Value <= 0)
                continue;

            tempCompoundsToProcess.Add(entry.Key);
        }

        if (tempCompoundsToProcess.Count > 0)
        {
            int count = tempCompoundsToProcess.Count;

            if (!reverseCompoundsLeftOrder)
            {
                for (int i = 0; i < count; ++i)
                {
                    // This breaks when out of compound use. A separate helper method is used to make these two loops
                    // share their logic without needing a temporary list
                    if (!GrowWithCompoundType(compounds, ref allowedCompoundUse, ref freeCompoundsLeft,
                            tempCompoundsToProcess[i], ref totalTaken))
                    {
                        break;
                    }
                }
            }
            else
            {
                for (int i = count - 1; i >= 0; --i)
                {
                    if (!GrowWithCompoundType(compounds, ref allowedCompoundUse, ref freeCompoundsLeft,
                            tempCompoundsToProcess[i], ref totalTaken))
                    {
                        break;
                    }
                }
            }

            tempCompoundsToProcess.Clear();
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
    /// <param name="visualSceneData">Data related to this scene to access extra metadata</param>
    public void ReportCreatedGraphics(Node3D visualsInstance, in LoadedSceneWithModelInfo visualSceneData)
    {
        if (OrganelleGraphics != null)
            throw new InvalidOperationException("Can't set organelle graphics multiple times");

        OrganelleGraphics = visualsInstance;
        LoadedGraphicsSceneInfo = visualSceneData;

        // Store animation player for later use
        if (visualSceneData.AnimationPlayerPath != null)
        {
            OrganelleAnimation = visualsInstance.GetNode<AnimationPlayer>(visualSceneData.AnimationPlayerPath);
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

    public Transform3D CalculateVisualsTransform()
    {
        var scale = CalculateTransformScale();

        return new Transform3D(new Basis(MathUtils.CreateRotationForOrganelle(1 * Orientation)).Scaled(scale),
            Hex.AxialToCartesian(Position) + Definition.ModelOffset);

        // TODO: check is this still needed
        // For some reason MathUtils.CreateRotationForOrganelle(Orientation) in the above transform doesn't work
        // OrganelleGraphics.RotateY(Orientation * -60 * MathUtils.DEGREES_TO_RADIANS);
    }

    public Transform3D CalculateVisualsTransformExternal(Vector3 externalPosition, Quaternion orientation)
    {
        var scale = CalculateTransformScale();

        cachedExternalOrientation = orientation;
        cachedExternalPosition = externalPosition;

        // TODO: check that the rotation of ModelOffset works correctly here (also in
        // CalculateVisualsTransformExternalCached)

        return new Transform3D(new Basis(orientation).Scaled(scale),
            externalPosition + orientation * Definition.ModelOffset);
    }

    /// <summary>
    ///   Variant of <see cref="CalculateVisualsTransformExternal"/> that uses cached external position values. This is
    ///   required as the <see cref="MicrobeReproductionSystem"/> must re-apply scale (and it would massively
    ///   complicate things there if it needed to re-calculate this information)
    /// </summary>
    /// <returns>The organelle transform</returns>
    public Transform3D CalculateVisualsTransformExternalCached()
    {
        var scale = CalculateTransformScale();

        // TODO: check that the rotation of ModelOffset works correctly here
        return new Transform3D(new Basis(cachedExternalOrientation).Scaled(scale),
            cachedExternalPosition + cachedExternalOrientation * Definition.ModelOffset);
    }

    public (Vector3 Position, Quaternion Rotation) CalculatePhysicsExternalTransform(Vector3 externalPosition,
        Quaternion orientation, bool isBacteria)
    {
        // The shape needs to be rotated 90 degrees to point forward for (so that the pilus is not a vertical column
        // but is instead a stabby thing)
        var extraRotation = new Quaternion(new Vector3(1, 0, 0), MathF.PI * 0.5f);

        // Maybe should have a variable for physics shape offset if different organelles need different things
        var offset = new Vector3(0, 0, -1.0f);

        // Need to adjust physics position for bacteria scale
        if (isBacteria)
        {
            // TODO: find the root cause and fix properly why this kind of very specific tweak is needed
            var length = externalPosition.Length() * Constants.BACTERIA_PILUS_ATTACH_ADJUSTMENT_MULTIPLIER;

            offset.Z += length;
        }

        return (externalPosition + orientation * offset, orientation * extraRotation);
    }

    /// <summary>
    ///   Clones this organelle, but doesn't preserve visual and graphics state. The new instance can be added to a
    ///   different microbe to finish initializing it.
    /// </summary>
    /// <returns>A cloned instance based on the same core data but no full runtime state</returns>
    public object Clone()
    {
        return new PlacedOrganelle(Definition, Position, Orientation, compoundsLeft.CloneShallow(),
            (OrganelleUpgrades?)Upgrades?.Clone())
        {
            WasSplit = WasSplit,
            IsDuplicate = IsDuplicate,
            SisterOrganelle = SisterOrganelle,
        };
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

    private Vector3 CalculateTransformScale()
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
        var scale = Definition.GetUpgradesSizeModification(Upgrades) + new Vector3(growth, growth, growth);

        return scale;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool GrowWithCompoundType(CompoundBag compounds, ref float allowedCompoundUse, ref float freeCompoundsLeft,
        Compound compoundType, ref float totalTaken)
    {
        var amountNeeded = compoundsLeft[compoundType];

        if (amountNeeded <= 0.0f)
            return true;

        if (allowedCompoundUse <= 0)
            return false;

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
            compounds.GetCompoundAmount(compoundType) - Constants.ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST;

        if (amountAvailable > MathUtils.EPSILON)
        {
            // We can take some
            var amountToTake = MathF.Min(allowedUseAmount, amountAvailable);

            usedAmount += compounds.TakeCompound(compoundType, amountToTake);
        }

        if (usedAmount < MathUtils.EPSILON)
            return true;

        allowedCompoundUse -= usedAmount;

        var left = amountNeeded - usedAmount;

        if (left < 0.0001f)
            left = 0;

        compoundsLeft[compoundType] = left;

        totalTaken += usedAmount;
        return false;
    }

    private void RecalculateGrowthValue()
    {
        growthValueDirty = false;

        growthValue = 1.0f - CalculateCompoundsLeft() / Definition.OrganelleCost;
    }
}
