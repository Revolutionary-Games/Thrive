using System.Collections.Generic;

public interface IFilter
{
    public interface IFilterItem
    {
        public List<FilterArgument> FilterArguments { get; }
    }

    public interface IFilterFactory
    {
        public IFilter Create();
    }

    public interface IFilterGroup
    {
        public List<IFilter> Filters { get; }

        public void Add(IFilter filter);

        public void Remove(IFilter filter);

        public void Clear();
    }

    public string FilterCategory { get; set; }
    public IEnumerable<string> FilterItemsNames { get; }

    public Dictionary<string, IFilterItem> FilterItems { get; }
}
