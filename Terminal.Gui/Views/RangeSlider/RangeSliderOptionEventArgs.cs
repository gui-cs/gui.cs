namespace Terminal.Gui;

/// <summary><see cref="EventArgs"/> for <see cref="Slider{T}"/> <see cref="RangeSliderOption{T}"/> events.</summary>
public class RangeSliderOptionEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="RangeSliderOptionEventArgs"/></summary>
    /// <param name="isSet"> indicates whether the option is set</param>
    public RangeSliderOptionEventArgs (bool isSet) { IsSet = isSet; }

    /// <summary>Gets whether the option is set or not.</summary>
    public bool IsSet { get; }
}