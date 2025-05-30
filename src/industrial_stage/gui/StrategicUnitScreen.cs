﻿using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Base class for strategy stage controllable unit popup screens. For showing info about the unit and giving options
///   to control it.
/// </summary>
/// <typeparam name="T">The type of unit controller</typeparam>
[GodotAbstract]
public partial class StrategicUnitScreen<T> : CustomWindow
    where T : GodotObject, IStrategicUnit, IEntity
{
    /// <summary>
    ///   Optional container for unit screens that have a list of subunits
    /// </summary>
#pragma warning disable CA2213
    [Export]
    protected Container actionButtonsContainer = null!;

    [Export]
    protected Container? unitListContainer;
#pragma warning restore CA2213

    protected EntityReference<T>? managedUnit;

    private double elapsed = 1;

    protected StrategicUnitScreen()
    {
    }

    [Signal]
    public delegate void OnOpenGodToolsEventHandler(GodotObject unit);

    /// <summary>
    ///   The unit this screen is open for, or null
    /// </summary>
    public T? OpenedForUnit => Visible ? managedUnit?.Value : null;

    protected T? UnitEvenWhileClosed => managedUnit?.Value;

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Visible)
            return;

        elapsed += delta;

        if (elapsed >= Constants.UNIT_SCREEN_UPDATE_INTERVAL)
        {
            if (UnitEvenWhileClosed == null)
            {
                GD.Print("Closing unit screen with now missing unit");
                Close();
                return;
            }

            elapsed = 0;
            RefreshShownData();
            UpdateTitle();

            // TODO: update subunit list if there are changes
        }
    }

    public void ShowForUnit(T unit, bool showGodTools)
    {
        if (Visible)
        {
            Close();
        }

        elapsed = 1;
        managedUnit = new EntityReference<T>(unit);

        UpdateTitle();
        UpdateAll();
        SetupAvailableActionButtons(showGodTools);

        if (unitListContainer != null)
        {
            SetupUnitList();
        }

        Show();
    }

    protected void UpdateTitle()
    {
        WindowTitle = UnitEvenWhileClosed?.UnitScreenTitle ?? "ERROR";
    }

    protected void SetupAvailableActionButtons(bool showGodTools)
    {
        actionButtonsContainer.QueueFreeChildren();

        // TODO: add parameters / abstract methods to control what buttons are shown
        var button1 = new Button
        {
            Text = Localization.Translate("UNIT_ACTION_MOVE"),
        };

        button1.Connect(BaseButton.SignalName.Pressed, new Callable(this, nameof(OnMoveStart)));

        actionButtonsContainer.AddChild(button1);

        // TODO: stop action (not added for now to avoid players accidentally leaving unfinishable buildings around

        var button2 = new Button
        {
            Text = Localization.Translate("UNIT_ACTION_CONSTRUCT"),
        };

        button2.Connect(BaseButton.SignalName.Pressed, new Callable(this, nameof(OnConstructStart)));

        actionButtonsContainer.AddChild(button2);

        if (showGodTools)
        {
            var godButton = new Button
            {
                Text = Localization.Translate("OPEN_GOD_TOOLS"),
            };

            godButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, nameof(ForwardGodTools)));

            actionButtonsContainer.AddChild(godButton);
        }
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
    protected virtual void UpdateAll()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    /// <summary>
    ///   Called periodically to refresh the shown unit data
    /// </summary>
    protected virtual void RefreshShownData()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void OnMoveStart()
    {
        OnUnhandledActionType();
    }

    protected virtual void OnConstructStart()
    {
        OnUnhandledActionType();
    }

    private void OnUnhandledActionType()
    {
        GD.PrintErr("Non-overridden base action type method was called for a unit screen");
    }

    private void ForwardGodTools()
    {
        var target = OpenedForUnit;
        if (target == null)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnOpenGodTools, target);
    }
}
