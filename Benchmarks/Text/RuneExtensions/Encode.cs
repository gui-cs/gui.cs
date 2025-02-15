using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui;

namespace Terminal.Gui.Benchmarks.Text.RuneExtensions;

[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.RuneExtensions))]
public class Encode
{
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public byte [] Current (Rune rune, byte [] destination, int start, int count)
    {
        _ = Tui.RuneExtensions.Encode (rune, destination, start, count);
        return destination;
    }

    public static IEnumerable<object []> DataSource ()
    {
        var runes = new [] {
                new Rune ('a'),
                "𝔞".EnumerateRunes().Single()
            };

        foreach (var rune in runes)
        {
            yield return new object [] { rune, new byte [16], 0, -1 };
            yield return new object [] { rune, new byte [16], 8, -1 };
            // Does not work in original (baseline) implementation
            //yield return new object [] { rune, new byte [16], 8, 8 };
        }
    }
}
