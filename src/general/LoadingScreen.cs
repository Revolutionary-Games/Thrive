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

    private Label artDescription;
    private Label loadingMessageLabel;
    private Label loadingDescriptionLabel;

    private string loadingMessage = "Loading";
    private string loadingDescription = string.Empty;

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

    public override void _Ready()
    {
        artDescription = GetNode<Label>(ArtDescriptionPath);
        loadingMessageLabel = GetNode<Label>(LoadingMessagePath);
        loadingDescriptionLabel = GetNode<Label>(LoadingDescriptionPath);

        // TODO: implement randomized art showing

        UpdateMessage();
        UpdateDescription();
    }

    public override void _Process(float delta)
    {
        // TODO: spinning thrive logo in the bottom right
    }

    private void UpdateMessage()
    {
        loadingMessageLabel.Text = LoadingMessage;
    }

    private void UpdateDescription()
    {
        loadingDescriptionLabel.Text = LoadingDescription;
    }
}
