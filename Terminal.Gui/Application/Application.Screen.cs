#nullable enable
namespace Terminal.Gui;

public static partial class Application // Screen related stuff
{
    private static Rectangle? _screen;

    /// <summary>
    ///     Gets or sets the size of the screen. By default, this is the size of the screen as reported by the <see cref="ConsoleDriver"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     If the <see cref="ConsoleDriver"/> has not been initialized, this will return a default size of 2048x2048; useful for unit tests.
    /// </para>
    /// </remarks>
    public static Rectangle Screen
    {
        get
        {
            if (_screen == null)
            {
                _screen = Driver?.Screen ?? new (new (0, 0), new (2048, 2048));
            }
            return _screen.Value;
        }
        set
        {
            if (value is {} && (value.X != 0 || value.Y != 0))
            {
                throw new NotImplementedException ($"Screen locations other than 0, 0 are not yet supported");
            }
            _screen = value;
        }
    }

    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    /// <remarks>
    ///     Event handlers can set <see cref="SizeChangedEventArgs.Cancel"/> to <see langword="true"/> to prevent
    ///     <see cref="Application"/> from changing it's size to match the new terminal size.
    /// </remarks>
    public static event EventHandler<SizeChangedEventArgs>? SizeChanging;

    /// <summary>
    ///     Called when the application's size changes. Sets the size of all <see cref="Toplevel"/>s and fires the
    ///     <see cref="SizeChanging"/> event.
    /// </summary>
    /// <param name="args">The new size.</param>
    /// <returns><see lanword="true"/>if the size was changed.</returns>
    public static bool OnSizeChanging (SizeChangedEventArgs args)
    {
        SizeChanging?.Invoke (null, args);

        if (args.Cancel || args.Size is null)
        {
            return false;
        }

        Screen = new (Point.Empty, args.Size.Value);

        foreach (Toplevel t in TopLevels)
        {
            t.OnSizeChanging (new (args.Size));
            t.SetNeedsLayout ();
        }

        LayoutAndDraw ();

        return true;
    }
}
