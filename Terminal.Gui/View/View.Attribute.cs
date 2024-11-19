﻿#nullable enable
namespace Terminal.Gui;

public partial class View
{
    // TODO: Rename "Color"->"Attribute" given we'll soon have non-color information in Attributes?
    // TODO: See https://github.com/gui-cs/Terminal.Gui/issues/457

    #region ColorScheme

    private ColorScheme? _colorScheme;

    /// <summary>The color scheme for this view, if it is not defined, it returns the <see cref="SuperView"/>'s color scheme.</summary>
    public virtual ColorScheme? ColorScheme
    {
        get => _colorScheme ?? SuperView?.ColorScheme;
        set
        {
            if (_colorScheme == value)
            {
                return;
            }

            _colorScheme = value;

            // BUGBUG: This should be in Border.cs somehow
            if (Border is { } && Border.LineStyle != LineStyle.None && Border.ColorScheme is { })
            {
                Border.ColorScheme = _colorScheme;
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="ColorScheme.Focus"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetFocusColor ()
    {
        ColorScheme? cs = ColorScheme ?? new ();

        return Enabled ? GetColor (cs.Focus) : cs.Disabled;
    }

    /// <summary>Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="ColorScheme.Focus"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetHotFocusColor ()
    {
        ColorScheme? cs = ColorScheme ?? new ();

        return Enabled ? GetColor (cs.HotFocus) : cs.Disabled;
    }

    /// <summary>Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="ColorScheme.HotNormal"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetHotNormalColor ()
    {
        ColorScheme? cs = ColorScheme ?? new ();

        return Enabled ? GetColor (cs.HotNormal) : cs.Disabled;
    }

    /// <summary>Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="ColorScheme.Normal"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetNormalColor ()
    {
        ColorScheme? cs = ColorScheme ?? new ();

        Attribute disabled = new (cs.Disabled.Foreground, cs.Disabled.Background);

        if (Diagnostics.HasFlag (ViewDiagnosticFlags.Hover) && _hovering)
        {
            disabled = new (disabled.Foreground.GetDarkerColor (), disabled.Background.GetDarkerColor ());
        }

        return Enabled ? GetColor (cs.Normal) : disabled;
    }

    private Attribute GetColor (Attribute inputAttribute)
    {
        Attribute attr = inputAttribute;

        if (Diagnostics.HasFlag (ViewDiagnosticFlags.Hover) && _hovering)
        {
            attr = new (attr.Foreground.GetDarkerColor (), attr.Background.GetDarkerColor ());
        }

        return attr;
    }

    #endregion ColorScheme

    #region Attribute

    /// <summary>Selects the specified attribute as the attribute to use for future calls to AddRune and AddString.</summary>
    /// <remarks></remarks>
    /// <param name="attribute">THe Attribute to set.</param>
    public Attribute SetAttribute (Attribute attribute) { return Driver?.SetAttribute (attribute) ?? Attribute.Default; }

    /// <summary>Gets the current <see cref="Attribute"/>.</summary>
    /// <returns>The current attribute.</returns>
    public Attribute GetAttribute () { return Driver?.GetAttribute () ?? Attribute.Default; }

    #endregion Attribute
}
