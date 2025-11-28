using System;
using System.Collections.Generic;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Editor for the behaviour of a (microbe) species
/// </summary>
[IgnoreNoMethodsTakingInput]
public partial class BehaviourEditorSubComponent : EditorComponentBase<ICellEditorData>, IArchiveUpdatable
{
    public const ushort SERIALIZATION_VERSION = 1;

#pragma warning disable CA2213
    [Export]
    private Slider aggressionSlider = null!;

    [Export]
    private Slider opportunismSlider = null!;

    [Export]
    private Slider fearSlider = null!;

    [Export]
    private Slider activitySlider = null!;

    [Export]
    private Slider focusSlider = null!;
#pragma warning restore CA2213

    private BehaviourDictionary? behaviour;

    [Signal]
    public delegate void OnBehaviourChangedEventHandler();

    public override bool IsSubComponent => true;

    public BehaviourDictionary? Behaviour
    {
        get => behaviour;
        private set => behaviour = value;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.BehaviourEditorSubComponent;

    public bool CanBeSpecialReference => true;

    public override void _Ready()
    {
        base._Ready();

        RegisterTooltips();
    }

    public override void OnEditorSpeciesSetup(Species species)
    {
        base.OnEditorSpeciesSetup(species);

        Behaviour = Editor.EditedBaseSpecies.Behaviour.Clone();
    }

    public override void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        writer.Write(SERIALIZATION_VERSION_BASE);
        base.WritePropertiesToArchive(writer);

        // For example, in multicellular, there are instances of this component that are unused, so can be not
        // initialised
        writer.WriteObjectOrNull(Behaviour);
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        base.ReadPropertiesFromArchive(reader, reader.ReadUInt16());

        Behaviour = reader.ReadObjectOrNull<BehaviourDictionary>();
    }

    public override void OnFinishEditing()
    {
        Editor.EditedBaseSpecies.ModifiableBehaviour =
            Behaviour ?? throw new Exception("Editor has not created behaviour object");
    }

    public override void UpdateUndoRedoButtons(bool canUndo, bool canRedo)
    {
    }

    public override void OnInsufficientMP(bool playSound = true)
    {
    }

    public override void OnActionBlockedWhileAnotherIsInProgress()
    {
    }

    public override void OnMutationPointsChanged(double mutationPoints)
    {
    }

    public override void OnValidAction(IEnumerable<CombinableActionData> actions)
    {
    }

    public void ResetBehaviour()
    {
        behaviour = new BehaviourDictionary();
        UpdateAllBehaviouralSliders(behaviour);
        EmitSignal(SignalName.OnBehaviourChanged);
    }

    public void SetBehaviouralValue(BehaviouralValueType type, float value)
    {
        UpdateBehaviourSlider(type, value);

        if (Behaviour == null)
            throw new Exception($"{nameof(Behaviour)} is not set for editor");

        var oldValue = Behaviour[type];

        if (Math.Abs(value - oldValue) < MathUtils.EPSILON)
            return;

        var action = new SingleEditorAction<BehaviourActionData>(DoBehaviourChangeAction, UndoBehaviourChangeAction,
            new BehaviourActionData(value, oldValue, type));

        Editor.EnqueueAction(action);
    }

    public void UpdateAllBehaviouralSliders(BehaviourDictionary newBehaviour)
    {
        foreach (var pair in newBehaviour)
            UpdateBehaviourSlider(pair.Key, pair.Value);
    }

    internal void UpdateBehaviourSlider(BehaviouralValueType type, float value)
    {
        switch (type)
        {
            case BehaviouralValueType.Activity:
                activitySlider.Value = value;
                break;
            case BehaviouralValueType.Aggression:
                aggressionSlider.Value = value;
                break;
            case BehaviouralValueType.Opportunism:
                opportunismSlider.Value = value;
                break;
            case BehaviouralValueType.Fear:
                fearSlider.Value = value;
                break;
            case BehaviouralValueType.Focus:
                focusSlider.Value = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, $"BehaviouralValueType {type} is not valid");
        }
    }

    protected override void OnTranslationsChanged()
    {
    }

    protected override void RegisterTooltips()
    {
        base.RegisterTooltips();

        aggressionSlider.RegisterToolTipForControl("aggressionSlider", "editor");
        opportunismSlider.RegisterToolTipForControl("opportunismSlider", "editor");
        fearSlider.RegisterToolTipForControl("fearSlider", "editor");
        activitySlider.RegisterToolTipForControl("activitySlider", "editor");
        focusSlider.RegisterToolTipForControl("focusSlider", "editor");
    }

    private void OnBehaviourValueChanged(float value, string behaviourName)
    {
        if (!Enum.TryParse(behaviourName, out BehaviouralValueType behaviouralValueType))
            throw new ArgumentException($"{behaviourName} is not a valid BehaviouralValueType");

        SetBehaviouralValue(behaviouralValueType, value);
    }

    [ArchiveAllowedMethod]
    private void DoBehaviourChangeAction(BehaviourActionData data)
    {
        if (Behaviour == null)
            throw new InvalidOperationException($"Editor has no {nameof(Behaviour)} set for change action to use");

        Behaviour[data.Type] = data.NewValue;
        UpdateBehaviourSlider(data.Type, data.NewValue);

        EmitSignal(SignalName.OnBehaviourChanged);
    }

    [ArchiveAllowedMethod]
    private void UndoBehaviourChangeAction(BehaviourActionData data)
    {
        if (Behaviour == null)
            throw new InvalidOperationException($"Editor has no {nameof(Behaviour)} set for change action to use");

        Behaviour[data.Type] = data.OldValue;
        UpdateBehaviourSlider(data.Type, data.OldValue);

        EmitSignal(SignalName.OnBehaviourChanged);
    }
}
