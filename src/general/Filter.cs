using System;
using System.Collections.Generic;
using System.Linq;

// TODO CONSIDER GENERIC
public sealed class Filter<T> : IFilter
{
    private string filterCategory = "NONE";

    private Dictionary<string, IFilter.IFilterItem> filterItems = new Dictionary<string, IFilter.IFilterItem>();

    string IFilter.FilterCategory
    {
        get => filterCategory;
        set => filterCategory = value;
    }

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
        return ((FilterItem)filterItems[filterCategory]).ToFunction();
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

        public FilterItemFactory ToFactory()
        {
            return new FilterItemFactory(FilterFunction, filterArguments.Select(a => a.Clone()));
        }

        public class FilterItemFactory
        {
            private readonly Func<List<FilterArgument>, Func<T, bool>> filterFunction;
            private readonly IEnumerable<FilterArgument> filterArguments;

            public FilterItemFactory(Func<List<FilterArgument>, Func<T, bool>> filterFunction, IEnumerable<FilterArgument> filterArguments)
            {
                this.filterFunction = filterFunction;
                this.filterArguments = filterArguments;
            }

            public FilterItem Create()
            {
                // We use ToList here because we want filterFunction to use indexing for the user's sake.
                return new FilterItem(filterFunction, filterArguments.Select(a => a.Clone()).ToList());
            }
        }
    }

    public class FilterFactory : IFilter.IFilterFactory
    {
        private Dictionary<string, Filter<T>.FilterItem.FilterItemFactory> filterItems = new();

        public IFilter Create()
        {
            var filterInstance = new Filter<T>();

            foreach (var categorizedItem in filterItems)
            {
                filterInstance.AddFilterItem(categorizedItem.Key, categorizedItem.Value.Create());
            }

            return filterInstance;
        }

        public void AddFilterItemFactory(string category, FilterItem.FilterItemFactory itemFactory)
        {
            filterItems.Add(category, itemFactory);
        }

        public void AddFilterItemFactory(string category, FilterItem item)
        {
            AddFilterItemFactory(category, item.ToFactory());
        }

        public void ClearItems()
        {
            filterItems.Clear();
        }
    }

    public class FilterGroup : IFilter.IFilterGroup
    {
        public List<Filter<T>> TypedFilters = new();

        public List<IFilter> Filters => TypedFilters.ConvertAll(new Converter<Filter<T>, IFilter>(f => f));

        public void Add(IFilter filter)
        {
            if (!(filter is Filter<T> tFilter))
                throw new ArgumentException($"Filter has to apply on type {nameof(T)}");

            TypedFilters.Add(tFilter);
        }

        public void Remove(IFilter filter)
        {
            if (!(filter is Filter<T> tFilter))
                throw new ArgumentException($"Filter has to apply on type {nameof(T)}");

            TypedFilters.Remove(tFilter);
        }

        public void Clear()
        {
            TypedFilters.Clear();
        }
    }
}
