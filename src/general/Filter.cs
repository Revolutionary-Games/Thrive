using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class Filter<T> : IFilter
{
    // A filter has a structure of type: [(leftComparand) comparisonArgument (rightComparand)];
    // e.g. BehaviorValue: Activity > Number: 100
    private ValueQuery leftComparand;
    private FilterArgument.ComparisonFilterArgument comparisonArgument;
    private ValueQuery rightComparand;

    /// <summary>
    ///   A factory object for creating filterItems from a predefined template;
    /// </summary>
    private ValueQuery.ValueQueryFactory filterItemTemplate;

    IValueQuery IFilter.LeftComparand => leftComparand;
    IValueQuery IFilter.RightComparand => rightComparand;

    public Filter(ValueQuery.ValueQueryFactory template)
    {
        filterItemTemplate = template;
        leftComparand = template.Create();
        rightComparand = template.Create();
        comparisonArgument = new FilterArgument.ComparisonFilterArgument();
    }

    public IEnumerable<IValueQuery> FilterItems => new List<IValueQuery>()
    {
        leftComparand,
        new ValueQuery(new Dictionary<string, Dictionary<string, Func<T, float>>>
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
    ///   Now, a get-value part of a filter with a category.
    ///   E.G; BehaviourValue: Activity
    /// </summary>
    public sealed class ValueQuery : IValueQuery
    {
        public static string NumberCategory = "NUMBER";

        private string currentCategory = null!;
        private string currentProperty = null!;

        // TODO FUNCTION : in filter argument or aside? => filter argument
        private readonly Dictionary<string, Dictionary<string, Func<T, float>>> categorizedArgumentFunctions = new();

        /// <summary>
        ///   A dictionary that maps categories to its argument, i.e. its possible values.
        /// </summary>
        private Dictionary<string, FilterArgument> categorizedArgument = new();

        public ValueQuery()
        {
            currentCategory = NumberCategory;

            // TODO DEAL WITH VALUES AND AVOID NUMBER-NUMBER COMPARISON
            categorizedArgument.Add(NumberCategory, new FilterArgument.NumberFilterArgument(0, 500, 100));
        }

        public ValueQuery(Dictionary<string, Dictionary<string, Func<T, float>>> categorizedArgumentFunctions) : base()
        {
            if (categorizedArgumentFunctions.Count <= 0)
                throw new ArgumentException("Can not initialize with an empty dictionary!");

            this.categorizedArgumentFunctions = categorizedArgumentFunctions;

            foreach (var item in categorizedArgumentFunctions)
            {
                categorizedArgument.Add(item.Key, new FilterArgument.MultipleChoiceFilterArgument(item.Value.Keys.ToList()));
            }

            // TODO USE NUMBER CATEGORY WHEN AVAILABLE
            currentCategory = categorizedArgument.First(_ => true).Key;
            currentProperty = categorizedArgument[currentCategory].GetStringValue();
        }

        public IEnumerable<FilterArgument> FilterArguments => categorizedArgument.Values;

        public IEnumerable<string> PossibleCategories => categorizedArgument.Keys;

        public string CurrentCategory { get => currentCategory; set => currentCategory = value; }
        public string CurrentProperty { get => currentProperty; set => currentProperty = value; }

        public Dictionary<string, IEnumerable<string>> CategorizedProperties =>
            categorizedArgumentFunctions.ToDictionary(c => c.Key, c => c.Value.Select(p => p.Key));

        public void AddArgumentCategory(string name, Dictionary<string, Func<T, float>> options)
        {
            categorizedArgument.Add(name, new FilterArgument.MultipleChoiceFilterArgument(options.Keys.ToList()));
            categorizedArgumentFunctions.Add(name, options);
        }

        // Issue with IDictionary<object, float> conversion...
        // TODO FIX OR REMOVE
        public void AddArgumentCategoryFromEnum<TEnumeration>(
            string name, Func<T, IDictionary<object, float>> enumerationKeyMapping)
        {
            var options = new Dictionary<string, Func<T, float>>();

            foreach (var behaviourKey in Enum.GetValues(typeof(TEnumeration))) //enumerationType))
            {
                options.Add(behaviourKey.ToString(), s => enumerationKeyMapping.Invoke(s)[behaviourKey]);
            }

            AddArgumentCategory(name, options);
        }

        /// <summary>
        ///   Returns the value of the filter's field for the specified target.
        /// </summary>
        public float Apply(T target)
        {
            if (CurrentCategory == NumberCategory)
                return categorizedArgument[CurrentCategory].GetNumberValue();

            return categorizedArgumentFunctions[CurrentCategory][CurrentProperty](target);
        }

        public ValueQueryFactory ToFactory()
        {
            return new ValueQueryFactory(categorizedArgumentFunctions);
        }

        public class ValueQueryFactory
        {
            private Dictionary<string, Dictionary<string, Func<T, float>>> categorizedArgumentWithOptions;

            public ValueQueryFactory(
                Dictionary<string, Dictionary<string, Func<T, float>>> categorizedArgumentWithOptions)
            {
                this.categorizedArgumentWithOptions = categorizedArgumentWithOptions;
            }

            public ValueQuery Create()
            {
                // We use ToList here because we want filterFunction to use indexing for the user's sake.
                return new ValueQuery(categorizedArgumentWithOptions);
            }
        }
    }

    /// <summary>
    ///   A template for filter, to create several filters from it.
    /// </summary>
    /// TODO: Rename to template
    public class FilterFactory : IFilter.IFilterFactory
    {
        private ValueQuery.ValueQueryFactory valueQueryTemplate;

        public FilterFactory(Filter<T>.ValueQuery.ValueQueryFactory filterItemTemplate)
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
