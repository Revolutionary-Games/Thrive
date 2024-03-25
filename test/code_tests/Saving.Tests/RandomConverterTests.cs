namespace ThriveTest.Saving.Tests;

using System.IO;
using System.Text;
using Newtonsoft.Json;
using Xoshiro.PRNG64;
using Xunit;

public class RandomConverterTests
{
    private const int SequenceVerifyLength = 25;

    [Theory]
    [InlineData(12323, 1)]
    [InlineData(12323, 0)]
    [InlineData(54656453, 5)]
    [InlineData(null, 0)]
    [InlineData(null, 1)]
    public static void RandomConverter_StateResumesCorrectly(int? seed, int initialValuesToTake)
    {
        var serializer = new JsonSerializer
        {
            Converters = { new RandomConverter() },
        };

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

        var memoryBuffer = new MemoryStream();

        var textWriter = new StreamWriter(memoryBuffer, Encoding.UTF8);

        serializer.Serialize(textWriter, original);

        textWriter.Flush();

        Assert.NotEqual(0, memoryBuffer.Position);
        memoryBuffer.Seek(0, SeekOrigin.Begin);

        var textReader = new StreamReader(memoryBuffer, Encoding.UTF8);

        var jsonReader = new JsonTextReader(textReader);

        var deserialized = serializer.Deserialize<XoShiRo256starstar>(jsonReader);

        Assert.NotNull(deserialized);

        for (int i = 0; i < SequenceVerifyLength; ++i)
        {
            Assert.Equal(original.Next64U(), deserialized!.Next64U());
        }
    }
}
