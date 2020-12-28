using System.Collections.Generic;
using Godot;

/// <summary>
///   Shows a list of processes in a container
/// </summary>
public class ProcessList : VBoxContainer
{
    private PackedScene chemicalEquationScene;

    private ChildObjectCache<IProcessDisplayInfo, ChemicalEquation> createdProcessControls;

    public List<IProcessDisplayInfo> ProcessesToShow { get; set; }

    public override void _Ready()
    {
        chemicalEquationScene = GD.Load<PackedScene>("res://src/gui_common/ChemicalEquation.tscn");

        createdProcessControls = new ChildObjectCache<IProcessDisplayInfo, ChemicalEquation>(this, CreateEquation);
    }

    public override void _Process(float delta)
    {
        if (!IsVisibleInTree())
            return;

        if (ProcessesToShow == null)
        {
            createdProcessControls.Clear();
            return;
        }

        // Check that all children are up to date
        createdProcessControls.UnMarkAll();

        foreach (var process in ProcessesToShow)
        {
            createdProcessControls.GetChild(process);

            // TODO: update the child here?
        }

        createdProcessControls.DeleteUnmarked();
    }

    private void ClearChildren()
    {
        this.FreeChildren();
    }

    private ChemicalEquation CreateEquation(IProcessDisplayInfo process)
    {
        var equation = (ChemicalEquation)chemicalEquationScene.Instance();
        equation.ShowSpinner = true;
        equation.EquationFromProcess = process;

        return equation;
    }
}
