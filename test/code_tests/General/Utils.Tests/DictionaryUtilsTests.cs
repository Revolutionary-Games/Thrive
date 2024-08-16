namespace ThriveTest.General.Utils.Tests;

using System.Collections.Generic;
using System.Linq;
using Xunit;

public class DictionaryUtilsTests
{
    [Fact]
    public void Dictionary_SequenceEqual()
    {
        var dictionary1 = new Dictionary<string, int>
        {
            { "key1", 1 },
            { "key2", 1 },
            { "key3", 14 },
        };

        var dictionary2 = new Dictionary<string, int>
        {
            { "key1", 1 },
            { "key2", 1 },
            { "key3", 14 },
        };

        Assert.True(dictionary1.DictionaryEquals(dictionary2));
        Assert.True(dictionary1.AsEnumerable().DictionaryEquals(dictionary2));
        Assert.True(dictionary1.AsEnumerable().DictionaryEquals(dictionary2.AsEnumerable()));

        Assert.True(dictionary1.DictionaryEquals(dictionary1.CloneShallow()));
        Assert.True(dictionary1.AsEnumerable().DictionaryEquals(dictionary2.CloneShallow()));
        Assert.True(dictionary1.AsEnumerable().DictionaryEquals(dictionary1.CloneShallow()));
    }

    [Fact]
    public void Dictionary_SequenceNotEqual()
    {
        var dictionary1 = new Dictionary<string, int>
        {
            { "key1", 1 },
            { "key2", 1 },
            { "key3", 14 },
        };

        var dictionary2 = new Dictionary<string, int>
        {
            { "key1", 1 },
            { "key2", 2 },
            { "key3", 14 },
        };

        var dictionary3 = new Dictionary<string, int>
        {
            { "key1", 1 },
            { "key2", 1 },
            { "key5", 14 },
        };

        var dictionary4 = new Dictionary<string, int>
        {
            { "key1", 1 },
            { "key2", 1 },
            { "key3", 14 },
            { "key6", 1 },
        };

        var dictionary5 = new Dictionary<string, int>
        {
            { "key1", 1 },
            { "key2", 1 },
        };

        Assert.False(dictionary1.DictionaryEquals(dictionary2));
        Assert.False(dictionary1.AsEnumerable().DictionaryEquals(dictionary2));
        Assert.False(dictionary1.DictionaryEquals(dictionary3));
        Assert.False(dictionary1.AsEnumerable().DictionaryEquals(dictionary3));
        Assert.False(dictionary1.DictionaryEquals(dictionary4));
        Assert.False(dictionary1.AsEnumerable().DictionaryEquals(dictionary4));
        Assert.False(dictionary1.DictionaryEquals(dictionary5));
        Assert.False(dictionary1.AsEnumerable().DictionaryEquals(dictionary5));
    }
}
