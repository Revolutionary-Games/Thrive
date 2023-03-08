﻿using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Shows a list of processes in a container
/// </summary>
public class ProcessList : VBoxContainer
{
#pragma warning disable CA2213
    private PackedScene chemicalEquationScene = null!;
#pragma warning restore CA2213

    private ChildObjectCache<StrictProcessDisplayInfoEquality, ChemicalEquation> createdProcessControls = null!;
    private List<StrictProcessDisplayInfoEquality>? processesToShow;

    public IEnumerable<IProcessDisplayInfo>? ProcessesToShow
    {
        set => processesToShow = value?.Select(d => new StrictProcessDisplayInfoEquality(d)).ToList();
    }

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

        // To ensure chemical equations are up to date we use this strict comparison helper here as now normal
        // process equality doesn't take speed into account to make some other parts of the code work much better
        // TODO: would it be ultimately more performant to just let the chemical equations auto update themselves
        // while this is visible? As the comparison operator is pretty expensive for the strict value equality.
        createdProcessControls =
            new ChildObjectCache<StrictProcessDisplayInfoEquality, ChemicalEquation>(this, CreateEquation);
    }

    public override void _Process(float delta)
    {
        if (!IsVisibleInTree())
            return;

        if (processesToShow == null)
        {
            createdProcessControls.Clear();
            return;
        }

        // Check that all children are up to date
        createdProcessControls.UnMarkAll();

        foreach (var process in processesToShow)
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

    private ChemicalEquation CreateEquation(StrictProcessDisplayInfoEquality process)
    {
        var equation = (ChemicalEquation)chemicalEquationScene.Instance();
        equation.ShowSpinner = ShowSpinners;
        equation.EquationFromProcess = process.DisplayInfo;
        equation.DefaultTitleColour = ProcessesTitleColour;
        equation.MarkRedOnLimitingCompounds = MarkRedOnLimitingCompounds;

        return equation;
    }
}
