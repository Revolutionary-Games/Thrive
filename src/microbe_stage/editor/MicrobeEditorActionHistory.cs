public class MicrobeEditorActionHistory : ActionHistory<MicrobeEditorAction>
{
    public new void AddAction(MicrobeEditorAction action)
    {
        // If action is RigidityChange, try to combine it with the previous one.
        if (CanUndo())
        {
            RigidityChangeActionData data = action.Data as RigidityChangeActionData;
            RigidityChangeActionData previousData = actions[actionIndex - 1].Data as RigidityChangeActionData;
            if (data != null && previousData != null)
            {
                data.PreviousRigidity = previousData.PreviousRigidity;
                MicrobeEditorAction combinedAction = new MicrobeEditorAction(action.Editor,
                    action.Cost + actions[actionIndex - 1].Cost, action.Redo, action.Undo, action.Data);

                Undo();
                base.AddAction(combinedAction);
                return;
            }
        }

        base.AddAction(action);
    }
}
