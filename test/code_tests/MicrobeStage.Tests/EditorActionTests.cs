namespace ThriveTest.MicrobeStage;

using System;
using System.Linq;
using Xunit;

public class EditorActionTests
{
    [Fact]
    public void EditorAction_SubsequentRigidityChangesCombine()
    {
        var history = new EditorActionHistory<EditorAction>();

        var rigidityAction1 = new RigidityActionData(0.2f, 0.1f);

        bool done = false;

        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => done = true, _ => { }, rigidityAction1));

        var rigidityAction2 = new RigidityActionData(0.3f, 0.2f);
        Assert.True(done);
        done = false;

        history.AddAction(new SingleEditorAction<RigidityActionData>(_ => done = true, _ => { }, rigidityAction2));

        Assert.True(history.CanUndo());
        Assert.True(done);

        var action = history.PopTopAction();
        var combinedAction = Assert.IsAssignableFrom<SingleEditorAction<RigidityActionData>>(action);

        var data = combinedAction.Data.ToList();
        Assert.Single(data);

        var castedData = Assert.IsAssignableFrom<RigidityActionData>(data[0]);
        Assert.Equal(0.1f, castedData.PreviousRigidity);
        Assert.Equal(0.3f, castedData.NewRigidity);

        Assert.False(history.CanUndo());

        // There's no easy way to verify the history is completely empty, so we misuse an API here and expect an error
        Assert.Throws<ArgumentOutOfRangeException>(() => history.PopTopAction());
    }
}
