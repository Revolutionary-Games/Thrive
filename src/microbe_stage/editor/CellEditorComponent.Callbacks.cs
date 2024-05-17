using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

/// <summary>
///   This partial class has all of the editor action callbacks needed for the microbe editor
/// </summary>
[DeserializedCallbackTarget]
public partial class CellEditorComponent
{
    [DeserializedCallbackAllowed]
    private void OnOrganelleAdded(OrganelleTemplate organelle)
    {
        organelleDataDirty = true;
        microbeVisualizationOrganellePositionsAreDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void OnOrganelleRemoved(OrganelleTemplate organelle)
    {
        organelleDataDirty = true;
        microbeVisualizationOrganellePositionsAreDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void DoOrganellePlaceAction(OrganellePlacementActionData data)
    {
        data.ReplacedCytoplasm = new List<OrganelleTemplate>();
        var organelle = data.PlacedHex;

        // Check if there is cytoplasm under this organelle.
        foreach (var hex in organelle.RotatedHexes)
        {
            var organelleHere = editedMicrobeOrganelles.GetElementAt(hex + organelle.Position, hexTemporaryMemory);

            if (organelleHere == null)
                continue;

            if (organelleHere.Definition.InternalName != "cytoplasm")
            {
                throw new Exception("Can't place organelle on top of something " +
                    "else than cytoplasm");
            }

            // First we save the organelle data and then delete it
            data.ReplacedCytoplasm.Add(organelleHere);
            editedMicrobeOrganelles.Remove(organelleHere);
        }

        GD.Print("Placing organelle '", organelle.Definition.InternalName, "' at: ",
            organelle.Position);

        editedMicrobeOrganelles.AddFast(organelle, hexTemporaryMemory, hexTemporaryMemory2);
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganellePlaceAction(OrganellePlacementActionData data)
    {
        if (!editedMicrobeOrganelles.Remove(data.PlacedHex))
        {
            ThrowIfNotMulticellular();

            var newlyInitializedOrganelle = editedMicrobeOrganelles.First(o => o.Position == data.PlacedHex.Position);
            data.PlacedHex = newlyInitializedOrganelle;

            editedMicrobeOrganelles.Remove(newlyInitializedOrganelle);
        }

        if (data.ReplacedCytoplasm != null)
        {
            foreach (var replacedCytoplasm in data.ReplacedCytoplasm)
            {
                GD.Print("Replacing ", replacedCytoplasm.Definition.InternalName, " at: ",
                    replacedCytoplasm.Position);

                editedMicrobeOrganelles.AddFast(replacedCytoplasm, hexTemporaryMemory, hexTemporaryMemory2);
            }
        }
    }

    [DeserializedCallbackAllowed]
    private void DoOrganelleRemoveAction(OrganelleRemoveActionData data)
    {
        if (!editedMicrobeOrganelles.Remove(data.RemovedHex))
        {
            // TODO: it seems very rarely this can be hit in "normal" microbe freebuild by spamming undo and redo with
            // new cell button sprinkled in (reproduction steps for the bug are unconfirmed)
            ThrowIfNotMulticellular();

            var newlyInitializedOrganelle = editedMicrobeOrganelles.First(o => o.Position == data.RemovedHex.Position);
            data.RemovedHex = newlyInitializedOrganelle;

            editedMicrobeOrganelles.Remove(newlyInitializedOrganelle);
        }
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganelleRemoveAction(OrganelleRemoveActionData data)
    {
        editedMicrobeOrganelles.AddFast(data.RemovedHex, hexTemporaryMemory, hexTemporaryMemory2);
    }

    [DeserializedCallbackAllowed]
    private void DoOrganelleMoveAction(OrganelleMoveActionData data)
    {
        if (IsMulticellularEditor)
        {
            // Try to recover if there is a new organelle instance we should act on instead
            var newlyInitializedOrganelle = editedMicrobeOrganelles.FirstOrDefault(o =>
                o.Position == data.OldLocation && o.Orientation == data.OldRotation);

            if (newlyInitializedOrganelle != null)
                data.MovedHex = newlyInitializedOrganelle;
        }

        data.MovedHex.Position = data.NewLocation;
        data.MovedHex.Orientation = data.NewRotation;

        if (editedMicrobeOrganelles.Contains(data.MovedHex))
        {
            UpdateAlreadyPlacedVisuals();

            // Organelle placement *might* affect auto-evo in the future so this is here for that reason
            StartAutoEvoPrediction();

            UpdateStats();
        }
        else
        {
            editedMicrobeOrganelles.AddFast(data.MovedHex, hexTemporaryMemory, hexTemporaryMemory2);
        }

        // TODO: dynamic MP PR had this line:
        // OnMembraneChanged();
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganelleMoveAction(OrganelleMoveActionData data)
    {
        if (IsMulticellularEditor)
        {
            // Try to recover if there is a new organelle instance we should act on instead
            var newlyInitializedOrganelle = editedMicrobeOrganelles.FirstOrDefault(o =>
                o.Position == data.NewLocation && o.Orientation == data.NewRotation);

            if (newlyInitializedOrganelle != null)
                data.MovedHex = newlyInitializedOrganelle;
        }

        data.MovedHex.Position = data.OldLocation;
        data.MovedHex.Orientation = data.OldRotation;

        UpdateAlreadyPlacedVisuals();
        StartAutoEvoPrediction();
        UpdateStats();

        // TODO: dynamic MP PR had this line:
        // OnMembraneChanged();
    }

    [DeserializedCallbackAllowed]
    private void DoNewMicrobeAction(NewMicrobeActionData data)
    {
        // TODO: could maybe grab the current organelles and put them in the action here? This could be more safe
        // against weird situations where it might be possible if the undo / redo system is changed to restore
        // the wrong organelles

        Editor.MutationPoints = Constants.BASE_MUTATION_POINTS;
        Membrane = SimulationParameters.Instance.GetMembrane("single");
        editedMicrobeOrganelles.Clear();
        editedMicrobeOrganelles.AddFast(new OrganelleTemplate(GetOrganelleDefinition("cytoplasm"),
            new Hex(0, 0), 0), hexTemporaryMemory, hexTemporaryMemory2);
        Rigidity = 0;
        Colour = Colors.White;

        if (!IsMulticellularEditor)
            behaviourEditor.ResetBehaviour();

        OnPostNewMicrobeChange();
    }

    [DeserializedCallbackAllowed]
    private void UndoNewMicrobeAction(NewMicrobeActionData data)
    {
        editedMicrobeOrganelles.Clear();
        Membrane = data.OldMembrane;
        Rigidity = data.OldMembraneRigidity;
        Colour = data.OldMembraneColour;

        foreach (var organelle in data.OldEditedMicrobeOrganelles)
        {
            editedMicrobeOrganelles.AddFast(organelle, hexTemporaryMemory, hexTemporaryMemory2);
        }

        if (!IsMulticellularEditor)
        {
            if (data.OldBehaviourValues == null)
                throw new InvalidOperationException("Behaviour data should have been initialized for restore");

            foreach (var oldBehaviour in data.OldBehaviourValues)
            {
                behaviourEditor.SetBehaviouralValue(oldBehaviour.Key, oldBehaviour.Value);
            }
        }

        OnPostNewMicrobeChange();
    }

    [DeserializedCallbackAllowed]
    private void DoMembraneChangeAction(MembraneActionData data)
    {
        var membrane = data.NewMembrane;
        GD.Print("Changing membrane to '", membrane.InternalName, "'");
        Membrane = membrane;

        // TODO: dynamic MP PR had this line:
        // OnMembraneChanged();

        UpdateMembraneButtons(Membrane.InternalName);
        UpdateSpeed(CalculateSpeed());
        UpdateHitpoints(CalculateHitpoints());
        CalculateEnergyAndCompoundBalance(editedMicrobeOrganelles.Organelles, Membrane);
        SetMembraneTooltips(Membrane);

        StartAutoEvoPrediction();

        if (previewMicrobeSpecies != null)
        {
            previewMicrobeSpecies.MembraneType = membrane;

            if (previewMicrobe.IsAlive)
                previewSimulation!.ApplyMicrobeMembraneType(previewMicrobe, membrane);
        }
    }

    [DeserializedCallbackAllowed]
    private void UndoMembraneChangeAction(MembraneActionData data)
    {
        Membrane = data.OldMembrane;
        GD.Print("Changing membrane back to '", Membrane.InternalName, "'");
        UpdateMembraneButtons(Membrane.InternalName);
        UpdateSpeed(CalculateSpeed());
        UpdateHitpoints(CalculateHitpoints());
        CalculateEnergyAndCompoundBalance(editedMicrobeOrganelles.Organelles, Membrane);
        SetMembraneTooltips(Membrane);

        StartAutoEvoPrediction();

        if (previewMicrobeSpecies != null)
        {
            previewMicrobeSpecies.MembraneType = Membrane;

            if (previewMicrobe.IsAlive)
                previewSimulation!.ApplyMicrobeMembraneType(previewMicrobe, Membrane);
        }
    }

    [DeserializedCallbackAllowed]
    private void DoRigidityChangeAction(RigidityActionData data)
    {
        Rigidity = data.NewRigidity;

        // TODO: when rigidity affects auto-evo this also needs to re-run the prediction, though there should probably
        // be some kind of throttling, this also applies to the behaviour values

        OnRigidityChanged();
    }

    [DeserializedCallbackAllowed]
    private void UndoRigidityChangeAction(RigidityActionData data)
    {
        Rigidity = data.PreviousRigidity;
        OnRigidityChanged();
    }

    [DeserializedCallbackAllowed]
    private void DoColourChangeAction(ColourActionData data)
    {
        Colour = data.NewColour;
        OnColourChanged();
    }

    [DeserializedCallbackAllowed]
    private void UndoColourChangeAction(ColourActionData data)
    {
        Colour = data.PreviousColour;
        OnColourChanged();
    }

    [DeserializedCallbackAllowed]
    private void DoOrganelleUpgradeAction(OrganelleUpgradeActionData data)
    {
        data.UpgradedOrganelle.Upgrades = data.NewUpgrades;

        // Uncomment when upgrades can visually affect the cell
        // UpdateAlreadyPlacedVisuals();

        UpdateStats();

        // Organelle upgrades will eventually affect auto-evo
        StartAutoEvoPrediction();
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganelleUpgradeAction(OrganelleUpgradeActionData data)
    {
        data.UpgradedOrganelle.Upgrades = data.OldUpgrades;

        // Uncomment when upgrades can visually affect the cell
        // UpdateAlreadyPlacedVisuals();

        UpdateStats();

        // Organelle upgrades will eventually affect auto-evo
        StartAutoEvoPrediction();
    }

    [DeserializedCallbackAllowed]
    private void DoEndosymbiontPlaceAction(EndosymbiontPlaceActionData data)
    {
        // Perform unlock to make the endosymbiont placeable for later
        if (Editor.CurrentGame.GameWorld.UnlockProgress.UnlockOrganelle(data.PlacedOrganelle.Definition,
                Editor.CurrentGame))
        {
            GD.Print("Unlocking organelle due to endosymbiosis place");
            data.PerformedUnlock = true;
        }

        var organelle = data.PlacedOrganelle;

        // TODO: if wanted could allow replacing cytoplasm with this action type

        GD.Print("Placing endosymbiont '", organelle.Definition.InternalName, "' at: ",
            organelle.Position);

        editedMicrobeOrganelles.AddFast(organelle, hexTemporaryMemory, hexTemporaryMemory2);

        if (data.PerformedUnlock)
        {
            OnUnlockedOrganellesChanged();
        }

        // With the endosymbiosis place done, the current endosymbiosis action is done
        data.RelatedEndosymbiosisAction =
            Editor.EditedBaseSpecies.Endosymbiosis.MarkEndosymbiosisDone(data.RelatedEndosymbiosisAction);

        // Restore any inprogress data we overwrote on undo
        if (data.OverriddenEndosymbiosisOnUndo != null)
        {
            var overwrote =
                Editor.EditedBaseSpecies.Endosymbiosis.ResumeEndosymbiosisProcess(data.OverriddenEndosymbiosisOnUndo);

            // Hopefully there's no way to hit this condition, if there is then this needs some fix
            if (overwrote != null && overwrote != data.RelatedEndosymbiosisAction)
            {
                GD.PrintErr("Losing an in-progress endosymbiosis info on redo");
            }

            data.OverriddenEndosymbiosisOnUndo = null;
        }
    }

    [DeserializedCallbackAllowed]
    private void UndoEndosymbiontPlaceAction(EndosymbiontPlaceActionData data)
    {
        if (!editedMicrobeOrganelles.Remove(data.PlacedOrganelle))
        {
            throw new Exception("Couldn't find endosymbiont placement to remove");
        }

        // Undo unlock if required
        if (data.PerformedUnlock)
        {
            GD.Print("Locking organelle type due to endosymbiont undo: ", data.PlacedOrganelle.Definition.InternalName);

            // If the player selected the organelle type to place that is now again locked, reset it
            if (activeActionName == data.PlacedOrganelle.Definition.InternalName)
            {
                activeActionName = null;
                GD.Print("Resetting active action as it is an organelle type that will be locked now");
            }

            // NOTE: if we ever add support for unlock condition re-check after entering the editor, this needs to be
            // re-thought out to make sure this doesn't interact badly with such a feature
            Editor.CurrentGame.GameWorld.UnlockProgress.UndoOrganelleUnlock(data.PlacedOrganelle.Definition);

            OnUnlockedOrganellesChanged();
        }

        // Need to restore the previous endosymbiosis action
        data.OverriddenEndosymbiosisOnUndo =
            Editor.EditedBaseSpecies.Endosymbiosis.ResumeEndosymbiosisProcess(data.RelatedEndosymbiosisAction);
    }

    /// <summary>
    ///   In the case of the multicellular editor some actions need to work even if the editor has been reinitialized
    ///   in the meantime since they were performed. For sanity checking sake we throw an exception in those cases
    ///   if they are reached in non-multicellular editor mode.
    /// </summary>
    /// <exception cref="InvalidOperationException">The exception thrown if we aren't in multicellular</exception>
    private void ThrowIfNotMulticellular()
    {
        if (!IsMulticellularEditor)
        {
#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif

            throw new InvalidOperationException("This operation should only happen in multicellular");
        }
    }
}
