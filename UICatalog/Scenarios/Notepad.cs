﻿using System.IO;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Notepad", "Multi-tab text editor using the TabView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TabView")]
[ScenarioCategory ("TextView")]
public class Notepad : Scenario {
    private int _numbeOfNewTabs = 1;
    private StatusItem _lenStatusItem;
    private TabView _focusedTabView;
    private TabView _tabView;

    // Don't create a Window, just return the top-level view
    public override void Init () {
        Application.Init ();
        Application.Top.ColorScheme = Colors.ColorSchemes["Base"];
    }

    public void Save () { Save (_focusedTabView, _focusedTabView.SelectedTab); }

    public void Save (TabView tabViewToSave, Tab tabToSave) {
        var tab = tabToSave as OpenedFile;

        if (tab == null) {
            return;
        }

        if (tab.File == null) {
            SaveAs ();
        }

        tab.Save ();
        tabViewToSave.SetNeedsDisplay ();
    }

    public bool SaveAs () {
        var tab = _focusedTabView.SelectedTab as OpenedFile;

        if (tab == null) {
            return false;
        }

        var fd = new SaveDialog ();
        Application.Run (fd);

        if (string.IsNullOrWhiteSpace (fd.Path)) {
            return false;
        }

        if (fd.Canceled) {
            return false;
        }

        tab.File = new FileInfo (fd.Path);
        tab.Text = fd.FileName;
        tab.Save ();

        return true;
    }

    public override void Setup () {
        var menu = new MenuBar {
            Menus = [
                        new MenuBarItem (
                            "_File",
                            new MenuItem[] {
                                new (
                                    "_New",
                                    "",
                                    () => New (),
                                    null,
                                    null,
                                    KeyCode.N | KeyCode.CtrlMask
                                              | KeyCode.AltMask
                                ),
                                new ("_Open", "", () => Open ()),
                                new ("_Save", "", () => Save ()),
                                new ("Save _As", "", () => SaveAs ()),
                                new ("_Close", "", () => Close ()),
                                new ("_Quit", "", () => Quit ())
                            }
                        ),
                        new MenuBarItem (
                            "_About",
                            "",
                            () => MessageBox.Query ("Notepad", "About Notepad...", "Ok")
                        )
                    ]
        };
        Application.Top.Add (menu);

        _tabView = CreateNewTabView ();

        _tabView.Style.ShowBorder = true;
        _tabView.ApplyStyleChanges ();

        // Start with only a single view but support splitting to show side by side
        var split = new TileView (1) { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };
        split.Tiles.ElementAt (0).ContentView.Add (_tabView);
        split.LineStyle = LineStyle.None;

        Application.Top.Add (split);

        _lenStatusItem = new StatusItem (KeyCode.CharMask, "Len: ", null);
        var statusBar = new StatusBar (
            new[] {
                new (
                    Application.QuitKey,
                    $"{Application.QuitKey} to Quit",
                    () => Quit ()
                ),

                // These shortcut keys don't seem to work correctly in linux 
                //new StatusItem(Key.CtrlMask | Key.N, "~^O~ Open", () => Open()),
                //new StatusItem(Key.CtrlMask | Key.N, "~^N~ New", () => New()),

                new (KeyCode.CtrlMask | KeyCode.S, "~^S~ Save", () => Save ()),
                new (KeyCode.CtrlMask | KeyCode.W, "~^W~ Close", () => Close ()),
                _lenStatusItem
            }
        );
        _focusedTabView = _tabView;
        _tabView.SelectedTabChanged += TabView_SelectedTabChanged;
        _tabView.Enter += (s, e) => _focusedTabView = _tabView;

        Application.Top.Add (statusBar);
        Application.Top.Ready += (s, e) => New ();
    }

    private void Close () { Close (_focusedTabView, _focusedTabView.SelectedTab); }

    private void Close (TabView tv, Tab tabToClose) {
        var tab = tabToClose as OpenedFile;

        if (tab == null) {
            return;
        }

        _focusedTabView = tv;

        if (tab.UnsavedChanges) {
            int result = MessageBox.Query (
                "Save Changes",
                $"Save changes to {tab.Text.TrimEnd ('*')}",
                "Yes",
                "No",
                "Cancel"
            );

            if (result == -1 || result == 2) {
                // user cancelled
                return;
            }

            if (result == 0) {
                if (tab.File == null) {
                    SaveAs ();
                } else {
                    tab.Save ();
                }
            }
        }

        // close and dispose the tab
        tv.RemoveTab (tab);
        tab.View.Dispose ();
        _focusedTabView = tv;

        if (tv.Tabs.Count == 0) {
            var split = (TileView)tv.SuperView.SuperView;

            // if it is the last TabView on screen don't drop it or we will
            // be unable to open new docs!
            if (split.IsRootTileView () && split.Tiles.Count == 1) {
                return;
            }

            int tileIndex = split.IndexOf (tv);
            split.RemoveTile (tileIndex);

            if (split.Tiles.Count == 0) {
                TileView parent = split.GetParentTileView ();

                if (parent == null) {
                    return;
                }

                int idx = parent.IndexOf (split);

                if (idx == -1) {
                    return;
                }

                parent.RemoveTile (idx);
            }
        }
    }

    private TabView CreateNewTabView () {
        var tv = new TabView { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

        tv.TabClicked += TabView_TabClicked;
        tv.SelectedTabChanged += TabView_SelectedTabChanged;
        tv.Enter += (s, e) => _focusedTabView = tv;

        return tv;
    }

    private void New () { Open (null, $"new {_numbeOfNewTabs++}"); }

    private void Open () {
        var open = new OpenDialog { Title = "Open", AllowsMultipleSelection = true };

        Application.Run (open);

        if (!open.Canceled) {
            foreach (string path in open.FilePaths) {
                if (string.IsNullOrEmpty (path) || !File.Exists (path)) {
                    return;
                }

                // TODO should open in focused TabView
                Open (new FileInfo (path), Path.GetFileName (path));
            }
        }
    }

    /// <summary>Creates a new tab with initial text</summary>
    /// <param name="fileInfo">File that was read or null if a new blank document</param>
    private void Open (FileInfo fileInfo, string tabName) {
        var tab = new OpenedFile { DisplayText = tabName, File = fileInfo };
        tab.View = tab.CreateTextView (fileInfo);
        tab.SavedText = tab.View.Text;
        tab.RegisterTextViewEvents (_focusedTabView);

        _focusedTabView.AddTab (tab, true);
    }

    private void Quit () { Application.RequestStop (); }

    private void Split (int offset, Orientation orientation, TabView sender, OpenedFile tab) {
        var split = (TileView)sender.SuperView.SuperView;
        int tileIndex = split.IndexOf (sender);

        if (tileIndex == -1) {
            return;
        }

        if (orientation != split.Orientation) {
            split.TrySplitTile (tileIndex, 1, out split);
            split.Orientation = orientation;
            tileIndex = 0;
        }

        Tile newTile = split.InsertTile (tileIndex + offset);
        TabView newTabView = CreateNewTabView ();
        tab.CloneTo (newTabView);
        newTile.ContentView.Add (newTabView);

        newTabView.EnsureFocus ();
        newTabView.FocusFirst ();
        newTabView.FocusNext ();
    }

    private void SplitDown (TabView sender, OpenedFile tab) { Split (1, Orientation.Horizontal, sender, tab); }
    private void SplitLeft (TabView sender, OpenedFile tab) { Split (0, Orientation.Vertical, sender, tab); }
    private void SplitRight (TabView sender, OpenedFile tab) { Split (1, Orientation.Vertical, sender, tab); }
    private void SplitUp (TabView sender, OpenedFile tab) { Split (0, Orientation.Horizontal, sender, tab); }

    private void TabView_SelectedTabChanged (object sender, TabChangedEventArgs e) {
        _lenStatusItem.Title = $"Len:{e.NewTab?.View?.Text?.Length ?? 0}";
        e.NewTab?.View?.SetFocus ();
    }

    private void TabView_TabClicked (object sender, TabMouseEventArgs e) {
        // we are only interested in right clicks
        if (!e.MouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)) {
            return;
        }

        MenuBarItem items;

        if (e.Tab == null) {
            items = new MenuBarItem (
                new MenuItem[] { new ("Open", "", () => Open ()) }
            );
        } else {
            var tv = (TabView)sender;
            var t = (OpenedFile)e.Tab;

            items = new MenuBarItem (
                new MenuItem[] {
                    new ("Save", "", () => Save (_focusedTabView, e.Tab)),
                    new ("Close", "", () => Close (tv, e.Tab)),
                    null,
                    new ("Split Up", "", () => SplitUp (tv, t)),
                    new ("Split Down", "", () => SplitDown (tv, t)),
                    new ("Split Right", "", () => SplitRight (tv, t)),
                    new ("Split Left", "", () => SplitLeft (tv, t))
                }
            );
        }

        ((View)sender).BoundsToScreen (e.MouseEvent.X, e.MouseEvent.Y, out int screenX, out int screenY);

        var contextMenu = new ContextMenu { Position = new Point (screenX, screenY), MenuItems = items };

        contextMenu.Show ();
        e.MouseEvent.Handled = true;
    }

    private class OpenedFile : Tab {
        public bool UnsavedChanges => !string.Equals (SavedText, View.Text);
        public FileInfo File { get; set; }

        /// <summary>The text of the tab the last time it was saved</summary>
        /// <value></value>
        public string SavedText { get; set; }

        public OpenedFile CloneTo (TabView other) {
            var newTab = new OpenedFile { DisplayText = base.Text, File = File };
            newTab.View = newTab.CreateTextView (newTab.File);
            newTab.SavedText = newTab.View.Text;
            newTab.RegisterTextViewEvents (other);
            other.AddTab (newTab, true);

            return newTab;
        }

        public View CreateTextView (FileInfo file) {
            var initialText = string.Empty;
            if (file != null && file.Exists) {
                initialText = System.IO.File.ReadAllText (file.FullName);
            }

            return new TextView {
                X = 0,
                Y = 0,
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                Text = initialText,
                AllowsTab = false
            };
        }

        public void RegisterTextViewEvents (TabView parent) {
            var textView = (TextView)View;

            // when user makes changes rename tab to indicate unsaved
            textView.KeyUp += (s, k) => {
                // if current text doesn't match saved text
                bool areDiff = UnsavedChanges;

                if (areDiff) {
                    if (!Text.EndsWith ('*')) {
                        Text = Text + '*';
                        parent.SetNeedsDisplay ();
                    }
                } else {
                    if (Text.EndsWith ('*')) {
                        Text = Text.TrimEnd ('*');
                        parent.SetNeedsDisplay ();
                    }
                }
            };
        }

        internal void Save () {
            string newText = View.Text;

            if (File is null || string.IsNullOrWhiteSpace (File.FullName)) {
                return;
            }

            System.IO.File.WriteAllText (File.FullName, newText);
            SavedText = newText;

            Text = Text.TrimEnd ('*');
        }
    }
}
