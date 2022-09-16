using System;
using System.Collections.Generic;

public class Filter
{
    public readonly List<BaseFilterDescription> Descriptions = new();
}

public abstract class BaseFilterDescription
{
    public string DescriptionName;

    protected BaseFilterDescription(string name)
    {
        DescriptionName = name;
    }
}

public class MultipleChoiceFilterDescription : BaseFilterDescription
{
    public Dictionary<string, bool> Values = new();

    public bool MultipleChoice;

    public MultipleChoiceFilterDescription(string name) : base(name) { }
}

public class NumericFilterDescription : BaseFilterDescription
{
    private double value;

    public enum Operators
    {
        Equals,
        GreaterThan,
        NotLessThan,
        LessThan,
        NotGreaterThan,
    }

    public double MaxValue { get; set; }
    public double MinValue { get; set; }

    public double Value
    {
        get => value;
        set
        {
            if (value < MinValue || value > MaxValue)
                throw new InvalidOperationException();

            this.value = value;
        }
    }

    public Operators Operator { get; set; }

    public NumericFilterDescription(string name) : base(name) { }
}
