using Godot;

/// <summary>
///   Base GUI for editor types that derive from <see cref="HexEditorBase{TGUI,TAction,TStage,THexMove}"/>
/// </summary>
public abstract class HexEditorGUIBase<TEditor> : EditorWithPatchesGUIBase<TEditor>, IHexEditorGUI
    where TEditor : Object, IHexEditor, IEditorWithPatches
{
    [Export]
    public NodePath SymmetryButtonPath = null!;

    [Export]
    public NodePath SymmetryIconPath = null!;

    [Export]
    public NodePath IslandErrorPath = null!;

    private TextureButton symmetryButton = null!;
    private TextureRect symmetryIcon = null!;

    private Texture symmetryIconDefault = null!;
    private Texture symmetryIcon2X = null!;
    private Texture symmetryIcon4X = null!;
    private Texture symmetryIcon6X = null!;

    private CustomConfirmationDialog islandPopup = null!;

    private HexEditorSymmetry symmetry = HexEditorSymmetry.None;

    public override void _Ready()
    {
        base._Ready();

        symmetryButton = GetNode<TextureButton>(SymmetryButtonPath);
        symmetryIcon = GetNode<TextureRect>(SymmetryIconPath);

        symmetryIconDefault = GD.Load<Texture>("res://assets/textures/gui/bevel/1xSymmetry.png");
        symmetryIcon2X = GD.Load<Texture>("res://assets/textures/gui/bevel/2xSymmetry.png");
        symmetryIcon4X = GD.Load<Texture>("res://assets/textures/gui/bevel/4xSymmetry.png");
        symmetryIcon6X = GD.Load<Texture>("res://assets/textures/gui/bevel/6xSymmetry.png");

        islandPopup = GetNode<CustomConfirmationDialog>(IslandErrorPath);
    }

    public void SetSymmetry(HexEditorSymmetry newSymmetry)
    {
        symmetry = newSymmetry;
        editor!.Symmetry = newSymmetry;

        UpdateSymmetryIcon();
    }

    public void ResetSymmetryButton()
    {
        symmetryIcon.Texture = symmetryIconDefault;
        symmetry = 0;
    }

    protected override void RegisterTooltips()
    {
        base.RegisterTooltips();
        symmetryButton.RegisterToolTipForControl("symmetryButton", "editor");
    }

    protected override bool EditorCanFinishEditingLate()
    {
        if (!base.EditorCanFinishEditingLate())
            return false;

        // Can't exit the editor with disconnected organelles
        if (editor.HasIslands)
        {
            islandPopup.PopupCenteredShrink();
            return false;
        }

        return true;
    }

    protected void OnSymmetryClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (symmetry == HexEditorSymmetry
                .SixWaySymmetry)
        {
            ResetSymmetryButton();
        }
        else if (symmetry == HexEditorSymmetry.None)
        {
            symmetry = HexEditorSymmetry.XAxisSymmetry;
        }
        else if (symmetry == HexEditorSymmetry
                     .XAxisSymmetry)
        {
            symmetry = HexEditorSymmetry
                .FourWaySymmetry;
        }
        else if (symmetry == HexEditorSymmetry
                     .FourWaySymmetry)
        {
            symmetry = HexEditorSymmetry
                .SixWaySymmetry;
        }

        editor!.Symmetry = symmetry;
        UpdateSymmetryIcon();
    }

    protected void OnSymmetryHold()
    {
        symmetryIcon.Modulate = new Color(0, 0, 0);
    }

    protected void OnSymmetryReleased()
    {
        symmetryIcon.Modulate = new Color(1, 1, 1);
    }

    private void UpdateSymmetryIcon()
    {
        switch (symmetry)
        {
            case HexEditorSymmetry.None:
                symmetryIcon.Texture = symmetryIconDefault;
                break;
            case HexEditorSymmetry.XAxisSymmetry:
                symmetryIcon.Texture = symmetryIcon2X;
                break;
            case HexEditorSymmetry.FourWaySymmetry:
                symmetryIcon.Texture = symmetryIcon4X;
                break;
            case HexEditorSymmetry.SixWaySymmetry:
                symmetryIcon.Texture = symmetryIcon6X;
                break;
        }
    }
}
