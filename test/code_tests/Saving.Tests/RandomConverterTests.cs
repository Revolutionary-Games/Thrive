namespace ThriveTest.Saving.Tests;

using System.IO;
using System.Text;
using global::Saving.Serializers;
using Newtonsoft.Json;
using SharedBase.Archive;
using Xoshiro.PRNG64;
using Xunit;

public class RandomConverterTests
{
    private const int SequenceVerifyLength = 25;

    private readonly ThriveArchiveManager manager = new();

    [Theory]
    [InlineData(12323, 1)]
    [InlineData(12323, 0)]
    [InlineData(54656453, 5)]
    [InlineData(null, 0)]
    [InlineData(null, 1)]
    public void RandomConverter_StateResumesCorrectly(int? seed, int initialValuesToTake)
    {
        var memoryStream = new MemoryStream();
        var writer = new SArchiveMemoryWriter(memoryStream, manager);
        var reader = new SArchiveMemoryReader(memoryStream, manager);

        XoShiRo256starstar original;
        if (seed != null)
        {
            original = new XoShiRo256starstar(seed.Value);
        }
        else
        {
            original = new XoShiRo256starstar();
        }

        for (int i = 0; i < initialValuesToTake; ++i)
        {
            original.Next64U();
        }

        writer.WriteAnyRegisteredValueAsObject(original);

        Assert.NotEqual(0, memoryStream.Position);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var deserialized = reader.ReadObjectOrNull<XoShiRo256starstar>();

        Assert.NotNull(deserialized);

        for (int i = 0; i < SequenceVerifyLength; ++i)
        {
            Assert.Equal(original.Next64U(), deserialized.Next64U());
        }
    }
}
