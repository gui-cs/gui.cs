﻿//#nullable enable

//using System.ComponentModel;
//using static Unix.Terminal.Delegates;

//namespace Terminal.Gui;

///// <summary>
/////     Provides a visual indicator that content can be scrolled. ScrollBars consist of two buttons, one each for scrolling
/////     forward or backwards, a <see cref="Scroll"/> that can be dragged
/////     to scroll continuously. ScrollBars can be oriented either horizontally or vertically and support the user dragging
/////     and clicking with the mouse to scroll.
///// </summary>
///// <remarks>
///// </remarks>
//public class ScrollBar : View, IOrientation, IDesignable
//{
//    private readonly Scroll _scroll;
//    private readonly Button _decreaseButton;
//    private readonly Button _increaseButton;

//    /// <inheritdoc/>
//    public ScrollBar ()
//    {
//        CanFocus = false;

//        _scroll = new ();
//        _scroll.SliderPositionChanged += ScrollOnSliderPositionChanged;
//        _scroll.ContentPositionChanging += OnScrollOnContentPositionChanging;
//        _scroll.ContentPositionChanged += OnScrollOnContentPositionChanged;
//        _scroll.Scrolled += ScrollOnScrolled;
//        _scroll.SizeChanged += OnScrollOnSizeChanged;

//        _decreaseButton = new ()
//        {
//            CanFocus = false,
//            NoDecorations = true,
//            NoPadding = true,
//            ShadowStyle = ShadowStyle.None,
//            WantContinuousButtonPressed = true
//        };
//        _decreaseButton.Accepting += OnDecreaseButtonOnAccept;

//        _increaseButton = new ()
//        {
//            CanFocus = false,
//            NoDecorations = true,
//            NoPadding = true,
//            ShadowStyle = ShadowStyle.None,
//            WantContinuousButtonPressed = true
//        };
//        _increaseButton.Accepting += OnIncreaseButtonOnAccept;
//        Add (_decreaseButton, _scroll, _increaseButton);

//        _orientationHelper = new (this); // Do not use object initializer!
//        _orientationHelper.Orientation = Orientation.Vertical;
//        _orientationHelper.OrientationChanging += (sender, e) => OrientationChanging?.Invoke (this, e);
//        _orientationHelper.OrientationChanged += (sender, e) => OrientationChanged?.Invoke (this, e);

//        // This sets the width/height etc...
//        OnOrientationChanged (Orientation);

//        return;

//        void OnDecreaseButtonOnAccept (object? s, CommandEventArgs e)
//        {
//            ContentPosition -= Increment;
//            e.Cancel = true;
//        }

//        void OnIncreaseButtonOnAccept (object? s, CommandEventArgs e)
//        {
//            ContentPosition += Increment;
//            e.Cancel = true;
//        }
//    }

//    #region IOrientation members

//    private readonly OrientationHelper _orientationHelper;

//    /// <inheritdoc/>
//    public Orientation Orientation
//    {
//        get => _orientationHelper.Orientation;
//        set => _orientationHelper.Orientation = value;
//    }

//    /// <inheritdoc/>
//    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

//    /// <inheritdoc/>
//    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;

//    /// <inheritdoc/>
//    public void OnOrientationChanged (Orientation newOrientation)
//    {
//        TextDirection = Orientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;
//        TextAlignment = Alignment.Center;
//        VerticalTextAlignment = Alignment.Center;

//        if (Orientation == Orientation.Vertical)
//        {
//            Width = 1;
//            Height = Dim.Fill ();
//        }
//        else
//        {
//            Width = Dim.Fill ();
//            Height = 1;
//        }

//        // Force a layout to ensure _scroll 
//        Layout ();
//        _scroll.Orientation = newOrientation;
//    }

//    #endregion

//    private bool _autoHide = true;

//    /// <summary>
//    ///     Gets or sets whether <see cref="View.Visible"/> will be set to <see langword="false"/> if the dimension of the
//    ///     scroll bar is greater than or equal to <see cref="Size"/>.
//    /// </summary>
//    public bool AutoHide
//    {
//        get => _autoHide;
//        set
//        {
//            if (_autoHide != value)
//            {
//                _autoHide = value;

//                if (!AutoHide)
//                {
//                    Visible = true;
//                }

//                SetNeedsLayout ();
//            }
//        }
//    }

//    /// <inheritdoc/>
//    protected override void OnFrameChanged (in Rectangle frame)
//    {
//        ShowHide ();
//        if (!_viewportDimension.HasValue)
//        {
//            _scroll.ViewportDimension = (Orientation == Orientation.Vertical ? Frame.Height : Frame.Width) - 2;
//        }
//    }

//    private void ShowHide ()
//    {
//        if (!AutoHide || !IsInitialized)
//        {
//            return;
//        }

//        if (Orientation == Orientation.Vertical)
//        {
//            Visible = Frame.Height < Size;
//        }
//        else
//        {
//            Visible = Frame.Width < Size;
//        }
//    }

//    /// <summary>
//    ///     Gets or sets whether the Scroll will show the percentage the slider
//    ///     takes up within the <see cref="Size"/>.
//    /// </summary>
//    public bool ShowPercent
//    {
//        get => _scroll.ShowPercent;
//        set => _scroll.ShowPercent = value;
//    }


//    /// <summary>Get or sets if the view-port is kept in all visible area of this <see cref="ScrollBar"/>.</summary>
//    public bool KeepContentInAllViewport
//    {
//        //get => _scroll.KeepContentInAllViewport;
//        //set => _scroll.KeepContentInAllViewport = value;
//        get;
//        set;
//    }

//    /// <summary>Gets or sets the position of the slider within the ScrollBar's Viewport.</summary>
//    /// <returns>The position.</returns>
//    public int GetSliderPosition () => _scroll.GetSliderPosition ();

//    private void ScrollOnSliderPositionChanged (object? sender, EventArgs<int> e) { SliderPositionChanged?.Invoke (this, e); }

//    /// <summary>Raised when the position of the slider has changed.</summary>
//    public event EventHandler<EventArgs<int>>? SliderPositionChanged;

//    private void ScrollOnScrolled (object? sender, EventArgs<int> e) { Scrolled?.Invoke (this, e); }

//    /// <summary>Raised when the position of the slider has changed.</summary>
//    public event EventHandler<EventArgs<int>>? Scrolled;

//    /// <summary>
//    ///     Gets or sets the size of the Scroll. This is the total size of the content that can be scrolled through.
//    /// </summary>
//    public int Size
//    {
//        get => _scroll.Size;
//        set => _scroll.Size = value;
//    }

//    private int? _viewportDimension;

//    /// <summary>
//    ///     Gets or sets the size of the viewport into the content being scrolled, bounded by <see cref="Size"/>.
//    /// </summary>
//    /// <remarks>
//    ///     If not explicitly set, will be the appropriate dimension of the Scroll's Frame.
//    /// </remarks>
//    public int ViewportDimension
//    {
//        get
//        {
//            if (_viewportDimension.HasValue)
//            {
//                return _viewportDimension.Value;
//            }
//            return Orientation == Orientation.Vertical ? Frame.Height : Frame.Width;

//        }
//        set
//        {
//            _scroll.ViewportDimension = value - 2;
//            _viewportDimension = value;
//        }
//    }

//    /// <summary>
//    ///     Gets or sets the position of the ScrollSlider within the range of 0...<see cref="Size"/>.
//    /// </summary>
//    public int ContentPosition
//    {
//        get => _scroll.ContentPosition;
//        set => _scroll.ContentPosition = value;
//    }

//    private void OnScrollOnContentPositionChanging (object? sender, CancelEventArgs<int> e) { ContentPositionChanging?.Invoke (this, e); }
//    private void OnScrollOnContentPositionChanged (object? sender, EventArgs<int> e) { ContentPositionChanged?.Invoke (this, e); }

//    /// <summary>
//    ///     Raised when the <see cref="ContentPosition"/> is changing. Set <see cref="CancelEventArgs.Cancel"/> to
//    ///     <see langword="true"/> to prevent the position from being changed.
//    /// </summary>
//    public event EventHandler<CancelEventArgs<int>>? ContentPositionChanging;

//    /// <summary>Raised when the <see cref="ContentPosition"/> has changed.</summary>
//    public event EventHandler<EventArgs<int>>? ContentPositionChanged;

//    /// <summary>Raised when <see cref="Size"/> has changed.</summary>
//    public event EventHandler<EventArgs<int>>? SizeChanged;

//    private void OnScrollOnSizeChanged (object? sender, EventArgs<int> e)
//    {
//        ShowHide ();
//        SizeChanged?.Invoke (this, e);
//    }

//    /// <summary>
//    ///     Gets or sets the amount each click of the increment/decrement buttons and each
//    ///     mouse wheel event will incremenet/decrement the <see cref="ContentPosition"/>.
//    /// </summary>
//    /// <remarks>
//    ///     The default is 1.
//    /// </remarks>
//    public int Increment { get => _scroll.Increment; set => _scroll.Increment = value; }

//    /// <inheritdoc/>
//    protected override void OnSubviewLayout (LayoutEventArgs args) { PositionSubviews (); }

//    private void PositionSubviews ()
//    {
//        if (Orientation == Orientation.Vertical)
//        {
//            _decreaseButton.Y = 0;
//            _decreaseButton.X = 0;
//            _decreaseButton.Width = Dim.Fill ();
//            _decreaseButton.Height = 1;
//            _decreaseButton.Title = Glyphs.UpArrow.ToString ();
//            _increaseButton.Y = Pos.Bottom (_scroll);
//            _increaseButton.X = 0;
//            _increaseButton.Width = Dim.Fill ();
//            _increaseButton.Height = 1;
//            _increaseButton.Title = Glyphs.DownArrow.ToString ();
//            _scroll.X = 0;
//            _scroll.Y = Pos.Bottom (_decreaseButton);
//            _scroll.Height = Dim.Fill (1);
//            _scroll.Width = Dim.Fill ();
//        }
//        else
//        {
//            _decreaseButton.Y = 0;
//            _decreaseButton.X = 0;
//            _decreaseButton.Width = 1;
//            _decreaseButton.Height = Dim.Fill ();
//            _decreaseButton.Title = Glyphs.LeftArrow.ToString ();
//            _increaseButton.Y = 0;
//            _increaseButton.X = Pos.Right (_scroll);
//            _increaseButton.Width = 1;
//            _increaseButton.Height = Dim.Fill ();
//            _increaseButton.Title = Glyphs.RightArrow.ToString ();
//            _scroll.Y = 0;
//            _scroll.X = Pos.Right (_decreaseButton);
//            _scroll.Width = Dim.Fill (1);
//            _scroll.Height = Dim.Fill ();
//        }
//    }

//    /// <inheritdoc/>
//    public bool EnableForDesign ()
//    {
//        OrientationChanged += (sender, args) =>
//                              {
//                                  if (args.CurrentValue == Orientation.Vertical)
//                                  {
//                                      Width = 1;
//                                      Height = Dim.Fill ();
//                                  }
//                                  else
//                                  {
//                                      Width = Dim.Fill ();
//                                      Height = 1;
//                                  }
//                              };

//        Width = 1;
//        Height = Dim.Fill ();
//        Size = 200;
//        //ShowPercent = true;
//        return true;
//    }
//}
