using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Represents a view of a microbe species with a set of actions applied on top of the base data
/// </summary>
public class MicrobeEditsFacade : SpeciesEditsFacade, IReadOnlyMicrobeSpecies,
    IReadOnlyOrganelleLayout<IReadOnlyOrganelleTemplate>
{
    private readonly IReadOnlyMicrobeSpecies microbeSpecies;

    private readonly OrganelleDefinition nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");

    private readonly List<IReadOnlyOrganelleTemplate> removedOrganelles = new();
    private readonly List<IReadOnlyOrganelleTemplate> addedOrganelles = new();

    private MembraneType? newMembrane;
    private bool overrideMembrane;

    private float newMembraneRigidity;
    private bool overrideMembraneRigidity;

    public MicrobeEditsFacade(IReadOnlyMicrobeSpecies microbeSpecies) : base(microbeSpecies)
    {
        this.microbeSpecies = microbeSpecies;
    }

    public IReadOnlyOrganelleLayout<IReadOnlyOrganelleTemplate> Organelles => this;

    public MembraneType MembraneType
    {
        get
        {
            ResolveDataIfDirty();
            return overrideMembrane && newMembrane != null ? newMembrane : microbeSpecies.MembraneType;
        }
    }

    public float MembraneRigidity
    {
        get
        {
            ResolveDataIfDirty();
            return overrideMembraneRigidity ? newMembraneRigidity : microbeSpecies.MembraneRigidity;
        }
    }

    public Color Colour => SpeciesColour;

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

    // TODO: check that this is right (there might sometimes be too many items in removedOrganelles)
    // Though this seems to not be relied on currently
    public int Count => microbeSpecies.Organelles.Count + addedOrganelles.Count - removedOrganelles.Count;

    public IEnumerator<IReadOnlyOrganelleTemplate> GetEnumerator()
    {
        return new OrganelleEnumerator(this);
    }

    public IReadOnlyOrganelleTemplate? GetElementAt(Hex location, List<Hex> temporaryHexesStorage)
    {
        var originalItem = microbeSpecies.Organelles.GetElementAt(location, temporaryHexesStorage);

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
        var originalItem = microbeSpecies.Organelles.GetByExactElementRootPosition(location);

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

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    protected override void OnStartApplyChanges()
    {
        base.OnStartApplyChanges();

        overrideMembrane = false;
        overrideMembraneRigidity = false;

        addedOrganelles.Clear();
        removedOrganelles.Clear();
    }

    protected override bool ApplyAction(EditorCombinableActionData actionData)
    {
        if (actionData is OrganellePlacementActionData organellePlacementActionData)
        {
            // This is probably right to consider these deleted
            var cytoplasm = organellePlacementActionData.ReplacedCytoplasm;
            if (cytoplasm is { Count: > 0 })
                removedOrganelles.AddRange(cytoplasm);

            addedOrganelles.Add(organellePlacementActionData.PlacedHex);

            // In case resurrecting organelles is possible, clear it from the removed list
            removedOrganelles.Remove(organellePlacementActionData.PlacedHex);

            return true;
        }

        if (actionData is EndosymbiontPlaceActionData endosymbiontPlaceActionData)
        {
            addedOrganelles.Add(endosymbiontPlaceActionData.PlacedOrganelle);
            return true;
        }

        if (actionData is OrganelleMoveActionData organelleMoveActionData)
        {
            // Match the previous instance first which needs to be removed
            IReadOnlyOrganelleTemplate? original =
                microbeSpecies.Organelles.GetByExactElementRootPosition(organelleMoveActionData.OldLocation);

            if (original == null || original.Orientation != organelleMoveActionData.OldRotation ||
                original.Definition != organelleMoveActionData.MovedHex.Definition)
            {
                // Tricky part of finding an intermediate result and needing to update it
                original = null;

                foreach (var addedOrganelle in addedOrganelles)
                {
                    if (addedOrganelle.Position == organelleMoveActionData.OldLocation &&
                        addedOrganelle.Orientation == organelleMoveActionData.OldRotation)
                    {
                        if (addedOrganelle.Definition != organelleMoveActionData.MovedHex.Definition)
                            GD.PrintErr("Found unrelated organelle at exact position of moved organelle");

                        original = addedOrganelle;

                        // Remove from added as we are replacing this
                        addedOrganelles.Remove(addedOrganelle);
                        break;
                    }
                }

                // Make really sure the old hex doesn't show up
                if (!removedOrganelles.Contains(organelleMoveActionData.MovedHex))
                {
                    removedOrganelles.Add(organelleMoveActionData.MovedHex);
                }
            }
            else
            {
                // Make sure the original instance is removed
                if (removedOrganelles.Contains(original))
                    GD.PrintErr("Original should not be in removed yet");

                removedOrganelles.Add(original);
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
            // Match the previous instance first which needs to be removed
            IReadOnlyOrganelleTemplate? original =
                microbeSpecies.Organelles.GetByExactElementRootPosition(organelleRemoveActionData.Location);

            if (original == null || original.Orientation != organelleRemoveActionData.Orientation ||
                original.Definition != organelleRemoveActionData.RemovedHex.Definition)
            {
                // Tricky part of finding an intermediate result and needing to update it
                original = null;

                foreach (var addedOrganelle in addedOrganelles)
                {
                    if (addedOrganelle.Position == organelleRemoveActionData.Location &&
                        addedOrganelle.Orientation == organelleRemoveActionData.Orientation)
                    {
                        if (addedOrganelle.Definition != organelleRemoveActionData.RemovedHex.Definition)
                            GD.PrintErr("Found unrelated organelle at exact position of removed organelle");

                        original = addedOrganelle;
                        break;
                    }
                }
            }

            if (original == null)
                throw new InvalidOperationException("Could not find the organelle a remove operation is related to");

            // And then we can remove the organelle
            if (!addedOrganelles.Remove(original))
            {
                if (!removedOrganelles.Contains(original))
                {
                    removedOrganelles.Add(original);
                }
                else
                {
                    // Match by position in added. This can trigger if a remove incidentally matches an original
                    // position
                    bool removed = false;
                    foreach (var addedOrganelle in addedOrganelles)
                    {
                        if (addedOrganelle.Position == original.Position &&
                            addedOrganelle.Orientation == original.Orientation &&
                            addedOrganelle.Definition == original.Definition)
                        {
                            removed = addedOrganelles.Remove(addedOrganelle);
                            break;
                        }
                    }

                    if (!removed)
                        throw new InvalidOperationException("Could not find delete target for a remove operation");
                }
            }

            return true;
        }

        if (actionData is OrganelleUpgradeActionData organelleUpgradeActionData)
        {
            // Match the previous instance first which needs to be removed
            IReadOnlyOrganelleTemplate? original =
                microbeSpecies.Organelles.GetByExactElementRootPosition(organelleUpgradeActionData.UpgradedOrganelle
                    .Position);

            if (original == null || original.Orientation != organelleUpgradeActionData.UpgradedOrganelle.Orientation ||
                original.Definition != organelleUpgradeActionData.UpgradedOrganelle.Definition)
            {
                original = null;

                if (addedOrganelles.Contains(organelleUpgradeActionData.UpgradedOrganelle))
                {
                    // Directly added organelle, can use it directly
                    original = organelleUpgradeActionData.UpgradedOrganelle;
                }
                else
                {
                    // Tricky part of finding an intermediate result and needing to update it
                    foreach (var addedOrganelle in addedOrganelles)
                    {
                        if (addedOrganelle is OrganelleWithOriginalReference organelleWithReference)
                        {
                            if (organelleWithReference.OriginalFrom == organelleUpgradeActionData.UpgradedOrganelle)
                            {
                                // TODO: should this try to match anything else? The reference is a pretty strong check
                                // already.
                                original = addedOrganelle;
                                break;
                            }
                        }
                    }
                }
            }

            if (original == null)
                throw new InvalidOperationException("Could not find the organelle an upgrade operation is related to");

            // Remove existing results
            if (!removedOrganelles.Contains(original))
                removedOrganelles.Add(original);

            addedOrganelles.Remove(original);

            // And then we can add a new organelle with the upgrade applied
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
            // TODO: make sure this works correctly

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

            addedOrganelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("cytoplasm"),
                new Hex(0, 0), 0));

            overrideMembraneRigidity = true;
            newMembraneRigidity = 0;

            SetNewColour(Colors.White);

            return true;
        }

        return base.ApplyAction(actionData);
    }

    private OrganelleWithOriginalReference GetModifiable(IReadOnlyOrganelleTemplate organelleTemplate)
    {
        // TODO: should we have a local cache of objects that can be reused?
        return new OrganelleWithOriginalReference(organelleTemplate);
    }

    private sealed class OrganelleWithOriginalReference : OrganelleTemplate
    {
        private OrganelleUpgrades? upgradeOverride;

        public OrganelleWithOriginalReference(IReadOnlyOrganelleTemplate original) : base(original.Definition,
            original.Position, original.Orientation)
        {
            // Make sure creating further reference objects keeps the original reference
            if (original is OrganelleWithOriginalReference withAncestorReference)
            {
                OriginalFrom = withAncestorReference.OriginalFrom;
            }
            else
            {
                OriginalFrom = original;
            }
        }

        public IReadOnlyOrganelleTemplate OriginalFrom { get; }

        public override OrganelleUpgrades? ModifiableUpgrades
        {
            get => upgradeOverride ?? throw new Exception("Not overridden");
            set => upgradeOverride = value;
        }

        public override IReadOnlyOrganelleUpgrades? Upgrades => upgradeOverride ?? OriginalFrom.Upgrades;
    }

    private class OrganelleEnumerator : IEnumerator<IReadOnlyOrganelleTemplate>
    {
        private readonly MicrobeEditsFacade dataSource;

        private readonly IEnumerator<IReadOnlyOrganelleTemplate> originalReader;

        private int readIndex = -1;

        private IReadOnlyOrganelleTemplate? current;

        public OrganelleEnumerator(MicrobeEditsFacade dataSource)
        {
            this.dataSource = dataSource;
            originalReader = dataSource.microbeSpecies.Organelles.GetEnumerator();
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
