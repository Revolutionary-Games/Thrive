using System;
using System.Collections.Generic;

// TODO CONSIDER GENERIC
public sealed class Filter<T> : IFilter
{
    string IFilter.FilterCategory
    {
        get => filterCategory;
        set => filterCategory = value;
    }

    private string filterCategory = "NONE";
    //public string FilterCategory = "NONE";

    private Dictionary<string, IFilter.IFilterItem> filterItems = new Dictionary<string, IFilter.IFilterItem>();

    public IEnumerable<string> FilterItemsNames => filterItems.Keys;

    Dictionary<string, IFilter.IFilterItem> IFilter.FilterItems => filterItems;

    public void AddFilterItem(string category, FilterItem item)
    {
        filterItems.Add(category, item);
    }

    public void ClearItems()
    {
        filterItems.Clear();
    }

    public Func<T, bool> ComputeFilterFunction()
    {
        if (!filterItems.TryGetValue(filterCategory, out var filterItem))
            throw new KeyNotFoundException($"No such filter category: {filterCategory}");

        return ((FilterItem)filterItem).ToFunction();
    }

    public sealed class FilterItem : IFilter.IFilterItem
    {
        public readonly Func<List<FilterArgument>, Func<T, bool>> FilterFunction;
        private readonly List<FilterArgument> filterArguments;

        public FilterItem(Func<List<FilterArgument>, Func<T, bool>> filterFunction,
            List<FilterArgument> filterArguments)
        {
            FilterFunction = filterFunction;
            this.filterArguments = filterArguments;
        }

        List<FilterArgument> IFilter.IFilterItem.FilterArguments => filterArguments;

        public Func<T, bool> ToFunction()
        {
            return FilterFunction(filterArguments);
        }
    }
}
