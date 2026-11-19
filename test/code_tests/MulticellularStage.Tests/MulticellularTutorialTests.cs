namespace ThriveTest.MulticellularStage.Tests;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Tutorial;
using Xunit;

/// <summary>
///   Tests the multicellular editor tutorials
/// </summary>
public class MulticellularTutorialTests
{
    [Fact]
    public void CellCavityTutorialWaitsForMacroscopicTutorialAndSixthEditorEntry()
    {
        var state = CreateTutorialState(out var macroscopicTutorial, out var tutorial);

        SendEditorEntryEvents(tutorial, state, 5);
        Assert.False(tutorial.CanTrigger);
        tutorial.CheckEvent(state, TutorialEventType.MulticellularEditorTabChanged,
            new StringEventArgs(nameof(EditorTab.CellEditor)), state);
        Assert.False(tutorial.ShownCurrently);

        tutorial.CheckEvent(state, TutorialEventType.EnteredMulticellularEditor, EventArgs.Empty, state);
        Assert.True(tutorial.CanTrigger);
        tutorial.CheckEvent(state, TutorialEventType.MulticellularEditorTabChanged,
            new StringEventArgs(nameof(EditorTab.CellEditor)), state);
        Assert.False(tutorial.ShownCurrently);

        macroscopicTutorial.Show();
        tutorial.CheckEvent(state, TutorialEventType.MulticellularEditorTabChanged,
            new StringEventArgs(nameof(EditorTab.CellEditor)), state);
        Assert.False(tutorial.ShownCurrently);

        macroscopicTutorial.Hide();
        tutorial.CheckEvent(state, TutorialEventType.MulticellularEditorTabChanged,
            new StringEventArgs(nameof(EditorTab.CellEditor)), state);
        Assert.True(tutorial.ShownCurrently);
    }

    [Fact]
    public void CellCavityTutorialClosesOnlyOnCavityCreationEvent()
    {
        var state = CreateTutorialState(out _, out var tutorial);

        tutorial.Show();
        tutorial.CheckEvent(state, TutorialEventType.MulticellularEditorCellPlaced, EventArgs.Empty, state);
        Assert.True(tutorial.ShownCurrently);

        tutorial.CheckEvent(state, TutorialEventType.MulticellularCellMadeCavity, EventArgs.Empty, state);
        Assert.True(tutorial.Complete);
    }

    private static void SendEditorEntryEvents(MulticellularEditorEntryCountingTutorial tutorial, TutorialState state,
        int count)
    {
        for (int i = 0; i < count; ++i)
        {
            tutorial.CheckEvent(state, TutorialEventType.EnteredMulticellularEditor, EventArgs.Empty, state);
        }
    }

    private static TutorialState CreateTutorialState(out MacroscopicRequirementsTutorial macroscopicTutorial,
        out MulticellularCellCavityTutorial cavityTutorial)
    {
        // Avoid initializing Godot-backed global settings when testing tutorial event logic.
        var state = (TutorialState)RuntimeHelpers.GetUninitializedObject(typeof(TutorialState));
        macroscopicTutorial = new MacroscopicRequirementsTutorial();
        cavityTutorial = new MulticellularCellCavityTutorial();

        var stateType = typeof(TutorialState);
        SetTutorialProperty(state, stateType,
            nameof(TutorialState.MacroscopicRequirementsTutorial), macroscopicTutorial);
        SetTutorialProperty(state, stateType, nameof(TutorialState.MulticellularCellCavityTutorial), cavityTutorial);
        stateType.GetField("cachedTutorials", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(state,
            new List<TutorialPhase> { macroscopicTutorial, cavityTutorial });

        return state;
    }

    private static void SetTutorialProperty(TutorialState state, Type stateType, string propertyName,
        TutorialPhase tutorial)
    {
        stateType.GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(state, tutorial);
    }
}
