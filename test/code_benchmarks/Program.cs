using BenchmarkDotNet.Running;
using CodeBenchmark;

// _ = BenchmarkRunner.Run<ComparerBenchmark>();
_ = BenchmarkRunner.Run<FrozenDictionaryBenchmark>();

// If this needs to be profiled in Rider, this main needs to be modified to call that instead of the benchmark runner
// FrozenDictionaryBenchmark.ExampleRun();
