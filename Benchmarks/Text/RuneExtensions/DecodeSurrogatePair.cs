using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui;

namespace Terminal.Gui.Benchmarks.Text.RuneExtensions;

[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.RuneExtensions))]
public class DecodeSurrogatePair
{
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public char [] Current (Rune rune)
    {
        Tui.RuneExtensions.DecodeSurrogatePair (rune, out char [] chars);
        return chars;
    }

    public static IEnumerable<object> DataSource ()
    {
        yield return new Rune ('a');
        yield return "𝔹".EnumerateRunes ().Single ();
    }
}
