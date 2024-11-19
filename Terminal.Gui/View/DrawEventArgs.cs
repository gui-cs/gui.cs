using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>Event args for draw events</summary>
public class DrawEventArgs : CancelEventArgs
{
    /// <summary>Creates a new instance of the <see cref="DrawEventArgs"/> class.</summary>
    /// <param name="newViewport">
    ///     The Content-relative rectangle describing the new visible viewport into the
    ///     <see cref="View"/>.
    /// </param>
    /// <param name="oldViewport">
    ///     The Content-relative rectangle describing the old visible viewport into the
    ///     <see cref="View"/>.
    /// </param>
    public DrawEventArgs (Rectangle newViewport, Rectangle oldViewport)
    {
        NewViewport = newViewport;
        OldViewport = oldViewport;
    }

    /// <summary>Gets the Content-relative rectangle describing the old visible viewport into the <see cref="View"/>.</summary>
    public Rectangle OldViewport { get; }

    /// <summary>Gets the Content-relative rectangle describing the currently visible viewport into the <see cref="View"/>.</summary>
    public Rectangle NewViewport { get; }
}
