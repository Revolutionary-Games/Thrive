using System.Collections.Generic;
using Godot;

public interface IDataSetsLegend
{
    Control OnCreate(Dictionary<string, ChartDataSet> datasets, string title);

    void OnDataSetVisibilityChange(bool visible, string dataset);
}
