using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("LinearRanges", "Demonstrates the LinearRange view.")]
[ScenarioCategory ("Controls")]
public class LinearRanges : Scenario
{
    public void MakeSliders (View v, List<object> options)
    {
        List<LinearRangeType> types = Enum.GetValues (typeof (LinearRangeType)).Cast<LinearRangeType> ().ToList ();
        LinearRange prev = null;

        foreach (LinearRangeType type in types)
        {
            var view = new LinearRange (options)
            {
                Title = type.ToString (),
                X = 0,
                Y = prev == null ? 0 : Pos.Bottom (prev),
                BorderStyle = LineStyle.Single,
                Type = type,
                AllowEmpty = true
            };
            view.Style.OptionChar = new Cell { Rune = CM.Glyphs.Cross };
            v.Add (view);
            prev = view;
        }

        List<object> singleOptions = new ()
        {
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9,
            10,
            11,
            12,
            13,
            14,
            15,
            16,
            17,
            18,
            19,
            20,
            21,
            22,
            23,
            24,
            25,
            26,
            27,
            28,
            29,
            30,
            31,
            32,
            33,
            34,
            35,
            36,
            37,
            38,
            39
        };

        var single = new LinearRange (singleOptions)
        {
            Title = "_Continuous",
            X = 0,
            Y = prev == null ? 0 : Pos.Bottom (prev),
            Type = LinearRangeType.Single,
            BorderStyle = LineStyle.Single,
            AllowEmpty = false
        };

        single.SubviewLayout += (s, e) =>
                                {
                                    if (single.Orientation == Orientation.Horizontal)
                                    {
                                        single.Style.SpaceChar = new Cell { Rune = CM.Glyphs.HLine };
                                        single.Style.OptionChar = new Cell { Rune = CM.Glyphs.HLine };
                                    }
                                    else
                                    {
                                        single.Style.SpaceChar = new Cell { Rune = CM.Glyphs.VLine };
                                        single.Style.OptionChar = new Cell { Rune = CM.Glyphs.VLine };
                                    }
                                };
        single.Style.SetChar = new Cell { Rune = CM.Glyphs.ContinuousMeterSegment };
        single.Style.DragChar = new Cell { Rune = CM.Glyphs.ContinuousMeterSegment };

        v.Add (single);

        single.OptionsChanged += (s, e) =>
                                 {
                                     single.Title = $"_Continuous {e.Options.FirstOrDefault ().Key}";
                                 };

        List<object> oneOption = new () { "The Only Option" };

        var one = new LinearRange (oneOption)
        {
            Title = "_One Option",
            X = 0,
            Y = prev == null ? 0 : Pos.Bottom (single),
            Type = LinearRangeType.Single,
            BorderStyle = LineStyle.Single,
            AllowEmpty = false
        };
        v.Add (one);
    }

    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        MakeSliders (
                     app,
                     new List<object>
                     {
                         500,
                         1000,
                         1500,
                         2000,
                         2500,
                         3000,
                         3500,
                         4000,
                         4500,
                         5000
                     }
                    );

        var configView = new FrameView
        {
            Title = "Confi_guration",
            X = Pos.Percent (50),
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Dialog"]
        };

        app.Add (configView);

        #region Config Slider

        LinearRange<string> optionsSlider = new ()
        {
            Title = "Options",
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Type = LinearRangeType.Multiple,
            AllowEmpty = true,
            BorderStyle = LineStyle.Single
        };

        optionsSlider.Style.SetChar = optionsSlider.Style.SetChar with { Attribute = new Attribute (Color.BrightGreen, Color.Black) };
        optionsSlider.Style.LegendAttributes.SetAttribute = new Attribute (Color.Green, Color.Black);

        optionsSlider.Options = new List<LinearRangeOption<string>>
        {
            new () { Legend = "Legends" },
            new () { Legend = "RangeAllowSingle" },
            new () { Legend = "EndSpacing" },
            new () { Legend = "DimAuto" }
        };

        configView.Add (optionsSlider);

        optionsSlider.OptionsChanged += (sender, e) =>
                                 {
                                     foreach (LinearRange s in app.Subviews.OfType<LinearRange> ())
                                     {
                                         s.ShowLegends = e.Options.ContainsKey (0);
                                         s.RangeAllowSingle = e.Options.ContainsKey (1);
                                         s.ShowEndSpacing = e.Options.ContainsKey (2);

                                         if (e.Options.ContainsKey (3))
                                         {
                                             s.Width = Dim.Auto (DimAutoStyle.Content);
                                             s.Height = Dim.Auto (DimAutoStyle.Content);
                                         }
                                         else
                                         {
                                             if (s.Orientation == Orientation.Horizontal)
                                             {
                                                 s.Width = Dim.Percent (50);

                                                 int h = s.ShowLegends && s.LegendsOrientation == Orientation.Vertical
                                                             ? s.Options.Max (o => o.Legend.Length) + 3
                                                             : 4;
                                                 s.Height = h;
                                             }
                                             else
                                             {
                                                 int w = s.ShowLegends ? s.Options.Max (o => o.Legend.Length) + 3 : 3;
                                                 s.Width = w;
                                                 s.Height = Dim.Fill ();
                                             }
                                         }
                                     }
                                 };
        optionsSlider.SetOption (0); // Legends
        optionsSlider.SetOption (1); // RangeAllowSingle
        optionsSlider.SetOption (3); // DimAuto

        CheckBox dimAutoUsesMin = new ()
        {
            Text = "Use minimum size (vs. ideal)",
            X = 0,
            Y = Pos.Bottom (optionsSlider)
        };

        dimAutoUsesMin.CheckedStateChanging += (sender, e) =>
                                  {
                                      foreach (LinearRange s in app.Subviews.OfType<LinearRange> ())
                                      {
                                          s.UseMinimumSize = !s.UseMinimumSize;
                                      }
                                  };
        configView.Add (dimAutoUsesMin);

        #region Slider Orientation Slider

        LinearRange<string> orientationSlider = new (new List<string> { "Horizontal", "Vertical" })
        {
            Title = "Slider Orientation",
            X = 0,
            Y = Pos.Bottom (dimAutoUsesMin) + 1,
            BorderStyle = LineStyle.Single
        };

        orientationSlider.SetOption (0);

        configView.Add (orientationSlider);

        orientationSlider.OptionsChanged += (sender, e) =>
                                                    {
                                                        View prev = null;

                                                        foreach (LinearRange s in app.Subviews.OfType<LinearRange> ())
                                                        {
                                                            if (e.Options.ContainsKey (0))
                                                            {
                                                                s.Orientation = Orientation.Horizontal;

                                                                s.Style.SpaceChar = new Cell { Rune = CM.Glyphs.HLine };

                                                                if (prev == null)
                                                                {
                                                                    s.Y = 0;
                                                                }
                                                                else
                                                                {
                                                                    s.Y = Pos.Bottom (prev) + 1;
                                                                }

                                                                s.X = 0;
                                                                prev = s;
                                                            }
                                                            else if (e.Options.ContainsKey (1))
                                                            {
                                                                s.Orientation = Orientation.Vertical;

                                                                s.Style.SpaceChar = new Cell { Rune = CM.Glyphs.VLine };

                                                                if (prev == null)
                                                                {
                                                                    s.X = 0;
                                                                }
                                                                else
                                                                {
                                                                    s.X = Pos.Right (prev) + 2;
                                                                }

                                                                s.Y = 0;
                                                                prev = s;
                                                            }

                                                            if (optionsSlider.GetSetOptions ().Contains (3))
                                                            {
                                                                s.Width = Dim.Auto (DimAutoStyle.Content);
                                                                s.Height = Dim.Auto (DimAutoStyle.Content);
                                                            }
                                                            else
                                                            {
                                                                if (s.Orientation == Orientation.Horizontal)
                                                                {
                                                                    s.Width = Dim.Percent (50);

                                                                    int h = s.ShowLegends && s.LegendsOrientation == Orientation.Vertical
                                                                                ? s.Options.Max (o => o.Legend.Length) + 3
                                                                                : 4;
                                                                    s.Height = h;
                                                                }
                                                                else
                                                                {
                                                                    int w = s.ShowLegends ? s.Options.Max (o => o.Legend.Length) + 3 : 3;
                                                                    s.Width = w;
                                                                    s.Height = Dim.Fill ();
                                                                }
                                                            }
                                                        }
                                                    };

        #endregion Slider Orientation Slider

        #region Legends Orientation Slider

        LinearRange<string> legendsOrientationSlider = new (new List<string> { "Horizontal", "Vertical" })
        {
            Title = "Legends Orientation",
            X = 0,
            Y = Pos.Bottom (orientationSlider) + 1,
            BorderStyle = LineStyle.Single
        };

        legendsOrientationSlider.SetOption (0);

        configView.Add (legendsOrientationSlider);

        legendsOrientationSlider.OptionsChanged += (sender, e) =>
                                                     {
                                                         foreach (LinearRange s in app.Subviews.OfType<LinearRange> ())
                                                         {
                                                             if (e.Options.ContainsKey (0))
                                                             {
                                                                 s.LegendsOrientation = Orientation.Horizontal;
                                                             }
                                                             else if (e.Options.ContainsKey (1))
                                                             {
                                                                 s.LegendsOrientation = Orientation.Vertical;
                                                             }

                                                             if (optionsSlider.GetSetOptions ().Contains (3))
                                                             {
                                                                 s.Width = Dim.Auto (DimAutoStyle.Content);
                                                                 s.Height = Dim.Auto (DimAutoStyle.Content);
                                                             }
                                                             else
                                                             {
                                                                 if (s.Orientation == Orientation.Horizontal)
                                                                 {
                                                                     s.Width = Dim.Percent (50);

                                                                     int h = s.ShowLegends && s.LegendsOrientation == Orientation.Vertical
                                                                                 ? s.Options.Max (o => o.Legend.Length) + 3
                                                                                 : 4;
                                                                     s.Height = h;
                                                                 }
                                                                 else
                                                                 {
                                                                     int w = s.ShowLegends ? s.Options.Max (o => o.Legend.Length) + 3 : 3;
                                                                     s.Width = w;
                                                                     s.Height = Dim.Fill ();
                                                                 }
                                                             }
                                                         }

                                                     };

        #endregion Legends Orientation Slider


        #region Spacing Options

        FrameView spacingOptions = new ()
        {
            Title = "Spacing Options",
            X = Pos.Right (orientationSlider),
            Y = Pos.Top (orientationSlider),
            Width = Dim.Fill (),
            Height = Dim.Auto (),
            BorderStyle = LineStyle.Single
        };

        Label label = new ()
        {
            Text = "Min _Inner Spacing:",
        };

        NumericUpDown<int> innerSpacingUpDown = new ()
        {
            X = Pos.Right (label) + 1
        };

        innerSpacingUpDown.Value = app.Subviews.OfType<LinearRange> ().First ().MinimumInnerSpacing;

        innerSpacingUpDown.ValueChanging += (sender, e) =>
                                            {
                                                if (e.NewValue < 0)
                                                {
                                                    e.Cancel = true;

                                                    return;
                                                }

                                                foreach (LinearRange s in app.Subviews.OfType<LinearRange> ())
                                                {
                                                    s.MinimumInnerSpacing = e.NewValue;
                                                }
                                            };



        spacingOptions.Add (label, innerSpacingUpDown);
        configView.Add (spacingOptions);

        #endregion

        #region Color Slider

        foreach (LinearRange s in app.Subviews.OfType<LinearRange> ())
        {
            s.Style.OptionChar = s.Style.OptionChar with { Attribute = app.GetNormalColor () };
            s.Style.SetChar = s.Style.SetChar with { Attribute = app.GetNormalColor () };
            s.Style.LegendAttributes.SetAttribute = app.GetNormalColor ();
            s.Style.RangeChar = s.Style.RangeChar with { Attribute = app.GetNormalColor () };
        }

        LinearRange<(Color, Color)> sliderFGColor = new ()
        {
            Title = "FG Color",
            X = 0,
            Y = Pos.Bottom (
                            legendsOrientationSlider
                           )
                + 1,
            Type = LinearRangeType.Single,
            BorderStyle = LineStyle.Single,
            AllowEmpty = false,
            Orientation = Orientation.Vertical,
            LegendsOrientation = Orientation.Horizontal,
            MinimumInnerSpacing = 0,
            UseMinimumSize = true
        };

        sliderFGColor.Style.SetChar = sliderFGColor.Style.SetChar with { Attribute = new Attribute (Color.BrightGreen, Color.Black) };
        sliderFGColor.Style.LegendAttributes.SetAttribute = new Attribute (Color.Green, Color.Blue);

        List<LinearRangeOption<(Color, Color)>> colorOptions = new ();

        foreach (ColorName16 colorIndex in Enum.GetValues<ColorName16> ())
        {
            var colorName = colorIndex.ToString ();

            colorOptions.Add (
                              new LinearRangeOption<(Color, Color)>
                              {
                                  Data = (new Color (colorIndex),
                                          new Color (colorIndex)),
                                  Legend = colorName,
                                  LegendAbbr = (Rune)colorName [0]
                              }
                             );
        }

        sliderFGColor.Options = colorOptions;

        configView.Add (sliderFGColor);

        sliderFGColor.OptionsChanged += (sender, e) =>
                                        {
                                            if (e.Options.Count != 0)
                                            {
                                                (Color, Color) data = e.Options.First ().Value.Data;

                                                foreach (LinearRange s in app.Subviews.OfType<LinearRange> ())
                                                {
                                                    s.ColorScheme = new ColorScheme (s.ColorScheme);

                                                    s.ColorScheme = new ColorScheme (s.ColorScheme)
                                                    {
                                                        Normal = new Attribute (
                                                                                data.Item2,
                                                                                s.ColorScheme.Normal.Background
                                                                               )
                                                    };

                                                    s.Style.OptionChar = s.Style.OptionChar with
                                                    {
                                                        Attribute = new Attribute (data.Item1, s.ColorScheme.Normal.Background)
                                                    };

                                                    s.Style.SetChar = s.Style.SetChar with
                                                    {
                                                        Attribute = new Attribute (
                                                                                   data.Item1,
                                                                                   s.Style.SetChar.Attribute?.Background
                                                                                   ?? s.ColorScheme.Normal.Background
                                                                                  )
                                                    };
                                                    s.Style.LegendAttributes.SetAttribute = new Attribute (data.Item1, s.ColorScheme.Normal.Background);

                                                    s.Style.RangeChar = s.Style.RangeChar with
                                                    {
                                                        Attribute = new Attribute (data.Item1, s.ColorScheme.Normal.Background)
                                                    };

                                                    s.Style.SpaceChar = s.Style.SpaceChar with
                                                    {
                                                        Attribute = new Attribute (data.Item1, s.ColorScheme.Normal.Background)
                                                    };

                                                    s.Style.LegendAttributes.NormalAttribute =
                                                        new Attribute (data.Item1, s.ColorScheme.Normal.Background);
                                                }
                                            }
                                        };

        LinearRange<(Color, Color)> sliderBGColor = new ()
        {
            Title = "BG Color",
            X = Pos.Right (sliderFGColor),
            Y = Pos.Top (sliderFGColor),
            Type = LinearRangeType.Single,
            BorderStyle = LineStyle.Single,
            AllowEmpty = false,
            Orientation = Orientation.Vertical,
            LegendsOrientation = Orientation.Horizontal,
            MinimumInnerSpacing = 0,
            UseMinimumSize = true
        };

        sliderBGColor.Style.SetChar = sliderBGColor.Style.SetChar with { Attribute = new Attribute (Color.BrightGreen, Color.Black) };
        sliderBGColor.Style.LegendAttributes.SetAttribute = new Attribute (Color.Green, Color.Blue);

        sliderBGColor.Options = colorOptions;

        configView.Add (sliderBGColor);

        sliderBGColor.OptionsChanged += (sender, e) =>
                                        {
                                            if (e.Options.Count != 0)
                                            {
                                                (Color, Color) data = e.Options.First ().Value.Data;

                                                foreach (LinearRange s in app.Subviews.OfType<LinearRange> ())
                                                {
                                                    s.ColorScheme = new ColorScheme (s.ColorScheme)
                                                    {
                                                        Normal = new Attribute (
                                                                                s.ColorScheme.Normal.Foreground,
                                                                                data.Item2
                                                                               )
                                                    };
                                                }
                                            }
                                        };

        #endregion Color Slider

        #endregion Config Slider

        ObservableCollection<string> eventSource = new ();
        var eventLog = new ListView
        {
            X = Pos.Right (sliderBGColor),
            Y = Pos.Bottom (spacingOptions),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
            Source = new ListWrapper<string> (eventSource)
        };
        configView.Add (eventLog);


        foreach (LinearRange slider in app.Subviews.Where (v => v is LinearRange)!)
        {
            slider.Accepting += (o, args) =>
                             {
                                 eventSource.Add ($"Accept: {string.Join(",", slider.GetSetOptions ())}");
                                 eventLog.MoveDown ();
                                 args.Cancel = true;
                             };
            slider.OptionsChanged += (o, args) =>
                             {
                                 eventSource.Add ($"OptionsChanged: {string.Join (",", slider.GetSetOptions ())}");
                                 eventLog.MoveDown ();
                                 args.Cancel = true;
                             };
        }

        app.FocusDeepest (NavigationDirection.Forward, null);

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
