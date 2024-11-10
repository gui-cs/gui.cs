﻿namespace Terminal.Gui;

/// <summary>The <see cref="ColorPicker16"/> <see cref="View"/> Color picker.</summary>
public class ColorPicker16 : View
{
    /// <summary>Initializes a new instance of <see cref="ColorPicker16"/>.</summary>
    public ColorPicker16 () { SetInitialProperties (); }

    /// <summary>Columns of color boxes</summary>
    private readonly int _cols = 8;

    /// <summary>Rows of color boxes</summary>
    private readonly int _rows = 2;

    private int _boxHeight = 2;
    private int _boxWidth = 4;
    private int _selectColorIndex = (int)Color.Black;

    /// <summary>Height of a color box</summary>
    public int BoxHeight
    {
        get => _boxHeight;
        set
        {
            if (_boxHeight != value)
            {
                _boxHeight = value;
                Width = Dim.Auto (minimumContentDim: _boxWidth * _cols);
                Height = Dim.Auto (minimumContentDim: _boxHeight * _rows);
                SetContentSize (new (_boxWidth * _cols, _boxHeight * _rows));
                SetNeedsLayout ();
            }
        }
    }

    /// <summary>Width of a color box</summary>
    public int BoxWidth
    {
        get => _boxWidth;
        set
        {
            if (_boxWidth != value)
            {
                _boxWidth = value;
                Width = Dim.Auto (minimumContentDim: _boxWidth * _cols);
                Height = Dim.Auto (minimumContentDim: _boxHeight * _rows);
                SetContentSize (new (_boxWidth * _cols, _boxHeight * _rows));
                SetNeedsLayout ();
            }
        }
    }

    /// <summary>Fired when a color is picked.</summary>
    [CanBeNull]
    public event EventHandler<ColorEventArgs> ColorChanged;

    /// <summary>Cursor for the selected color.</summary>
    public Point Cursor
    {
        get => new (_selectColorIndex % _cols, _selectColorIndex / _cols);
        set
        {
            int colorIndex = value.Y * _cols + value.X;
            SelectedColor = (ColorName16)colorIndex;
        }
    }

    /// <summary>Moves the selected item index to the next row.</summary>
    /// <returns></returns>
    private bool MoveDown (CommandContext ctx)
    {
        if (RaiseSelecting (ctx) == true)
        {
            return true;
        }
        if (Cursor.Y < _rows - 1)
        {
            SelectedColor += _cols;
        }

        return true;
    }

    /// <summary>Moves the selected item index to the previous column.</summary>
    /// <returns></returns>
    private bool MoveLeft (CommandContext ctx)
    {
        if (RaiseSelecting (ctx) == true)
        {
            return true;
        }

        if (Cursor.X > 0)
        {
            SelectedColor--;
        }

        return true;
    }

    /// <summary>Moves the selected item index to the next column.</summary>
    /// <returns></returns>
    private bool MoveRight (CommandContext ctx)
    {
        if (RaiseSelecting (ctx) == true)
        {
            return true;
        }
        if (Cursor.X < _cols - 1)
        {
            SelectedColor++;
        }

        return true;
    }

    /// <summary>Moves the selected item index to the previous row.</summary>
    /// <returns></returns>
    private bool MoveUp (CommandContext ctx)
    {
        if (RaiseSelecting (ctx) == true)
        {
            return true;
        }
        if (Cursor.Y > 0)
        {
            SelectedColor -= _cols;
        }

        return true;
    }

    ///<inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        SetAttribute (HasFocus ? ColorScheme.Focus : GetNormalColor ());
        var colorIndex = 0;

        for (var y = 0; y < Math.Max (2, Viewport.Height / BoxHeight); y++)
        {
            for (var x = 0; x < Math.Max (8, Viewport.Width / BoxWidth); x++)
            {
                int foregroundColorIndex = y == 0 ? colorIndex + _cols : colorIndex - _cols;

                if (foregroundColorIndex > 15 || colorIndex > 15)
                {
                    continue;
                }

                SetAttribute (new ((ColorName16)foregroundColorIndex, (ColorName16)colorIndex));
                bool selected = x == Cursor.X && y == Cursor.Y;
                DrawColorBox (x, y, selected);
                colorIndex++;
            }
        }

        return true;
    }

    /// <summary>Selected color.</summary>
    public ColorName16 SelectedColor
    {
        get => (ColorName16)_selectColorIndex;
        set
        {
            if (value == (ColorName16)_selectColorIndex)
            {
                return;
            }

            _selectColorIndex = (int)value;

            ColorChanged?.Invoke (
                                  this,
                                  new (value)
                                 );
            SetNeedsDraw ();
        }
    }

    /// <summary>Add the commands.</summary>
    private void AddCommands ()
    {
        AddCommand (Command.Left, (ctx) => MoveLeft (ctx));
        AddCommand (Command.Right, (ctx) => MoveRight (ctx));
        AddCommand (Command.Up, (ctx) => MoveUp (ctx));
        AddCommand (Command.Down, (ctx) => MoveDown (ctx));

        AddCommand (Command.Select, (ctx) =>
                                    {
                                        bool set = false;
                                        if (ctx.Data is MouseEventArgs me)
                                        {
                                            Cursor = new (me.Position.X / _boxWidth, me.Position.Y / _boxHeight);
                                            set = true;
                                        }
                                        return RaiseAccepting (ctx) == true || set;
                                    });
    }

    /// <summary>Add the KeyBindings.</summary>
    private void AddKeyBindings ()
    {
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
    }

    // TODO: Decouple Cursor from SelectedColor so that mouse press-and-hold can show the color under the cursor.


    /// <summary>Draw a box for one color.</summary>
    /// <param name="x">X location.</param>
    /// <param name="y">Y location</param>
    /// <param name="selected"></param>
    private void DrawColorBox (int x, int y, bool selected)
    {
        var index = 0;

        for (var zoomedY = 0; zoomedY < BoxHeight; zoomedY++)
        {
            for (var zoomedX = 0; zoomedX < BoxWidth; zoomedX++)
            {
                AddRune (x * BoxWidth + zoomedX, y * BoxHeight + zoomedY, (Rune)' ');
                index++;
            }
        }

        if (selected)
        {
            DrawFocusRect (new (x * BoxWidth, y * BoxHeight, BoxWidth, BoxHeight));
        }
    }

    private void DrawFocusRect (Rectangle rect)
    {
        var lc = new LineCanvas ();

        if (rect.Width == 1)
        {
            lc.AddLine (rect.Location, rect.Height, Orientation.Vertical, LineStyle.Dotted);
        }
        else if (rect.Height == 1)
        {
            lc.AddLine (rect.Location, rect.Width, Orientation.Horizontal, LineStyle.Dotted);
        }
        else
        {
            lc.AddLine (rect.Location, rect.Width, Orientation.Horizontal, LineStyle.Dotted);

            lc.AddLine (
                        rect.Location with { Y = rect.Location.Y + rect.Height - 1 },
                        rect.Width,
                        Orientation.Horizontal,
                        LineStyle.Dotted
                       );

            lc.AddLine (rect.Location, rect.Height, Orientation.Vertical, LineStyle.Dotted);

            lc.AddLine (
                        rect.Location with { X = rect.Location.X + rect.Width - 1 },
                        rect.Height,
                        Orientation.Vertical,
                        LineStyle.Dotted
                       );
        }

        foreach (KeyValuePair<Point, Rune> p in lc.GetMap ())
        {
            AddRune (p.Key.X, p.Key.Y, p.Value);
        }
    }

    private void SetInitialProperties ()
    {
        HighlightStyle = HighlightStyle.PressedOutside | HighlightStyle.Pressed;

        CanFocus = true;
        AddCommands ();
        AddKeyBindings ();

        Width = Dim.Auto (minimumContentDim: _boxWidth * _cols);
        Height = Dim.Auto (minimumContentDim: _boxHeight * _rows);
        SetContentSize (new (_boxWidth * _cols, _boxHeight * _rows));
    }
}
