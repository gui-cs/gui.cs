using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui;

namespace Terminal.Gui.Benchmarks.ConsoleDrivers.EscSeqUtils;

/// <summary>
/// Compares the Set and Append implementations in combination.
/// </summary>
/// <remarks>
/// A bit misleading because *CursorPosition is called very seldom compared to the other operations
/// but they are very similar in performance because they do very similar things.
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.EscSeqUtils))]
public class CSI_SetVsAppend
{
    private readonly StringBuilder _stringBuilder = new();

    [Benchmark (Baseline = true)]
    public StringBuilder ReturnStringAndAppend ()
    {
        _stringBuilder.Append (Tui.EscSeqUtils.CSI_SetBackgroundColorRGB (1, 2, 3));
        _stringBuilder.Append (Tui.EscSeqUtils.CSI_SetForegroundColorRGB (3, 2, 1));
        _stringBuilder.Append (Tui.EscSeqUtils.CSI_SetCursorPosition (4, 2));
        // Clear to prevent out of memory exception from consecutive iterations.
        _stringBuilder.Clear ();
        return _stringBuilder;
    }

    [Benchmark]
    public StringBuilder AppendDirectlyToStringBuilder ()
    {
        Tui.EscSeqUtils.CSI_AppendBackgroundColorRGB (_stringBuilder, 1, 2, 3);
        Tui.EscSeqUtils.CSI_AppendForegroundColorRGB (_stringBuilder, 3, 2, 1);
        Tui.EscSeqUtils.CSI_AppendCursorPosition (_stringBuilder, 4, 2);
        // Clear to prevent out of memory exception from consecutive iterations.
        _stringBuilder.Clear ();
        return _stringBuilder;
    }
}
