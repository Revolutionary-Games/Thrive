﻿using System;
using Godot;

/// <summary>
///   A single item listing an available construction project for a city
/// </summary>
public partial class AvailableConstructionProjectItem : HBoxContainer
{
#pragma warning disable CA2213
    [Export]
    private Button? button;
#pragma warning restore CA2213

    private ICityConstructionProject? constructionProject;
    private bool disabled;

    public delegate void OnItemSelected(ICityConstructionProject project);

    public event OnItemSelected? OnItemSelectedHandler;

    public ICityConstructionProject? ConstructionProject
    {
        get => constructionProject;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(ConstructionProject));

            if (constructionProject == value)
                return;

            constructionProject = value;
            UpdateText();
        }
    }

    public bool Disabled
    {
        get => disabled;
        set
        {
            disabled = value;
            ApplyDisabledState();
        }
    }

    public override void _Ready()
    {
        UpdateText();
        ApplyDisabledState();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnItemSelectedHandler = null;
        }

        base.Dispose(disposing);
    }

    private void UpdateText()
    {
        if (button == null)
            return;

        if (constructionProject == null)
            throw new InvalidOperationException("Construction project not set");

        button.Text = constructionProject.ProjectName.ToString();
    }

    private void ApplyDisabledState()
    {
        if (button == null)
            return;

        button.Disabled = disabled;
    }

    private void OnButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (constructionProject == null || OnItemSelectedHandler == null)
        {
            GD.PrintErr("Construction project item not setup properly, can't forward click");
            return;
        }

        OnItemSelectedHandler?.Invoke(constructionProject);
    }
}
