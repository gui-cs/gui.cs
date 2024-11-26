#nullable enable

using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
/// </summary>
/// <remarks>
/// </remarks>
public class Slider : View, IOrientation, IDesignable
{
    private readonly ScrollSlider _slider;

    /// <inheritdoc/>
    public Slider ()
    {
        // Set the default width and height based on the orientation - fill Viewport
        Width = Dim.Auto (
                          DimAutoStyle.Content,
                          Dim.Func (() => Orientation == Orientation.Vertical ? 2 : SuperView?.Viewport.Width ?? 0));

        Height = Dim.Auto (
                           DimAutoStyle.Content,
                           Dim.Func (() => Orientation == Orientation.Vertical ? SuperView?.Viewport.Height ?? 0 : 1));


        _slider = new ()
        {
            Segment = $" {Glyphs.RightArrow}",
        };
        _slider.Scrolled += SliderOnScroll;
        _slider.PositionChanged += SliderOnPositionChanged;
        _slider.Size = 1;
        _slider.ColorScheme = Colors.ColorSchemes ["Error"];

        Add (_slider);

        CanFocus = true;

        _orientationHelper = new (this); // Do not use object initializer!
        _orientationHelper.Orientation = Orientation.Vertical;

        // This sets the width/height etc...
        OnOrientationChanged (Orientation);

        return;
    }
    
    private void PositionSubviews ()
    {
        if (Orientation == Orientation.Vertical)
        {
 
            _slider.X = 0;
            _slider.Y = 0;
            _slider.Width = Dim.Fill ();
}
        else
        {
            _slider.Y = 0;
            _slider.X = 0;
            _slider.Height = Dim.Fill ();
        }
    }

    #region IOrientation members

    private readonly OrientationHelper _orientationHelper;

    /// <inheritdoc/>
    public Orientation Orientation
    {
        get => _orientationHelper.Orientation;
        set => _orientationHelper.Orientation = value;
    }

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;

    /// <inheritdoc/>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        TextDirection = Orientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;
        TextAlignment = Alignment.Center;
        VerticalTextAlignment = Alignment.Center;
        _slider.Orientation = newOrientation;
        PositionSubviews ();

        OrientationChanged?.Invoke (this, new (newOrientation));
    }

    #endregion

    /// <summary>
    ///     Gets or sets the amount each mouse wheel event, or click on the increment/decrement buttons, will
    ///     incremenet/decrement the <see cref="Position"/>.
    /// </summary>
    /// <remarks>
    ///     The default is 1.
    /// </remarks>
    public int Increment { get; set; } = 1;


    #region Position

    private int _position;

    /// <summary>
    ///     Gets or sets the position of the slider relative to <see cref="ScrollableContentSize"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The content position is clamped to 0 and <see cref="ScrollableContentSize"/> minus
    ///         <see cref="VisibleContentSize"/>.
    ///     </para>
    ///     <para>
    ///         Setting will result in the <see cref="PositionChanging"/> and <see cref="PositionChanged"/>
    ///         events being raised.
    ///     </para>
    /// </remarks>
    public int Position
    {
        get => _position;
        set
        {
            if (value == _position || !Visible)
            {
                return;
            }

            // Clamp the value between 0 and Size - VisibleContentSize

            if (OnPositionChanging (_position, value))
            {
                return;
            }

            CancelEventArgs<int> args = new (ref _position, ref value);
            PositionChanging?.Invoke (this, args);

            if (args.Cancel)
            {
                return;
            }

            int distance = value - _position;

            if (_position == value)
            {
                return;
            }

            _position = value;

            _sliderPosition = _position;

            if (_slider.Position != _sliderPosition)
            {
                _slider.Position = _sliderPosition.Value;
            }

            OnPositionChanged (_position);
            PositionChanged?.Invoke (this, new (in _position));
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     Called when <see cref="Position"/> is changing. Return true to cancel the change.
    /// </summary>
    protected virtual bool OnPositionChanging (int currentPos, int newPos) { return false; }

    /// <summary>
    ///     Raised when the <see cref="Position"/> is changing. Set <see cref="CancelEventArgs.Cancel"/> to
    ///     <see langword="true"/> to prevent the position from being changed.
    /// </summary>
    public event EventHandler<CancelEventArgs<int>>? PositionChanging;

    /// <summary>Called when <see cref="Position"/> has changed.</summary>
    protected virtual void OnPositionChanged (int position) { }

    /// <summary>Raised when the <see cref="Position"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? PositionChanged;


    #endregion Position

    #region Slider Management

    private int? _sliderPosition;

    private void SliderOnPositionChanged (object? sender, EventArgs<int> e)
    {
        RaiseSliderPositionChangeEvents (_sliderPosition, e.CurrentValue);
    }

    private void SliderOnScroll (object? sender, EventArgs<int> e)
    {
        if (_slider.VisibleContentSize == 0)
        {
            return;
        }

        int calculatedSliderPos = _position;

        if (calculatedSliderPos == _sliderPosition)
        {
            return;
        }

        int sliderScrolledAmount = e.CurrentValue;
        int calculatedPosition = calculatedSliderPos + sliderScrolledAmount;

        Position = calculatedPosition;
    }

    private void RaiseSliderPositionChangeEvents (int? currentSliderPosition, int newSliderPosition)
    {
        if (currentSliderPosition == newSliderPosition)
        {
            return;
        }

        _sliderPosition = newSliderPosition;

        OnSliderPositionChanged (newSliderPosition);
        SliderPositionChanged?.Invoke (this, new (in newSliderPosition));
    }

    /// <summary>Called when the slider position has changed.</summary>
    protected virtual void OnSliderPositionChanged (int position) { }

    /// <summary>Raised when the slider position has changed.</summary>
    public event EventHandler<EventArgs<int>>? SliderPositionChanged;

    #endregion Slider Management

    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        //if (Orientation == Orientation.Vertical)
        //{
        //    FillRect (Viewport with { Y = Viewport.Y, Height = Viewport.Height }, Glyphs.Stipple);
        //}
        //else
        //{
        //    FillRect (Viewport with { X = Viewport.X, Width = Viewport.Width }, Glyphs.Stipple);
        //}

        //SetNeedsDraw ();

        return true;
    }

    // TODO: Change this to work OnMouseEvent with continuouse press and grab so it's continous.
    /// <inheritdoc/>
    protected override bool OnMouseClick (MouseEventArgs args)
    {
        // Check if the mouse click is a single click
        if (!args.IsSingleClicked)
        {
            return false;
        }

        int sliderCenter;
        int distanceFromCenter;

        if (Orientation == Orientation.Vertical)
        {
            sliderCenter = 1 + _slider.Frame.Y + _slider.Frame.Height / 2;
            distanceFromCenter = args.Position.Y - sliderCenter;
        }
        else
        {
            sliderCenter = 1 + _slider.Frame.X + _slider.Frame.Width / 2;
            distanceFromCenter = args.Position.X - sliderCenter;
        }

        int jump = Increment;

        // Adjust the content position based on the distance
        if (distanceFromCenter < 0)
        {
            Position = Math.Max (0, Position - jump);
        }
        else
        {
            Position = Math.Min (Viewport.Height - _slider.Size, Position + jump);
        }

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        if (SuperView is null)
        {
            return false;
        }

        if (!mouseEvent.IsWheel)
        {
            return false;
        }

        if (Orientation == Orientation.Vertical)
        {
            if (mouseEvent.Flags.HasFlag (MouseFlags.WheeledDown))
            {
                Position += Increment;
            }

            if (mouseEvent.Flags.HasFlag (MouseFlags.WheeledUp))
            {
                Position -= Increment;
            }
        }
        else
        {
            if (mouseEvent.Flags.HasFlag (MouseFlags.WheeledRight))
            {
                Position += Increment;
            }

            if (mouseEvent.Flags.HasFlag (MouseFlags.WheeledLeft))
            {
                Position -= Increment;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        OrientationChanged += (sender, args) =>
                              {
                                  if (args.CurrentValue == Orientation.Vertical)
                                  {
                                      Width = 2;
                                      Height = Dim.Fill ();
                                  }
                                  else
                                  {
                                      Width = Dim.Fill ();
                                      Height = 2;
                                  }
                              };

        Width = 2;
        Height = Dim.Fill();

        return true;
    }
}
