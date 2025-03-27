﻿namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

public class AutoEvoPrediction : CellEditorEntryCountingTutorial
{
    public AutoEvoPrediction()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => "AutoEvoPrediction";

    [JsonIgnore]
    public Control? EditorAutoEvoPredictionPanel { get; set; }

    protected override int TriggersOnNthEditorSession => 3;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        if (gui.AutoEvoPredictionHighlight == null)
            throw new InvalidOperationException($"{nameof(gui.AutoEvoPredictionHighlight)} has not been set");

        gui.AutoEvoPredictionVisible = ShownCurrently;

        gui.AutoEvoPredictionHighlight.TargetControl = ShownCurrently ? EditorAutoEvoPredictionPanel : null;
        gui.AutoEvoPredictionHighlight.Visible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        // Disallow showing if the control we highlight is not visible.
        // Or if the prerequisite tutorial is not complete.
        if (eventType == TutorialEventType.MicrobeEditorTabChanged && (EditorAutoEvoPredictionPanel == null ||
                !GodotObject.IsInstanceValid(EditorAutoEvoPredictionPanel) || !EditorAutoEvoPredictionPanel.Visible ||
                !overallState.EarlyGameGoalTutorial.Complete))
        {
            return false;
        }

        if (base.CheckEvent(overallState, eventType, args, sender))
            return true;

        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorAutoEvoPredictionOpened:
            {
                if (ShownCurrently)
                {
                    Hide();
                }

                break;
            }
        }

        return false;
    }
}
