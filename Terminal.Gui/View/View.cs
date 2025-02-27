#nullable enable
using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui;

#region API Docs

/// <summary>
///     View is the base class all visible elements. View can render itself and
///     contains zero or more nested views, called SubViews. View provides basic functionality for layout, arrangement, and
///     drawing. In addition, View provides keyboard and mouse event handling. See the
///     <see href="../docs/view.html">
///         View
///         Deep Dive
///     </see>
///     for more.
/// </summary>
/// <remarks>
///     <list type="table">
///         <listheader>
///             <term>Term</term><description>Definition</description>
///         </listheader>
///         <item>
///             <term>SubView</term>
///             <description>
///                 A View that is contained in another view and will be rendered as part of the containing view's
///                 ContentArea. SubViews are added to another view via the <see cref="View.Add(View)"/>` method. A View
///                 may only be a SubView of a single View.
///             </description>
///         </item>
///         <item>
///             <term>SuperView</term><description>The View that is a container for SubViews.</description>
///         </item>
///         <item>
///             <term>Input</term>
///             <description>
///                 <para>
///                     Key Bindings is the preferred way of handling keyboard input in View implementations.
///                     The View calls
///                     <see cref="AddCommand(Terminal.Gui.Command,Terminal.Gui.View.CommandImplementation)"/> to declare
///                     it supports a particular command and then uses <see cref="KeyBindings"/>
///                     to indicate which key presses will invoke the command.
///                 </para>
///                 <para>
///                     Mouse Bindings is the preferred way of handling mouse input in View implementations. The View calls
///                     <see cref="AddCommand(Terminal.Gui.Command,Terminal.Gui.View.CommandImplementation)"/> to declare
///                     it supports a
///                     particular command and then uses <see cref="MouseBindings"/> to indicate which mouse events will
///                     invoke the command.
///                 </para>
///                 <para>
///                     See the
///                     <see href="../docs/mouse.html">
///                         Mouse
///                         Deep Dive
///                     </see>
///                     and
///                     <see href="../docs/keyboard.html">
///                         Keyboard
///                         Deep Dive
///                     </see>
///                     for more information.
///                 </para>
///             </description>
///         </item>
///         <item>
///             <term>Layout</term>
///             <description>
///                 <para>
///                     Terminal.Gui provides a rich system for how View objects are laid out relative to each other. The
///                     layout system also defines how coordinates are specified.
///                 </para>
///                 <para>
///                     The <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties are
///                     <see cref="Dim"/> and <see cref="Pos"/> objects that dynamically update the position of a view. The
///                     X and Y properties are of type <see cref="Pos"/> and you can use either absolute positions,
///                     percentages, or anchor points. The Width and Height properties are of type <see cref="Dim"/> and
///                     can use absolute position, percentages, and anchors. These are useful as they will take care of
///                     repositioning views when view's adornments are resized or if the terminal size changes.
///                 </para>
///                 <para>
///                     See the
///                     <see href="../docs/layout.html">
///                         Layout
///                         Deep Dive
///                     </see>
///                     for more information.
///                 </para>
///             </description>
///         </item>
///         <item>
///             <term>Arrangement</term>
///             <description>
///                 <para>
///                     Complimenting the Layout system, <see cref="Arrangement"/> controls how the user can use the mouse
///                     and keyboard to arrange views and enables either Tiled or Overlapped layouts.
///                 </para>
///                 <para>
///                     See the
///                     <see href="../docs/arrangement.html">
///                         Arrangement
///                         Deep Dive
///                     </see>
///                     for more information.
///                 </para>
///             </description>
///         </item>
///         <item>
///             <term>Drawing</term>
///             <description>
///                 <para>
///                     Apps draw using the <see cref="Move"/> and <see cref="AddRune(Rune)"/> APIs. Move selects the
///                     column and row of the Cell and AddRune places
///                     the specified glyph in that cell using the <see cref="Attribute"/> that was most recently set via
///                     <see cref="SetAttribute"/>.
///                     The ConsoleDriver caches all changed Cells and efficiently outputs them to the terminal each
///                     iteration of the Application. In other words, Terminal.Gui uses deferred rendering.
///                 </para>
///                 <para>
///                     The View draw APIs all take coordinates specified in Viewport-Relative coordinates. That is,
///                     <c>(0,0)</c> is the top-left cell visible to the user.
///                 </para>
///                 <para>
///                     If a View need to redraw because something changed within it's Content Area it can call
///                     <see cref="SetNeedsDraw()"/>.
///                 </para>
///                 <para>
///                     Terminal.Gui supports the full range of Unicode/wide characters.
///                     This includes emoji, CJK characters, and other wide characters. For Unicode characters that require
///                     more than one cell,
///                     AddRune and the ConsoleDriver automatically manage the cells. Extension methods to Rune are
///                     provided to determine if a Rune is a wide character and to get the width of a Rune.
///                 </para>
///                 <para>
///                     The <see cref="ColorScheme"/> provides consistent colors across all views. The
///                     <see cref="ColorScheme"/> is inherited from the <see cref="SuperView"/>. The
///                     <see cref="ColorScheme"/> is used to set the <see cref="Attribute"/> for drawing.
///                 </para>
///                 The <see cref="Color"/> class represents a color. It provides automatic mapping between the legacy
///                 4-bit (16-color) system and 24-bit colors. It contains properties for the red, green, and blue
///                 components of the color.
///                 The Color class also contains a static property for each of the 16 ANSI colors. Use
///                 <see cref="SetAttribute"/> to change the colors used when drawing.</para>
///                 <para>
///                 </para>
///                 <para>
///                     Clipping enables better performance by ensuring on regions of the terminal that need to be drawn
///                     actually get drawn by the ConsoleDriver. Terminal.Gui supports non-rectangular clip regions with
///                     <see cref="Region"/>.
///                     There is an <see cref="Application"/>-managed clip region. Developers cannot change this directly,
///                     but can use <see cref="ClipFrame"/>, <see cref="ClipViewport"/>, and <see cref="SetClip"/> to
///                     modify the clip region.
///                 </para>
///                 <para>
///                     <see cref="LineCanvas"/> provides auto join, a smart TUI drawing system that automatically selects
///                     the correct line/box drawing glyphs for intersections making drawing complex shapes easy.
///                 </para>
///                 <para>
///                     A set of static properties are provided for the common glyphs used in TUI apps. See
///                     <see cref="Glyphs"/>.
///                 </para>
///                 <para>
///                     See the
///                     <see href="../docs/drawing.html">
///                         Drawing
///                         Deep Dive
///                     </see>
///                     for more information.
///                 </para>
///             </description>
///         </item>
///         <item>
///             <term>Text</term>
///             <description>
///                 <para>
///                     A rich text formatting engine is provided in <see cref="TextFormatter"/>. TextFormatter provides
///                     methods for formatting text with horizontal and vertical alignment, word wrapping, and hotkeys.
///                 </para>
///                 <para>
///                     See the
///                     <see href="../docs/navigation.html">
///                         Navigation
///                         Deep Dive
///                     </see>
///                     for more information.
///                 </para>
///             </description>
///         </item>
///         <item>
///             <term>Navigation</term>
///             <description>
///                 <para>
///                     Navigation refers to the user experience for moving focus between views in the application
///                     view-hierarchy. Focus is a concept that is used to describe which View is currently receiving user
///                     input. Only
///                     Views that are
///                     <see cref="Enabled"/>, <see cref="Visible"/>, and <see cref="CanFocus"/> will receive focus. NOTE:
///                     <see cref="CanFocus"/> is <see langword="false"/> by default.
///                 </para>
///                 <para>
///                     Views that are focusable should override <see cref="PositionCursor"/> to make sure that the cursor
///                     is
///                     placed in a location that makes sense. Some terminals do not have a way of hiding the cursor, so it
///                     can be
///                     distracting to have the cursor left at the last focused view. So views should make sure that they
///                     place the
///                     cursor in a visually sensible place. The default implementation of <see cref="PositionCursor"/>
///                     will place the
///                     cursor at either the hotkey (if defined) or <c>0,0</c>.
///                 </para>
///                 <para>
///                     See the
///                     <see href="../docs/navigation.html">
///                         Navigation
///                         Deep Dive
///                     </see>
///                     for more information.
///                 </para>
///             </description>
///         </item>
///         <item>
///             <term>Scrolling</term>
///             <description>
///                 <para>
///                     The ability to scroll content is built into View. The <see cref="Viewport"/> represents the
///                     scrollable "viewport" into the View's Content Area (which is defined by the return value of
///                     <see cref="GetContentSize"/>).
///                 </para>
///                 <para>
///                     Terminal.Gui also provides the ability show a visual scroll bar that responds to mouse input. This
///                     ability is not enabled by default given how precious TUI screen real estate is.
///                     Use <see cref="VerticalScrollBar"/> and <see cref="HorizontalScrollBar"/> to enable this feature.
///                 </para>
///                 <para>
///                     Use <see cref="ViewportSettings"/> to adjust the behavior of scrolling.
///                 </para>
///                 <para>
///                     See the
///                     <see href="../docs/scrolling.html">
///                         Scrolling
///                         Deep Dive
///                     </see>
///                     for more information.
///                 </para>
///             </description>
///         </item>
///     </list>
///     <para>
///         Views can opt in to more sophisticated initialization by implementing overrides to
///         <see cref="ISupportInitialize.BeginInit"/> and <see cref="ISupportInitialize.EndInit"/> which will be called
///         when the view is added to a <see cref="SuperView"/>.
///     </para>
///     <para>
///         If first-run-only initialization is preferred, overrides to <see cref="ISupportInitializeNotification"/> can
///         be implemented, in which case the <see cref="ISupportInitialize"/> methods will only be called if
///         <see cref="ISupportInitializeNotification.IsInitialized"/> is <see langword="false"/>. This allows proper
///         <see cref="View"/> inheritance hierarchies to override base class layout code optimally by doing so only on
///         first run, instead of on every run.
///     </para>
///     <para>See <see href="../docs/keyboard.md"> for an overview of View keyboard handling.</see></para>
/// </remarks>

#endregion API Docs

public partial class View : IDisposable, ISupportInitializeNotification
{
    #region Constructors and Initialization

    /// <summary>Gets or sets arbitrary data for the view.</summary>
    /// <remarks>This property is not used internally.</remarks>
    public object? Data { get; set; }

    /// <summary>Gets or sets an identifier for the view;</summary>
    /// <value>The identifier.</value>
    /// <remarks>The id should be unique across all Views that share a SuperView.</remarks>
    public string Id { get; set; } = "";

    /// <summary>
    ///     Points to the current driver in use by the view, it is a convenience property for simplifying the development
    ///     of new views.
    /// </summary>
    public static IConsoleDriver? Driver => Application.Driver;

    /// <summary>Initializes a new instance of <see cref="View"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         Use <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties to dynamically
    ///         control the size and location of the view.
    ///     </para>
    /// </remarks>
    public View ()
    {
#if DEBUG_IDISPOSABLE
        Instances.Add (this);
#endif

        SetupAdornments ();

        SetupCommands ();

        SetupKeyboard ();

        SetupMouse ();

        SetupText ();

        SetupScrollBars ();
    }

    /// <summary>
    ///     Raised once when the <see cref="View"/> is being initialized for the first time. Allows
    ///     configurations and assignments to be performed before the <see cref="View"/> being shown.
    ///     View implements <see cref="ISupportInitializeNotification"/> to allow for more sophisticated initialization.
    /// </summary>
    public event EventHandler? Initialized;

    /// <summary>
    ///     Get or sets if  the <see cref="View"/> has been initialized (via <see cref="ISupportInitialize.BeginInit"/>
    ///     and <see cref="ISupportInitialize.EndInit"/>).
    /// </summary>
    /// <para>
    ///     If first-run-only initialization is preferred, overrides to
    ///     <see cref="ISupportInitializeNotification.IsInitialized"/> can be implemented, in which case the
    ///     <see cref="ISupportInitialize"/> methods will only be called if
    ///     <see cref="ISupportInitializeNotification.IsInitialized"/> is <see langword="false"/>. This allows proper
    ///     <see cref="View"/> inheritance hierarchies to override base class layout code optimally by doing so only on first
    ///     run, instead of on every run.
    /// </para>
    public virtual bool IsInitialized { get; set; }

    /// <summary>Signals the View that initialization is starting. See <see cref="ISupportInitialize"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         Views can opt-in to more sophisticated initialization by implementing overrides to
    ///         <see cref="ISupportInitialize.BeginInit"/> and <see cref="ISupportInitialize.EndInit"/> which will be called
    ///         when the <see cref="SuperView"/> is initialized.
    ///     </para>
    ///     <para>
    ///         If first-run-only initialization is preferred, overrides to <see cref="ISupportInitializeNotification"/> can
    ///         be implemented too, in which case the <see cref="ISupportInitialize"/> methods will only be called if
    ///         <see cref="ISupportInitializeNotification.IsInitialized"/> is <see langword="false"/>. This allows proper
    ///         <see cref="View"/> inheritance hierarchies to override base class layout code optimally by doing so only on
    ///         first run, instead of on every run.
    ///     </para>
    /// </remarks>
    public virtual void BeginInit ()
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException ("The view is already initialized.");
        }
#if AUTO_CANFOCUS
        _oldCanFocus = CanFocus;
        _oldTabIndex = _tabIndex;
#endif

        BeginInitAdornments ();

        if (_subviews?.Count > 0)
        {
            foreach (View view in _subviews)
            {
                if (!view.IsInitialized)
                {
                    view.BeginInit ();
                }
            }
        }
    }

    // TODO: Implement logic that allows EndInit to throw if BeginInit has not been called
    // TODO: See EndInit_Called_Without_BeginInit_Throws test.

    /// <summary>Signals the View that initialization is ending. See <see cref="ISupportInitialize"/>.</summary>
    /// <remarks>
    ///     <para>Initializes all Subviews and Invokes the <see cref="Initialized"/> event.</para>
    /// </remarks>
    public virtual void EndInit ()
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException ("The view is already initialized.");
        }

        IsInitialized = true;

        EndInitAdornments ();

        // TODO: Move these into ViewText.cs as EndInit_Text() to consolidate.
        // TODO: Verify UpdateTextDirection really needs to be called here.
        // These calls were moved from BeginInit as they access Viewport which is indeterminate until EndInit is called.
        UpdateTextDirection (TextDirection);
        UpdateTextFormatterText ();

        if (_subviews is { })
        {
            foreach (View view in _subviews)
            {
                if (!view.IsInitialized)
                {
                    view.EndInit ();
                }
            }
        }

        // TODO: Figure out how to move this out of here and just depend on LayoutNeeded in Mainloop
        Layout (); // the EventLog in AllViewsTester fails to layout correctly if this is not here (convoluted Dim.Fill(Func)).
        SetNeedsLayout ();

        Initialized?.Invoke (this, EventArgs.Empty);
    }

    #endregion Constructors and Initialization

    #region Visibility

    private bool _enabled = true;

    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> can respond to user interaction.</summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;

            if (!_enabled && HasFocus)
            {
                HasFocus = false;
            }

            if (_enabled
                && CanFocus
                && Visible
                && !HasFocus
                && SuperView is null or { HasFocus: true, Visible: true, Enabled: true, Focused: null })
            {
                SetFocus ();
            }

            OnEnabledChanged ();
            SetNeedsDraw ();

            if (Border is { })
            {
                Border.Enabled = _enabled;
            }

            if (_subviews is null)
            {
                return;
            }

            foreach (View view in _subviews)
            {
                view.Enabled = Enabled;
            }
        }
    }

    /// <summary>Raised when the <see cref="Enabled"/> value is being changed.</summary>
    public event EventHandler? EnabledChanged;

    // TODO: Change this event to match the standard TG event model.
    /// <summary>Invoked when the <see cref="Enabled"/> property from a view is changed.</summary>
    public virtual void OnEnabledChanged () { EnabledChanged?.Invoke (this, EventArgs.Empty); }

    private bool _visible = true;

    // TODO: Remove virtual once Menu/MenuBar are removed. MenuBar is the only override.
    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> is visible.</summary>
    public virtual bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value)
            {
                return;
            }

            if (OnVisibleChanging ())
            {
                return;
            }

            CancelEventArgs<bool> args = new (in _visible, ref value);
            VisibleChanging?.Invoke (this, args);

            if (args.Cancel)
            {
                return;
            }

            _visible = value;

            if (!_visible)
            {
                if (HasFocus)
                {
                    HasFocus = false;
                }
            }

            if (_visible
                && CanFocus
                && Enabled
                && !HasFocus
                && SuperView is null or { HasFocus: true, Visible: true, Enabled: true, Focused: null })
            {
                SetFocus ();
            }

            OnVisibleChanged ();
            VisibleChanged?.Invoke (this, EventArgs.Empty);

            SetNeedsLayout ();
            SuperView?.SetNeedsLayout ();
            SetNeedsDraw ();

            if (SuperView is { })
            {
                SuperView?.SetNeedsDraw ();
            }
            else
            {
                Application.ClearScreenNextIteration = true;
            }
        }
    }

    /// <summary>Called when <see cref="Visible"/> is changing. Can be cancelled by returning <see langword="true"/>.</summary>
    protected virtual bool OnVisibleChanging () { return false; }

    /// <summary>
    ///     Raised when the <see cref="Visible"/> value is being changed. Can be cancelled by setting Cancel to
    ///     <see langword="true"/>.
    /// </summary>
    public event EventHandler<CancelEventArgs<bool>>? VisibleChanging;

    /// <summary>Called when <see cref="Visible"/> has changed.</summary>
    protected virtual void OnVisibleChanged () { }

    /// <summary>Raised when <see cref="Visible"/> has changed.</summary>
    public event EventHandler? VisibleChanged;

    /// <summary>
    ///     INTERNAL Indicates whether all views up the Superview hierarchy are visible.
    /// </summary>
    /// <param name="view">The view to test.</param>
    /// <returns>
    ///     <see langword="false"/> if `view.Visible` is  <see langword="false"/> or any Superview is not visible,
    ///     <see langword="true"/> otherwise.
    /// </returns>
    internal static bool CanBeVisible (View view)
    {
        if (!view.Visible)
        {
            return false;
        }

        for (View? c = view.SuperView; c != null; c = c.SuperView)
        {
            if (!c.Visible)
            {
                return false;
            }
        }

        return true;
    }

    #endregion Visibility

    #region Title

    private string _title = string.Empty;

    /// <summary>Gets the <see cref="Gui.TextFormatter"/> used to format <see cref="Title"/>.</summary>
    internal TextFormatter TitleTextFormatter { get; init; } = new ();

    /// <summary>
    ///     The title to be displayed for this <see cref="View"/>. The title will be displayed if <see cref="Border"/>.
    ///     <see cref="Thickness.Top"/> is greater than 0. The title can be used to set the <see cref="HotKey"/>
    ///     for the view by prefixing character with <see cref="HotKeySpecifier"/> (e.g. <c>"T_itle"</c>).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Set the <see cref="HotKeySpecifier"/> to enable hotkey support. To disable Title-based hotkey support set
    ///         <see cref="HotKeySpecifier"/> to <c>(Rune)0xffff</c>.
    ///     </para>
    ///     <para>
    ///         Only the first HotKey specifier found in <see cref="Title"/> is supported.
    ///     </para>
    ///     <para>
    ///         To cause the hotkey to be rendered with <see cref="Text"/>,
    ///         set <c>View.</c><see cref="TextFormatter.HotKeySpecifier"/> to the desired character.
    ///     </para>
    /// </remarks>
    /// <value>The title.</value>
    public string Title
    {
        get
        {
#if DEBUG_IDISPOSABLE
            if (WasDisposed)
            {
                throw new ObjectDisposedException (GetType ().FullName);
            }
#endif
            return _title;
        }
        set
        {
#if DEBUG_IDISPOSABLE
            if (WasDisposed)
            {
                throw new ObjectDisposedException (GetType ().FullName);
            }
#endif
            if (value == _title)
            {
                return;
            }

            if (!OnTitleChanging (ref value))
            {
                string old = _title;
                _title = value;
                TitleTextFormatter.Text = _title;

                SetTitleTextFormatterSize ();
                SetHotKeyFromTitle ();
                SetNeedsDraw ();
#if DEBUG
                if (string.IsNullOrEmpty (Id))
                {
                    Id = _title;
                }
#endif // DEBUG
                OnTitleChanged ();
            }
        }
    }

    private void SetTitleTextFormatterSize ()
    {
        TitleTextFormatter.ConstrainToSize = new (
                                                  TextFormatter.GetWidestLineLength (TitleTextFormatter.Text)
                                                  - (TitleTextFormatter.Text?.Contains ((char)HotKeySpecifier.Value) == true
                                                         ? Math.Max (HotKeySpecifier.GetColumns (), 0)
                                                         : 0),
                                                  1);
    }

    // TODO: Change this event to match the standard TG event model.
    /// <summary>Called when the <see cref="View.Title"/> has been changed. Invokes the <see cref="TitleChanged"/> event.</summary>
    protected void OnTitleChanged () { TitleChanged?.Invoke (this, new (in _title)); }

    /// <summary>
    ///     Called before the <see cref="View.Title"/> changes. Invokes the <see cref="TitleChanging"/> event, which can
    ///     be cancelled.
    /// </summary>
    /// <param name="newTitle">The new <see cref="View.Title"/> to be replaced.</param>
    /// <returns>`true` if an event handler canceled the Title change.</returns>
    protected bool OnTitleChanging (ref string newTitle)
    {
        CancelEventArgs<string> args = new (ref _title, ref newTitle);
        TitleChanging?.Invoke (this, args);

        return args.Cancel;
    }

    /// <summary>Raised after the <see cref="View.Title"/> has been changed.</summary>
    public event EventHandler<EventArgs<string>>? TitleChanged;

    /// <summary>
    ///     Raised when the <see cref="View.Title"/> is changing. Set <see cref="CancelEventArgs.Cancel"/> to `true`
    ///     to cancel the Title change.
    /// </summary>
    public event EventHandler<CancelEventArgs<string>>? TitleChanging;

    #endregion

    /// <summary>Pretty prints the View</summary>
    /// <returns></returns>
    public override string ToString () { return $"{GetType ().Name}({Id}){Frame}"; }

    private bool _disposedValue;

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    /// <remarks>
    ///     If disposing equals true, the method has been called directly or indirectly by a user's code. Managed and
    ///     unmanaged resources can be disposed. If disposing equals false, the method has been called by the runtime from
    ///     inside the finalizer and you should not reference other objects. Only unmanaged resources can be disposed.
    /// </remarks>
    /// <param name="disposing"></param>
    protected virtual void Dispose (bool disposing)
    {
        LineCanvas.Dispose ();

        DisposeMouse ();
        DisposeKeyboard ();
        DisposeAdornments ();
        DisposeScrollBars ();

        for (int i = InternalSubviews.Count - 1; i >= 0; i--)
        {
            View subview = InternalSubviews [i];
            Remove (subview);
            subview.Dispose ();
        }

        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            _disposedValue = true;
        }

        Debug.Assert (InternalSubviews.Count == 0);
    }

    /// <summary>
    ///     Riased when the <see cref="View"/> is being disposed.
    /// </summary>
    public event EventHandler? Disposing;

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resource.</summary>
    public void Dispose ()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Disposing?.Invoke (this, EventArgs.Empty);
        Dispose (true);
        GC.SuppressFinalize (this);
#if DEBUG_IDISPOSABLE
        WasDisposed = true;

        foreach (View instance in Instances.Where (x => x.WasDisposed).ToList ())
        {
            Instances.Remove (instance);
        }
#endif
    }

#if DEBUG_IDISPOSABLE
    /// <summary>For debug purposes to verify objects are being disposed properly</summary>
    public bool WasDisposed { get; set; }

    /// <summary>For debug purposes to verify objects are being disposed properly</summary>
    public int DisposedCount { get; set; } = 0;

    /// <summary>For debug purposes</summary>
    public static List<View> Instances { get; set; } = [];
#endif
}
