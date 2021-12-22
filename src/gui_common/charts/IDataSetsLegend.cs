using System;
using System.Collections.Generic;
using Godot;

public interface IDataSetsLegend : ICloneable
{
    /// <summary>
    ///   Creates datasets legend UI element. This should be called on a chart plotting/construction method.
    /// </summary>
    /// <param name="datasets">The datasets of a particular chart</param>
    /// <param name="title">The legend title</param>
    /// <returns>The legend's UI element</returns>
    Control CreateLegend(Dictionary<string, ChartDataSet> datasets, string title);

    /// <summary>
    ///   Should be called whenever a dataset in a chart is shown/hidden.
    /// </summary>
    /// <param name="visible">User's visibility change request</param>
    /// <param name="dataset">Which dataset should this visibility change applies to</param>
    void OnDataSetVisibilityChange(bool visible, string dataset);
}
