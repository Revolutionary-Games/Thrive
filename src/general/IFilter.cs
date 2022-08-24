using Godot;
using System;
using System.Collections.Generic;

public interface IFilter
{
    public interface IFilterItem
    {
        public List<FilterArgument> FilterArguments { get; }
    }

    public string FilterCategory { get; set; }
    public IEnumerable<string> FilterItemsNames { get; }

    public Dictionary<string, IFilterItem> FilterItems { get; }
}
