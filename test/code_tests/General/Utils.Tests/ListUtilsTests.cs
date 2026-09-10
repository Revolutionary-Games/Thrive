namespace ThriveTest.General.Utils.Tests;

using System;
using System.Collections.Generic;
using Xunit;

public class ListUtilsTests
{
    [Fact]
    public void RandomOrDefault_NullOrEmptyListReturnsNullWithoutDrawingRandomness()
    {
        List<object>? missing = null;
        var empty = new List<object>();
        var random = new IndexRandom(0);

        Assert.Null(missing.RandomOrDefault(random));
        Assert.Null(empty.RandomOrDefault(random));
        Assert.Equal(0, random.Calls);
    }

    [Fact]
    public void RandomOrDefault_SingleItemReturnsThatInstance()
    {
        var item = new object();
        var items = new List<object> { item };

        Assert.Same(item, items.RandomOrDefault(new Random(42)));
        Assert.Single(items);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RandomOrDefault_EveryCandidateIsReachableWithoutChangingTheList(int index)
    {
        var items = new List<object> { new(), new(), new() };
        var original = items.ToArray();
        var random = new IndexRandom(index);

        Assert.Same(original[index], items.RandomOrDefault(random));
        Assert.Equal(original, items);
        Assert.Equal(1, random.Calls);
        Assert.Equal(items.Count, random.UpperBound);
    }

    private sealed class IndexRandom(int index) : Random
    {
        public int Calls { get; private set; }
        public int UpperBound { get; private set; }

        public override int Next(int minValue, int maxValue)
        {
            Assert.Equal(0, minValue);
            Assert.InRange(index, minValue, maxValue - 1);
            ++Calls;
            UpperBound = maxValue;
            return index;
        }
    }
}
