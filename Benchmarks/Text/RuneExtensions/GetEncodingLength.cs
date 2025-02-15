using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui;

namespace Terminal.Gui.Benchmarks.Text.RuneExtensions;

[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.RuneExtensions))]
public class GetEncodingLength
{
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public int Current (Rune rune, Encoding encoding)
    {
        return Tui.RuneExtensions.GetEncodingLength (rune, encoding);
    }

    public static IEnumerable<object []> DataSource ()
    {
        var encodings = new[] { Encoding.UTF8, Encoding.UTF32 };
        foreach (var encoding in encodings)
        {
            yield return new object [] { new Rune ('a'), encoding };
            yield return new object [] { "𝔹".EnumerateRunes ().Single (), encoding };
        }
    }
}
