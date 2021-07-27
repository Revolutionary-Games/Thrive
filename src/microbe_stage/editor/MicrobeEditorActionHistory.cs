public class MicrobeEditorActionHistory : ActionHistory<MicrobeEditorAction>
{
    public new void AddAction(MicrobeEditorAction action)
    {
        // If action is RigidityChange, try to combine it with the previous one.
        if (CanUndo())
        {
            if (action is MicrobeRigidityChangeAction && actions[actionIndex - 1] is MicrobeRigidityChangeAction)
            {
                ((MicrobeRigidityChangeAction)actions[actionIndex - 1]).Combine((MicrobeRigidityChangeAction)action);
                return;
            }
        }

        base.AddAction(action);
    }
}
