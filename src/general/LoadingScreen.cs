using System;
using Godot;

/// <summary>
///   A loading screen that shows cool stuff. For now this is meant to be used as a sub-scene to block out something the
///   player shouldn't see yet.
/// </summary>
public class LoadingScreen : Control
{
    [Export]
    public NodePath ArtDescriptionPath;
    [Export]
    public NodePath LoadingMessagePath;
    [Export]
    public NodePath LoadingDescriptionPath;
    [Export]
    public NodePath TipLabelPath;

    /// <summary>
    ///   How fast the loading indicator spins
    /// </summary>
    [Export]
    public float SpinnerSpeed = 180.0f;

    private Label artDescription;
    private Label loadingMessageLabel;
    private Label loadingDescriptionLabel;
    private Label tipLabel;
    private Control spinner;

    private string loadingMessage = "Loading";
    private string loadingDescription = string.Empty;
    private string tip = "TIP: press the undo button in the editor to correct a mistake";

    private float totalElapsed = 0;

    public string LoadingMessage
    {
        get
        {
            return loadingMessage;
        }
        set
        {
            if (loadingMessage == value)
                return;

            loadingMessage = value;

            if (loadingMessageLabel != null)
            {
                UpdateMessage();
            }
        }
    }

    public string LoadingDescription
    {
        get
        {
            return loadingDescription;
        }
        set
        {
            if (loadingDescription == value)
                return;

            loadingDescription = value;

            if (loadingDescriptionLabel != null)
            {
                UpdateDescription();
            }
        }
    }

    public string Tip
    {
        get
        {
            return tip;
        }
        set
        {
            if (tip == value)
                return;

            tip = value;

            if (tipLabel != null)
            {
                UpdateTip();
            }
        }
    }

    public override void _Ready()
    {
        artDescription = GetNode<Label>(ArtDescriptionPath);
        loadingMessageLabel = GetNode<Label>(LoadingMessagePath);
        loadingDescriptionLabel = GetNode<Label>(LoadingDescriptionPath);
        tipLabel = GetNode<Label>(TipLabelPath);

        spinner = GetNode<Control>("Spinner");

        // TODO: implement randomized art showing

        // TODO: implement randomized tip showing

        UpdateMessage();
        UpdateDescription();
        UpdateTip();
    }

    public override void _Process(float delta)
    {
        // Spin the spinner
        totalElapsed += delta;

        spinner.RectRotation = (int)(totalElapsed * SpinnerSpeed) % 360;
    }

    private void UpdateMessage()
    {
        loadingMessageLabel.Text = LoadingMessage;
    }

    private void UpdateDescription()
    {
        loadingDescriptionLabel.Text = LoadingDescription;
    }

    private void UpdateTip()
    {
        tipLabel.Text = Tip;
    }
}
