namespace Terminal.Gui;

/// <summary>Represents an option in a <see cref="RangeSlider{T}"/> .</summary>
/// <typeparam name="T">Data type of the option.</typeparam>
public class RangeSliderOption<T>
{
    /// <summary>Creates a new empty instance of the <see cref="RangeSliderOption{T}"/> class.</summary>
    public RangeSliderOption () { }

    /// <summary>Creates a new instance of the <see cref="RangeSliderOption{T}"/> class with values for each property.</summary>
    public RangeSliderOption (string legend, Rune legendAbbr, T data)
    {
        Legend = legend;
        LegendAbbr = legendAbbr;
        Data = data;
    }

    /// <summary>Event fired when an option has changed.</summary>
    public event EventHandler<RangeSliderOptionEventArgs> Changed;

    /// <summary>Custom data of the option.</summary>
    public T Data { get; set; }

    /// <summary>Legend of the option.</summary>
    public string Legend { get; set; }

    /// <summary>
    ///     Abbreviation of the Legend. When the <see cref="RangeSlider{T}.MinimumInnerSpacing"/> too small to fit
    ///     <see cref="Legend"/>.
    /// </summary>
    public Rune LegendAbbr { get; set; }

    /// <summary>Event Raised when this option is set.</summary>
    public event EventHandler<RangeSliderOptionEventArgs> Set;

    /// <summary>Creates a human-readable string that represents this <see cref="RangeSliderOption{T}"/>.</summary>
    public override string ToString () { return "{Legend=" + Legend + ", LegendAbbr=" + LegendAbbr + ", Data=" + Data + "}"; }

    /// <summary>Event Raised when this option is unset.</summary>
    public event EventHandler<RangeSliderOptionEventArgs> UnSet;

    /// <summary>To Raise the <see cref="Changed"/> event from the Slider.</summary>
    internal void OnChanged (bool isSet) { Changed?.Invoke (this, new (isSet)); }

    /// <summary>To Raise the <see cref="Set"/> event from the Slider.</summary>
    internal void OnSet () { Set?.Invoke (this, new (true)); }

    /// <summary>To Raise the <see cref="UnSet"/> event from the Slider.</summary>
    internal void OnUnSet () { UnSet?.Invoke (this, new (false)); }
}