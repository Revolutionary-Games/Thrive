using System;
using System.Collections.Generic;

// TODO CONSIDER GENERIC
public class Filter
{
    public string FilterCategory = "NONE";

    private Dictionary<string, FilterItem> filterItems = new Dictionary<string, FilterItem>();

    public IEnumerable<string> FilterItemsNames => filterItems.Keys;

    public Dictionary<string, FilterItem> FilterItems => filterItems;

    public void AddFilterItem(string category, FilterItem item)
    {
        filterItems.Add(category, item);
    }

    public void ClearItems()
    {
        filterItems.Clear();
    }

    // TODO NUMBER EQUIVALENT
    public void SetArgumentValue(int argumentIndex, string argumentValue)
    {
        var filterArgument = filterItems[FilterCategory].FilterArguments[argumentIndex]
            as MultipleChoiceFilterArgument;

        if (filterArgument == null)
        {
            throw new InvalidCastException($"Filter argument at index {argumentIndex}" +
                $" is not a multiple choice argument!");
        }

        filterArgument!.Value = argumentValue;

        filterItems[FilterCategory].FilterArguments[argumentIndex] = filterArgument;
    }

    public Func<Species, bool> ComputeFilterFunction()
    {
        if (!filterItems.TryGetValue(FilterCategory, out var filterItem))
            throw new KeyNotFoundException($"No such filter category: {FilterCategory}");

        return filterItem.ToFunction();
    }

    public class FilterItem
    {
        public readonly Func<List<FilterArgument>, Func<Species, bool>> FilterFunction;
        public readonly List<FilterArgument> FilterArguments;

        public FilterItem(Func<List<FilterArgument>, Func<Species, bool>> filterFunction,
            List<FilterArgument> filterArguments)
        {
            FilterFunction = filterFunction;
            FilterArguments = filterArguments;
        }

        public Func<Species, bool> ToFunction()
        {
            return FilterFunction(FilterArguments);
        }
    }

    /// <summary>
    ///   Parent class for filters arguments
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Used to list arguments of different types. Not generic because dictionaries require non-generic types.
    ///   </para>
    /// </remarks>
    public abstract class FilterArgument
    {
        /// <summary>
        ///   Helper function to get Number from NumberFilterArgument without cast.
        /// </summary>
        public float GetNumberValue()
        {
            try
            {
                return (this as NumberFilterArgument)!.Value;
            }
            catch (NullReferenceException)
            {
                throw new InvalidOperationException("Can't get number value from a non-numeric filter argument!");
            }
        }

        public string GetStringValue()
        {
            try
            {
                return (this as MultipleChoiceFilterArgument)!.Value;
            }
            catch (NullReferenceException)
            {
                throw new InvalidOperationException("Can't get string value from a non-string filter argument!");
            }
        }
    }

    public class NumberFilterArgument : FilterArgument
    {
        public readonly float MinValue;
        public readonly float MaxValue;

        public float Value;

        public NumberFilterArgument(float minValue, float maxValue, float defaultValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Value = defaultValue;
        }
    }

    public class MultipleChoiceFilterArgument : FilterArgument
    {
        public readonly List<string> Options;
        public string Value;

        public MultipleChoiceFilterArgument(List<string> options, string defaultValue)
        {
            Options = options;
            Value = defaultValue;
        }
    }
}