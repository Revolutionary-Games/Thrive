using System;
using Godot;
using Newtonsoft.Json;
using Object = Godot.Object;

/// <summary>
///   Base HUD class for stages where the player uses the strategic view
/// </summary>
/// <typeparam name="TStage">The type of the stage this HUD is for</typeparam>
[JsonObject(MemberSerialization.OptIn)]
public abstract class StrategyStageHUDBase<TStage> : HUDWithPausing, IStrategyStageHUD
    where TStage : Object, IStrategyStage
{
    [Export]
    public NodePath? HintTextPath;

    [Export]
    public NodePath HotBarPath = null!;

    [Export]
    public NodePath BottomLeftBarPath = null!;

#pragma warning disable CA2213
    protected Label hintText = null!;

    protected HUDBottomBar bottomLeftBar = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Access to the stage to retrieve information for display
    /// </summary>
    protected TStage? stage;

    // These are private so this is a separate block
#pragma warning disable CA2213
    private HBoxContainer hotBar = null!;
#pragma warning restore CA2213

    // These signals need to be copied to inheriting classes for Godot editor to pick them up
    [Signal]
    public delegate void OnOpenMenu();

    [Signal]
    public delegate void OnOpenMenuToHelp();

    /// <summary>
    ///   Gets and sets the text that appears at the upper HUD.
    /// </summary>
    public string HintText
    {
        get => hintText.Text;
        set => hintText.Text = value;
    }

    public override void _Ready()
    {
        base._Ready();

        hintText = GetNode<Label>(HintTextPath);
        hotBar = GetNode<HBoxContainer>(HotBarPath);

        bottomLeftBar = GetNode<HUDBottomBar>(BottomLeftBarPath);
    }

    public void Init(TStage containedInStage)
    {
        stage = containedInStage;
    }

    public override void OnEnterStageTransition(bool longerDuration, bool returningFromEditor)
    {
        if (stage == null)
            throw new InvalidOperationException("Stage not setup for HUD");

        if (stage.IsLoadedFromSave && !returningFromEditor)
        {
            // TODO: make it so that the below sequence can be added anyway to not have to have this special logic here
            stage.OnFinishTransitioning();
            return;
        }

        AddFadeIn(stage, longerDuration);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (HintTextPath != null)
            {
                HintTextPath.Dispose();
                HotBarPath.Dispose();
                BottomLeftBarPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
