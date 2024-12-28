﻿using Microsoft.Extensions.Logging;

namespace Terminal.Gui;
public class NetOutput : IConsoleOutput
{
    public bool IsWinPlatform { get; }

    private CursorVisibility? _cachedCursorVisibility;
    public NetOutput ()
    {
        Logging.Logger.LogInformation ($"Creating {nameof(NetOutput)}");

        PlatformID p = Environment.OSVersion.Platform;

        if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            IsWinPlatform = true;
        }
    }

    /// <inheritdoc />
    public void Write (string text)
    {
        Console.Write (text);
    }

    /// <inheritdoc />
    public void Write (IOutputBuffer buffer)
    {
        if ( Console.WindowHeight < 1
            || buffer.Contents.Length != buffer.Rows * buffer.Cols
            || buffer.Rows != Console.WindowHeight)
        {
       //     return;
        }

        var top = 0;
        var left = 0;
        int rows = buffer.Rows;
        int cols = buffer.Cols;
        var output = new StringBuilder ();
        Attribute? redrawAttr = null;
        int lastCol = -1;

        CursorVisibility? savedVisibility = _cachedCursorVisibility;
        SetCursorVisibility (CursorVisibility.Invisible);

        for (int row = top; row < rows; row++)
        {
            if (Console.WindowHeight < 1)
            {
                return;
            }

            if (!buffer.DirtyLines [row])
            {
                continue;
            }

            if (!SetCursorPositionImpl (0, row))
            {
                return;
            }

            buffer.DirtyLines [row] = false;
            output.Clear ();

            for (int col = left; col < cols; col++)
            {
                lastCol = -1;
                var outputWidth = 0;

                for (; col < cols; col++)
                {
                    if (!buffer.Contents [row, col].IsDirty)
                    {
                        if (output.Length > 0)
                        {
                            WriteToConsole (output, ref lastCol, row, ref outputWidth);
                        }
                        else if (lastCol == -1)
                        {
                            lastCol = col;
                        }

                        if (lastCol + 1 < cols)
                        {
                            lastCol++;
                        }

                        continue;
                    }

                    if (lastCol == -1)
                    {
                        lastCol = col;
                    }

                    Attribute attr = buffer.Contents [row, col].Attribute.Value;

                    // Performance: Only send the escape sequence if the attribute has changed.
                    if (attr != redrawAttr)
                    {
                        redrawAttr = attr;

                        output.Append (
                                       EscSeqUtils.CSI_SetForegroundColorRGB (
                                                                              attr.Foreground.R,
                                                                              attr.Foreground.G,
                                                                              attr.Foreground.B
                                                                             )
                                      );

                        output.Append (
                                       EscSeqUtils.CSI_SetBackgroundColorRGB (
                                                                              attr.Background.R,
                                                                              attr.Background.G,
                                                                              attr.Background.B
                                                                             )
                                      );
                    }

                    outputWidth++;
                    Rune rune = buffer.Contents [row, col].Rune;
                    output.Append (rune);

                    if (buffer.Contents [row, col].CombiningMarks.Count > 0)
                    {
                        // AtlasEngine does not support NON-NORMALIZED combining marks in a way
                        // compatible with the driver architecture. Any CMs (except in the first col)
                        // are correctly combined with the base char, but are ALSO treated as 1 column
                        // width codepoints E.g. `echo "[e`u{0301}`u{0301}]"` will output `[é  ]`.
                        // 
                        // For now, we just ignore the list of CMs.
                        //foreach (var combMark in Contents [row, col].CombiningMarks) {
                        //	output.Append (combMark);
                        //}
                        // WriteToConsole (output, ref lastCol, row, ref outputWidth);
                    }
                    else if (rune.IsSurrogatePair () && rune.GetColumns () < 2)
                    {
                        WriteToConsole (output, ref lastCol, row, ref outputWidth);
                        SetCursorPositionImpl (col - 1, row);
                    }

                    buffer.Contents [row, col].IsDirty = false;
                }
            }

            if (output.Length > 0)
            {
                SetCursorPositionImpl (lastCol, row);
                Console.Write (output);
            }
        }

        foreach (var s in Application.Sixel)
        {
            if (!string.IsNullOrWhiteSpace (s.SixelData))
            {
                SetCursorPositionImpl (s.ScreenPosition.X, s.ScreenPosition.Y);
                Console.Write (s.SixelData);
            }
        }

        SetCursorPositionImpl (0, 0);

        _cachedCursorVisibility = savedVisibility;
    }

    /// <inheritdoc />
    public Size GetWindowSize ()
    {
        return new Size (Console.WindowWidth, Console.WindowHeight);
    }

    void WriteToConsole (StringBuilder output, ref int lastCol, int row, ref int outputWidth)
    {
        SetCursorPositionImpl (lastCol, row);
        Console.Write (output);
        output.Clear ();
        lastCol += outputWidth;
        outputWidth = 0;
    }
    public bool SetCursorVisibility (CursorVisibility visibility)
    {
        _cachedCursorVisibility = visibility;

        Console.Out.Write (visibility == CursorVisibility.Default ? EscSeqUtils.CSI_ShowCursor : EscSeqUtils.CSI_HideCursor);

        return visibility == CursorVisibility.Default;
    }

    /// <inheritdoc />
    public void SetCursorPosition (int col, int row)
    {
        SetCursorPositionImpl (col, row);
    }

    private bool SetCursorPositionImpl (int col, int row)
    {
        if (IsWinPlatform)
        {
            // Could happens that the windows is still resizing and the col is bigger than Console.WindowWidth.
            try
            {
                Console.SetCursorPosition (col, row);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // + 1 is needed because non-Windows is based on 1 instead of 0 and
        // Console.CursorTop/CursorLeft isn't reliable.
        Console.Out.Write (EscSeqUtils.CSI_SetCursorPosition (row + 1, col + 1));

        return true;
    }

    /// <inheritdoc />
    public void Dispose ()
    {
        Console.Clear ();
    }

    void IConsoleOutput.SetCursorVisibility (CursorVisibility visibility)
    {
        Console.Out.Write (visibility == CursorVisibility.Default ? EscSeqUtils.CSI_ShowCursor : EscSeqUtils.CSI_HideCursor);
    }

}
