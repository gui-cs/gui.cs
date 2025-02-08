#nullable enable
using static Unix.Terminal.Curses;

namespace Terminal.Gui;

public static partial class Application // Popover handling
{
    private static PopoverHost? _popoverHost;

    /// <summary>Gets or sets the Application Popover View.</summary>
    /// <remarks>
    ///     <para>
    ///         To show or hide the Popover, set it's <see cref="View.Visible"/> property.
    ///     </para>
    /// </remarks>
    public static PopoverHost? PopoverHost
    {
        get
        {
            if (_popoverHost is null)
            {
                _popoverHost = new PopoverHost ();
            }
            return _popoverHost;
        }
        internal set => _popoverHost = value;
        //{
        //    if (_popoverHost == value)
        //    {
        //        return;
        //    }

        //    if (_popoverHost is { })
        //    {
        //        _popoverHost.Visible = false;
        //        _popoverHost.VisibleChanging -= PopoverVisibleChanging;
        //    }

        //    _popoverHost = value;

        //    if (_popoverHost is { })
        //    {
        //        if (!_popoverHost.IsInitialized)
        //        {
        //            _popoverHost.BeginInit ();
        //            _popoverHost.EndInit ();
        //        }

        //        _popoverHost.Arrangement |= ViewArrangement.Overlapped;

        //        if (_popoverHost.ColorScheme is null)
        //        {
        //            _popoverHost.ColorScheme = Top?.ColorScheme;
        //        }

        //        _popoverHost.SetRelativeLayout (Screen.Size);

        //        _popoverHost.VisibleChanging += PopoverVisibleChanging;
        //    }
        //}
    }

    private static void PopoverVisibleChanging (object? sender, CancelEventArgs<bool> e)
    {
        if (PopoverHost is null)
        {
            return;
        }

        if (e.NewValue)
        {
            PopoverHost.Arrangement |= ViewArrangement.Overlapped;

            PopoverHost.ColorScheme ??= Top?.ColorScheme;

            if (PopoverHost.NeedsLayout)
            {
                PopoverHost.SetRelativeLayout (Screen.Size);
            }

            View.GetLocationEnsuringFullVisibility (
                                                    PopoverHost,
                                                    PopoverHost.Frame.X,
                                                    PopoverHost.Frame.Y,
                                                    out int nx,
                                                    out int ny);

            PopoverHost.X = nx;
            PopoverHost.Y = ny;

            PopoverHost.SetRelativeLayout (Screen.Size);

            if (Top is { })
            {
                Top.HasFocus = false;
            }

            PopoverHost?.SetFocus ();
        }
    }
}

public class PopoverHost : View
{
    public PopoverHost()
    {
        Id = "popoverHost";
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        Visible = false;
    }

    /// <inheritdoc />
    protected override bool OnClearingViewport () { return true; }

    /// <inheritdoc />
    protected override bool OnVisibleChanging ()
    {
        if (!Visible)
        {
            ColorScheme ??= Application.Top?.ColorScheme;
            Frame = Application.Screen;

            SetRelativeLayout (Application.Screen.Size);
        }

        return false;
    }
}
