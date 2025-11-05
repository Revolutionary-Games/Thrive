using System;
using System.Collections.Generic;
using System.Linq;
using SharedBase.Archive;

/// <summary>
///   Combines multiple <see cref="EditorAction"/>s into one singular action to act as a single unit in the undo
///   system.
///   Note that this may not be used to combine actions that would interfere with each other in terms of MP usage as
///   that is not currently handled.
/// </summary>
public class CombinedEditorAction : EditorAction
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly List<EditorAction> actions;

    public CombinedEditorAction(params EditorAction[] actions)
    {
        if (actions.Length < 1)
            throw new ArgumentException("Actions can't be empty");

        this.actions = actions.ToList();
    }

    public CombinedEditorAction(IEnumerable<EditorAction> actions)
    {
        this.actions = actions.ToList();

        if (Actions.Count < 1)
            throw new ArgumentException("Actions can't be empty");
    }

    /// <summary>
    ///   Constructor variant that takes ownership of the action list
    /// </summary>
    public CombinedEditorAction(List<EditorAction> actions)
    {
        this.actions = actions;

        if (Actions.Count < 1)
            throw new ArgumentException("Actions can't be empty");
    }

    public IReadOnlyList<EditorAction> Actions => actions;

    // TODO: this probably allocates memory, so optimize this somehow further
    public override IEnumerable<EditorCombinableActionData> Data => Actions.SelectMany(a => a.Data);

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.CombinedEditorAction;

    public static CombinedEditorAction ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new CombinedEditorAction(reader.ReadObject<List<EditorAction>>());
        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(actions);

        writer.Write(SERIALIZATION_VERSION_REVERSIBLE);
        base.WriteToArchive(writer);
    }

    public override void DoAction()
    {
        foreach (var action in Actions)
            action.DoAction();
    }

    public override void UndoAction()
    {
        foreach (var action in Actions.Reverse())
            action.UndoAction();
    }

    public override double GetBaseCost()
    {
        return Actions.Sum(a => a.GetBaseCost());
    }

    public override double CalculateCost(IReadOnlyList<EditorAction> history, int insertPosition)
    {
        // TODO: hopefully the different actions don't interfere with each other in terms of MP usage, because then
        // this is not an accurate calculation

        double sum = 0;

        var count = Actions.Count;
        for (var i = 0; i < count; ++i)
        {
            sum += Actions[i].CalculateCost(history, insertPosition);
        }

        return sum;
    }

    public override void ApplyMergedData(IEnumerable<EditorCombinableActionData> newData)
    {
        var newDataList = newData.ToList();

        if (newDataList.Count < 1)
            throw new InvalidOperationException("Merged data didn't contain anything");

        int consumedItems = ApplyPartialMergedData(newDataList, 0);

        if (consumedItems != newDataList.Count)
            throw new InvalidOperationException("Merged data didn't contain right number of data items");
    }

    public override int ApplyPartialMergedData(List<EditorCombinableActionData> newData, int startIndex)
    {
        int index = 0;

        foreach (var subAction in actions.ToList())
        {
            int applied = subAction.ApplyPartialMergedData(newData, startIndex + index);

            if (applied < 1)
            {
                // This sub action didn't have data to apply to it, so destroy it as it is wanted to be removed
                actions.Remove(subAction);
            }
            else
            {
                index += applied;
            }
        }

        if (Actions.Count < 1)
            throw new InvalidOperationException("Merging data into a combined action caused it to become empty");

        return index;
    }

    public override void CopyData(ICollection<EditorCombinableActionData> target)
    {
        foreach (var action in actions)
        {
            action.CopyData(target);
        }
    }
}
