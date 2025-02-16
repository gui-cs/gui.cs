using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui;

namespace Terminal.Gui.Benchmarks.Text.RuneExtensions;

/// <summary>
/// Benchmarks for <see cref="Tui.RuneExtensions.GetEncodingLength"/> performance fine-tuning.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.RuneExtensions))]
public class GetEncodingLength
{
    /// <summary>
    /// Benchmark for previous implementation.
    /// </summary>
    [Benchmark]
    [ArgumentsSource (nameof (DataSource))]
    public int Previous (Rune rune, Encoding encoding)
    {
        return WithEncodingGetBytesArray (rune, encoding);
    }

    /// <summary>
    /// Benchmark for current implementation.
    /// </summary>
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public int Current (Rune rune, Encoding encoding)
    {
        return Tui.RuneExtensions.GetEncodingLength (rune, encoding);
    }

    /// <summary>
    /// Previous implementation with intermediate byte array, string, and char array allocation.
    /// </summary>
    private static int WithEncodingGetBytesArray (Rune rune, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        byte [] bytes = encoding.GetBytes (rune.ToString ().ToCharArray ());
        var offset = 0;

        if (bytes [^1] == 0)
        {
            offset++;
        }

        return bytes.Length - offset;
    }

    public static IEnumerable<object []> DataSource ()
    {
        Encoding[] encodings = [ Encoding.UTF8, Encoding.Unicode, Encoding.UTF32 ];
        Rune[] runes = [ new Rune ('a'), "𝔹".EnumerateRunes ().Single () ];

        foreach (Encoding encoding in encodings)
        {
            foreach (Rune rune in runes)
            {
                yield return [rune, encoding];
            }
        }
    }
}
