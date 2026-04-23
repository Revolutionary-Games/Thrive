using Xunit;

public class SceneInstantiationHelpersTests
{
    [Fact]
    public void RetryCreateHandlesFirstNullResult()
    {
        int callCount = 0;

        var created = SceneInstantiationHelpers.RetryCreate(() =>
            {
                ++callCount;

                if (callCount == 1)
                    return null;

                return new object();
            },
            "test scene", reportFailure: _ => { });

        Assert.NotNull(created);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RetryCreateDiscardsInvalidInstancesBeforeRetrying()
    {
        var invalidInstance = new TestSceneInstance();
        int discardCount = 0;

        var created = SceneInstantiationHelpers.RetryCreate(() => discardCount == 0 ? invalidInstance :
                new TestSceneInstance(),
            "test scene", instance => ReferenceEquals(instance, invalidInstance) ? "missing node" : null,
            _ => ++discardCount, reportFailure: _ => { });

        Assert.NotNull(created);
        Assert.NotSame(invalidInstance, created);
        Assert.Equal(1, discardCount);
    }

    [Fact]
    public void RetryCreateReturnsNullAfterExhaustingAttempts()
    {
        int callCount = 0;

        var created = SceneInstantiationHelpers.RetryCreate(() =>
            {
                ++callCount;
                return (object?)null;
            },
            "test scene", attempts: 2, reportFailure: _ => { });

        Assert.Null(created);
        Assert.Equal(2, callCount);
    }

    private sealed class TestSceneInstance
    {
    }
}
