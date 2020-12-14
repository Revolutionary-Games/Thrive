﻿namespace Tutorial
{
    using System;

    /// <summary>
    ///   Welcome message and intro to the report tab
    /// </summary>
    public class EditorWelcome : TutorialPhase
    {
        private readonly string reportTab = MicrobeEditorGUI.EditorTab.Report.ToString();

        public override string ClosedByName { get; } = "MicrobeEditorReport";

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            gui.EditorEntryReportVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.EnteredMicrobeEditor:
                {
                    if (!HasBeenShown && CanTrigger)
                    {
                        Show();
                    }

                    break;
                }

                case TutorialEventType.MicrobeEditorTabChanged:
                {
                    string tab = ((StringEventArgs)args).Data;

                    // Hide when switched to another tab
                    if (tab != reportTab)
                    {
                        if (ShownCurrently)
                        {
                            Hide();
                        }
                    }

                    break;
                }
            }

            return false;
        }
    }
}
