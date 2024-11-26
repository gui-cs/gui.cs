namespace Terminal.Gui;

/// <summary>
///     Draws a single line using the <see cref="LineStyle"/> specified by <see cref="View.BorderStyle"/>.
/// </summary>
/// <remarks>
/// </remarks>
public class Line : View, IOrientation
{
    private readonly OrientationHelper _orientationHelper;

    /// <summary>Constructs a Line object.</summary>
    public Line ()
    {
        CanFocus = false;

        base.SuperViewRendersLineCanvas = true;

        _orientationHelper = new (this);
        _orientationHelper.Orientation = Orientation.Horizontal;
        OnOrientationChanged (Orientation);
    }

    private LineStyle? _lineStyle;

    /// <summary>
    ///     Gets or sets the style of the Line. The default is the <see cref="Border.BorderStyle"/> of the SuperView.
    /// </summary>
    public LineStyle LineStyle
    {
        get
        {
            if (_lineStyle.HasValue)
            {
                return _lineStyle.Value;
            }

            return SuperView?.BorderStyle ?? LineStyle.Single;
        }
        set => _lineStyle = value;
    }

    #region IOrientation members
    /// <summary>
    ///     The direction of the line.  If you change this you will need to manually update the Width/Height of the
    ///     control to cover a relevant area based on the new direction.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientationHelper.Orientation;
        set => _orientationHelper.Orientation = value;
    }

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>> OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>> OrientationChanged;

    /// <summary>Called when <see cref="Orientation"/> has changed.</summary>
    /// <param name="newOrientation"></param>
    public void OnOrientationChanged (Orientation newOrientation)
    {

        switch (newOrientation)
        {
            case Orientation.Horizontal:
                Height = 1;
                Width = Dim.Fill ();

                break;
            case Orientation.Vertical:
                Width = 1;
                Height = Dim.Fill ();

                break;

        }
    }
    #endregion

    /// <inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        Point pos = ViewportToScreen (Viewport).Location;
        int length = Orientation == Orientation.Horizontal ? Frame.Width : Frame.Height;

        if (length == 0)
        {
            return true;
        }
        LineCanvas?.AddLine (
                    pos,
                    length,
                    Orientation,
                    LineStyle);

        //SuperView?.SetNeedsDraw ();
        return true;
    }
}
