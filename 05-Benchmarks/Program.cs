using BenchmarkDotNet.Running;
using StringBenchmarks;

// Always run benchmarks in Release: Debug JIT output skews the numbers.
//   dotnet run --project 05-Benchmarks -c Release
BenchmarkRunner.Run<StringConcatBenchmarks>();
