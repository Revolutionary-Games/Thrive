public class MicrobeEditorActionHistory : ActionHistory<MicrobeEditorAction>
{
    public new void AddAction(MicrobeEditorAction action)
    {
        // If action is RigidityChange, try to combine it with the previous one.
        if (CanUndo())
        {
            if (action is MicrobeRigidityChangeAction newAction
                && actions[actionIndex - 1] is MicrobeRigidityChangeAction previousAction)
            {
                previousAction.Combine(newAction);
                return;
            }
        }

        base.AddAction(action);
    }
}
