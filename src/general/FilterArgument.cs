using System;
using System.Collections.Generic;
using System.Linq;

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
        return (this as NumberFilterArgument)?.Value ??
            throw new InvalidOperationException("Can't get number value from a non-numeric filter argument!");
    }

    public string GetStringValue()
    {
        return (this as MultipleChoiceFilterArgument)?.Value ??
            throw new InvalidOperationException("Can't get string value from a non-string filter argument!");
    }

    public Func<float, float, bool> GetComparison()
    {
        return (this as ComparisonFilterArgument)?.GetComparison() ??
            throw new InvalidOperationException("Can't get string value from a non-string filter argument!");
    }

    public abstract FilterArgument Clone();

    public class NumberFilterArgument : FilterArgument
    {
        public readonly float MinValue;
        public readonly float MaxValue;

        public float Value;

        public NumberFilterArgument(float minValue, float maxValue, float defaultValue)
        {
            // The arguments are expected to be defined by users, so we check they are sensible to spot typos and such
            if (defaultValue < minValue || defaultValue > maxValue)
                throw new ArgumentOutOfRangeException($"{defaultValue} is outside the range {minValue}-{maxValue}!");

            MinValue = minValue;
            MaxValue = maxValue;
            Value = defaultValue;
        }

        public override FilterArgument Clone()
        {
            return new NumberFilterArgument(MinValue, MaxValue, Value);
        }
    }

    public class MultipleChoiceFilterArgument : FilterArgument
    {
        public readonly List<string> Options;
        public string Value;

        public MultipleChoiceFilterArgument(List<string> options, string value)
        {
            if (!options.Contains(value))
            {
                throw new ArgumentOutOfRangeException("Value " + value + " not in passed options list!");
            }

            Options = options;
            Value = value;
        }

        public MultipleChoiceFilterArgument(List<string> options) :
            this(options, options.Count > 0 ? options[0] : "--") { }

        public override FilterArgument Clone()
        {
            return new MultipleChoiceFilterArgument(Options, Value);
        }
    }

    public class ComparisonFilterArgument : MultipleChoiceFilterArgument
    {
        private static Dictionary<string, Comparators> comparatorsTable = new()
        {
            { "EQUALS", Comparators.Equals },
            { "GREATER_THAN", Comparators.GreaterThan },
            { "LESS_THAN", Comparators.LessThan },
            { "STRICT_GREATER_THAN", Comparators.StrictGreaterThan },
            { "STRICT_LESSER_THAN", Comparators.StrictLesserThan },
        };

        public ComparisonFilterArgument(string comparator) : base(comparatorsTable.Keys.ToList(), comparator) { }

        public ComparisonFilterArgument() : base(comparatorsTable.Keys.ToList()) { }

        public enum Comparators
        {
            Equals,
            GreaterThan,
            LessThan,
            StrictGreaterThan,
            StrictLesserThan,
        }

        public List<string> ComparatorsNames => comparatorsTable.Keys.ToList();

        public new Func<float, float, bool> GetComparison()
        {
            switch (comparatorsTable[Value])
            {
                case Comparators.Equals:
                    return (f1, f2) => f1 == f2;
                case Comparators.GreaterThan:
                    return (f1, f2) => f1 >= f2;
                case Comparators.LessThan:
                    return (f1, f2) => f1 <= f2;
                case Comparators.StrictGreaterThan:
                    return (f1, f2) => f1 > f2;
                case Comparators.StrictLesserThan:
                    return (f1, f2) => f1 <= f2;
            }

            throw new InvalidOperationException();
        }

        public override FilterArgument Clone()
        {
            return new ComparisonFilterArgument(Value);
        }
    }
}
