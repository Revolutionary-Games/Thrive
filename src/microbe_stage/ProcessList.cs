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

    public bool ShowSpinners { get; set; } = true;

    /// <summary>
    ///   The default color for all the process titles in this list.
    /// </summary>
    public Color ProcessesTitleColour { get; set; } = Colors.White;

    /// <summary>
    ///   If true the color of one of the process titles in this list will be changed to red
    ///   if it has any limiting compounds.
    /// </summary>
    public bool MarkRedOnLimitingCompounds { get; set; }

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
        }

        createdProcessControls.ApplyOrder();
        createdProcessControls.DeleteUnmarked();
    }

    private void ClearChildren()
    {
        this.FreeChildren();
    }

    private ChemicalEquation CreateEquation(IProcessDisplayInfo process)
    {
        var equation = (ChemicalEquation)chemicalEquationScene.Instance();
        equation.ShowSpinner = ShowSpinners;
        equation.EquationFromProcess = process;
        equation.DefaultTitleColour = ProcessesTitleColour;
        equation.MarkRedOnLimitingCompounds = MarkRedOnLimitingCompounds;

        return equation;
    }
}
