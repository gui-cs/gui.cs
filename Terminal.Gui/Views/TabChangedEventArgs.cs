namespace Terminal.Gui;

/// <summary>Describes a change in <see cref="TabView.SelectedTabIndex"/></summary>
public class TabChangedEventArgs : EventArgs
{
    /// <summary>Documents a tab change</summary>
    /// <param name="oldTabIndex"></param>
    /// <param name="newTabIndex"></param>
    public TabChangedEventArgs (int? oldTabIndex, int? newTabIndex)
    {
        OldTabIndex = oldTabIndex;
        NewTabIndex = newTabIndex;
    }

    /// <summary>The currently selected tab.</summary>
    public int? NewTabIndex { get; }

    /// <summary>The previously selected tab.</summary>
    public int? OldTabIndex{ get; }
}
