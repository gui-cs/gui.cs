#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public sealed class ArrangementEditor : EditorBase
{
    public ArrangementEditor ()
    {
        Title = "ArrangementEditor";
        TabStop = TabBehavior.TabGroup;

        Initialized += ArrangementEditor_Initialized;

        _arrangementSlider.MinimumInnerSpacing = 0;

        _arrangementSlider.Options =
        [
            new LinearRangeOption<ViewArrangement>
            {
                Legend = $"{ViewArrangement.Movable}",
                Data = ViewArrangement.Movable
            },

            new LinearRangeOption<ViewArrangement>
            {
                Legend = ViewArrangement.LeftResizable.ToString (),
                Data = ViewArrangement.LeftResizable
            },

            new LinearRangeOption<ViewArrangement>
            {
                Legend = ViewArrangement.RightResizable.ToString (),
                Data = ViewArrangement.RightResizable
            },

            new LinearRangeOption<ViewArrangement>
            {
                Legend = ViewArrangement.TopResizable.ToString (),
                Data = ViewArrangement.TopResizable
            },

            new LinearRangeOption<ViewArrangement>
            {
                Legend = ViewArrangement.BottomResizable.ToString (),
                Data = ViewArrangement.BottomResizable
            },

            new LinearRangeOption<ViewArrangement>
            {
                Legend = ViewArrangement.Overlapped.ToString (),
                Data = ViewArrangement.Overlapped
            }
        ];

        Add (_arrangementSlider);
    }

    private readonly LinearRange<ViewArrangement> _arrangementSlider = new()
    {
        Orientation = Orientation.Vertical,
        UseMinimumSize = true,
        Type = LinearRangeType.Multiple,
        AllowEmpty = true,
    };

    protected override void OnViewToEditChanged ()
    {
        _arrangementSlider.Enabled = ViewToEdit is not Adornment;

        _arrangementSlider.OptionsChanged -= ArrangementSliderOnOptionsChanged;

        // Set the appropriate options in the slider based on _viewToEdit.Arrangement
        if (ViewToEdit is { })
        {
            _arrangementSlider.Options.ForEach (
                                                option =>
                                                {
                                                    _arrangementSlider.ChangeOption (
                                                                                     _arrangementSlider.Options.IndexOf (option),
                                                                                     (ViewToEdit.Arrangement & option.Data) == option.Data);
                                                });
        }

        _arrangementSlider.OptionsChanged += ArrangementSliderOnOptionsChanged;
    }

    private void ArrangementEditor_Initialized (object? sender, EventArgs e)
    {
        _arrangementSlider.OptionsChanged += ArrangementSliderOnOptionsChanged;
        _arrangementSlider.Style.OptionChar = new Cell { Rune = CM.Glyphs.CheckStateUnChecked, Attribute = GetNormalColor () };
        _arrangementSlider.Style.SetChar = new Cell { Rune = CM.Glyphs.CheckStateChecked, Attribute = GetNormalColor () };
        _arrangementSlider.Style.StartRangeChar = new Cell { Rune = CM.Glyphs.CheckStateChecked, Attribute = GetNormalColor () };
        _arrangementSlider.Style.EndRangeChar = new Cell { Rune = CM.Glyphs.CheckStateChecked, Attribute = GetNormalColor () };
        _arrangementSlider.Style.EmptyChar = new Cell { Rune = (Rune)'e', Attribute = GetNormalColor () };
        _arrangementSlider.Style.RangeChar = new Cell { Rune = (Rune)'r', Attribute = GetNormalColor () };
        _arrangementSlider.Style.SpaceChar = new Cell { Rune = (Rune)'s', Attribute = GetNormalColor () };
        _arrangementSlider.Style.DragChar = new Cell { Rune = (Rune)'d', Attribute = GetNormalColor () };
    }

    private void ArrangementSliderOnOptionsChanged (object? sender, LinearRangeEventArgs<ViewArrangement> e)
    {
        if (ViewToEdit is { })
        {
            // Set the arrangement based on the selected options
            var arrangement = ViewArrangement.Fixed;

            foreach (KeyValuePair<int, LinearRangeOption<ViewArrangement>> option in e.Options)
            {
                arrangement |= option.Value.Data;
            }

            ViewToEdit.Arrangement = arrangement;

            if (ViewToEdit.Arrangement.HasFlag (ViewArrangement.Overlapped))
            {
                ViewToEdit.ShadowStyle = ShadowStyle.Transparent;
                ViewToEdit.ColorScheme = Colors.ColorSchemes ["Toplevel"];
            }
            else
            {
                ViewToEdit.ShadowStyle = ShadowStyle.None;
                ViewToEdit.ColorScheme = ViewToEdit!.SuperView!.ColorScheme;
            }

            if (ViewToEdit.Arrangement.HasFlag (ViewArrangement.Movable))
            {
                ViewToEdit.BorderStyle = LineStyle.Double;
            }
            else
            {
                ViewToEdit.BorderStyle = LineStyle.Single;
            }
        }
    }
}
