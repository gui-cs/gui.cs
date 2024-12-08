#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Terminal.Gui;

namespace UICatalog.Scenarios;

/// <summary>
///     A scrollable map of the Unicode codepoints.
/// </summary>
/// <remarks>
///     See <see href="CharacterMap/README.md"/> for details.
/// </remarks>
public class CharMap : View, IDesignable
{
    private const int COLUMN_WIDTH = 3; // Width of each column of glyphs
    private const int HEADER_HEIGHT = 1; // Height of the header
    private int _rowHeight = 1; // Height of each row of 16 glyphs - changing this is not tested

    private ContextMenu _contextMenu = new ();

    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    public CharMap ()
    {
        base.ColorScheme = Colors.ColorSchemes ["Dialog"];
        CanFocus = true;
        CursorVisibility = CursorVisibility.Default;

        AddCommand (
                    Command.Up,
                    () =>
                    {
                        SelectedCodePoint -= 16;

                        return true;
                    }
                   );

        AddCommand (
                    Command.Down,
                    () =>
                    {
                        SelectedCodePoint += 16;

                        return true;
                    }
                   );

        AddCommand (
                    Command.Left,
                    () =>
                    {
                        SelectedCodePoint--;

                        return true;
                    }
                   );

        AddCommand (
                    Command.Right,
                    () =>
                    {
                        SelectedCodePoint++;

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageUp,
                    () =>
                    {
                        int page = (Viewport.Height - HEADER_HEIGHT / _rowHeight) * 16;
                        SelectedCodePoint -= page;

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageDown,
                    () =>
                    {
                        int page = (Viewport.Height - HEADER_HEIGHT / _rowHeight) * 16;
                        SelectedCodePoint += page;

                        return true;
                    }
                   );

        AddCommand (
                    Command.Start,
                    () =>
                    {
                        SelectedCodePoint = 0;

                        return true;
                    }
                   );

        AddCommand (
                    Command.End,
                    () =>
                    {
                        SelectedCodePoint = MAX_CODE_POINT;

                        return true;
                    }
                   );

        AddCommand (
                    Command.Accept,
                    () =>
                    {
                        ShowDetails ();

                        return true;
                    }
                   );

        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);

        MouseClick += Handle_MouseClick;

        SetContentSize (new (COLUMN_WIDTH * 16 + RowLabelWidth, MAX_CODE_POINT / 16 * _rowHeight + HEADER_HEIGHT));

        // Set up the horizontal scrollbar. Turn off AutoShow since we do it manually.
        HorizontalScrollBar.AutoShow = false;
        HorizontalScrollBar.Increment = COLUMN_WIDTH;

        // This prevents scrolling past the last column
        HorizontalScrollBar.ScrollableContentSize = GetContentSize ().Width - RowLabelWidth;
        HorizontalScrollBar.X = RowLabelWidth;
        HorizontalScrollBar.Y = Pos.AnchorEnd ();
        HorizontalScrollBar.Width = Dim.Fill (1);

        // We want the horizontal scrollbar to only show when needed.
        // We can't use ScrollBar.AutoShow because we are using custom ContentSize
        // So, we do it manually on ViewportChanged events.
        ViewportChanged += (sender, args) =>
                           {
                               if (Viewport.Width < GetContentSize ().Width)
                               {
                                   HorizontalScrollBar.Visible = true;
                               }
                               else
                               {
                                   HorizontalScrollBar.Visible = false;
                               }
                           };

        // Set up the vertical scrollbar. Turn off AutoShow since it's always visible.
        VerticalScrollBar.AutoShow = false;
        VerticalScrollBar.Visible = true; // Force always visible
        VerticalScrollBar.X = Pos.AnchorEnd ();
        VerticalScrollBar.Y = HEADER_HEIGHT; // Header
    }

    private void ScrollToMakeCursorVisible (Point offsetToNewCursor)
    {
        // Adjust vertical scrolling
        if (offsetToNewCursor.Y < 1) // Header is at Y = 0
        {
            ScrollVertical (offsetToNewCursor.Y - HEADER_HEIGHT);
        }
        else if (offsetToNewCursor.Y >= Viewport.Height)
        {
            ScrollVertical (offsetToNewCursor.Y - Viewport.Height + HEADER_HEIGHT);
        }

        // Adjust horizontal scrolling
        if (offsetToNewCursor.X < RowLabelWidth + 1)
        {
            ScrollHorizontal (offsetToNewCursor.X - (RowLabelWidth + 1));
        }
        else if (offsetToNewCursor.X >= Viewport.Width)
        {
            ScrollHorizontal (offsetToNewCursor.X - Viewport.Width + 1);
        }
    }

    #region Cursor

    private Point GetCursor (int codePoint)
    {
        // + 1 for padding between label and first column
        int x = codePoint % 16 * COLUMN_WIDTH + RowLabelWidth + 1 - Viewport.X;
        int y = codePoint / 16 * _rowHeight + HEADER_HEIGHT - Viewport.Y;

        return new (x, y);
    }

    public override Point? PositionCursor ()
    {
        Point cursor = GetCursor (SelectedCodePoint);

        if (HasFocus
            && cursor.X >= RowLabelWidth
            && cursor.X < Viewport.Width
            && cursor.Y > 0
            && cursor.Y < Viewport.Height)
        {
            Move (cursor.X, cursor.Y);
        }
        else
        {
            return null;
        }

        return cursor;
    }

    #endregion Cursor

    // ReSharper disable once InconsistentNaming
    private static readonly int MAX_CODE_POINT = UnicodeRange.Ranges.Max (r => r.End);
    private int _selectedCodepoint; // Currently selected codepoint
    private int _startCodepoint; // The codepoint that will be displayed at the top of the Viewport

    /// <summary>
    ///     Gets or sets the currently selected codepoint. Causes the Viewport to scroll to make the selected code point
    ///     visible.
    /// </summary>
    public int SelectedCodePoint
    {
        get => _selectedCodepoint;
        set
        {
            if (_selectedCodepoint == value)
            {
                return;
            }

            int newSelectedCodePoint = Math.Clamp (value, 0, MAX_CODE_POINT);

            Point offsetToNewCursor = GetCursor (newSelectedCodePoint);

            _selectedCodepoint = newSelectedCodePoint;

            // Ensure the new cursor position is visible
            ScrollToMakeCursorVisible (offsetToNewCursor);

            SetNeedsDraw ();
            SelectedCodePointChanged?.Invoke (this, new (SelectedCodePoint));
        }
    }

    /// <summary>
    ///     Raised when the selected code point changes.
    /// </summary>
    public event EventHandler<EventArgs<int>>? SelectedCodePointChanged;

    /// <summary>
    ///     Specifies the starting offset for the character map. The default is 0x2500 which is the Box Drawing
    ///     characters.
    /// </summary>
    public int StartCodePoint
    {
        get => _startCodepoint;
        set
        {
            _startCodepoint = value;
            SelectedCodePoint = value;
        }
    }

    /// <summary>
    ///     Gets or sets whether the number of columns each glyph is displayed.
    /// </summary>
    public bool ShowGlyphWidths
    {
        get => _rowHeight == 2;
        set
        {
            _rowHeight = value ? 2 : 1;
            SetNeedsDraw ();
        }
    }

    private void CopyCodePoint () { Clipboard.Contents = $"U+{SelectedCodePoint:x5}"; }
    private void CopyGlyph () { Clipboard.Contents = $"{new Rune (SelectedCodePoint)}"; }

    #region Drawing

    private static int RowLabelWidth => $"U+{MAX_CODE_POINT:x5}".Length + 1;

    protected override bool OnDrawingContent ()
    {
        ConsoleDriver.IgnoreIsCombiningMark = true;

        if (Viewport.Height == 0 || Viewport.Width == 0)
        {
            return true;
        }

        int cursorCol = GetCursor (SelectedCodePoint).X + Viewport.X - RowLabelWidth - 1;
        int cursorRow = GetCursor (SelectedCodePoint).Y + Viewport.Y - 1;

        SetAttribute (GetHotNormalColor ());
        Move (0, 0);
        AddStr (new (' ', RowLabelWidth + 1));

        int firstColumnX = RowLabelWidth - Viewport.X;

        // Header
        for (var hexDigit = 0; hexDigit < 16; hexDigit++)
        {
            int x = firstColumnX + hexDigit * COLUMN_WIDTH;

            if (x > RowLabelWidth - 2)
            {
                Move (x, 0);
                SetAttribute (GetHotNormalColor ());
                AddStr (" ");
                SetAttribute (HasFocus && cursorCol + firstColumnX == x ? GetHotFocusColor () : GetHotNormalColor ());
                AddStr ($"{hexDigit:x}");
                SetAttribute (GetHotNormalColor ());
                AddStr (" ");
            }
        }

        // Start at 1 because Header.
        for (var y = 1; y < Viewport.Height; y++)
        {
            // What row is this?
            int row = (y + Viewport.Y - 1) / _rowHeight;

            int val = row * 16;

            if (val > MAX_CODE_POINT)
            {
                break;
            }

            Move (firstColumnX + COLUMN_WIDTH, y);
            SetAttribute (GetNormalColor ());

            bool lastWasNonSpacingMark = false;

            for (var col = 0; col < 16; col++)
            {
                int x = firstColumnX + COLUMN_WIDTH * col + 1;

                if (x < 0 || x > Viewport.Width - 1)
                {
                    continue;
                }

                Move (x, y);

                // If we're at the cursor position, and we don't have focus, invert the colors.
                if (row == cursorRow && x == cursorCol && !HasFocus)
                {
                    SetAttribute (GetFocusColor ());
                }

                int scalar = val + col;
                var rune = (Rune)'?';

                if (Rune.IsValid (scalar))
                {
                    rune = new (scalar);
                }

                int width = rune.GetColumns ();

                if (!ShowGlyphWidths || (y + Viewport.Y) % _rowHeight > 0)
                {
                    // Draw the rune
                    //if (width > 0)
                    //{
                        AddRune (rune);
                    //}
                    //else
                    //{
                    //    if (rune.IsCombiningMark ())
                    //    {
                    //        // This is a hack to work around the fact that combining marks
                    //        // a) can't be rendered on their own
                    //        // b) that don't normalize are not properly supported in 
                    //        //    any known terminal (esp Windows/AtlasEngine). 
                    //        // See Issue #2616
                    //        var sb = new StringBuilder ();
                    //        sb.Append ('a');
                    //        sb.Append (rune);

                    //        // Try normalizing after combining with 'a'. If it normalizes, at least 
                    //        // it'll show on the 'a'. If not, just show the replacement char.
                    //        string normal = sb.ToString ().Normalize (NormalizationForm.FormC);

                    //        if (normal.Length == 1)
                    //        {
                    //            AddRune ((Rune)normal [0]);
                    //        }
                    //        else
                    //        {
                    //            AddRune (Rune.ReplacementChar);
                    //        }
                    //    }
                    //}
                }
                else
                {
                    // Draw the width of the rune
                    SetAttribute (GetHotNormalColor ());
                    AddStr ($"{width}");
                }

                // If we're at the cursor position, and we don't have focus, revert the colors to normal
                if (row == cursorRow && x == cursorCol && !HasFocus)
                {
                    SetAttribute (GetNormalColor ());
                }
            }

            // Draw row label (U+XXXX_)
            Move (0, y);

            SetAttribute (HasFocus && y + Viewport.Y - 1 == cursorRow ? GetHotFocusColor () : GetHotNormalColor ());

            if (!ShowGlyphWidths || (y + Viewport.Y) % _rowHeight > 0)
            {
                AddStr ($"U+{val / 16:x5}_ ");
            }
            else
            {
                AddStr (new (' ', RowLabelWidth));
            }
        }

        ConsoleDriver.IgnoreIsCombiningMark = false;

        return true;
    }

    /// <summary>
    ///     Helper to convert a string into camel case.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToCamelCase (string str)
    {
        if (string.IsNullOrEmpty (str))
        {
            return str;
        }

        TextInfo textInfo = new CultureInfo ("en-US", false).TextInfo;

        str = textInfo.ToLower (str);
        str = textInfo.ToTitleCase (str);

        return str;
    }

    #endregion Drawing

    #region Mouse Handling

    // TODO: Use this to demonstrate using a popover to show glyph info on hover
    public event EventHandler<ListViewItemEventArgs>? Hover;

    /// <inheritdoc />
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        if (mouseEvent.Flags == MouseFlags.WheeledDown)
        {
            if (Viewport.Y + Viewport.Height - HEADER_HEIGHT < GetContentSize ().Height)
            {
                ScrollVertical (1);
            }
            return mouseEvent.Handled = true;
        }

        if (mouseEvent.Flags == MouseFlags.WheeledUp)
        {
            ScrollVertical (-1);
            return mouseEvent.Handled = true;
        }

        if (mouseEvent.Flags == MouseFlags.WheeledRight)
        {
            if (Viewport.X + Viewport.Width < GetContentSize ().Width)
            {
                ScrollHorizontal (1);
            }
            return mouseEvent.Handled = true;
        }

        if (mouseEvent.Flags == MouseFlags.WheeledLeft)
        {
            ScrollHorizontal (-1);
            return mouseEvent.Handled = true;
        }

        return false;
    }

    private void Handle_MouseClick (object? sender, MouseEventArgs me)
    {
        if (me.Flags != MouseFlags.ReportMousePosition && me.Flags != MouseFlags.Button1Clicked && me.Flags != MouseFlags.Button1DoubleClicked)
        {
            return;
        }

        if (me.Position.Y == 0)
        {
            me.Position = me.Position with { Y = GetCursor (SelectedCodePoint).Y };
        }

        if (me.Position.X < RowLabelWidth || me.Position.X > RowLabelWidth + 16 * COLUMN_WIDTH - 1)
        {
            me.Position = me.Position with { X = GetCursor (SelectedCodePoint).X };
        }

        int row = (me.Position.Y - 1 - -Viewport.Y) / _rowHeight; // -1 for header
        int col = (me.Position.X - RowLabelWidth - -Viewport.X) / COLUMN_WIDTH;

        if (col > 15)
        {
            col = 15;
        }

        int val = row * 16 + col;

        if (val > MAX_CODE_POINT)
        {
            return;
        }

        if (me.Flags == MouseFlags.ReportMousePosition)
        {
            Hover?.Invoke (this, new (val, null));
        }

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        me.Handled = true;

        if (me.Flags == MouseFlags.Button1Clicked)
        {
            SelectedCodePoint = val;

            return;
        }

        if (me.Flags == MouseFlags.Button1DoubleClicked)
        {
            SelectedCodePoint = val;
            ShowDetails ();

            return;
        }

        if (me.Flags == _contextMenu.MouseFlags)
        {
            SelectedCodePoint = val;

            _contextMenu = new ()
            {
                Position = new (me.Position.X + 1, me.Position.Y + 1)
            };

            MenuBarItem menuItems = new (
                                         new MenuItem []
                                         {
                                             new (
                                                  "_Copy Glyph",
                                                  "",
                                                  CopyGlyph,
                                                  null,
                                                  null,
                                                  (KeyCode)Key.C.WithCtrl
                                                 ),
                                             new (
                                                  "Copy Code _Point",
                                                  "",
                                                  CopyCodePoint,
                                                  null,
                                                  null,
                                                  (KeyCode)Key.C.WithCtrl
                                                              .WithShift
                                                 )
                                         }
                                        );
            _contextMenu.Show (menuItems);
        }
    }

    #endregion Mouse Handling

    #region Details Dialog

    private void ShowDetails ()
    {
        UcdApiClient? client = new ();
        var decResponse = string.Empty;
        var getCodePointError = string.Empty;

        Dialog? waitIndicator = new ()
        {
            Title = "Getting Code Point Information",
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = 40,
            Height = 10,
            Buttons = [new () { Text = "_Cancel" }]
        };

        var errorLabel = new Label
        {
            Text = UcdApiClient.BaseUrl,
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (3),
            TextAlignment = Alignment.Center
        };

        var spinner = new SpinnerView
        {
            X = Pos.Center (),
            Y = Pos.Bottom (errorLabel),
            Style = new SpinnerStyle.Aesthetic ()
        };
        spinner.AutoSpin = true;
        waitIndicator.Add (errorLabel);
        waitIndicator.Add (spinner);

        waitIndicator.Ready += async (s, a) =>
                               {
                                   try
                                   {
                                       decResponse = await client.GetCodepointDec (SelectedCodePoint);
                                       Application.Invoke (() => waitIndicator.RequestStop ());
                                   }
                                   catch (HttpRequestException e)
                                   {
                                       getCodePointError = errorLabel.Text = e.Message;
                                       Application.Invoke (() => waitIndicator.RequestStop ());
                                   }
                               };
        Application.Run (waitIndicator);
        waitIndicator.Dispose ();

        if (!string.IsNullOrEmpty (decResponse))
        {
            var name = string.Empty;

            using (JsonDocument document = JsonDocument.Parse (decResponse))
            {
                JsonElement root = document.RootElement;

                // Get a property by name and output its value
                if (root.TryGetProperty ("name", out JsonElement nameElement))
                {
                    name = nameElement.GetString ();
                }

                //// Navigate to a nested property and output its value
                //if (root.TryGetProperty ("property3", out JsonElement property3Element)
                //&& property3Element.TryGetProperty ("nestedProperty", out JsonElement nestedPropertyElement)) {
                //	Console.WriteLine (nestedPropertyElement.GetString ());
                //}
                decResponse = JsonSerializer.Serialize (
                                                        document.RootElement,
                                                        new
                                                            JsonSerializerOptions
                                                        { WriteIndented = true }
                                                       );
            }

            var title = $"{ToCamelCase (name!)} - {new Rune (SelectedCodePoint)} U+{SelectedCodePoint:x5}";

            Button? copyGlyph = new () { Text = "Copy _Glyph" };
            Button? copyCodepoint = new () { Text = "Copy Code _Point" };
            Button? cancel = new () { Text = "Cancel" };

            var dlg = new Dialog { Title = title, Buttons = [copyGlyph, copyCodepoint, cancel] };

            copyGlyph.Accepting += (s, a) =>
                                   {
                                       CopyGlyph ();
                                       dlg!.RequestStop ();
                                   };

            copyCodepoint.Accepting += (s, a) =>
                                       {
                                           CopyCodePoint ();
                                           dlg!.RequestStop ();
                                       };
            cancel.Accepting += (s, a) => dlg!.RequestStop ();

            var rune = (Rune)SelectedCodePoint;
            var label = new Label { Text = "IsAscii: ", X = 0, Y = 0 };
            dlg.Add (label);

            label = new () { Text = $"{rune.IsAscii}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new () { Text = ", Bmp: ", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new () { Text = $"{rune.IsBmp}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new () { Text = ", CombiningMark: ", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new () { Text = $"{rune.IsCombiningMark ()}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new () { Text = ", SurrogatePair: ", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new () { Text = $"{rune.IsSurrogatePair ()}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new () { Text = ", Plane: ", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new () { Text = $"{rune.Plane}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new () { Text = "Columns: ", X = 0, Y = Pos.Bottom (label) };
            dlg.Add (label);

            label = new () { Text = $"{rune.GetColumns ()}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new () { Text = ", Utf16SequenceLength: ", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new () { Text = $"{rune.Utf16SequenceLength}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new ()
            {
                Text =
                    $"Code Point Information from {UcdApiClient.BaseUrl}codepoint/dec/{SelectedCodePoint}:",
                X = 0,
                Y = Pos.Bottom (label)
            };
            dlg.Add (label);

            var json = new TextView
            {
                X = 0,
                Y = Pos.Bottom (label),
                Width = Dim.Fill (),
                Height = Dim.Fill (2),
                ReadOnly = true,
                Text = decResponse
            };

            dlg.Add (json);

            Application.Run (dlg);
            dlg.Dispose ();
        }
        else
        {
            MessageBox.ErrorQuery (
                                   "Code Point API",
                                   $"{UcdApiClient.BaseUrl}codepoint/dec/{SelectedCodePoint} did not return a result for\r\n {new Rune (SelectedCodePoint)} U+{SelectedCodePoint:x5}.",
                                   "_Ok"
                                  );
        }

        // BUGBUG: This is a workaround for some weird ScrollView related mouse grab bug
        Application.GrabMouse (this);
    }

    #endregion Details Dialog
}
