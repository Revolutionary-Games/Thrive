using System;
using System.Collections.Generic;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   A ProgressBar that is split up into IconProgressBars, data is stored in a dictionary
/// </summary>
public class SegmentedBar : HBoxContainer
{
    public readonly List<IconProgressBar> SubBars = new();

    public Type SelectedType;

    public bool IsProduction;

    /// <summary>
    ///   Maximum value in the given data for the bars
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This only takes effect after calling UpdateAndMoveBars
    ///   </para>
    /// </remarks>
    public float MaxValue;

#pragma warning disable CA2213
    private readonly PackedScene iconProgressBarScene;
#pragma warning restore CA2213

    private List<KeyValuePair<string, float>> barValues = new();

    public SegmentedBar()
    {
        iconProgressBarScene = GD.Load<PackedScene>("res://src/gui_common/IconProgressBar.tscn");
    }

    [Signal]
    public delegate void SubBarMouseEnter();

    [Signal]
    public delegate void SubBarMouseExit();

    public enum Type
    {
        /// <summary>
        ///   ATP showing bar
        /// </summary>
        ATP,
    }

    /// <summary>
    ///   The main method to call to update the bar properties
    /// </summary>
    /// <param name="data">The bar data to show in this object</param>
    public void UpdateAndMoveBars(List<KeyValuePair<string, float>> data)
    {
        barValues = data;

        RemoveUnusedBars(barValues);

        foreach (var dataPair in barValues)
        {
            CreateAndUpdateBar(dataPair);
        }

        MoveBars();
    }

    /// <summary>
    ///   Removes the bars that are no longer present in data
    /// </summary>
    private void RemoveUnusedBars(List<KeyValuePair<string, float>> data)
    {
        var unusedBars = new List<IconProgressBar>();

        foreach (var progressBar in SubBars)
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

        foreach (var unusedBar in unusedBars)
        {
            unusedBar.Free();
            SubBars.Remove(unusedBar);
        }
    }

    private IconProgressBar? FindBar(string name)
    {
        foreach (var bar in SubBars)
        {
            if (bar.Name == name)
                return bar;
        }

        return null;
    }

    private void CreateAndUpdateBar(KeyValuePair<string, float> dataPair)
    {
        var progressBar = FindBar(dataPair.Key);

        var progressBarBarSize = new Vector2((float)Math.Floor(dataPair.Value / MaxValue * RectSize.x), RectSize.y);

        if (progressBar != null)
        {
            progressBar.BarSize = progressBarBarSize;
        }
        else
        {
            progressBar = (IconProgressBar)iconProgressBarScene.Instance();
            progressBar.Name = dataPair.Key;
            AddChild(progressBar);
            SubBars.Add(progressBar);

            progressBar.Color = BarHelper.GetBarColour(SelectedType, dataPair.Key, IsProduction);
            progressBar.HighlightColor = new Color(progressBar.Color.r + 0.5f, progressBar.Color.g + 0.5f,
                progressBar.Color.b + 0.5f, 0.3f);
            progressBar.BarSize = progressBarBarSize;
            progressBar.IconTexture = BarHelper.GetBarIcon(SelectedType, dataPair.Key);
            progressBar.IconModulation = BarHelper.GetBarIconColor(SelectedType);

            progressBar.MouseFilter = MouseFilterEnum.Pass;

            progressBar.Connect("gui_input", this, nameof(BarToggled), new Array { progressBar });
            progressBar.Connect("mouse_entered", this, nameof(OnBarMouseOver), new Array { progressBar });
            progressBar.Connect("mouse_exited", this, nameof(OnBarMouseExit), new Array { progressBar });
        }
    }

    private void BarToggled(InputEvent @event, IconProgressBar bar)
    {
        if (@event is InputEventMouseButton eventMouse && @event.IsPressed())
        {
            if (eventMouse.ButtonIndex != (int)ButtonList.Left)
                return;

            bar.Disabled = !bar.Disabled;
            HandleBarDisabling(bar);
        }
    }

    private void OnBarMouseOver(IconProgressBar bar)
    {
        bar.Highlight = true;

        EmitSignal(nameof(SubBarMouseEnter));
    }

    private void OnBarMouseExit(IconProgressBar bar)
    {
        bar.Highlight = false;

        EmitSignal(nameof(SubBarMouseExit));
    }

    private void HandleBarDisabling(IconProgressBar bar)
    {
        if (bar.Disabled)
        {
            bar.Color = new Color(0.73f, 0.73f, 0.73f);
        }
        else
        {
            bar.Color = BarHelper.GetBarColour(SelectedType, bar.Name, IsProduction);
        }

        MoveBars();
    }

    private void MoveBars()
    {
        // Sort the bars list based on what is in subBars as well as what is disabled
        SubBars.Sort((a, b) =>
        {
            if (a.Disabled && !b.Disabled)
                return 1;

            if (b.Disabled && !a.Disabled)
                return -1;

            return barValues.FindIndex(v => v.Key == a.Name) -
                barValues.FindIndex(v => v.Key == b.Name);
        });

        int location = 0;
        foreach (var bar in SubBars)
        {
            if (GetChild(location) != bar)
            {
                MoveChild(bar, location);
            }

            ++location;
        }
    }
}
