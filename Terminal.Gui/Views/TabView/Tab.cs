#nullable enable
using System.Net.Security;

namespace Terminal.Gui;

/// <summary>A single tab in a <see cref="TabView"/>.</summary>
public class Tab : View
{
    private string? _displayText;

    /// <summary>Creates a new unnamed tab with no controls inside.</summary>
    public Tab ()
    {
        BorderStyle = LineStyle.Single;
        CanFocus = true;
        TabStop = TabBehavior.TabStop;
        Width = Dim.Auto (DimAutoStyle.Text);
        SuperViewRendersLineCanvas = true;
    }

    /// <summary>The text to display in a <see cref="TabView"/>.</summary>
    /// <value></value>
    public string DisplayText
    {
        get => _displayText ?? "Unnamed";
        set
        {
            _displayText = value;
            SetNeedsDraw ();
        }
    }

    /// <summary>The View that will be made visible in the <see cref="TabView"/> content area when the tab is selected.</summary>
    /// <value></value>
    public View? View { get; set; }
}
