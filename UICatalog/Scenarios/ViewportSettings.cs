using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ViewportSettings", "Demonstrates manipulating Viewport, ViewportSettings, and ContentSize to scroll content.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Scrolling")]
[ScenarioCategory ("Adornments")]
public class ViewportSettings : Scenario
{
    public class ScrollingDemoView : FrameView
    {
        public ScrollingDemoView ()
        {
            Id = "ScrollingDemoView";
            Width = Dim.Fill ();
            Height = Dim.Fill ();
            base.ColorScheme = Colors.ColorSchemes ["Base"];

            base.Text =
                "Text (ScrollingDemoView.Text). This is long text.\nThe second line.\n3\n4\n5th line\nLine 6. This is a longer line that should wrap automatically.";
            CanFocus = true;
            BorderStyle = LineStyle.Rounded;
            Arrangement = ViewArrangement.Resizable;

            SetContentSize (new (60, 40));
            ViewportSettings |= Terminal.Gui.ViewportSettings.ClearContentOnly;
            ViewportSettings |= Terminal.Gui.ViewportSettings.ClipContentOnly;

            // Things this view knows how to do
            AddCommand (Command.ScrollDown, () => ScrollVertical (1));
            AddCommand (Command.ScrollUp, () => ScrollVertical (-1));

            AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));
            AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));

            // Default keybindings for all ListViews
            KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
            KeyBindings.Add (Key.CursorDown, Command.ScrollDown);
            KeyBindings.Add (Key.CursorLeft, Command.ScrollLeft);
            KeyBindings.Add (Key.CursorRight, Command.ScrollRight);

            // Add a status label to the border that shows Viewport and ContentSize values. Bit of a hack.
            // TODO: Move to Padding with controls
            Border?.Add (new Label { X = 20 });

            ViewportChanged += VirtualDemoView_LayoutComplete;

            MouseEvent += VirtualDemoView_MouseEvent;
        }

        private void VirtualDemoView_MouseEvent (object sender, MouseEventArgs e)
        {
            if (e.Flags == MouseFlags.WheeledDown)
            {
                ScrollVertical (1);

                return;
            }

            if (e.Flags == MouseFlags.WheeledUp)
            {
                ScrollVertical (-1);

                return;
            }

            if (e.Flags == MouseFlags.WheeledRight)
            {
                ScrollHorizontal (1);

                return;
            }

            if (e.Flags == MouseFlags.WheeledLeft)
            {
                ScrollHorizontal (-1);
            }
        }

        private void VirtualDemoView_LayoutComplete (object sender, DrawEventArgs drawEventArgs)
        {
            Label frameLabel = Padding?.Subviews.OfType<Label> ().FirstOrDefault ();

            if (frameLabel is { })
            {
                frameLabel.Text = $"Viewport: {Viewport}\nFrame: {Frame}";
            }
        }
    }

    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),

            // Use a different colorscheme so ViewSettings.ClearContentOnly is obvious
            ColorScheme = Colors.ColorSchemes ["Toplevel"]
        };

        var editor = new AdornmentsEditor
        {
            AutoSelectViewToEdit = true,
            ShowViewIdentifier = true
        };
        app.Add (editor);

        var view = new ScrollingDemoView
        {
            Title = "Demo View",
            X = Pos.Right (editor),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        app.Add (view);

        // Add Scroll Setting UI to Padding
        view.Padding!.Thickness = view.Padding.Thickness with { Top = view.Padding.Thickness.Top + 6 };
        view.Padding.CanFocus = true;

        Label frameLabel = new ()
        {
            Text = "Frame\nContent",
            Id = "frameLabel",
            Y = 0
        };
        view.Padding.Add (frameLabel);

        var cbAllowNegativeX = new CheckBox
        {
            Title = "Allow _X < 0",
            Y = Pos.Bottom (frameLabel),
            CanFocus = true
        };
        cbAllowNegativeX.CheckedState = view.ViewportSettings.HasFlag  (Terminal.Gui.ViewportSettings.AllowNegativeX) ? CheckState.Checked : CheckState.UnChecked;

        view.Padding.Add (cbAllowNegativeX);

        var cbAllowNegativeY = new CheckBox
        {
            Title = "Allow _Y < 0",
            X = Pos.Right (cbAllowNegativeX) + 1,
            Y = Pos.Bottom (frameLabel),
            CanFocus = true,
        };
        cbAllowNegativeY.CheckedState = view.ViewportSettings.HasFlag  (Terminal.Gui.ViewportSettings.AllowNegativeY) ? CheckState.Checked : CheckState.UnChecked;

        view.Padding.Add (cbAllowNegativeY);

        var cbAllowXGreaterThanContentWidth = new CheckBox
        {
            Title = "All_ow X > Content",
            Y = Pos.Bottom (cbAllowNegativeX),
            CanFocus = true
        };
        cbAllowXGreaterThanContentWidth.CheckedState = view.ViewportSettings.HasFlag  (Terminal.Gui.ViewportSettings.AllowXGreaterThanContentWidth) ? CheckState.Checked : CheckState.UnChecked;

        view.Padding.Add (cbAllowXGreaterThanContentWidth);

        void AllowNegativeXToggle (object sender, CancelEventArgs<CheckState> e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                view.ViewportSettings |= Terminal.Gui.ViewportSettings.AllowNegativeX;
            }
            else
            {
                view.ViewportSettings &= ~Terminal.Gui.ViewportSettings.AllowNegativeX;
            }
        }

        void AllowXGreaterThanContentWidthToggle (object sender, CancelEventArgs<CheckState> e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                view.ViewportSettings |= Terminal.Gui.ViewportSettings.AllowXGreaterThanContentWidth;
            }
            else
            {
                view.ViewportSettings &= ~Terminal.Gui.ViewportSettings.AllowXGreaterThanContentWidth;
            }
        }

        var cbAllowYGreaterThanContentHeight = new CheckBox
        {
            Title = "Allo_w Y > Content",
            X = Pos.Right (cbAllowXGreaterThanContentWidth) + 1,
            Y = Pos.Bottom (cbAllowNegativeX),
            CanFocus = true
        };
        cbAllowYGreaterThanContentHeight.CheckedState = view.ViewportSettings.HasFlag  (Terminal.Gui.ViewportSettings.AllowYGreaterThanContentHeight) ? CheckState.Checked : CheckState.UnChecked;

        view.Padding.Add (cbAllowYGreaterThanContentHeight);

        void AllowNegativeYToggle (object sender, CancelEventArgs<CheckState> e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                view.ViewportSettings |= Terminal.Gui.ViewportSettings.AllowNegativeY;
            }
            else
            {
                view.ViewportSettings &= ~Terminal.Gui.ViewportSettings.AllowNegativeY;
            }
        }

        void AllowYGreaterThanContentHeightToggle (object sender, CancelEventArgs<CheckState> e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                view.ViewportSettings |= Terminal.Gui.ViewportSettings.AllowYGreaterThanContentHeight;
            }
            else
            {
                view.ViewportSettings &= ~Terminal.Gui.ViewportSettings.AllowYGreaterThanContentHeight;
            }
        }

        var labelContentSize = new Label
        {
            Title = "ContentSi_ze:",
            Y = Pos.Bottom (cbAllowYGreaterThanContentHeight)
        };

        NumericUpDown<int> contentSizeWidth = new NumericUpDown<int>
        {
            Value = view.GetContentSize ().Width,
            X = Pos.Right (labelContentSize) + 1,
            Y = Pos.Top (labelContentSize),
            CanFocus = true
        };
        contentSizeWidth.ValueChanging += ContentSizeWidthValueChanged;

        void ContentSizeWidthValueChanged (object sender, CancelEventArgs<int> e)
        {
            if (e.NewValue < 0)
            {
                e.Cancel = true;

                return;
            }
            // BUGBUG: set_ContentSize is supposed to be `protected`. 
            view.SetContentSize (view.GetContentSize () with { Width = e.NewValue });
        }

        var labelComma = new Label
        {
            Title = ",",
            X = Pos.Right (contentSizeWidth),
            Y = Pos.Top (labelContentSize)
        };

        NumericUpDown<int> contentSizeHeight = new NumericUpDown<int>
        {
            Value = view.GetContentSize ().Height,
            X = Pos.Right (labelComma) + 1,
            Y = Pos.Top (labelContentSize),
            CanFocus = true
        };
        contentSizeHeight.ValueChanging += ContentSizeHeightValueChanged;

        void ContentSizeHeightValueChanged (object sender, CancelEventArgs<int> e)
        {
            if (e.NewValue < 0)
            {
                e.Cancel = true;

                return;
            }
            // BUGBUG: set_ContentSize is supposed to be `protected`. 
            view.SetContentSize (view.GetContentSize () with { Height = e.NewValue });
        }

        var cbClearContentOnly = new CheckBox
        {
            Title = "C_learContentOnly",
            X = Pos.Right (contentSizeHeight) + 1,
            Y = Pos.Top (labelContentSize),
            CanFocus = true
        };
        cbClearContentOnly.CheckedState = view.ViewportSettings.HasFlag (Terminal.Gui.ViewportSettings.ClearContentOnly) ? CheckState.Checked : CheckState.UnChecked;
        cbClearContentOnly.CheckedStateChanging += ClearContentOnlyToggle;

        void ClearContentOnlyToggle (object sender, CancelEventArgs<CheckState> e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                view.ViewportSettings |= Terminal.Gui.ViewportSettings.ClearContentOnly;
            }
            else
            {
                view.ViewportSettings &= ~Terminal.Gui.ViewportSettings.ClearContentOnly;
            }
        }

        var cbClipContentOnly = new CheckBox
        {
            Title = "_ClipContentOnly",
            X = Pos.Right (cbClearContentOnly) + 1,
            Y = Pos.Top (labelContentSize),
            CanFocus = true
        };
        cbClipContentOnly.CheckedState = view.ViewportSettings.HasFlag (Terminal.Gui.ViewportSettings.ClipContentOnly) ? CheckState.Checked : CheckState.UnChecked;
        cbClipContentOnly.CheckedStateChanging += ClipContentOnlyOnlyToggle;

        void ClipContentOnlyOnlyToggle (object sender, CancelEventArgs<CheckState> e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                view.ViewportSettings |= Terminal.Gui.ViewportSettings.ClipContentOnly;
            }
            else
            {
                view.ViewportSettings &= ~Terminal.Gui.ViewportSettings.ClipContentOnly;
            }
        }

        var cbVerticalScrollBar = new CheckBox
        {
            Title = "_VerticalScrollBar.Visible",
            X = 0,
            Y = Pos.Bottom (labelContentSize),
            CanFocus = false
        };
        cbVerticalScrollBar.CheckedState = view.VerticalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked;
        cbVerticalScrollBar.CheckedStateChanging += VerticalScrollBarToggle;

        void VerticalScrollBarToggle (object sender, CancelEventArgs<CheckState> e)
        {
            view.VerticalScrollBar.Visible = e.NewValue == CheckState.Checked;
        }

        var cbHorizontalScrollBar = new CheckBox
        {
            Title = "_HorizontalScrollBar.Visible",
            X = Pos.Right (cbVerticalScrollBar) + 1,
            Y = Pos.Bottom (labelContentSize),
            CanFocus = false,
        };
        cbHorizontalScrollBar.CheckedState = view.HorizontalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked;
        cbHorizontalScrollBar.CheckedStateChanging += HorizontalScrollBarToggle;

        void HorizontalScrollBarToggle (object sender, CancelEventArgs<CheckState> e)
        {
            view.HorizontalScrollBar.Visible = e.NewValue == CheckState.Checked;
        }

        view.VerticalScrollBar.AutoShow = true;
        var cbAutoShowVerticalScrollBar = new CheckBox
        {
            Title = "VerticalScrollBar._AutoShow",
            X = Pos.Right (cbHorizontalScrollBar) + 1,
            Y = Pos.Bottom (labelContentSize),
            CanFocus = false,
            CheckedState = view.VerticalScrollBar.AutoShow ? CheckState.Checked : CheckState.UnChecked
        };
        cbAutoShowVerticalScrollBar.CheckedStateChanging += AutoShowVerticalScrollBarToggle;

        void AutoShowVerticalScrollBarToggle (object sender, CancelEventArgs<CheckState> e)
        {
            view.VerticalScrollBar.AutoShow = e.NewValue == CheckState.Checked;
        }

        view.HorizontalScrollBar.AutoShow = true;
        var cbAutoShowHorizontalScrollBar = new CheckBox
        {
            Title = "HorizontalScrollBar.A_utoShow ",
            X = Pos.Right (cbAutoShowVerticalScrollBar) + 1,
            Y = Pos.Bottom (labelContentSize),
            CanFocus = false,
            CheckedState = view.HorizontalScrollBar.AutoShow ? CheckState.Checked : CheckState.UnChecked
        };
        cbAutoShowHorizontalScrollBar.CheckedStateChanging += AutoShowHorizontalScrollBarToggle;

        void AutoShowHorizontalScrollBarToggle (object sender, CancelEventArgs<CheckState> e)
        {
            view.HorizontalScrollBar.AutoShow = e.NewValue == CheckState.Checked;
        }

        cbAllowNegativeX.CheckedStateChanging += AllowNegativeXToggle;
        cbAllowNegativeY.CheckedStateChanging += AllowNegativeYToggle;

        cbAllowXGreaterThanContentWidth.CheckedStateChanging += AllowXGreaterThanContentWidthToggle;
        cbAllowYGreaterThanContentHeight.CheckedStateChanging += AllowYGreaterThanContentHeightToggle;


        view.Padding.Add (labelContentSize, contentSizeWidth, labelComma, contentSizeHeight, cbClearContentOnly, cbClipContentOnly, cbVerticalScrollBar, cbHorizontalScrollBar, cbAutoShowVerticalScrollBar, cbAutoShowHorizontalScrollBar);

        // Add demo views to show that things work correctly
        var textField = new TextField { X = 20, Y = 7, Width = 15, Text = "Test Te_xtField" };

        var colorPicker = new ColorPicker16 { Title = "_BG", BoxHeight = 1, BoxWidth = 1, X = Pos.AnchorEnd (), Y = 10 };
        colorPicker.BorderStyle = LineStyle.RoundedDotted;

        colorPicker.ColorChanged += (s, e) =>
                                    {
                                        colorPicker.SuperView.ColorScheme = new (colorPicker.SuperView.ColorScheme)
                                        {
                                            Normal = new (
                                                          colorPicker.SuperView.ColorScheme.Normal.Foreground,
                                                          e.CurrentValue
                                                         )
                                        };
                                    };

        var textView = new TextView
        {
            X = Pos.Center (),
            Y = 10,
            Title = "TextVie_w",
            Text = "I have a 3 row top border.\nMy border inherits from the SuperView.\nI have 3 lines of text with room for 2.",
            AllowsTab = false,
            Width = 30,
            Height = 6 // TODO: Use Dim.Auto
        };
        textView.Border!.Thickness = new (1, 3, 1, 1);

        var charMap = new CharMap
        {
            X = Pos.Center (),
            Y = Pos.Bottom (textView) + 1,
            Width = Dim.Auto (DimAutoStyle.Content, maximumContentDim: Dim.Func (() => view.GetContentSize ().Width)),
            Height = Dim.Auto (DimAutoStyle.Content, maximumContentDim: Dim.Percent (20)),
        };

        charMap.Accepting += (s, e) =>
                              MessageBox.Query (20, 7, "Hi", $"Am I a {view.GetType ().Name}?", "Yes", "No");

        var buttonAnchored = new Button
        {
            X = Pos.AnchorEnd (), Y = Pos.AnchorEnd (), Text = "Bottom Rig_ht"
        };
        buttonAnchored.Accepting += (sender, args) => MessageBox.Query ("Hi", $"You pressed {((Button)sender)?.Text}", "_Ok");

        view.Margin!.Data = "Margin";
        view.Margin.Thickness = new (0);

        view.Border!.Data = "Border";
        view.Border.Thickness = new (3);

        view.Padding.Data = "Padding";

        view.Add (buttonAnchored, textField, colorPicker, charMap, textView);

        var longLabel = new Label
        {
            Id = "label2",
            X = 0,
            Y = 30,
            Text =
                "This label is long. It should clip to the ContentArea if ClipContentOnly is set. This is a virtual scrolling demo. Use the arrow keys and/or mouse wheel to scroll the content."
        };
        longLabel.TextFormatter.WordWrap = true;
        view.Add (longLabel);

        List<object> options = new () { "Option 1", "Option 2", "Option 3" };

        RangeSlider slider = new (options)
        {
            X = 0,
            Y = Pos.Bottom (textField) + 1,
            Orientation = Orientation.Vertical,
            Type = RangeSliderType.Multiple,
            AllowEmpty = false,
            BorderStyle = LineStyle.Double,
            Title = "_Slider"
        };
        view.Add (slider);

        editor.Initialized += (s, e) => { editor.ViewToEdit = view; };

        editor.AutoSelectViewToEdit = true;
        editor.AutoSelectSuperView = view;
        editor.AutoSelectAdornments = false;

        view.SetFocus ();
        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    public override List<Key> GetDemoKeyStrokes ()
    {
        var keys = new List<Key> ();

        for (int i = 0; i < 50; i++)
        {
            keys.Add (Key.CursorRight);
        }

        for (int i = 0; i < 25; i++)
        {
            keys.Add (Key.CursorLeft);
        }

        for (int i = 0; i < 50; i++)
        {
            keys.Add (Key.CursorDown);
        }

        for (int i = 0; i < 25; i++)
        {
            keys.Add (Key.CursorUp);
        }

        return keys;
    }
}
