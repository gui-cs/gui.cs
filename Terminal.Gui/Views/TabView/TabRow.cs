#nullable enable
namespace Terminal.Gui;

/// <summary>
///     The host of the <see cref="Tab"/> objects for a <see cref="TabView"/>. Scrolls the Tabs horizontally within
///     it's Viewport (the ContentSize is set to the sum of the widths of all the Tabs). Includes two scroll buttons
///     that are made visible when needed to enable mouse scrolling across the tabs.
/// </summary>
public class TabRow : View
{
    private readonly View _leftScrollButton;
    private readonly View _rightScrollButton;

    public TabRow ()
    {
        Id = "tabRowView";

        CanFocus = true;
        Height = Dim.Auto ();
        Width = Dim.Fill ();
        SuperViewRendersLineCanvas = true;

        _rightScrollButton = new View
        {
            Id = "rightScrollButton",
            X = Pos.Func (() => Viewport.X + Viewport.Width - 1),
            Y = Pos.AnchorEnd (),
            Width = 1,
            Height = 1,
            Visible = true,
            Text = Glyphs.RightArrow.ToString ()
        };

        _leftScrollButton = new View
        {
            Id = "leftScrollButton",
            X = Pos.Func (() => Viewport.X),
            Y = Pos.AnchorEnd (),
            Width = 1,
            Height = 1,
            Visible = true,
            Text = Glyphs.LeftArrow.ToString ()
        };

        Add (_rightScrollButton, _leftScrollButton);

        Initialized += OnInitialized;
    }

    private void OnInitialized (object? sender, EventArgs e)
    {
        if (SuperView is TabView tabView)
        {
            _leftScrollButton.MouseClick += (o, args) =>
                                               {
                                                   tabView.InvokeCommand (Command.ScrollLeft);
                                               };
            _rightScrollButton.MouseClick += (o, args) =>
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
            MoveSubviewToEnd (_leftScrollButton);
            MoveSubviewToEnd (_rightScrollButton);

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
        //TabView? host = SuperView as TabView;

        //if (host is null)
        //{
        //    return;
        //}


        //if (e is { NewFocused: Tab tab, NewValue: true })
        //{
        //    e.Cancel = RaiseSInvokeCommand (Command.Select, new CommandContext () { Data = tab }) is true;
        //}
    }

    // TODO: This is hacky - it both calculates the content size AND changes the Adornments of the Tabs
    // TODO: These concepts should separated into two methods.
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

    /// <summary>
    ///     Gets the Subviews of this View, cast to type <see cref="Tab"/>.
    /// </summary>
    public IEnumerable<Tab> Tabs => Subviews.Where (v => v is Tab).Cast<Tab> ();

    private int? _selectedTabIndex = null;
}
