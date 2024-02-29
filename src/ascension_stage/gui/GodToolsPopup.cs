using System.Linq;
using Godot;

/// <summary>
///   Shows the god tools available to mess with a game object
/// </summary>
public partial class GodToolsPopup : CustomWindow
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

    public override void _Process(double delta)
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

        switch (entity)
        {
            case SpaceFleet:
                AddActionButton(Localization.Translate("ACTION_DUPLICATE_UNITS"), nameof(OnDuplicateUnits));
                break;
            case PlacedPlanet:
                AddActionButton(Localization.Translate("ACTION_DOUBLE_POPULATION"), nameof(OnDoublePopulation));
                AddActionButton(Localization.Translate("ACTION_HALF_POPULATION"), nameof(OnHalfPopulation));
                break;
        }

        if (entity is Node3D)
        {
            AddActionButton(Localization.Translate("ACTION_TELEPORT"), nameof(OnTeleportState), true);
        }

        AddActionButton(Localization.Translate("ACTION_DELETE"), nameof(OnDelete));

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

            if (target is Node3D spatial)
            {
                spatial.GlobalPosition = location;
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
            button.Connect(BaseButton.SignalName.Toggled, new Callable(this, methodName));
        }
        else
        {
            button.Connect(BaseButton.SignalName.Pressed, new Callable(this, methodName));
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

    private void OnDuplicateUnits()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var target = GetTarget();
        if (target == null)
            return;

        switch (target)
        {
            case SpaceFleet fleet:
                foreach (var unit in fleet.Ships.ToList())
                {
                    fleet.AddShip(unit);
                }

                break;
            default:
                GD.PrintErr("Unknown entity to handle to duplicate units");
                break;
        }
    }

    private void OnDoublePopulation()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var target = GetTarget();
        if (target == null)
            return;

        switch (target)
        {
            case PlacedPlanet planet:
                planet.Population *= 2;

                break;
            default:
                GD.PrintErr("Unknown entity to handle to double population");
                break;
        }
    }

    private void OnHalfPopulation()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var target = GetTarget();
        if (target == null)
            return;

        switch (target)
        {
            case PlacedPlanet planet:
                planet.Population /= 2;

                // Make sure there's at least someone there so that the planet can still grow
                if (planet.Population < 1)
                    planet.Population = 1;

                break;
            default:
                GD.PrintErr("Unknown entity to handle to half population");
                break;
        }
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
