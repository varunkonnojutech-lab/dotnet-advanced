using System.Text;
using BenchmarkDotNet.Attributes;

namespace StringBenchmarks;

[MemoryDiagnoser] // adds allocated-bytes columns: allocations are the real story
public class StringConcatBenchmarks
{
    private readonly string[] _words =
        Enumerable.Range(0, 100).Select(i => "word" + i).ToArray();

    [Benchmark(Baseline = true)]
    public string Concat_PlusEquals()
    {
        string s = "";
        foreach (string w in _words)
            s += w;
        return s;
    }

    [Benchmark]
    public string Concat_StringBuilder()
    {
        var sb = new StringBuilder();
        foreach (string w in _words)
            sb.Append(w);
        return sb.ToString();
    }

    [Benchmark]
    public string Concat_StringCreate()
    {
        // Allocate the final string once, then fill it directly.
        // string.Create<TState>(int length, TState state, SpanAction<char, TState>)
        return string.Create(_words.Sum(w => w.Length), _words, (span, words) =>
        {
            int pos = 0;
            foreach (string w in words)
            {
                w.AsSpan().CopyTo(span.Slice(pos));
                pos += w.Length;
            }
        });
    }
}
