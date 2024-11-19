#nullable enable
using System.Linq;
using static Terminal.Gui.SpinnerStyle;
using static Unix.Terminal.Delegates;

namespace Terminal.Gui;

/// <summary>Control that hosts multiple sub views, presenting a single one at once.</summary>
public class TabView : View, IDesignable
{
    /// <summary>The default <see cref="MaxTabTextWidth"/> to set on new <see cref="TabView"/> controls.</summary>
    public const uint DefaultMaxTabTextWidth = 30;

    /// <summary>This SubView is the 2 or 3 line control that represents the actual tabs themselves.</summary>
    private readonly TabRowView _tabRowView;

    // private TabToRender []? _tabLocations;

    /// <summary>Initializes a <see cref="TabView"/> class.</summary>
    public TabView ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabStop; // Because TabView has focusable subviews, it must be a TabGroup

        Width = Dim.Fill ();
        Height = Dim.Auto (minimumContentDim: GetTabHeight (!Style.TabsOnBottom));

        _tabRowView = new TabRowView ();
        _tabRowView.Selecting += _tabRowView_Selecting;
        base.Add (_tabRowView);

        ApplyStyleChanges ();

        // Things this view knows how to do
        AddCommand (Command.Left, () => SwitchTabBy (-1));

        AddCommand (Command.Right, () => SwitchTabBy (1));

        AddCommand (
                    Command.LeftStart,
                    () =>
                    {
                        FirstVisibleTabIndex = 0;
                        SelectedTabIndex = 0;

                        return true;
                    }
                   );

        AddCommand (
                    Command.RightEnd,
                    () =>
                    {
                        FirstVisibleTabIndex = Tabs.Count - 1;
                        SelectedTabIndex = Tabs.Count - 1;

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageDown,
                    () =>
                    {
                        // FirstVisibleTabIndex += _tabLocations!.Length;
                        SelectedTabIndex = FirstVisibleTabIndex;

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageUp,
                    () =>
                    {
                        //  FirstVisibleTabIndex -= _tabLocations!.Length;
                        SelectedTabIndex = FirstVisibleTabIndex;

                        return true;
                    }
                   );

        AddCommand (Command.ScrollLeft, () =>
                                        {
                                            var visibleTabs = GetTabsThatCanBeVisible (Viewport).ToArray ();
                                            int? first = visibleTabs.FirstOrDefault ();

                                            if (first > 0)
                                            {
                                                int scroll = -_tabRowView.Tabs.ToArray () [first.Value].Frame.Width;
                                                _tabRowView.Viewport = _tabRowView.Viewport with { X = _tabRowView.Viewport.X + scroll };
                                                SetNeedsLayout ();
                                                FirstVisibleTabIndex--;
                                                return true;
                                            }

                                            return false;
                                        });

        AddCommand (Command.ScrollRight, () =>
                                         {
                                             var visibleTabs = GetTabsThatCanBeVisible (Viewport).ToArray ();
                                             int? last = visibleTabs.LastOrDefault ();

                                             if (last is { })
                                             {
                                                 _tabRowView.ScrollHorizontal (_tabRowView.Tabs.ToArray () [last.Value + 1].Frame.Width);
                                                 SetNeedsLayout ();
                                                 FirstVisibleTabIndex++;
                                                 return true;
                                             }

                                             return false;
                                         });

        //// Space or single-click - Raise Selecting
        //AddCommand (Command.Select, (ctx) =>
        //                            {
        //                                //if (RaiseSelecting (ctx) is true)
        //                                //{
        //                                //    return true;
        //                                //}

        //                                if (ctx.Data is Tab tab)
        //                                {
        //                                    int? current = SelectedTabIndex;
        //                                    SelectedTabIndex = _tabRowView.Tabs.ToArray ().IndexOf (tab);
        //                                    SetNeedsDraw ();

        //                                    // e.Cancel = HasFocus;
        //                                    return true;
        //                                }

        //                                return false;
        //                            });

        // Default keybindings for this view
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.Home, Command.LeftStart);
        KeyBindings.Add (Key.End, Command.RightEnd);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
    }

    private void _tabRowView_Selecting (object? sender, CommandEventArgs e)
    {
        if (e.Context.Data is int tabIndex)
        {
            int? current = SelectedTabIndex;
            SelectedTabIndex = tabIndex;
            Layout ();
            e.Cancel = true;
        }
    }

    /// <inheritdoc />
    protected override void OnSubviewLayout (LayoutEventArgs args)
    {
        _tabRowView.CalcContentSize ();
    }

    /// <inheritdoc />
    protected override void OnSubviewsLaidOut (LayoutEventArgs args)
    {
        // hide all that can't fit
        var visibleTabs = GetTabsThatCanBeVisible (Viewport).ToArray ();

        for (var index = 0; index < _tabRowView.Tabs.ToArray ().Length; index++)
        {
            Tab tab = _tabRowView.Tabs.ToArray () [index];
            tab.Visible = visibleTabs.Contains (index);
        }
    }

    /// <inheritdoc />
    public bool EnableForDesign ()
    {
        AddTab (new () { Text = "Tab_1", Id = "tab1", View = new Label { Text = "Label in Tab1" } }, false);
        AddTab (new () { Text = "Tab _2", Id = "tab2", View = new TextField { Text = "TextField in Tab2", Width = 10 } }, false);
        AddTab (new () { Text = "Tab _Three", Id = "tab3", View = new Label { Text = "Label in Tab3" } }, false);
        AddTab (new () { Text = "Tab _Quattro", Id = "tab4", View = new TextField { Text = "TextField in Tab4", Width = 10 } }, false);

        return true;
    }

    /// <summary>
    ///     The maximum number of characters to render in a Tab header.  This prevents one long tab from pushing out all
    ///     the others.
    /// </summary>
    public uint MaxTabTextWidth { get; set; } = DefaultMaxTabTextWidth;

    private int? _selectedTabIndex;

    /// <summary>The currently selected member of <see cref="Tabs"/> chosen by the user.</summary>
    /// <value></value>
    public int? SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            // If value is outside the range of Tabs, throw an exception
            if (value < 0 || value >= Tabs.Count)
            {
                throw new ArgumentOutOfRangeException (nameof (value), value, @"SelectedTab the range of Tabs.");
            }

            if (value == _selectedTabIndex)
            {
                return;
            }

            int? old = _selectedTabIndex;

            // Get once to avoid multiple enumerations
            Tab [] tabs = _tabRowView.Tabs.ToArray ();

            if (_selectedTabIndex is { } && tabs [_selectedTabIndex.Value].View is { })
            {
                Remove (tabs [_selectedTabIndex.Value].View);
            }

            _selectedTabIndex = value;

            if (_selectedTabIndex is { } && tabs [_selectedTabIndex.Value].View is { })
            {
                Add (tabs [_selectedTabIndex.Value].View);
            }

            EnsureSelectedTabIsVisible ();

            if (_selectedTabIndex is { })
            {
                ApplyStyleChanges ();

                if (HasFocus)
                {
                    tabs [_selectedTabIndex.Value].View.SetFocus ();
                }
            }

            OnSelectedTabIndexChanged (old, _selectedTabIndex!);
            SelectedTabChanged?.Invoke (this, new TabChangedEventArgs (old, _selectedTabIndex));
            SetNeedsLayout ();
        }
    }

    private TabStyle _style = new ();

    /// <summary>Render choices for how to display tabs.  After making changes, call <see cref="ApplyStyleChanges()"/>.</summary>
    /// <value></value>
    public TabStyle Style
    {
        get => _style;
        set
        {
            if (_style == value)
            {
                return;
            }

            _style = value;
            SetNeedsLayout ();
        }
    }

    /// <summary>All tabs currently hosted by the control.</summary>
    /// <value></value>
    public IReadOnlyCollection<Tab> Tabs => _tabRowView.Tabs.ToArray ().AsReadOnly ();

    private int _firstVisibleTabIndex;

    /// <summary>Gets or sets the index of first visible tab. This enables horizontal scrolling of the tabs.</summary>
    /// <remarks>
    ///     <para>
    ///         On set, if the value is less than 0, it will be set to 0.  If the value is greater than the number of tabs
    ///         it will be set to the last tab index.
    ///     </para>
    /// </remarks>
    public int FirstVisibleTabIndex
    {
        get => _firstVisibleTabIndex;
        set
        {
            _firstVisibleTabIndex = Math.Max (Math.Min (value, Tabs.Count - 1), 0);
            ;
            SetNeedsLayout ();
        }
    }

    /// <summary>Adds the given <paramref name="tab"/> to <see cref="Tabs"/>.</summary>
    /// <param name="tab"></param>
    /// <param name="andSelect">True to make the newly added Tab the <see cref="SelectedTabIndex"/>.</param>
    public void AddTab (Tab tab, bool andSelect)
    {
        // Ok to use Subviews here instead of Tabs
        if (_tabRowView.Subviews.Contains (tab))
        {
            return;
        }

        // Add to the TabRowView as a subview
        _tabRowView.Add (tab);

        if (_tabRowView.Tabs.Count () == 1 || andSelect)
        {
            SelectedTabIndex = _tabRowView.Tabs.Count () - 1;

            EnsureSelectedTabIsVisible ();

            if (HasFocus)
            {
                tab.View?.SetFocus ();
            }
        }

        ApplyStyleChanges ();
        SetNeedsLayout ();
    }


    /// <summary>
    ///     Removes the given <paramref name="tab"/> from <see cref="Tabs"/>. Caller is responsible for disposing the
    ///     tab's hosted <see cref="Tab.View"/> if appropriate.
    /// </summary>
    /// <param name="tab"></param>
    public void RemoveTab (Tab? tab)
    {
        if (tab is null || !_tabRowView.Subviews.Contains (tab))
        {
            return;
        }

        int idx = _tabRowView.Tabs.ToArray ().IndexOf (tab);
        if (idx == SelectedTabIndex)
        {
            SelectedTabIndex = null;
        }

        _tabRowView.Remove (tab);

        // Get once to avoid multiple enumerations
        Tab [] tabs = _tabRowView.Tabs.ToArray ();

        if (SelectedTabIndex is null)
        {
            // Either no tab was previously selected or the selected tab was removed

            // select the tab closest to the one that disappeared
            int toSelect = Math.Max (idx - 1, 0);

            if (toSelect < tabs.Length)
            {
                SelectedTabIndex = toSelect;
            }
            else
            {
                SelectedTabIndex = tabs.Length - 1;
            }
        }

        if (SelectedTabIndex > tabs.Length - 1)
        {
            // Removing the tab, caused the selected tab to be out of range
            SelectedTabIndex = tabs.Length - 1;
        }

        EnsureSelectedTabIsVisible ();
        SetNeedsLayout ();
    }

    /// <summary>
    ///     Applies the settings in <see cref="Style"/>. This can change the dimensions of
    ///     <see cref="Tab.View"/> (for rendering the selected tab's content). This method includes a call to
    ///     <see cref="View.SetNeedsDraw()"/>.
    /// </summary>
    public void ApplyStyleChanges ()
    {
        // Get once to avoid multiple enumerations
        Tab [] tabs = _tabRowView.Tabs.ToArray ();

        View? selectedView = null;

        if (SelectedTabIndex is { })
        {
            selectedView = tabs [SelectedTabIndex.Value].View;
        }

        if (selectedView is { })
        {
            selectedView.BorderStyle = Style.ShowBorder ? LineStyle.Single : LineStyle.None;
            selectedView.Width = Dim.Fill ();
        }

        int tabHeight = GetTabHeight (!Style.TabsOnBottom);

        if (Style.TabsOnBottom)
        {
            _tabRowView.Height = tabHeight;
            _tabRowView.Y = Pos.AnchorEnd ();

            if (selectedView is { })
            {
                // Tabs are along the bottom so just dodge the border
                if (Style.ShowBorder && selectedView?.Border is { })
                {
                    selectedView.Border.Thickness = new Thickness (1, 1, 1, 0);
                }

                // Fill client area leaving space at bottom for tabs
                selectedView!.Y = 0;
                selectedView.Height = Dim.Fill (tabHeight);
            }
        }
        else
        {
            // Tabs are along the top
            _tabRowView.Height = tabHeight;
            _tabRowView.Y = 0;

            if (selectedView is { })
            {
                if (Style.ShowBorder && selectedView.Border is { })
                {
                    selectedView.Border.Thickness = new Thickness (1, 0, 1, 1);
                }


                //move content down to make space for tabs
                selectedView.Y = Pos.Bottom (_tabRowView);

                // Fill client area leaving space at bottom for border
                selectedView.Height = Dim.Fill ();
            }
        }

        SetNeedsLayout ();
    }

    /// <summary>Updates <see cref="FirstVisibleTabIndex"/> to ensure that <see cref="SelectedTabIndex"/> is visible.</summary>
    public void EnsureSelectedTabIsVisible ()
    {
        if (SelectedTabIndex is null)
        {
            return;
        }

        // Get once to avoid multiple enumerations
        Tab [] tabs = _tabRowView.Tabs.ToArray ();
        View? selectedView = tabs [SelectedTabIndex.Value].View;

        if (selectedView is null)
        {
            return;
        }

        // if current viewport does not include the selected tab
        if (!GetTabsThatCanBeVisible (Viewport).Any (r => Equals (SelectedTabIndex.Value, r)))
        {
            // Set scroll offset so the first tab rendered is the
            FirstVisibleTabIndex = Math.Max (0, SelectedTabIndex.Value);
        }
    }

    /// <summary>Event for when <see cref="SelectedTabIndex"/> changes.</summary>
    public event EventHandler<TabChangedEventArgs>? SelectedTabChanged;

    /// <summary>
    ///     Changes the <see cref="SelectedTabIndex"/> by the given <paramref name="amount"/>. Positive for right, negative for
    ///     left. If no tab is currently selected then the first tab will become selected.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns><see langword="true"/> if a change was made.</returns>
    public bool SwitchTabBy (int amount)
    {

        // Get once to avoid multiple enumerations
        Tab [] tabs = _tabRowView.Tabs.ToArray ();

        if (tabs.Length == 0)
        {
            return false;
        }

        int? currentIdx = SelectedTabIndex;

        // if there is only one tab anyway or nothing is selected
        if (tabs.Length == 1)
        {
            SelectedTabIndex = 0;

            return SelectedTabIndex != currentIdx;
        }

        // Currently selected tab has vanished!
        if (currentIdx is null)
        {
            SelectedTabIndex = 0;

            return true;
        }

        int newIdx = Math.Max (0, Math.Min (currentIdx.Value + amount, tabs.Length - 1));

        if (newIdx == currentIdx)
        {
            return false;
        }

        SelectedTabIndex = newIdx;

        return true;
    }

    /// <summary>Called when the <see cref="SelectedTabIndex"/> has changed.</summary>
    protected virtual void OnSelectedTabIndexChanged (int? oldTabIndex, int? newTabIndex) { }

    /// <summary>Returns which tabs will be visible given the dimensions of the TabView, which tab is selected, and how the tabs have been scrolled.</summary>
    /// <paramref name="bounds">Same as this.Frame.</paramref>
    /// <returns></returns>
    private IEnumerable<int> GetTabsThatCanBeVisible (Rectangle bounds)
    {
        var curWidth = 1;
        View? prevTab = null;

        // Get once to avoid multiple enumerations
        Tab [] tabs = _tabRowView.Tabs.ToArray ();

        // Starting at the first or scrolled to tab
        for (int i = FirstVisibleTabIndex; i < tabs.Length; i++)
        {
            if (curWidth >= bounds.Width)
            {
                break;
            }

            if (curWidth + tabs [i].Frame.Width < bounds.Width)
            {
                yield return i;
            }
            curWidth += tabs [i].Frame.Width;
        }
    }

    /// <summary>
    ///     Returns the number of rows occupied by rendering the tabs, this depends on <see cref="TabStyle.ShowTopLine"/>
    ///     and can be 0 (e.g. if <see cref="TabStyle.TabsOnBottom"/> and you ask for <paramref name="top"/>).
    /// </summary>
    /// <param name="top">True to measure the space required at the top of the control, false to measure space at the bottom.</param>
    /// .
    /// <returns></returns>
    private int GetTabHeight (bool top)
    {
        if (top && Style.TabsOnBottom)
        {
            return 0;
        }

        if (!top && !Style.TabsOnBottom)
        {
            return 0;
        }

        return Style.ShowTopLine ? 3 : 2;
    }

    /// <inheritdoc />
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            // Get once to avoid multiple enumerations
            Tab [] tabs = _tabRowView.Tabs.ToArray ();
            if (SelectedTabIndex is { })
            {
                Remove (tabs [SelectedTabIndex.Value].View);
            }
            foreach (Tab tab in tabs)
            {
                tab.View?.Dispose ();
                tab.View = null;
            }
        };
        base.Dispose (disposing);
    }

    private class TabRowView : View
    {
        private readonly View _leftScrollIndicator;
        private readonly View _rightScrollIndicator;

        public TabRowView ()
        {
            Id = "tabRowView";

            CanFocus = true;
            Height = Dim.Auto ();
            Width = Dim.Fill ();
            SuperViewRendersLineCanvas = true;

            _rightScrollIndicator = new View
            {
                Id = "rightScrollIndicator",
                X = Pos.Func (() => Viewport.X + Viewport.Width - 1),
                Y = Pos.AnchorEnd (),
                Width = 1,
                Height = 1,
                Visible = true,
                Text = Glyphs.RightArrow.ToString ()
            };

            _leftScrollIndicator = new View
            {
                Id = "leftScrollIndicator",
                X = Pos.Func (() => Viewport.X),
                Y = Pos.AnchorEnd (),
                Width = 1,
                Height = 1,
                Visible = true,
                Text = Glyphs.LeftArrow.ToString ()
            };

            Add (_rightScrollIndicator, _leftScrollIndicator);

            Initialized += OnInitialized;
        }

        private void OnInitialized (object? sender, EventArgs e)
        {
            if (SuperView is TabView tabView)
            {
                _leftScrollIndicator.MouseClick += (o, args) =>
                                                   {
                                                       tabView.InvokeCommand (Command.ScrollLeft);
                                                   };
                _rightScrollIndicator.MouseClick += (o, args) =>
                                                    {
                                                        tabView.InvokeCommand (Command.ScrollRight);
                                                    };
                tabView.SelectedTabChanged += TabView_SelectedTabChanged;
            }

            CalcContentSize ();
        }

        private void TabView_SelectedTabChanged (object? sender, TabChangedEventArgs e)
        {
            _selectedTabIndex = e.NewTabIndex;
            CalcContentSize ();
        }

        /// <inheritdoc />
        public override void OnAdded (SuperViewChangedEventArgs e)
        {
            if (e.SubView is Tab tab)
            {
                MoveSubviewToEnd (_leftScrollIndicator);
                MoveSubviewToEnd (_rightScrollIndicator);

                tab.HasFocusChanged += TabOnHasFocusChanged;
                tab.Selecting += Tab_Selecting;
            }
            CalcContentSize ();
        }

        private void Tab_Selecting (object? sender, CommandEventArgs e)
        {
            e.Cancel = RaiseSelecting (new CommandContext (Command.Select, null, data: Tabs.ToArray ().IndexOf (sender))) is true;
        }

        private void TabOnHasFocusChanged (object? sender, HasFocusEventArgs e)
        {
            TabView? host = SuperView as TabView;

            if (host is null)
            {
                return;
            }


            //if (e is { NewFocused: Tab tab, NewValue: true })
            //{
            //    e.Cancel = RaiseSInvokeCommand (Command.Select, new CommandContext () { Data = tab }) is true;
            //}
        }

        public void CalcContentSize ()
        {
            TabView? host = SuperView as TabView;

            if (host is null)
            {
                return;
            }

            Tab? selected = null;
            int topLine = host!.Style.ShowTopLine ? 1 : 0;

            Tab [] tabs = Tabs.ToArray ();

            for (int i = 0; i < tabs.Length; i++)
            {
                tabs [i].Height = Dim.Fill ();
                if (i == 0)
                {
                    tabs [i].X = 0;
                }
                else
                {
                    tabs [i].X = Pos.Right (tabs [i - 1]);
                }

                if (i == _selectedTabIndex)
                {
                    selected = tabs [i];

                    if (host.Style.TabsOnBottom)
                    {
                        tabs [i].Border.Thickness = new Thickness (1, 0, 1, topLine);
                        tabs [i].Margin.Thickness = new Thickness (0, 1, 0, 0);
                    }
                    else
                    {
                        tabs [i].Border.Thickness = new Thickness (1, topLine, 1, 0);
                        tabs [i].Margin.Thickness = new Thickness (0, 0, 0, topLine);
                    }
                }
                else if (selected is null)
                {
                    if (host.Style.TabsOnBottom)
                    {
                        tabs [i].Border.Thickness = new Thickness (1, 1, 0, topLine);
                        tabs [i].Margin.Thickness = new Thickness (0, 0, 0, 0);
                    }
                    else
                    {
                        tabs [i].Border.Thickness = new Thickness (1, topLine, 0, 1);
                        tabs [i].Margin.Thickness = new Thickness (0, 0, 0, 0);
                    }

                    //tabs [i].Width = Math.Max (tabs [i].Width!.GetAnchor (0) - 1, 1);
                }
                else
                {
                    if (host.Style.TabsOnBottom)
                    {
                        tabs [i].Border.Thickness = new Thickness (0, 1, 1, topLine);
                        tabs [i].Margin.Thickness = new Thickness (0, 0, 0, 0);
                    }
                    else
                    {
                        tabs [i].Border.Thickness = new Thickness (0, topLine, 1, 1);
                        tabs [i].Margin.Thickness = new Thickness (0, 0, 0, 0);
                    }

                    //tabs [i].Width = Math.Max (tabs [i].Width!.GetAnchor (0) - 1, 1);
                }

                //tabs [i].Text = toRender.TextToRender;
            }

            SetContentSize (null);
            Layout (Application.Screen.Size);

            var width = 0;
            foreach (Tab t in tabs)
            {
                width += t.Frame.Width;
            }
            SetContentSize (new (width, Viewport.Height));
        }

        internal IEnumerable<Tab> Tabs => Subviews.Where (v => v is Tab).Cast<Tab> ();

        private int? _selectedTabIndex = null;
    }
}
