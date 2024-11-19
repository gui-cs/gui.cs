#nullable enable
namespace Terminal.Gui;

/// <summary>Enables diagnostic functions for <see cref="View"/>.</summary>
[Flags]
public enum ViewDiagnosticFlags : uint
{
    /// <summary>All diagnostics off</summary>
    Off = 0b_0000_0000,

    /// <summary>
    ///     When enabled, <see cref="Adornment"/> will draw a ruler in the Thickness. See <see cref="Adornment.Diagnostics"/>.
    /// </summary>
    Ruler = 0b_0000_0001,

    /// <summary>
    ///     When enabled, <see cref="Adornment"/> will draw the first letter of the Adornment name ('M', 'B', or 'P')
    ///     in the Thickness. See <see cref="Adornment.Diagnostics"/>.
    /// </summary>
    Thickness = 0b_0000_0010,

    /// <summary>
    ///     When enabled the View's colors will be darker when the mouse is hovering over the View (See <see cref="View.MouseEnter"/> and <see cref="View.MouseLeave"/>.
    /// </summary>
    Hover = 0b_0000_00100,

    /// <summary>
    ///     When enabled a draw indicator will be shown; the indicator will change each time the View's Draw method is called with NeedsDraw set to true.
    /// </summary>
    DrawIndicator = 0b_0000_01000,
}

public partial class View
{
    /// <summary>Gets or sets whether diagnostic information will be drawn. This is a bit-field of <see cref="ViewDiagnosticFlags"/>.e <see cref="View"/> diagnostics.</summary>
    /// <remarks>
    /// <para>
    ///     <see cref="Adornment.Diagnostics"/> gets set to this property by default, enabling <see cref="ViewDiagnosticFlags.Ruler"/> and <see cref="ViewDiagnosticFlags.Thickness"/>.
    /// </para>
    /// <para>
    ///     <see cref="ViewDiagnosticFlags.Hover"/> and <see cref="ViewDiagnosticFlags.DrawIndicator"/> are enabled for all Views independently of Adornments.
    /// </para>
    /// </remarks>
    public static ViewDiagnosticFlags Diagnostics { get; set; }
}
