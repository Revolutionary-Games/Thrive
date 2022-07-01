﻿using System;
using System.Collections.Generic;
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
        membraneOrganellePositionsAreDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void OnOrganelleRemoved(OrganelleTemplate organelle)
    {
        organelleDataDirty = true;
        membraneOrganellePositionsAreDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void DoOrganellePlaceAction(OrganellePlacementActionData data)
    {
        data.ReplacedCytoplasm = new List<OrganelleTemplate>();
        var organelle = data.PlacedHex;

        // Check if there is cytoplasm under this organelle.
        foreach (var hex in organelle.RotatedHexes)
        {
            var organelleHere = editedMicrobeOrganelles.GetElementAt(
                hex + organelle.Position);

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

        editedMicrobeOrganelles.Add(organelle);
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
            foreach (var cytoplasm in data.ReplacedCytoplasm)
            {
                GD.Print("Replacing ", cytoplasm.Definition.InternalName, " at: ",
                    cytoplasm.Position);

                editedMicrobeOrganelles.Add(cytoplasm);
            }
        }
    }

    [DeserializedCallbackAllowed]
    private void DoOrganelleRemoveAction(OrganelleRemoveActionData data)
    {
        if (!editedMicrobeOrganelles.Remove(data.RemovedHex))
        {
            ThrowIfNotMulticellular();

            var newlyInitializedOrganelle = editedMicrobeOrganelles.First(o => o.Position == data.RemovedHex.Position);
            data.RemovedHex = newlyInitializedOrganelle;

            editedMicrobeOrganelles.Remove(newlyInitializedOrganelle);
        }
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganelleRemoveAction(OrganelleRemoveActionData data)
    {
        editedMicrobeOrganelles.Add(data.RemovedHex);
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
        }
        else
        {
            editedMicrobeOrganelles.Add(data.MovedHex);
        }

        // TODO: dynamic MP PR had this line:
        // OnMembraneChanged();
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganelleMoveAction(OrganelleMoveActionData data)
    {
        data.MovedHex.Position = data.OldLocation;
        data.MovedHex.Orientation = data.OldRotation;

        UpdateAlreadyPlacedVisuals();
        StartAutoEvoPrediction();

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
        editedMicrobeOrganelles.Add(new OrganelleTemplate(GetOrganelleDefinition("cytoplasm"),
            new Hex(0, 0), 0));
        Rigidity = 0;

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

        foreach (var organelle in data.OldEditedMicrobeOrganelles)
        {
            editedMicrobeOrganelles.Add(organelle);
        }

        if (!IsMulticellularEditor)
        {
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
        CalculateEnergyBalanceWithOrganellesAndMembraneType(editedMicrobeOrganelles.Organelles, Membrane,
            Editor.CurrentPatch);
        SetMembraneTooltips(Membrane);

        StartAutoEvoPrediction();

        if (previewMicrobe != null)
        {
            previewMicrobe.Membrane.Type = membrane;
            previewMicrobe.Membrane.Dirty = true;
            previewMicrobe.ApplyMembraneWigglyness();
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
        CalculateEnergyBalanceWithOrganellesAndMembraneType(editedMicrobeOrganelles.Organelles, Membrane,
            Editor.CurrentPatch);
        SetMembraneTooltips(Membrane);

        StartAutoEvoPrediction();

        if (previewMicrobe != null)
        {
            previewMicrobe.Membrane.Type = Membrane;
            previewMicrobe.Membrane.Dirty = true;
            previewMicrobe.ApplyMembraneWigglyness();
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

    /// <summary>
    ///   In the case of the multicellular editor some actions need to work even if the editor has been reinitialized
    ///   in the meantime since they were performed. For sanity checking sake we throw an exception in those cases
    ///   if they are reached in non-multicellular editor mode.
    /// </summary>
    /// <exception cref="InvalidOperationException">The exception thrown if we aren't in multicellular</exception>
    private void ThrowIfNotMulticellular()
    {
        if (!IsMulticellularEditor)
            throw new InvalidOperationException("This operation should only happen in multicellular");
    }
}
