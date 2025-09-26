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

    [Fact]
    public void EditorAction_ToleranceChangesCombine()
    {
        var history = new EditorActionHistory<EditorAction>();

        var initialTolerances = new EnvironmentalTolerances();

        var changedTolerances1 = initialTolerances.Clone();
        changedTolerances1.TemperatureTolerance += 8;

        var toleranceData1 = new ToleranceActionData(initialTolerances, changedTolerances1);
        history.AddAction(new SingleEditorAction<ToleranceActionData>(_ => { }, _ => { }, toleranceData1));

        var changedTolerances2 = changedTolerances1.Clone();
        changedTolerances2.TemperatureTolerance += 5;

        var toleranceData2 = new ToleranceActionData(changedTolerances1, changedTolerances2);
        history.AddAction(new SingleEditorAction<ToleranceActionData>(_ => { }, _ => { }, toleranceData2));

        // Then changing a different stat shouldn't combine
        var changedTolerances3 = changedTolerances2.Clone();
        changedTolerances3.UVResistance += 0.5f;

        var toleranceData3 = new ToleranceActionData(changedTolerances2, changedTolerances3);
        history.AddAction(new SingleEditorAction<ToleranceActionData>(_ => { }, _ => { }, toleranceData3));

        Assert.True(history.CanUndo());
        Assert.True(history.Undo());
        Assert.True(history.CanUndo());
        Assert.True(history.Undo());
        Assert.False(history.CanUndo());

        Assert.True(history.Redo());
        Assert.True(history.Redo());

        history.PopTopAction();
        var action = history.PopTopAction();

        var combinedAction = Assert.IsAssignableFrom<SingleEditorAction<ToleranceActionData>>(action);

        var data = combinedAction.Data.ToList();
        Assert.Single(data);

        var castedData = Assert.IsAssignableFrom<ToleranceActionData>(data[0]);

        Assert.Equal(initialTolerances, castedData.OldTolerances);
        Assert.Equal(changedTolerances2, castedData.NewTolerances);
    }

    [Fact]
    public void EditorAction_SubsequentBehaviourChangesCombine()
    {
        var history = new EditorActionHistory<EditorAction>();

        var behaviourAction1 = new BehaviourActionData(50, 1, BehaviouralValueType.Activity);

        bool done = false;

        history.AddAction(new SingleEditorAction<BehaviourActionData>(_ => done = true, _ => { }, behaviourAction1));
        Assert.True(done);
        done = false;

        var behaviourAction2 = new BehaviourActionData(100, 50, BehaviouralValueType.Activity);

        history.AddAction(new SingleEditorAction<BehaviourActionData>(_ => done = true, _ => { }, behaviourAction2));

        Assert.True(history.CanUndo());
        Assert.True(done);

        var action = history.PopTopAction();
        var combinedAction = Assert.IsAssignableFrom<SingleEditorAction<BehaviourActionData>>(action);

        var data = combinedAction.Data.ToList();
        Assert.Single(data);

        var castedData = Assert.IsAssignableFrom<BehaviourActionData>(data[0]);
        Assert.Equal(1, castedData.OldValue);
        Assert.Equal(100, castedData.NewValue);
        Assert.Equal(BehaviouralValueType.Activity, castedData.Type);

        Assert.False(history.CanUndo());

        // There's no easy way to verify the history is completely empty, so we misuse an API here and expect an error
        Assert.Throws<ArgumentOutOfRangeException>(() => history.PopTopAction());
    }

    [Fact]
    public void EditorAction_DifferentBehaviourChangeStatsDoNotCombine()
    {
        var history = new EditorActionHistory<EditorAction>();

        var behaviourAction1 = new BehaviourActionData(50, 1, BehaviouralValueType.Activity);

        bool done = false;

        history.AddAction(new SingleEditorAction<BehaviourActionData>(_ => done = true, _ => { }, behaviourAction1));
        Assert.True(done);
        done = false;

        var behaviourAction2 = new BehaviourActionData(100, 50, BehaviouralValueType.Aggression);

        history.AddAction(new SingleEditorAction<BehaviourActionData>(_ => done = true, _ => { }, behaviourAction2));

        Assert.True(history.CanUndo());
        Assert.True(done);

        Assert.NotNull(history.PopTopAction());

        var action = history.PopTopAction();
        var combinedAction = Assert.IsAssignableFrom<SingleEditorAction<BehaviourActionData>>(action);

        var data = combinedAction.Data.ToList();
        Assert.Single(data);

        var castedData = Assert.IsAssignableFrom<BehaviourActionData>(data[0]);
        Assert.Equal(1, castedData.OldValue);
        Assert.Equal(50, castedData.NewValue);
        Assert.Equal(BehaviouralValueType.Activity, castedData.Type);

        Assert.False(history.CanUndo());
    }
}
