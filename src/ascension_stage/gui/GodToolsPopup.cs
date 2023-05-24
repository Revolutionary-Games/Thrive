using System;
using Godot;

/// <summary>
///   Shows the god tools available to mess with a game object
/// </summary>
public class GodToolsPopup : CustomDialog
{
    [Export]
    public NodePath? ActionButtonsContainerPath;

    [Export]
    public NodePath TargetEntityNameLabelPath = null!;

#pragma warning disable CA2213
    private Container actionButtonsContainer = null!;
    private Label targetEntityNameLabel = null!;
#pragma warning restore CA2213

    private EntityReference<IEntity>? targetEntity;

    public bool WantsPlayerToPickLocation { get; private set; }

    public override void _Ready()
    {
        actionButtonsContainer = GetNode<Container>(ActionButtonsContainerPath);
        targetEntityNameLabel = GetNode<Label>(TargetEntityNameLabelPath);
    }

    public override void _Process(float delta)
    {
        if (!Visible)
            return;
    }

    public void OpenForEntity(IEntity entity)
    {
        if (!entity.AliveMarker.Alive || !IsInstanceValid(entity.EntityNode))
        {
            GD.PrintErr("Can't open god tools for deleted entity");
            Close();
            return;
        }

        actionButtonsContainer.QueueFreeChildren();

        targetEntityNameLabel.Text = entity.EntityNode.Name;

        targetEntity = new EntityReference<IEntity>(entity);

        // TODO: entity specific buttons

        AddActionButton(TranslationServer.Translate("ACTION_DELETE"), nameof(OnDelete));

        Open(true);
    }

    /// <summary>
    ///   Provide a location the player clicked
    /// </summary>
    /// <param name="location">The clicked world location</param>
    /// <returns>True when this acted on the click, false otherwise</returns>
    public bool PlayerClickedLocation(Vector3 location)
    {
        if (!WantsPlayerToPickLocation)
            return false;

        throw new NotImplementedException();

        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ActionButtonsContainerPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void AddActionButton(string text, string methodName)
    {
        var button = new Button
        {
            Text = text,
            SizeFlagsHorizontal = 0,
        };

        button.Connect("pressed", this, methodName);

        actionButtonsContainer.AddChild(button);
    }

    private void OnDelete()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var target = GetTarget();

        target?.DestroyAndQueueFree();
        Close();
    }

    private IEntity? GetTarget()
    {
        var target = targetEntity?.Value;

        if (target == null)
        {
            GD.Print("God tools no longer has a target");
            Close();
            return null;
        }

        return target;
    }
}
