namespace ThriveTest.General.Utils.Tests;

using System.Collections.Generic;
using Xunit;

public class StringHashTests
{
    [Theory]
    [InlineData("test", 16125048496228390453)]
    [InlineData("hello", 13669059543567756152)]
    [InlineData("", 17241709254077376921)]
    [InlineData("this is just some random test string ö", 3614804017094369030)]
    public void StringHash_CalculatesCorrectHash(string input, ulong expectedHash)
    {
        var calculated = PersistentStringHash.CalculateHashWithoutCache(input);

        Assert.Equal(expectedHash, calculated);

        // Test that caching at least returns the same result
        Assert.Equal(calculated, PersistentStringHash.GetHash(input));
        Assert.Equal(calculated, PersistentStringHash.GetHash(input));
    }

    [Theory]
    [InlineData("test", "test2", 14854156576463983298)]
    [InlineData("test2", "test", 4786901312550206867)]
    [InlineData("", "", 8682658969876241481)]
    [InlineData("random string ö", "start of ä", 8502252053830719418)]
    public void StringHash_DualStringsWork(string input1, string input2, ulong expectedHash)
    {
        var calculated = PersistentStringHash.CalculateHashWithoutCache(input1, input2);

        Assert.Equal(expectedHash, calculated);
    }

    [Fact]
    public void StringHash_ListCachingCausesNoTrouble()
    {
        Assert.Equal(11701810726207801343UL, PersistentStringHash.GetHash("item"));

        var list = new List<string> { "item" };

        Assert.Equal(2307676410883933182UL, PersistentStringHash.GetHash(list));

        Assert.Equal(11701810726207801343UL, PersistentStringHash.GetHash("item"));
    }

    [Fact]
    public void StringHash_StringListHashWorks()
    {
        var list = new List<string> { "item", "something else", "a third thing" };

        Assert.Equal(6950202300781778765UL, PersistentStringHash.GetHash(list));

        // Order shouldn't matter
        list.Reverse();
        Assert.Equal(6950202300781778765UL, PersistentStringHash.GetHash(list));
    }
}
