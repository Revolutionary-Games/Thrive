namespace CodeBenchmark;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class ComparerBenchmark
{
    [Params(1000, 10000)]
    public int N;

    private ValueTuple<int, int>[] data = [];

    [GlobalSetup]
    public void Setup()
    {
        data = new ValueTuple<int, int>[N];
        var random = new Random(42);

        for (int i = 0; i < data.Length; ++i)
        {
            data[i] = new ValueTuple<int, int>(random.Next(), random.Next());
        }
    }

    [Benchmark]
    public void GenericComparer()
    {
        var comparer = Comparer<ValueTuple<int, int>>.Default;

        for (int i = 1; i < data.Length; ++i)
        {
            _ = comparer.Compare(data[i - 1], data[i]);
        }
    }

    [Benchmark]
    public void EqualityComparer()
    {
        var comparer = EqualityComparer<ValueTuple<int, int>>.Default;

        for (int i = 1; i < data.Length; ++i)
        {
            _ = comparer.Equals(data[i - 1], data[i]);
        }
    }
}
