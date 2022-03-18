using System;
using System.Collections.Generic;
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
    private void DoOrganellePlaceAction(CellEditorAction action)
    {
        var data = (PlacementActionData?)action.Data ??
            throw new Exception($"{nameof(DoOrganellePlaceAction)} missing action data");

        data.ReplacedCytoplasm = new List<OrganelleTemplate>();
        var organelle = data.Organelle;

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
    private void UndoOrganellePlaceAction(CellEditorAction action)
    {
        var data = (PlacementActionData?)action.Data ??
            throw new Exception($"{nameof(UndoOrganellePlaceAction)} missing action data");

        editedMicrobeOrganelles.Remove(data.Organelle);

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
    private void DoOrganelleRemoveAction(CellEditorAction action)
    {
        var data = (RemoveActionData?)action.Data ??
            throw new Exception($"{nameof(DoOrganelleRemoveAction)} missing action data");
        editedMicrobeOrganelles.Remove(data.Organelle);
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganelleRemoveAction(CellEditorAction action)
    {
        var data = (RemoveActionData?)action.Data ??
            throw new Exception($"{nameof(UndoOrganelleRemoveAction)} missing action data");
        editedMicrobeOrganelles.Add(data.Organelle);
    }

    [DeserializedCallbackAllowed]
    private void DoOrganelleMoveAction(CellEditorAction action)
    {
        var data = (MoveActionData?)action.Data ??
            throw new Exception($"{nameof(DoOrganelleMoveAction)} missing action data");
        data.Organelle.Position = data.NewLocation;
        data.Organelle.Orientation = data.NewRotation;

        if (editedMicrobeOrganelles.Contains(data.Organelle))
        {
            UpdateAlreadyPlacedVisuals();

            // Organelle placement *might* affect auto-evo in the future so this is here for that reason
            StartAutoEvoPrediction();
        }
        else
        {
            editedMicrobeOrganelles.Add(data.Organelle);
        }

        ++data.Organelle.NumberOfTimesMoved;
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganelleMoveAction(CellEditorAction action)
    {
        var data = (MoveActionData?)action.Data ??
            throw new Exception($"{nameof(UndoOrganelleMoveAction)} missing action data");
        data.Organelle.Position = data.OldLocation;
        data.Organelle.Orientation = data.OldRotation;

        UpdateAlreadyPlacedVisuals();
        StartAutoEvoPrediction();

        --data.Organelle.NumberOfTimesMoved;
    }

    [DeserializedCallbackAllowed]
    private void DoNewMicrobeAction(CellEditorAction action)
    {
        // TODO: could maybe grab the current organelles and put them in the action here? This could be more safe
        // against weird situations where it might be possible if the undo / redo system is changed to restore
        // the wrong organelles

        Editor.MutationPoints = Constants.BASE_MUTATION_POINTS;
        Membrane = SimulationParameters.Instance.GetMembrane("single");
        editedMicrobeOrganelles.Clear();
        editedMicrobeOrganelles.Add(new OrganelleTemplate(GetOrganelleDefinition("cytoplasm"),
            new Hex(0, 0), 0));

        OnPostNewMicrobeChange();
    }

    [DeserializedCallbackAllowed]
    private void UndoNewMicrobeAction(CellEditorAction action)
    {
        var data = (NewMicrobeActionData?)action.Data ??
            throw new Exception($"{nameof(UndoNewMicrobeAction)} missing action data");

        editedMicrobeOrganelles.Clear();
        Editor.MutationPoints = data.PreviousMP;
        Membrane = data.OldMembrane;

        foreach (var organelle in data.OldEditedMicrobeOrganelles)
        {
            editedMicrobeOrganelles.Add(organelle);
        }

        OnPostNewMicrobeChange();
    }

    [DeserializedCallbackAllowed]
    private void DoMembraneChangeAction(CellEditorAction action)
    {
        var data = (MembraneActionData?)action.Data ??
            throw new Exception($"{nameof(DoMembraneChangeAction)} missing action data");

        var membrane = data.NewMembrane;
        GD.Print("Changing membrane to '", membrane.InternalName, "'");
        Membrane = membrane;
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
    private void UndoMembraneChangeAction(CellEditorAction action)
    {
        var data = (MembraneActionData?)action.Data ??
            throw new Exception($"{nameof(UndoMembraneChangeAction)} missing action data");
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
    private void DoRigidityChangeAction(CellEditorAction action)
    {
        var data = (RigidityChangeActionData?)action.Data ??
            throw new Exception($"{nameof(DoRigidityChangeAction)} missing action data");

        Rigidity = data.NewRigidity;

        // TODO: when rigidity affects auto-evo this also needs to re-run the prediction, though there should probably
        // be some kind of throttling, this also applies to the behaviour values

        OnRigidityChanged();
    }

    [DeserializedCallbackAllowed]
    private void UndoRigidityChangeAction(CellEditorAction action)
    {
        var data = (RigidityChangeActionData?)action.Data ??
            throw new Exception($"{nameof(UndoRigidityChangeAction)} missing action data");

        Rigidity = data.PreviousRigidity;
        OnRigidityChanged();
    }
}
