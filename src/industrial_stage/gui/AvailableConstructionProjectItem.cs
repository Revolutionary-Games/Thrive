using System;
using Godot;

public class AvailableConstructionProjectItem : HBoxContainer
{
    [Export]
    public NodePath? ButtonPath;

#pragma warning disable CA2213
    private Button? button;
#pragma warning restore CA2213

    private ICityConstructionProject? constructionProject;

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

    public override void _Ready()
    {
        button = GetNode<Button>(ButtonPath);
        UpdateText();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ButtonPath?.Dispose();
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
}
