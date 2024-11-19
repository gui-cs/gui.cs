namespace Terminal.Gui;

/// <summary><see cref="RangeSlider{T}"/> Style</summary>
public class RangeSliderStyle
{
    /// <summary>Constructs a new instance.</summary>
    public RangeSliderStyle () { LegendAttributes = new (); }

    /// <summary>The glyph and the attribute to indicate mouse dragging.</summary>
    public Cell DragChar { get; set; }

    /// <summary>The glyph and the attribute used for empty spaces on the slider.</summary>
    public Cell EmptyChar { get; set; }

    /// <summary>The glyph and the attribute used for the end of ranges on the slider.</summary>
    public Cell EndRangeChar { get; set; }

    /// <summary>Legend attributes</summary>
    public RangeSliderAttributes LegendAttributes { get; set; }

    /// <summary>The glyph and the attribute used for each option (tick) on the slider.</summary>
    public Cell OptionChar { get; set; }

    /// <summary>The glyph and the attribute used for filling in ranges on the slider.</summary>
    public Cell RangeChar { get; set; }

    /// <summary>The glyph and the attribute used for options (ticks) that are set on the slider.</summary>
    public Cell SetChar { get; set; }

    /// <summary>The glyph and the attribute used for spaces between options (ticks) on the slider.</summary>
    public Cell SpaceChar { get; set; }

    /// <summary>The glyph and the attribute used for the start of ranges on the slider.</summary>
    public Cell StartRangeChar { get; set; }
}