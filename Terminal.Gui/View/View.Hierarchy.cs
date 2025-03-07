#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui;

public partial class View // SuperView/SubView hierarchy management (SuperView, SubViews, Add, Remove, etc.)
{
    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    private static readonly IList<View> _empty = new List<View> (0).AsReadOnly ();

    private List<View>? _subviews; // This is null, and allocated on demand.

    // Internally, we use InternalSubViews rather than subviews, as we do not expect us
    // to make the same mistakes our users make when they poke at the SubViews.
    internal IList<View> InternalSubViews => _subviews ?? _empty;

    /// <summary>Gets the list of SubViews.</summary>
    /// <remarks>
    ///     Use <see cref="Add(Terminal.Gui.View?)"/> and <see cref="Remove(Terminal.Gui.View?)"/> to add or remove subviews.
    /// </remarks>
    public IList<View> SubViews => _subviews?.AsReadOnly () ?? _empty;

    private View? _superView;

    /// <summary>
    ///     Gets this Views SuperView (the View's container), or <see langword="null"/> if this view has not been added as a
    ///     SubView.
    /// </summary>
    public View? SuperView
    {
        get => _superView!;
        set => throw new InvalidOperationException (@"SuperView cannot be set.");
    }

    #region AddRemove

    private bool _isAdded;

    /// <summary>Indicates whether the view was added to <see cref="SuperView"/>.</summary>
    public bool IsAdded
    {
        get => _isAdded;
        private set
        {
            if (_isAdded == value)
            {
                return;
            }

            _isAdded = value;
            RaiseIsAddedChanged ();
        }
    }

    internal void RaiseIsAddedChanged ()
    {
        // Tell subclasses that a subview has been added
        EventArgs<bool> args = new (IsAdded);
        OnIsAddedChanged (args);

        IsAddedChanged?.Invoke (this, args);
    }

    /// <summary>Raised when this View has been added to a SuperView.</summary>
    public event EventHandler<EventArgs<bool>>? IsAddedChanged;

    /// <summary>Method invoked when a SubView has been added to this view.</summary>
    /// <param name="newValue">The new value of IsAdded</param>
    protected virtual void OnIsAddedChanged (EventArgs<bool> newValue) { }

    /// <summary>Adds a SubView (child) to this view.</summary>
    /// <remarks>
    ///     <para>
    ///         The Views that have been added to this view can be retrieved via the <see cref="SubViews"/> property. See also
    ///         <seealso cref="Remove(View)"/> <seealso cref="RemoveAll"/>
    ///     </para>
    ///     <para>
    ///         SubViews will be disposed when this View is disposed. In other-words, calling this method causes
    ///         the lifecycle of the subviews to be transferred to this View.
    ///     </para>
    ///     <para>
    ///         Calls/Raises the <see cref="OnSubViewAdded"/>/<see cref="SubViewAdded"/> event.
    ///     </para>
    /// </remarks>
    /// <param name="view">The view to add.</param>
    /// <returns>The view that was added.</returns>
    public virtual View? Add (View? view)
    {
        if (view is null)
        {
            return null;
        }

        _subviews ??= [];

        if (view.IsAdded)
        {
            Logging.Warning ($"{view} already has IsAdded == true.");
        }

        if (_subviews.Contains (view))
        {
            Logging.Warning ($"{view} has already been added to {this}.");
        }

        // TileView likes to add views that were previously added and have HasFocus = true. No bueno.
        view.HasFocus = false;

        // TODO: Make this thread safe
        _subviews.Add (view);
        view._superView = this;

        // This causes IsAddedChanged to be raised on view
        view.IsAdded = true;

        if (view is { Enabled: true, Visible: true, CanFocus: true })
        {
            // Add will cause the newly added subview to gain focus if it's focusable
            if (HasFocus)
            {
                view.SetFocus ();
            }
        }

        if (view.Enabled && !Enabled)
        {
            view.Enabled = false;
        }

        // Raise event indicating a subview has been added
        // We do this before Init.
        RaiseSubViewAdded (view);

        if (IsInitialized && !view.IsInitialized)
        {
            view.BeginInit ();
            view.EndInit ();
        }

        SetNeedsDraw ();
        SetNeedsLayout ();

        return view;
    }

    /// <summary>Adds the specified SubView (children) to the view.</summary>
    /// <param name="views">Array of one or more views (can be optional parameter).</param>
    /// <remarks>
    ///     <para>
    ///         The Views that have been added to this view can be retrieved via the <see cref="SubViews"/> property. See also
    ///         <seealso cref="Remove(View)"/> and <seealso cref="RemoveAll"/>.
    ///     </para>
    ///     <para>
    ///         SubViews will be disposed when this View is disposed. In other-words, calling this method causes
    ///         the lifecycle of the subviews to be transferred to this View.
    ///     </para>
    /// </remarks>
    public void Add (params View []? views)
    {
        if (views is null)
        {
            return;
        }

        foreach (View view in views)
        {
            Add (view);
        }
    }

    internal void RaiseSubViewAdded (View view)
    {
        OnSubViewAdded (view);
        SubViewAdded?.Invoke (this, new (this, view));
    }

    /// <summary>
    ///     Called when a SubView has been added to this View.
    /// </summary>
    /// <remarks>
    ///     If the SubView has not been initialized, this happens before BeginInit/EndInit is called.
    /// </remarks>
    /// <param name="view"></param>
    protected virtual void OnSubViewAdded (View view) { }

    /// <summary>Raised when a SubView has been added to this View.</summary>
    /// <remarks>
    ///     If the SubView has not been initialized, this happens before BeginInit/EndInit is called.
    /// </remarks>
    public event EventHandler<SuperViewChangedEventArgs>? SubViewAdded;

    /// <summary>Removes a SubView added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.</summary>
    /// <remarks>
    ///     <para>
    ///         Normally SubViews will be disposed when this View is disposed. Removing a SubView causes ownership of the
    ///         SubView's
    ///         lifecycle to be transferred to the caller; the caller must call <see cref="Dispose()"/>.
    ///     </para>
    ///     <para>
    ///         Calls/Raises the <see cref="OnSubViewRemoved"/>/<see cref="SubViewRemoved"/> event.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     The removed View. <see langword="null"/> if the View could not be removed.
    /// </returns>
    public virtual View? Remove (View? view)
    {
        if (view is null)
        {
            return null;
        }

        if (_subviews is null)
        {
            return view;
        }

        if (!view.IsAdded)
        {
            Logging.Warning ($"{view} has IsAdded == false.");
        }

        if (!_subviews.Contains (view))
        {
            Logging.Warning ($"{view} has not been added to {this}.");
        }

        Rectangle touched = view.Frame;

        bool hadFocus = view.HasFocus;
        bool couldFocus = view.CanFocus;

        if (hadFocus)
        {
            view.CanFocus = false; // If view had focus, this will ensure it doesn't and it stays that way
        }

        Debug.Assert (!view.HasFocus);

        _subviews.Remove (view);

        // Clean up focus stuff
        _previouslyFocused = null;

        if (view._superView is { } && view._superView._previouslyFocused == this)
        {
            view._superView._previouslyFocused = null;
        }

        view._superView = null;

        // This causes IsAddedChanged to be raised on view
        view.IsAdded = false;

        SetNeedsLayout ();
        SetNeedsDraw ();

        foreach (View v in _subviews)
        {
            if (v.Frame.IntersectsWith (touched))
            {
                view.SetNeedsDraw ();
            }
        }

        view.CanFocus = couldFocus; // Restore to previous value

        if (_previouslyFocused == view)
        {
            _previouslyFocused = null;
        }

        RaiseSubViewRemoved (view);

        return view;
    }

    internal void RaiseSubViewRemoved (View view)
    {
        OnSubViewRemoved (view);
        SubViewRemoved?.Invoke (this, new (this, view));
    }

    /// <summary>
    ///     Called when a SubView has been removed from this View.
    /// </summary>
    /// <param name="view"></param>
    protected virtual void OnSubViewRemoved (View view) { }

    /// <summary>Raised when a SubView has been added to this View.</summary>
    public event EventHandler<SuperViewChangedEventArgs>? SubViewRemoved;

    /// <summary>
    ///     Removes all SubView (children) added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Normally SubViews will be disposed when this View is disposed. Removing a SubView causes ownership of the
    ///         SubView's
    ///         lifecycle to be transferred to the caller; the caller must call <see cref="Dispose()"/> on any Views that were
    ///         added.
    ///     </para>
    /// </remarks>
    public virtual void RemoveAll ()
    {
        if (_subviews is null)
        {
            return;
        }

        while (_subviews.Count > 0)
        {
            Remove (_subviews [0]);
        }
    }

    /// <summary>Raised when a SubView has been removed from this View.</summary>
    public event EventHandler<SuperViewChangedEventArgs>? Removed;

    #endregion AddRemove

    // TODO: This drives a weird coupling of Application.Top and View. It's not clear why this is needed.
    /// <summary>Get the top superview of a given <see cref="View"/>.</summary>
    /// <returns>The superview view.</returns>
    internal View? GetTopSuperView (View? view = null, View? superview = null)
    {
        View? top = superview ?? Application.Top;

        for (View? v = view?.SuperView ?? this?.SuperView; v != null; v = v.SuperView)
        {
            top = v;

            if (top == superview)
            {
                break;
            }
        }

        return top;
    }

    /// <summary>
    ///     Gets whether <paramref name="view"/> is in the SubView hierarchy of <paramref name="start"/>.
    /// </summary>
    /// <param name="start">The View at the start of the hierarchy.</param>
    /// <param name="view">The View to test.</param>
    /// <param name="includeAdornments">Will search the subview hierarchy of the adornments if true.</param>
    /// <returns></returns>
    public static bool IsInHierarchy (View? start, View? view, bool includeAdornments = false)
    {
        if (view is null || start is null)
        {
            return false;
        }

        if (view == start)
        {
            return true;
        }

        foreach (View subView in start.InternalSubViews)
        {
            if (view == subView)
            {
                return true;
            }

            bool found = IsInHierarchy (subView, view, includeAdornments);

            if (found)
            {
                return found;
            }
        }

        if (includeAdornments)
        {
            bool found = IsInHierarchy (start.Padding, view, includeAdornments);

            if (found)
            {
                return found;
            }

            found = IsInHierarchy (start.Border, view, includeAdornments);

            if (found)
            {
                return found;
            }

            found = IsInHierarchy (start.Margin, view, includeAdornments);

            if (found)
            {
                return found;
            }
        }

        return false;
    }

    #region SubViewOrdering

    /// <summary>
    ///     Moves <paramref name="subview"/> one position towards the end of the <see cref="SubViews"/> list.
    /// </summary>
    /// <param name="subview">The subview to move.</param>
    public void MoveSubViewTowardsEnd (View subview)
    {
        PerformActionForSubView (
                                 subview,
                                 x =>
                                 {
                                     int idx = _subviews!.IndexOf (x);

                                     if (idx + 1 < _subviews.Count)
                                     {
                                         _subviews.Remove (x);
                                         _subviews.Insert (idx + 1, x);
                                     }
                                 }
                                );
    }

    /// <summary>
    ///     Moves <paramref name="subview"/> to the end of the <see cref="SubViews"/> list.
    /// </summary>
    /// <param name="subview">The subview to move.</param>
    public void MoveSubViewToEnd (View subview)
    {
        PerformActionForSubView (
                                 subview,
                                 x =>
                                 {
                                     _subviews!.Remove (x);
                                     _subviews.Add (x);
                                 }
                                );
    }

    /// <summary>
    ///     Moves <paramref name="subview"/> one position towards the start of the <see cref="SubViews"/> list.
    /// </summary>
    /// <param name="subview">The subview to move.</param>
    public void MoveSubViewTowardsStart (View subview)
    {
        PerformActionForSubView (
                                 subview,
                                 x =>
                                 {
                                     int idx = _subviews!.IndexOf (x);

                                     if (idx > 0)
                                     {
                                         _subviews.Remove (x);
                                         _subviews.Insert (idx - 1, x);
                                     }
                                 }
                                );
    }

    /// <summary>
    ///     Moves <paramref name="subview"/> to the start of the <see cref="SubViews"/> list.
    /// </summary>
    /// <param name="subview">The subview to move.</param>
    public void MoveSubViewToStart (View subview)
    {
        PerformActionForSubView (
                                 subview,
                                 x =>
                                 {
                                     _subviews!.Remove (x);
                                     _subviews.Insert (0, subview);
                                 }
                                );
    }

    /// <summary>
    ///     Internal API that runs <paramref name="action"/> on a subview if it is part of the <see cref="SubViews"/> list.
    /// </summary>
    /// <param name="subview"></param>
    /// <param name="action"></param>
    private void PerformActionForSubView (View subview, Action<View> action)
    {
        if (_subviews!.Contains (subview))
        {
            action (subview);
        }

        // BUGBUG: this is odd. Why is this needed?
        SetNeedsDraw ();
        subview.SetNeedsDraw ();
    }

    #endregion SubViewOrdering
}
