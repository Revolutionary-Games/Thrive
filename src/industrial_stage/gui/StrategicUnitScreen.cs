using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Base class for strategy stage controllable unit popup screens. For showing info about the unit and giving options
///   to control it.
/// </summary>
/// <typeparam name="T">The type of unit controller</typeparam>
public abstract class StrategicUnitScreen<T> : CustomDialog
    where T : class, IStrategicUnit
{
    [Export]
    public NodePath? ActionButtonsContainerPath;

    /// <summary>
    ///   Optional container for unit screens that have a list of subunits
    /// </summary>
    [Export]
    public NodePath? UnitListContainerPath;

#pragma warning disable CA2213
    protected Container actionButtonsContainer = null!;
    protected Container? unitListContainer;
#pragma warning restore CA2213

    protected T? managedUnit;

    private float elapsed = 1;

    /// <summary>
    ///   The unit this screen is open for, or null
    /// </summary>
    public T? OpenedForUnit => Visible ? managedUnit : null;

    public override void _Ready()
    {
        base._Ready();

        actionButtonsContainer = GetNode<Container>(ActionButtonsContainerPath);

        if (UnitListContainerPath != null)
            unitListContainer = GetNode<Container>(UnitListContainerPath);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        elapsed += delta;

        if (elapsed >= Constants.UNIT_SCREEN_UPDATE_INTERVAL && managedUnit != null)
        {
            elapsed = 0;
            RefreshShownData();
            UpdateTitle();

            // TODO: update subunit list if there are changes
        }
    }

    public void ShowForUnit(T unit)
    {
        if (Visible)
        {
            Close();
        }

        elapsed = 1;
        managedUnit = unit;

        UpdateTitle();
        UpdateAll();
        SetupAvailableActionButtons();

        if (unitListContainer != null)
        {
            SetupUnitList();
        }

        Show();
    }

    protected void UpdateTitle()
    {
        WindowTitle = managedUnit?.UnitScreenTitle ?? "ERROR";
    }

    protected void SetupAvailableActionButtons()
    {
        actionButtonsContainer.QueueFreeChildren();

        // TODO: add parameters / abstract methods to control what buttons are shown
        var button1 = new Button
        {
            Text = TranslationServer.Translate("UNIT_ACTION_MOVE"),
        };

        button1.Connect("pressed", this, nameof(OnMoveStart));

        actionButtonsContainer.AddChild(button1);

        var button2 = new Button
        {
            Text = TranslationServer.Translate("UNIT_ACTION_CONSTRUCT"),
        };

        button2.Connect("pressed", this, nameof(OnConstructStart));

        actionButtonsContainer.AddChild(button2);
    }

    protected virtual void SetupUnitList()
    {
        if (unitListContainer == null)
            throw new InvalidOperationException("This unit screen doesn't have subunit list set");

        unitListContainer.QueueFreeChildren();

        // TODO: probably some more properties will need to be returned by the sub unit listing
        foreach (var unit in ListSubUnits())
        {
            // TODO: make partial units selectable to easily split this apart

            var unitDisplay = new Label
            {
                Text = unit,
            };

            unitListContainer.AddChild(unitDisplay);
        }
    }

    protected virtual IEnumerable<string> ListSubUnits()
    {
        return Array.Empty<string>();
    }

    /// <summary>
    ///   Called when this is shown for a unit to update all the data. Note that <see cref="RefreshShownData"/> is not
    ///   automatically called right after this when this screen opens.
    /// </summary>
    protected abstract void UpdateAll();

    /// <summary>
    ///   Called periodically to refresh the shown unit data
    /// </summary>
    protected abstract void RefreshShownData();

    protected virtual void OnMoveStart()
    {
        OnUnhandledActionType();
    }

    protected virtual void OnConstructStart()
    {
        OnUnhandledActionType();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ActionButtonsContainerPath?.Dispose();
            UnitListContainerPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnUnhandledActionType()
    {
        GD.PrintErr("Non-overridden base action type method was called for a unit screen");
    }
}
