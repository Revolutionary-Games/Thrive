using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   A ProgressBar that is split up into IconProgressBars, data is stored in a dictionary
/// </summary>
public class SegmentedBar : HBoxContainer
{
    public Type SelectedType;

    public List<KeyValuePair<string, float>> GlobalData;

    public float MaxValue;

    private PackedScene iconProgressBarScene = GD.Load<PackedScene>("res://src/gui_common/IconProgressBar.tscn");

    private List<IconProgressBar> subBars = new List<IconProgressBar>();

    public enum Type { ATP }

    public void UpdateAndMoveBars(List<KeyValuePair<string, float>> data)
    {
        GlobalData = data;

        RemoveUnusedBars(data);

        int location = 0;
        foreach (var dataPair in data)
        {
            CreateAndUpdateBar(dataPair, location);
            location++;
        }

        foreach (var bar in subBars)
        {
            float value = bar.RectSize.x / RectSize.x * MaxValue;
            UpdateDisabledBars(new KeyValuePair<string, float>(bar.Name, value));
        }

        foreach (var bar in subBars)
        {
            MoveBars(GetNode<IconProgressBar>(bar.Name));
        }
    }

    private void RemoveUnusedBars(List<KeyValuePair<string, float>> data)
    {
        List<IconProgressBar> unusedBars = new List<IconProgressBar>();
        foreach (IconProgressBar progressBar in subBars)
        {
            bool match = false;
            foreach (var dataPair in data)
            {
                if (progressBar.Name == dataPair.Key)
                    match = true;
            }

            if (!match)
                unusedBars.Add(progressBar);
        }

        foreach (IconProgressBar unusedBar in unusedBars)
        {
            unusedBar.Free();
            subBars.Remove(unusedBar);
        }
    }

    private void CreateAndUpdateBar(KeyValuePair<string, float> dataPair, int location = -1)
    {
        if (HasNode(dataPair.Key))
        {
            IconProgressBar progressBar = (IconProgressBar)GetNode(dataPair.Key);
            if (location >= 0)
            {
                progressBar.Location = location;
                progressBar.ActualLocation = location;
            }

            if (progressBar.Disabled)
                return;
            progressBar.SetBarSize(new Vector2((float)Math.Floor(dataPair.Value / MaxValue * RectSize.x), RectSize.y));
        }
        else
        {
            IconProgressBar progressBar = (IconProgressBar)iconProgressBarScene.Instance();
            progressBar.SetBarName(dataPair.Key);
            progressBar.Color = BarHelper.GetBarColour(SelectedType, dataPair.Key, GetIndex() == 0);
            progressBar.SetBarSize(new Vector2((float)Math.Floor(dataPair.Value / MaxValue * RectSize.x), RectSize.y));
            progressBar.SetBarIconTexture(BarHelper.GetBarIcon(SelectedType, dataPair.Key));
            if (location >= 0)
            {
                progressBar.Location = location;
                progressBar.ActualLocation = location;
            }

            AddChild(progressBar);
            subBars.Add(progressBar);
            progressBar.Connect("gui_input", this, nameof(BarToggled), new Godot.Collections.Array() { progressBar });
        }
    }

    private void BarToggled(InputEvent @event, IconProgressBar bar)
    {
        if (@event is InputEventMouseButton eventMouse && @event.IsPressed())
        {
            bar.Disabled = !bar.Disabled;
            HandleBarDisabling(bar);
        }
    }

    private IconProgressBar GetPreviousBar(IconProgressBar currentBar)
    {
        return currentBar.GetIndex() > 0 ?
            GetChild<IconProgressBar>(currentBar.GetIndex() - 1) : new IconProgressBar();
    }

    private void UpdateDisabledBars(KeyValuePair<string, float> dataPair)
    {
        IconProgressBar progressBar = (IconProgressBar)GetNode(dataPair.Key);
        if (!progressBar.Disabled)
            return;
        progressBar.SetBarSize(new Vector2((float)Math.Floor(dataPair.Value / MaxValue * RectSize.x), RectSize.y));
    }

    private void CalculateActualLocation()
    {
        List<IconProgressBar> children = new List<IconProgressBar>();
        foreach (IconProgressBar childBar in GetChildren())
        {
            children.Add(childBar);
        }

        children = children.OrderBy(bar =>
        {
            return bar.Location + (bar.Disabled ? children.Count : 0);
        }).ToList();

        foreach (var childBar in children)
        {
            childBar.ActualLocation = children.IndexOf(childBar);
        }
    }

    private void MoveByIndexBars(IconProgressBar bar)
    {
        bar.GetParent().MoveChild(bar, bar.ActualLocation);
    }

    private void HandleBarDisabling(IconProgressBar bar)
    {
        if (bar.Disabled)
        {
            bar.SetBarIconModulation(new Color(0, 0, 0));
            bar.Color = new Color(0.73f, 0.73f, 0.73f);
            MoveBars(bar);
        }
        else
        {
            bar.SetBarIconModulation(new Color(1, 1, 1));
            bar.Color = BarHelper.GetBarColour(SelectedType, bar.Name, GetIndex() == 0);
            MoveBars(bar);
        }
    }

    private void MoveBars(IconProgressBar bar)
    {
        CalculateActualLocation();
        foreach (IconProgressBar iconBar in bar.GetParent().GetChildren())
            MoveByIndexBars(iconBar);

        foreach (IconProgressBar iconBar in bar.GetParent().GetChildren())
        {
            foreach (KeyValuePair<string, float> dataPair in GlobalData)
            {
                if (iconBar.Name == dataPair.Key)
                {
                    CreateAndUpdateBar(dataPair);
                    break;
                }
            }
        }

        foreach (IconProgressBar iconBar in bar.GetParent().GetChildren())
        {
            foreach (KeyValuePair<string, float> dataPair in GlobalData)
            {
                if (iconBar.Name == dataPair.Key)
                {
                    UpdateDisabledBars(dataPair);
                    break;
                }
            }
        }
    }
}
