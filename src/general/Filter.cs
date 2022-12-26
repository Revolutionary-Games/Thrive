using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class Filter<T> : IFilter
{
    // A filter has a structure of type: [(leftComparand) comparisonArgument (rightComparand)];
    // e.g. BehaviorValue: Activity > Number: 100
    private ValueQuery<T> leftComparand;
    private FilterArgument.ComparisonFilterArgument comparisonArgument;
    private ValueQuery<T> rightComparand;

    /// <summary>
    ///   A factory object for creating filterItems from a predefined template;
    /// </summary>
    private ValueQuery<T>.ValueQueryFactory filterItemTemplate;

    IValueQuery IFilter.LeftComparand => leftComparand;
    IValueQuery IFilter.RightComparand => rightComparand;

    public Filter(ValueQuery<T>.ValueQueryFactory template)
    {
        filterItemTemplate = template;
        leftComparand = template.Create();
        rightComparand = template.Create();
        comparisonArgument = new FilterArgument.ComparisonFilterArgument();
    }

    public IEnumerable<IValueQuery> FilterItems => new List<IValueQuery>()
    {
        leftComparand,
        new ValueQuery<T>(new Dictionary<string, Dictionary<string, Func<T, float>>>
        {
            { "VALUE_COMPARISON", new Dictionary<string, Func<T, float>>() },
        }), // TODO comparison
        rightComparand,
    };

    public IValueQuery LeftItem => leftComparand;

    // TODO PROBABLY BETTER IF NOT SUBCLASS OF FILTER ARGUMENT?
    public FilterArgument.ComparisonFilterArgument HeadArgument => comparisonArgument;
    public IValueQuery RightItem => rightComparand;

    public Func<T, bool> ComputeFilterFunction()
    {
        return t => comparisonArgument.Compare(leftComparand.Apply(t), rightComparand.Apply(t));
    }

    /// <summary>
    ///   A template for filter, to create several filters from it.
    /// </summary>
    /// TODO: Rename to template
    public class FilterFactory : IFilter.IFilterFactory
    {
        private ValueQuery<T>.ValueQueryFactory valueQueryTemplate;

        public FilterFactory(ValueQuery<T>.ValueQueryFactory filterItemTemplate)
        {
            valueQueryTemplate = filterItemTemplate;
        }

        public IFilter Create()
        {
            var filterInstance = new Filter<T>(valueQueryTemplate);

            return filterInstance;
        }
    }

    public class FiltersConjunction : IFilter.IFilterConjunction
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
