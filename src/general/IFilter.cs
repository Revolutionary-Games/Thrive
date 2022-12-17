using System.Collections.Generic;

public interface IFilter
{

    /*public IFilterItem LeftItem { get; }
    public FilterArgument HeadArgument { get; }
    public IFilterItem RightItem { get; }*/
    public IValueQuery LeftComparand { get; }
    public FilterArgument.ComparisonFilterArgument HeadArgument { get; }
    public IValueQuery RightComparand { get; }

    public interface IFilterItem
    {
        //public IEnumerable<FilterArgument> FilterArguments { get; }

        public IEnumerable<string> PossibleCategories { get; }
    }

    public interface IFilterFactory
    {
        public IFilter Create();
    }

    public interface IFilterConjunction
    {
        public List<IFilter> Filters { get; }

        public void Add(IFilter filter);

        public void Remove(IFilter filter);

        public void Clear();
    }

    //public string FilterCategory { get; set; }
    //public IEnumerable<string> FilterItemsNames { get; }

    //public IEnumerable<IFilterItem> FilterItems { get; }
}
