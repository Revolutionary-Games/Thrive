using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

public class CellTypeEditsFacade : EditsFacadeBase, IReadOnlyCellTypeDefinition,
    IReadOnlyOrganelleLayout<IReadOnlyOrganelleTemplate>
{
    private readonly OrganelleDefinition nucleus;

    private readonly List<IReadOnlyOrganelleTemplate> removedOrganelles = new();
    private readonly List<OrganelleWithOriginalReference> addedOrganelles = new();

    /// <summary>
    ///   Because we need to track initial data of organelles, we need to always use proxy objects with extra data, so
    ///   we cache them for efficiency.
    /// </summary>
    private readonly Stack<OrganelleWithOriginalReference> unusedOrganelles = new();

    /// <summary>
    ///   This is not readonly to allow reusing object instances of this
    /// </summary>
    private IReadOnlyCellTypeDefinition originalCell;

    private MembraneType? newMembrane;
    private bool overrideMembrane;

    private float newMembraneRigidity;
    private bool overrideMembraneRigidity;

    private bool overrideColour;

    public CellTypeEditsFacade(IReadOnlyCellTypeDefinition originalCell,
        OrganelleDefinition? nucleusDefinition = null)
    {
        this.originalCell = originalCell;
        nucleus = nucleusDefinition ?? SimulationParameters.Instance.GetOrganelleType("nucleus");
    }

    public IReadOnlyOrganelleLayout<IReadOnlyOrganelleTemplate> Organelles => this;

    public MembraneType MembraneType
    {
        get
        {
            ResolveDataIfDirty();
            return overrideMembrane && newMembrane != null ? newMembrane : originalCell.MembraneType;
        }
    }

    public float MembraneRigidity
    {
        get
        {
            ResolveDataIfDirty();
            return overrideMembraneRigidity ? newMembraneRigidity : originalCell.MembraneRigidity;
        }
    }

    public Color Colour
    {
        get => overrideColour ? field : originalCell.Colour;
        set
        {
            overrideColour = true;
            field = value;
        }
    }

    /// <summary>
    ///   Checks if this has a nucleus. Note that this is pretty inefficient as this needs to loop all the
    ///   organelles. And allocates an enumerator.
    /// </summary>
    public bool IsBacteria
    {
        get
        {
            foreach (var organelle in Organelles)
            {
                if (organelle.Definition == nucleus)
                    return false;
            }

            return true;
        }
    }

    public int MPCost => originalCell.MPCost;
    public string CellTypeName => originalCell.CellTypeName;
    public string ReadableName => CellTypeName;

    /// <summary>
    ///   For now, there's no action that changes what a type was split from, so we just forward this
    /// </summary>
    public string? SplitFromTypeName => originalCell.SplitFromTypeName;

    public float SpecializationBonus =>
        throw new NotSupportedException("This class doesn't dynamically recalculate the specialization bonus");

    // TODO: check that this is right (there might sometimes be too many items in removedOrganelles)
    // Though this seems to not be relied on currently
    public int Count => originalCell.Organelles.Count + addedOrganelles.Count - removedOrganelles.Count;

    public IEnumerator<IReadOnlyOrganelleTemplate> GetEnumerator()
    {
        ResolveDataIfDirty();
        return new OrganelleEnumerator(this);
    }

    public IReadOnlyOrganelleTemplate? GetElementAt(Hex location, List<Hex> temporaryHexesStorage)
    {
        ResolveDataIfDirty();
        var originalItem = originalCell.Organelles.GetElementAt(location, temporaryHexesStorage);

        if (originalItem != null && !removedOrganelles.Contains(originalItem))
            return originalItem;

        // We need to resolve all hex positions of our added organelles, so this is a bit trickier
        foreach (var addedOrganelle in addedOrganelles)
        {
            var basePosition = addedOrganelle.Position;

            var rotated = addedOrganelle.Definition.GetRotatedHexes(addedOrganelle.Orientation);
            var count = rotated.Count;
            for (int i = 0; i < count; ++i)
            {
                if (rotated[i] + basePosition == location)
                {
                    return addedOrganelle;
                }
            }
        }

        return null;
    }

    public IReadOnlyOrganelleTemplate? GetByExactElementRootPosition(Hex location)
    {
        ResolveDataIfDirty();
        var originalItem = originalCell.Organelles.GetByExactElementRootPosition(location);

        if (originalItem != null && !removedOrganelles.Contains(originalItem))
            return originalItem;

        // Check if we added such a thing
        foreach (var addedOrganelle in addedOrganelles)
        {
            if (addedOrganelle.Position == location)
                return addedOrganelle;
        }

        return null;
    }

    /// <summary>
    ///   Used when caching these objects to refresh this for a new use.
    /// </summary>
    /// <param name="typeDefinition">New type to base changes on</param>
    public void SwitchBase(IReadOnlyCellTypeDefinition typeDefinition)
    {
        ClearActiveActions();
        MarkDirty();
        originalCell = typeDefinition;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    internal override void OnStartApplyChanges()
    {
        base.OnStartApplyChanges();

        overrideMembrane = false;
        overrideMembraneRigidity = false;

        // Capture back all organelle instances
        foreach (var addedOrganelle in addedOrganelles)
        {
            unusedOrganelles.Push(addedOrganelle);
        }

        addedOrganelles.Clear();
        removedOrganelles.Clear();
    }

    internal override bool ApplyAction(EditorCombinableActionData actionData)
    {
        if (actionData is OrganellePlacementActionData organellePlacementActionData)
        {
            // This is just runtime data, replaced cytoplasm actions are in a separate remove organelle action
            // organellePlacementActionData.ReplacedCytoplasm;

            var newOrganelle = GetModifiable(organellePlacementActionData.PlacedHex);

            // Preserve the original position in history
            newOrganelle.Position = organellePlacementActionData.Location;
            newOrganelle.Orientation = organellePlacementActionData.Orientation;

            addedOrganelles.Add(newOrganelle);

            return true;
        }

        if (actionData is EndosymbiontPlaceActionData endosymbiontPlaceActionData)
        {
            var newOrganelle = GetModifiable(endosymbiontPlaceActionData.PlacedOrganelle);
            newOrganelle.Position = endosymbiontPlaceActionData.PlacementLocation;
            newOrganelle.Orientation = endosymbiontPlaceActionData.PlacementRotation;

            // Make certain this is marked as an endosymbiont
            newOrganelle.IsEndosymbiont = true;

            addedOrganelles.Add(newOrganelle);
            return true;
        }

        if (actionData is OrganelleMoveActionData organelleMoveActionData)
        {
            IReadOnlyOrganelleTemplate? original = null;

            // Find a match first if we have done something on this hex before
            foreach (var addedOrganelle in addedOrganelles)
            {
                if (addedOrganelle.Position == organelleMoveActionData.OldLocation &&
                    addedOrganelle.Orientation == organelleMoveActionData.OldRotation)
                {
                    original = addedOrganelle;

                    if (original.Definition != organelleMoveActionData.MovedHex.Definition)
                        throw new InvalidOperationException("Found an unrelated organelle at move old location");

                    addedOrganelles.Remove(addedOrganelle);
                    break;
                }
            }

            if (original == null)
            {
                // Then match to the original microbe organelles
                original = originalCell.Organelles.GetByExactElementRootPosition(organelleMoveActionData.OldLocation);

                if (original != null)
                {
                    if (original.Definition != organelleMoveActionData.MovedHex.Definition)
                        GD.PrintErr("Found unrelated organelle at exact position of moved organelle");

                    // Don't want the old instance to show up any more
                    removedOrganelles.Add(original);
                }
            }

            if (original == null)
                throw new InvalidOperationException("Could not find the organelle a move operation is related to");

            // And then we can add a new organelle
            var modifiable = GetModifiable(original);
            modifiable.Position = organelleMoveActionData.NewLocation;
            modifiable.Orientation = organelleMoveActionData.NewRotation;

            addedOrganelles.Add(modifiable);
            return true;
        }

        if (actionData is OrganelleRemoveActionData organelleRemoveActionData)
        {
            IReadOnlyOrganelleTemplate? original = null;

            // Find a match first if we have done something on this hex before
            foreach (var addedOrganelle in addedOrganelles)
            {
                if (addedOrganelle.Position == organelleRemoveActionData.Location &&
                    addedOrganelle.Orientation == organelleRemoveActionData.Orientation)
                {
                    original = addedOrganelle;

                    if (original.Definition != organelleRemoveActionData.RemovedHex.Definition)
                        throw new InvalidOperationException("Found an unrelated organelle at delete location");

                    addedOrganelles.Remove(addedOrganelle);
                    break;
                }
            }

            if (original == null)
            {
                // Then match to the original microbe organelles
                original = originalCell.Organelles.GetByExactElementRootPosition(organelleRemoveActionData.Location);

                if (original != null)
                {
                    if (original.Definition != organelleRemoveActionData.RemovedHex.Definition)
                        GD.PrintErr("Found unrelated organelle at exact position of removed organelle");

                    // Don't want the old instance to show up any more
                    removedOrganelles.Add(original);
                }
            }

            if (original == null)
                throw new InvalidOperationException("Could not find the organelle a remove operation is related to");

            // We already removed the original, so there's nothing more to do

            return true;
        }

        if (actionData is OrganelleUpgradeActionData organelleUpgradeActionData)
        {
            IReadOnlyOrganelleTemplate? original = null;

            // Find a match first if we have done something on this hex before
            foreach (var addedOrganelle in addedOrganelles)
            {
                // Match based on what the organelle was before the upgrade, not the upgraded organelle itself
                if (addedOrganelle.OriginalFrom == organelleUpgradeActionData.UpgradedOrganelle)
                {
                    original = addedOrganelle;

                    addedOrganelles.Remove(addedOrganelle);
                    break;
                }
            }

            if (original == null)
            {
                foreach (var addedOrganelle in addedOrganelles)
                {
                    // Make sure organelles where the original instance is different (due to the base species and
                    // upgrade action data not matching due to editor using temporary organelles) can still match
                    if (addedOrganelle.OriginalFrom.Definition ==
                        organelleUpgradeActionData.UpgradedOrganelle.Definition &&
                        addedOrganelle.Position == organelleUpgradeActionData.Position)
                    {
                        original = addedOrganelle;
                        addedOrganelles.Remove(addedOrganelle);
                        break;
                    }
                }
            }

            if (original == null)
            {
                // Then match to the original microbe organelles
                original = originalCell.Organelles.GetByExactElementRootPosition(organelleUpgradeActionData.Position);

                // TODO: it should not be necessary now to fallback to using
                // organelleUpgradeActionData.UpgradedOrganelle.Position as that can lead to finding the wrong
                // organelle if the player did a very complex shuffling and upgrading of multiple organelles of the
                // same type.

                if (original != null)
                {
                    // Don't want the old instance to show up any more
                    if (original.Definition != organelleUpgradeActionData.UpgradedOrganelle.Definition)
                        GD.PrintErr("Found unrelated organelle at old position of upgraded organelle");

                    // The reference doesn't really match here, but as we checked the new organelle list before,
                    // this should be safe. The reference doesn't equal because the editor has a temporary list of
                    // organelles that are modified that is separate from the underlying species.

                    // But we can check this for similar safety
                    if (removedOrganelles.Contains(original))
                    {
                        GD.PrintErr("Original organelle is in removed list when applying upgrade in facade");
                    }
                    else
                    {
                        removedOrganelles.Add(original);
                    }
                }
            }

            if (original == null)
                throw new InvalidOperationException("Could not find the organelle an upgrade operation is related to");

            // And then we can add a new organelle with the upgrade applied (as we removed the previous already)
            var modifiable = GetModifiable(original);

            modifiable.ModifiableUpgrades = organelleUpgradeActionData.NewUpgrades;

            addedOrganelles.Add(modifiable);
            return true;
        }

        if (actionData is RigidityActionData rigidityActionData)
        {
            overrideMembraneRigidity = true;
            newMembraneRigidity = rigidityActionData.NewRigidity;
            return true;
        }

        if (actionData is MembraneActionData membraneActionData)
        {
            overrideMembrane = true;
            newMembrane = membraneActionData.NewMembrane;
            return true;
        }

        if (actionData is NewMicrobeActionData newMicrobeActionData)
        {
            // Clear already applied things
            foreach (var organelle in newMicrobeActionData.OldEditedMicrobeOrganelles)
            {
                if (!removedOrganelles.Contains(organelle))
                    removedOrganelles.Add(organelle);
            }

            addedOrganelles.Clear();

            // Logic loaned from DoNewMicrobeAction
            overrideMembrane = true;
            newMembrane = SimulationParameters.Instance.GetMembrane("single");

            // Use a temporary new organelle reference as this is created from thin air in the editor as well
            var newCytoplasm = new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("cytoplasm"),
                new Hex(0, 0), 0);
            addedOrganelles.Add(new OrganelleWithOriginalReference(newCytoplasm));

            overrideMembraneRigidity = true;
            newMembraneRigidity = 0;

            // Colour needs to be applied by our caller as well if it is a microbe species
            Colour = Colors.White;

            return true;
        }

        if (actionData is ColourActionData colourActionData)
        {
            Colour = colourActionData.NewColour;
            return true;
        }

        throw new NotSupportedException($"Base cell type facade doesn't know how to handle: {actionData.GetType()}");
    }

    private OrganelleWithOriginalReference GetModifiable(IReadOnlyOrganelleTemplate organelleTemplate)
    {
        if (unusedOrganelles.TryPop(out var existing))
        {
            existing.ReuseFor(organelleTemplate);
            return existing;
        }

        // Ran out of cached things, make more
        return new OrganelleWithOriginalReference(organelleTemplate);
    }

    private sealed class OrganelleWithOriginalReference : OrganelleTemplate
    {
        private OrganelleUpgrades? upgradeOverride;

        private bool newIsEndosymbiont;
        private bool overrideEndosymbiont;

        public OrganelleWithOriginalReference(IReadOnlyOrganelleTemplate original) : base(original.Definition,
            original.Position, original.Orientation)
        {
            // Make sure creating further reference objects keeps the original reference
            if (original is OrganelleWithOriginalReference withAncestorReference)
            {
                OriginalFrom = withAncestorReference.OriginalFrom;

                // Keep upgrade override the same
                upgradeOverride = withAncestorReference.upgradeOverride;
                newIsEndosymbiont = withAncestorReference.newIsEndosymbiont;
                overrideEndosymbiont = withAncestorReference.overrideEndosymbiont;
            }
            else
            {
                OriginalFrom = original;
            }
        }

        public IReadOnlyOrganelleTemplate OriginalFrom { get; private set; }

        public override OrganelleUpgrades? ModifiableUpgrades
        {
            get => upgradeOverride ?? throw new Exception("Not overridden for modifiable get");
            set => upgradeOverride = value;
        }

        public override bool IsEndosymbiont
        {
            get => overrideEndosymbiont ? newIsEndosymbiont : OriginalFrom.IsEndosymbiont;
            set
            {
                overrideEndosymbiont = true;
                newIsEndosymbiont = value;
            }
        }

        public override IReadOnlyOrganelleUpgrades? Upgrades => upgradeOverride ?? OriginalFrom.Upgrades;

        internal void ReuseFor(IReadOnlyOrganelleTemplate original)
        {
            if (original is OrganelleWithOriginalReference withAncestorReference)
            {
                OriginalFrom = withAncestorReference.OriginalFrom;

                // Keep upgrade override the same
                upgradeOverride = withAncestorReference.upgradeOverride;

                overrideEndosymbiont = withAncestorReference.overrideEndosymbiont;
                newIsEndosymbiont = withAncestorReference.newIsEndosymbiont;
            }
            else
            {
                OriginalFrom = original;
                upgradeOverride = null;
                overrideEndosymbiont = false;
            }

            Definition = original.Definition;
            Position = original.Position;
            Orientation = original.Orientation;
        }
    }

    private class OrganelleEnumerator : IEnumerator<IReadOnlyOrganelleTemplate>
    {
        private readonly CellTypeEditsFacade dataSource;

        private readonly IEnumerator<IReadOnlyOrganelleTemplate> originalReader;

        private int readIndex = -1;

        private IReadOnlyOrganelleTemplate? current;

        public OrganelleEnumerator(CellTypeEditsFacade dataSource)
        {
            this.dataSource = dataSource;
            originalReader = dataSource.originalCell.Organelles.GetEnumerator();
        }

        IReadOnlyOrganelleTemplate IEnumerator<IReadOnlyOrganelleTemplate>.Current =>
            current ?? throw new InvalidOperationException("No element");

        object? IEnumerator.Current => current;

        public bool MoveNext()
        {
            if (readIndex == -1)
            {
                // Reading original items
                while (true)
                {
                    if (originalReader.MoveNext())
                    {
                        current = originalReader.Current;

                        // Need to read the next item if we are ignoring this item
                        if (dataSource.removedOrganelles.Contains(current))
                            continue;

                        // Otherwise we found a good item
                        return true;
                    }

                    // Original items ended
                    break;
                }
            }

            // Reading extra items now
            ++readIndex;

            if (readIndex >= dataSource.addedOrganelles.Count)
            {
                current = null;
                return false;
            }

            current = dataSource.addedOrganelles[readIndex];
            return true;
        }

        public void Reset()
        {
            current = null;
            readIndex = -1;
            originalReader.Reset();
        }

        public void Dispose()
        {
            originalReader.Dispose();
        }
    }
}
