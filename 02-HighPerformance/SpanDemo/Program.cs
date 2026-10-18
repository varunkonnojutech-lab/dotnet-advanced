using System.Buffers;
using System.Diagnostics;
using System.Text;

Console.WriteLine("=== (a) Zero-allocation CSV parsing with ReadOnlySpan<char> ===");

if (TryParseCsvLine("Alice,30,New York".AsSpan(), out var name, out var age, out var city))
    Console.WriteLine($"Parsed (zero allocations): name={name.ToString()}, age={age}, city={city.ToString()}");

Console.WriteLine();
Console.WriteLine("=== (b) stackalloc: array-like memory on the stack, no heap ===");

Span<int> numbers = stackalloc int[8];
for (int i = 0; i < numbers.Length; i++)
    numbers[i] = i + 1;
int sum = 0;
foreach (int n in numbers)
    sum += n;
Console.WriteLine($"Sum of 1..8 from stack memory: {sum}");

Console.WriteLine();
Console.WriteLine("=== (c) ArrayPool<byte>: rent buffers instead of allocating ===");

var pool = ArrayPool<byte>.Shared;
byte[] buffer = pool.Rent(1024);
try
{
    byte[] data = Encoding.UTF8.GetBytes("Hello from ArrayPool!");
    data.CopyTo(buffer, 0);
    Console.WriteLine($"Rented {buffer.Length} bytes, wrote {data.Length} bytes: " +
                      $"{Encoding.UTF8.GetString(buffer, 0, data.Length)}");
}
finally
{
    pool.Return(buffer); // always return, ideally in a finally
}

Console.WriteLine();
Console.WriteLine("=== (d) Honest benchmark: Span parsing vs string.Split (1,000,000 lines) ===");

const string csvLine = "Alice,30,New York";
const int iterations = 1_000_000;

var sw = Stopwatch.StartNew();
int spanTotal = 0;
for (int i = 0; i < iterations; i++)
{
    if (TryParseCsvLine(csvLine.AsSpan(), out _, out int spanAge, out _))
        spanTotal += spanAge;
}
sw.Stop();
Console.WriteLine($"Span slicing:  {sw.ElapsedMilliseconds,6} ms  (checksum {spanTotal})");

sw.Restart();
int splitTotal = 0;
for (int i = 0; i < iterations; i++)
{
    string[] parts = csvLine.Split(','); // allocates a string[] + 3 strings per line
    splitTotal += int.Parse(parts[1]);
}
sw.Stop();
Console.WriteLine($"string.Split:  {sw.ElapsedMilliseconds,6} ms  (checksum {splitTotal})");
Console.WriteLine("(Checksums prove both loops did real work — the JIT can't skip them.)");

// Parses "name,age,city" using only slices of the original span.
// No substrings, no string[], no heap allocations at all.
static bool TryParseCsvLine(ReadOnlySpan<char> line,
                            out ReadOnlySpan<char> name,
                            out int age,
                            out ReadOnlySpan<char> city)
{
    name = default;
    age = 0;
    city = default;

    int first = line.IndexOf(',');
    if (first < 0)
        return false;

    int second = line.Slice(first + 1).IndexOf(',');
    if (second < 0)
        return false;

    name = line.Slice(0, first);
    ReadOnlySpan<char> ageSpan = line.Slice(first + 1, second);
    city = line.Slice(first + 1 + second + 1);

    return int.TryParse(ageSpan, out age);
}
