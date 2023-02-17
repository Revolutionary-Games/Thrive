using System;
using Godot;
using Newtonsoft.Json;

[DeserializedCallbackTarget]
[IgnoreNoMethodsTakingInput]
[SceneLoadedClass("res://src/microbe_stage/editor/BehaviourEditorSubComponent.tscn", UsesEarlyResolve = false)]
public class BehaviourEditorSubComponent : EditorComponentBase<ICellEditorData>
{
    [Export]
    public NodePath? AggressionSliderPath;

    [Export]
    public NodePath OpportunismSliderPath = null!;

    [Export]
    public NodePath FearSliderPath = null!;

    [Export]
    public NodePath ActivitySliderPath = null!;

    [Export]
    public NodePath FocusSliderPath = null!;

#pragma warning disable CA2213
    private Slider aggressionSlider = null!;
    private Slider opportunismSlider = null!;
    private Slider fearSlider = null!;
    private Slider activitySlider = null!;
    private Slider focusSlider = null!;
#pragma warning restore CA2213

    private BehaviourDictionary? behaviour;

    // TODO: as this is mostly just to guard against Behaviour being missing (when loading older saves), this field
    // can probably be removed soon
    [JsonProperty]
    private Species? editedSpecies;

    [JsonIgnore]
    public override bool IsSubComponent => true;

    [JsonProperty]
    public BehaviourDictionary? Behaviour
    {
        get => behaviour ??= editedSpecies?.Behaviour;
        private set => behaviour = value;
    }

    public override void _Ready()
    {
        base._Ready();

        aggressionSlider = GetNode<Slider>(AggressionSliderPath);
        opportunismSlider = GetNode<Slider>(OpportunismSliderPath);
        fearSlider = GetNode<Slider>(FearSliderPath);
        activitySlider = GetNode<Slider>(ActivitySliderPath);
        focusSlider = GetNode<Slider>(FocusSliderPath);

        RegisterTooltips();
    }

    public override void OnEditorSpeciesSetup(Species species)
    {
        base.OnEditorSpeciesSetup(species);

        editedSpecies = Editor.EditedBaseSpecies;

        Behaviour = editedSpecies.Behaviour;
    }

    public override void OnFinishEditing()
    {
        Editor.EditedBaseSpecies.Behaviour =
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

    public override void OnMutationPointsChanged(int mutationPoints)
    {
    }

    public override void OnValidAction()
    {
    }

    public void ResetBehaviour()
    {
        behaviour = new BehaviourDictionary();
        UpdateAllBehaviouralSliders(behaviour);
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

    public void UpdateAllBehaviouralSliders(BehaviourDictionary behaviour)
    {
        foreach (var pair in behaviour)
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (AggressionSliderPath != null)
            {
                AggressionSliderPath.Dispose();
                OpportunismSliderPath.Dispose();
                FearSliderPath.Dispose();
                ActivitySliderPath.Dispose();
                FocusSliderPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnBehaviourValueChanged(float value, string behaviourName)
    {
        if (!Enum.TryParse(behaviourName, out BehaviouralValueType behaviouralValueType))
            throw new ArgumentException($"{behaviourName} is not a valid BehaviouralValueType");

        SetBehaviouralValue(behaviouralValueType, value);
    }

    [DeserializedCallbackAllowed]
    private void DoBehaviourChangeAction(BehaviourActionData data)
    {
        if (Behaviour == null)
            throw new InvalidOperationException($"Editor has no {nameof(Behaviour)} set for change action to use");

        Behaviour[data.Type] = data.NewValue;
        UpdateBehaviourSlider(data.Type, data.NewValue);
    }

    [DeserializedCallbackAllowed]
    private void UndoBehaviourChangeAction(BehaviourActionData data)
    {
        if (Behaviour == null)
            throw new InvalidOperationException($"Editor has no {nameof(Behaviour)} set for change action to use");

        Behaviour[data.Type] = data.OldValue;
        UpdateBehaviourSlider(data.Type, data.OldValue);
    }
}
