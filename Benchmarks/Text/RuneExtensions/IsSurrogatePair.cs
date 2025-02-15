using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui;

namespace Terminal.Gui.Benchmarks.Text.RuneExtensions;

[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.RuneExtensions))]
public class IsSurrogatePair
{
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public bool Current (Rune rune)
    {
        return Tui.RuneExtensions.IsSurrogatePair (rune);
    }

    public static IEnumerable<object> DataSource ()
    {
        yield return new Rune ('a');
        yield return "𝔹".EnumerateRunes ().Single ();
    }
}
