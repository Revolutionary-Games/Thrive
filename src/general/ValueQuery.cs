using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Now, a get-value part of a filter with a category.
///   E.G; BehaviourValue: Activity
/// </summary>
public sealed class ValueQuery<T> : IValueQuery
{
    public static string NumberCategory = "NUMBER";

    private string currentCategory = null!;
    private string currentProperty = null!;

    // For numbers
    private float currentNumericValue = 0;

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
    public float CurrentNumericValue { get => currentNumericValue; set => currentNumericValue = value; }

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

        foreach (var behaviourKey in Enum.GetValues(typeof(TEnumeration)))
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
        GD.Print(CurrentCategory == NumberCategory, CurrentCategory);
        if (CurrentCategory == NumberCategory)
            return currentNumericValue;

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

        public ValueQuery<T> Create()
        {
            // We use ToList here because we want filterFunction to use indexing for the user's sake.
            return new ValueQuery<T>(categorizedArgumentWithOptions);
        }
    }
}
