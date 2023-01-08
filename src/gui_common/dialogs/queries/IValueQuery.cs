using Godot;
using System;
using System.Collections.Generic;

public interface IValueQuery
{
    public string CurrentCategory { get; set; }
    public string CurrentProperty { get; set; }

    public float CurrentNumericValue { get; set; }

    /// <summary>
    ///   A dictionary of all the possible properties, categorized by the dictionary's keys.
    /// </summary>
    public Dictionary<string, IEnumerable<string>> CategorizedProperties { get; }

    // public float ComputeValue();
}
