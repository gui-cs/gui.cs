using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui;

namespace Terminal.Gui.Benchmarks.Text.RuneExtensions;

/// <summary>
/// Benchmarks for <see cref="Tui.RuneExtensions.IsSurrogatePair"/> performance fine-tuning.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.RuneExtensions))]
public class IsSurrogatePair
{
    /// <summary>
    /// Benchmark for the current implementation.
    /// </summary>
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public bool Current (Rune rune)
    {
        return Tui.RuneExtensions.IsSurrogatePair (rune);
    }

    /// <summary>
    /// Benchmark for new implementation.
    /// 
    /// Avoids intermediate heap allocations by using stack allocated buffer.
    /// </summary>
    [Benchmark]
    [ArgumentsSource (nameof (DataSource))]
    public bool New (Rune rune)
    {
        bool isSingleUtf16CodeUnit = rune.IsBmp;
        if (isSingleUtf16CodeUnit)
        {
            return false;
        }

        const int maxCharsPerRune = 2;
        Span<char> charBuffer = stackalloc char[maxCharsPerRune];
        int charsWritten = rune.EncodeToUtf16 (charBuffer);
        return charsWritten >= 2 && char.IsSurrogatePair (charBuffer [0], charBuffer [1]);
    }

    public static IEnumerable<object> DataSource ()
    {
        yield return new Rune ('a');
        yield return "𝔹".EnumerateRunes ().Single ();
    }
}
