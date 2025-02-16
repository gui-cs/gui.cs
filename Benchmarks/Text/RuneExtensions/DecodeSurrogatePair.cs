using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui;

namespace Terminal.Gui.Benchmarks.Text.RuneExtensions;

/// <summary>
/// Benchmarks for <see cref="Tui.RuneExtensions.DecodeSurrogatePair"/> performance fine-tuning.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.RuneExtensions))]
public class DecodeSurrogatePair
{
    /// <summary>
    /// Benchmark for current ToString -> ToCharArray implementation.
    /// </summary>
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public char [] Current (Rune rune)
    {
        Tui.RuneExtensions.DecodeSurrogatePair (rune, out char []? chars);
        return chars;
    }

    /// <summary>
    /// Benchmark for new implementation.
    /// </summary>
    /// <param name="rune"></param>
    /// <returns></returns>
    [Benchmark]
    [ArgumentsSource (nameof (DataSource))]
    public char []? New (Rune rune)
    {
        RuneEncodeToCharBuffer (rune, out char []? chars);
        return chars;
    }

    /// <summary>
    /// Utilizes Rune methods that take Span argument avoiding intermediate heap array allocation when combined with stack allocated intermediate buffer.
    /// When rune is not surrogate pair there will be no heap allocation.
    /// 
    /// Final surrogate pair array allocation cannot be avoided due to the current method signature design.
    /// Changing the method signature, or providing an alternative method, to take a destination Span would allow further optimizations by allowing caller to reuse buffer for consecutive calls.
    /// </summary>
    private static bool RuneEncodeToCharBuffer (Rune rune, out char []? chars)
    {
        bool isSingleUtf16CodeUnit = rune.IsBmp;
        if (isSingleUtf16CodeUnit)
        {
            chars = null;
            return false;
        }

        const int maxRuneUtf16Length = 2;
        Span<char> charBuffer = stackalloc char[maxRuneUtf16Length];
        int charsWritten = rune.EncodeToUtf16 (charBuffer);
        if (charsWritten >= 2 && char.IsSurrogatePair (charBuffer [0], charBuffer [1]))
        {
            chars = charBuffer [..charsWritten].ToArray ();
            return true;
        }

        chars = null;
        return false;
    }

    public static IEnumerable<object> DataSource ()
    {
        yield return new Rune ('a');
        yield return "𝔹".EnumerateRunes ().Single ();
    }
}
