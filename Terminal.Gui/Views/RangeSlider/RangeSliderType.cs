namespace Terminal.Gui;

/// <summary><see cref="RangeSlider{T}"/>  Types</summary>
public enum RangeSliderType
{
    /// <summary>
    ///     <code>
    /// ├─┼─┼─┼─┼─█─┼─┼─┼─┼─┼─┼─┤
    /// </code>
    /// </summary>
    Single,

    /// <summary>
    ///     <code>
    /// ├─┼─█─┼─┼─█─┼─┼─┼─┼─█─┼─┤
    /// </code>
    /// </summary>
    Multiple,

    /// <summary>
    ///     <code>
    /// ├▒▒▒▒▒▒▒▒▒█─┼─┼─┼─┼─┼─┼─┤
    /// </code>
    /// </summary>
    LeftRange,

    /// <summary>
    ///     <code>
    /// ├─┼─┼─┼─┼─█▒▒▒▒▒▒▒▒▒▒▒▒▒┤
    /// </code>
    /// </summary>
    RightRange,

    /// <summary>
    ///     <code>
    /// ├─┼─┼─┼─┼─█▒▒▒▒▒▒▒█─┼─┼─┤
    /// </code>
    /// </summary>
    Range
}