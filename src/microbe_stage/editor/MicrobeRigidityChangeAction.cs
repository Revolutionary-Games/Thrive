using System;

public class MicrobeRigidityChangeAction : MicrobeEditorAction
{
    public MicrobeRigidityChangeAction(MicrobeEditor editor, int cost,
        Action<MicrobeEditorAction> redo, Action<MicrobeEditorAction> undo, RigidityChangeActionData data = null)
        : base(editor, cost, redo, undo, data)
    {
    }

    public void Combine(MicrobeRigidityChangeAction newAction)
    {
        newAction.Perform();
        ((RigidityChangeActionData)Data).NewRigidity = ((RigidityChangeActionData)newAction.Data).NewRigidity;
        Cost += newAction.Cost;
    }
}
