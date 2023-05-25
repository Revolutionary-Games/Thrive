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

    private bool wantsTeleport;

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

        // Close if the target is gone
        if (GetTarget() == null)
        {
            GD.Print("Closing god tools for a disappeared target");
            Close();
        }
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
        wantsTeleport = false;

        targetEntityNameLabel.Text = entity.EntityNode.Name;

        targetEntity = new EntityReference<IEntity>(entity);

        // TODO: entity specific buttons

        if (entity is Spatial)
        {
            AddActionButton(TranslationServer.Translate("ACTION_TELEPORT"), nameof(OnTeleportState), true);
        }

        AddActionButton(TranslationServer.Translate("ACTION_DELETE"), nameof(OnDelete));

        Open(false);

        UpdateWantsToPickLocation();
    }

    /// <summary>
    ///   Provide a location the player clicked
    /// </summary>
    /// <param name="location">The clicked world location</param>
    /// <returns>True when this acted on the click, false otherwise</returns>
    public bool PlayerClickedLocation(Vector3 location)
    {
        if (!Visible || !WantsPlayerToPickLocation)
            return false;

        if (wantsTeleport)
        {
            // TODO: auto reset this? (needs to reset the button state)
            // wantsTeleport = false;

            var target = GetTarget();

            if (target is Spatial spatial)
            {
                spatial.GlobalTranslation = location;
            }
            else
            {
                GD.PrintErr("Can't teleport current entity (null or invalid cast)");
            }

            UpdateWantsToPickLocation();
            return true;
        }

        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ActionButtonsContainerPath != null)
            {
                ActionButtonsContainerPath.Dispose();
                TargetEntityNameLabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void AddActionButton(string text, string methodName, bool toggleButton = false)
    {
        var button = new Button
        {
            Text = text,
            SizeFlagsHorizontal = 0,
        };

        if (toggleButton)
        {
            button.ToggleMode = true;
            button.Connect("toggled", this, methodName);
        }
        else
        {
            button.Connect("pressed", this, methodName);
        }

        actionButtonsContainer.AddChild(button);
    }

    private void OnDelete()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var target = GetTarget();

        target?.DestroyAndQueueFree();
        Close();
    }

    private void OnTeleportState(bool pressed)
    {
        GUICommon.Instance.PlayButtonPressSound();

        wantsTeleport = pressed;
        UpdateWantsToPickLocation();

        // TODO: add some kind of visual indicator for when this is active that follows the cursor / world point
        // the cursor is over
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

    private void UpdateWantsToPickLocation()
    {
        WantsPlayerToPickLocation = wantsTeleport;
    }
}
