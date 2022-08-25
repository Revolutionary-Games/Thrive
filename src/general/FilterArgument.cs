using System;
using System.Collections.Generic;

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

        public MultipleChoiceFilterArgument(List<string> options)
        {
            Options = options;
            Value = Options.Count > 0 ? Options[0] : "--";
        }

        public override FilterArgument Clone()
        {
            return new MultipleChoiceFilterArgument(Options);
        }
    }
}
