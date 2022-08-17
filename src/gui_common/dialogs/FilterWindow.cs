using Godot;
using System;

public class FilterWindow : CustomDialog
{
    [Export]
    public bool HideOnApply = true;

    // Paths for nodes
    public NodePath DialogLabelPath = "VBoxContainer/Label";
    public NodePath ApplyButtonPath = "VBoxContainer/HBoxContainer/ApplyButton";
    public NodePath CancelButtonPath = "VBoxContainer/HBoxContainer/CancelButton";

    // Texts for buttons & labels
    private string dialogText = string.Empty;
    private string applyText = "APPLY";
    private string cancelText = "CANCEL";

    // Nodes
    private Label? dialogLabel;
    private Button? applyButton;
    private Button? cancelButton;

    [Signal]
    public delegate void Applied();

    [Signal]
    public delegate void Cancelled();

    /// <summary>
    ///   The text displayed by the dialog.
    /// </summary>
    [Export(PropertyHint.MultilineText)]
    public string DialogText
    {
        get => dialogText;
        set
        {
            dialogText = value;

            if (dialogLabel != null)
                UpdateLabel();
        }
    }

    /// <summary>
    ///   The text to be shown on the confirm button.
    /// </summary>
    [Export]
    public string ApplyText
    {
        get => applyText;
        set
        {
            applyText = value;

            if (applyButton != null)
                UpdateButtons();
        }
    }

    /// <summary>
    ///   The text to be shown on the cancel button.
    /// </summary>
    [Export]
    public string CancelText
    {
        get => cancelText;
        set
        {
            cancelText = value;

            if (cancelButton != null)
                UpdateButtons();
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        dialogLabel = GetNode<Label>(DialogLabelPath);
        applyButton = GetNode<Button>(ApplyButtonPath);
        cancelButton = GetNode<Button>(CancelButtonPath);

        UpdateLabel();
        UpdateButtons();
    }

    private void UpdateLabel()
    {
        if (dialogLabel == null)
            throw new SceneTreeAttachRequired();

        dialogLabel.Text = TranslationServer.Translate(dialogText);
    }

    private void UpdateButtons()
    {
        if (applyButton == null || cancelButton == null)
            throw new SceneTreeAttachRequired();

        applyButton.Text = applyText;
        cancelButton.Text = cancelText;
    }

    private void OnApplyPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (HideOnApply)
            Hide();

        EmitSignal(nameof(Applied));
    }

    private void onCancelPlaced()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Hide();
        EmitSignal(nameof(Cancelled));
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
