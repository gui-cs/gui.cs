
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data;
using NStack;
using Terminal.Gui.Trees;
using static System.Environment;
using System.Text.RegularExpressions;
using static Terminal.Gui.OpenDialog;
using System.Collections.ObjectModel;

namespace Terminal.Gui {

	/// <summary>
	/// Modal dialog for selecting files/directories.  Has auto-complete and expandable
	/// navigation pane (Recent, Root drives etc).
	/// </summary>
	public class FileDialog2 : Dialog {

		/// <summary>
		/// Determine which <see cref="System.IO.FileSystemInfo"/> type to open.
		/// Defaults to <see cref="OpenMode.Mixed"/> (i.e. <see cref="DirectoryInfo"/> or
		/// <see cref="FileInfo"/>).
		/// </summary>
		public OpenMode OpenMode { get; set; } = OpenMode.Mixed;

		/// <summary>
		/// The currently selected path in the dialog.  This is the result that should
		/// be used if <see cref="AllowsMultipleSelection"/> is off and <see cref="Canceled"/>
		/// is true.
		/// </summary>
		public string Path { get => tbPath.Text.ToString (); set => tbPath.Text = value; }

		/// <summary>
		/// True to allow the user to select multiple existing files/directories
		/// </summary>
		public bool AllowsMultipleSelection {
			get => tableView.MultiSelect;
			set => tableView.MultiSelect = value;
		}

		/// <summary>
		/// Collection of file types that the user can/must select.  Only applies
		/// when <see cref="OpenMode"/> is <see cref="OpenMode.File"/>.  See also
		/// <see cref="AllowedTypesIsStrict"/> if you only want to highlight files.
		/// </summary>
		public List<AllowedType> AllowedTypes { get; set; } = new List<AllowedType> ();

		/// <summary>
		/// Gets or sets a value indicating whether <see cref="AllowedTypes"/> is a strict
		/// requirement or simply a recommendation. Defaults to <see langword="true"/> (i.e.
		/// strict).
		/// </summary>
		public bool AllowedTypesIsStrict { get; set; }

		/// <summary>
		/// The UI selected <see cref="AllowedType"/> from combo box. May be null.
		/// </summary>
		private AllowedType currentFilter;

		/// <summary>
		/// True if the <see cref="FileDialog"/> was closed without confirming a selection
		/// </summary>
		public bool Canceled { get; private set; } = true;

		/// <summary>
		/// Returns all files/dialogs selected or an empty collection if 
		/// not <see cref="AllowsMultipleSelection"/> or <see cref="Canceled"/>.
		/// </summary>
		/// <remarks>If selecting only a single file/directory then you should use <see cref="Path"/> instead.</remarks>
		public IReadOnlyList<FileSystemInfo> MultiSelected { get; private set; }

		// TODO : expose these somehow for localization without compromising case/switch statements
		private const string HeaderFilename = "Filename";
		private const string HeaderSize = "Size";
		private const string HeaderModified = "Modified";
		private const string HeaderType = "Type";

		bool pushingState = false;

		private FileDialogState state;

		private TextFieldWithAppendAutocomplete tbPath;

		FileDialogSorter sorter;
		FileDialogHistory history;

		/// <summary>
		/// True to use Utc dates for date modified
		/// </summary>
		public static bool UseUtcDates = false;

		DataTable dtFiles;
		TableView tableView;
		TreeView<object> treeView;
		SplitContainer splitContainer;

		// TODO: refactor somewhere more user friendly
		private static ColorScheme ColorSchemeDirectory;
		private static ColorScheme ColorSchemeDefault;
		private static ColorScheme ColorSchemeImage;

		/// <summary>
		/// ColorScheme to use for entries that are executable or match the users file extension
		/// provided (e.g. if role of dialog is to pick a .csv file)
		/// </summary>
		public static ColorScheme ColorSchemeExeOrRecommended;

		private Button btnOk;
		private Button btnToggleSplitterCollapse;
		private Label lblForward;
		private Label lblBack;
		private Label lblUp;

		private string title;

		private static char [] separators = new [] {
			System.IO.Path.AltDirectorySeparatorChar,
			System.IO.Path.DirectorySeparatorChar
		};


		/// <summary>
		/// Characters to prevent entry into <see cref="tbPath"/>.  Note that this is not using
		/// <see cref="System.IO.Path.GetInvalidFileNameChars"/> because we do want to allow directory
		/// separators, arrow keys etc.
		/// </summary>
		private static char [] badChars = new [] {
			'"','<','>','|','*','?',
		};
		
		/// <summary>
		/// Creates a new instance of the <see cref="FileDialog2"/> class.
		/// </summary>
		public FileDialog2 ()
		{
			// TODO: handle Save File / Folder too
			title = "Open File";

			const int okWidth = 6;

			var lblPath = new Label (">");
			btnOk = new Button ("Ok") {
				X = Pos.AnchorEnd (okWidth)
			};
			btnOk.Clicked += Accept;
			btnOk.KeyPress += (k) => {
				NavigateIf (k, Key.CursorLeft, tbPath);
				NavigateIf (k, Key.CursorDown, tableView);
			};

			lblUp = new Label (Driver.UpArrow.ToString ()) { X = 0, Y = 1 };
			lblUp.Clicked += () => history.Up ();

			lblBack = new Label (Driver.LeftArrow.ToString ()) { X = 2, Y = 1 };
			lblBack.Clicked += () => history.Back ();

			lblForward = new Label (Driver.RightArrow.ToString ()) { X = 3, Y = 1 };
			lblForward.Clicked += () => history.Forward ();
			tbPath = new TextFieldWithAppendAutocomplete {
				X = Pos.Right (lblPath),
				Width = Dim.Fill (okWidth + 1)
			};
			tbPath.KeyPress += (k) => {

				NavigateIf (k, Key.CursorDown, tableView);

				if (tbPath.CursorIsAtEnd ()) {
					NavigateIf (k, Key.CursorRight, btnOk);
				}

				AcceptIf (k, Key.Enter);

				SuppressIfBadChar (k);
			};

			splitContainer = new SplitContainer () {
				X = 0,
				Y = 2,
				Width = Dim.Fill (0),
				Height = Dim.Fill (1),
				SplitterDistance = 30,
			};
			splitContainer.Border.BorderStyle = BorderStyle.None;
			splitContainer.Border.DrawMarginFrame = false;
			splitContainer.Panels [0].Visible = false;

			tableView = new TableView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				FullRowSelect = true,
			};
			tableView.KeyPress += (k) => {
				if (tableView.SelectedRow <= 0) {
					NavigateIf (k, Key.CursorUp, tbPath);
				}

			};

			treeView = new TreeView<object> () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			treeView.TreeBuilder = new FileDialogTreeBuilder ();
			treeView.AspectGetter = (m) => m is DirectoryInfo d ? d.Name : m.ToString ();


			try {
				treeView.AddObjects (
					Environment.GetLogicalDrives ()
					.Select (d =>
						new FileDialogRootTreeNode (d, new DirectoryInfo (d)))
					);

			} catch (Exception) {
				// Cannot get the system disks thats fine
			}


			treeView.AddObjects (
				Enum.GetValues (typeof (SpecialFolder))
				.Cast<SpecialFolder> ()
				.Where (IsValidSpecialFolder)
				.Select (GetTreeNode));

			treeView.SelectionChanged += TreeView_SelectionChanged;

			splitContainer.Panels [0].Add (treeView);
			splitContainer.Panels [1].Add (tableView);

			btnToggleSplitterCollapse = new Button (">>") {
				Y = Pos.AnchorEnd (1),
			};
			btnToggleSplitterCollapse.Clicked += () => {
				var newState = !splitContainer.Panels [0].Visible;
				splitContainer.Panels [0].Visible = newState;
				btnToggleSplitterCollapse.Text = newState ? "<<" : ">>";
			};

			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.ShowVerticalCellLines = false;
			tableView.Style.ShowVerticalHeaderLines = false;
			tableView.Style.AlwaysShowHeaders = true;


			SetupColorSchemes ();

			SetupTableColumns ();

			sorter = new FileDialogSorter (this, tableView);
			history = new FileDialogHistory (this);

			tableView.Table = dtFiles;

			tbPath.TextChanged += (s) => PathChanged ();

			tableView.CellActivated += CellActivate;
			tableView.KeyUp += (k) => k.Handled = this.TableView_KeyUp (k.KeyEvent);
			tableView.SelectedCellChanged += TableView_SelectedCellChanged;
			tableView.ColorScheme = ColorSchemeDefault;

			treeView.ColorScheme = ColorSchemeDefault;
			treeView.KeyDown += (k) => k.Handled = this.TreeView_KeyDown (k.KeyEvent);

			this.AllowsMultipleSelection = false;

			UpdateNavigationVisibility ();

			// Determines tab order
			this.Add (btnOk);
			this.Add (lblUp);
			this.Add (lblBack);
			this.Add (lblForward);
			this.Add (lblPath);
			this.Add (tbPath);
			this.Add (splitContainer);
			Add (btnToggleSplitterCollapse);
		}

		private void SuppressIfBadChar (KeyEventEventArgs k)
		{
			// don't let user type bad letters
			var ch = (char)k.KeyEvent.KeyValue;

			if (badChars.Contains (ch)) {
				k.Handled = true;
			}
		}

		private bool TreeView_KeyDown (KeyEvent keyEvent)
		{
			if (treeView.HasFocus && separators.Contains ((char)keyEvent.KeyValue)) {
				tbPath.FocusFirst ();

				// let that keystroke go through on the tbPath instead
				return true;
			}

			return false;
		}

		private void AcceptIf (KeyEventEventArgs keyEvent, Key isKey)
		{
			if (!keyEvent.Handled && keyEvent.KeyEvent.Key == isKey) {
				keyEvent.Handled = true;
				Accept ();
			}
		}

		private void Accept (IEnumerable<FileSystemInfoStats> toMultiAccept)
		{
			if (!AllowsMultipleSelection) {
				return;
			}

			MultiSelected = toMultiAccept.Select (s => s.FileSystemInfo).ToList ().AsReadOnly ();
			tbPath.Text = MultiSelected.Count == 1 ? MultiSelected [0].FullName : "";
			Canceled = false;
			Application.RequestStop ();
		}
		private void Accept (FileInfo f)
		{
			if (!IsCompatibleWithOpenMode (f)) {
				return;
			}

			tbPath.Text = f.FullName;
			Canceled = false;
			Application.RequestStop ();
		}

		private void Accept ()
		{
			// if an autocomplete is showing
			if (tbPath.AcceptSelectionIfAny ()) {

				// enter just accepts it
				return;
			}

			if (!IsCompatibleWithOpenMode (tbPath.Text.ToString ())) {
				return;
			}


			Canceled = false;
			Application.RequestStop ();
		}

		private void NavigateIf (KeyEventEventArgs keyEvent, Key isKey, View to)
		{
			if (!keyEvent.Handled && keyEvent.KeyEvent.Key == isKey) {

				to.FocusFirst ();
				keyEvent.Handled = true;
			}
		}

		private void TreeView_SelectionChanged (object sender, SelectionChangedEventArgs<object> e)
		{
			if (e.NewValue == null) {
				return;
			}

			tbPath.Text = FileDialogTreeBuilder.NodeToDirectory (e.NewValue).FullName;
		}

		private bool IsValidSpecialFolder (SpecialFolder arg)
		{
			try {
				var path = Environment.GetFolderPath (arg);
				return !string.IsNullOrWhiteSpace (path) && Directory.Exists (path);
			} catch (Exception) {

				return false;
			}
		}
		private FileDialogRootTreeNode GetTreeNode (SpecialFolder arg)
		{
			return new FileDialogRootTreeNode (
				arg.ToString (),
				new DirectoryInfo (Environment.GetFolderPath (arg)));
		}

		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			base.Redraw (bounds);

			Move (1, 0, false);

			var padding = ((bounds.Width - this.title.Sum (c => Rune.ColumnWidth (c))) / 2) - 1;

			padding = Math.Min (bounds.Width, padding);
			padding = Math.Max (0, padding);

			Driver.SetAttribute (
			    new Attribute (ColorScheme.Normal.Foreground, ColorScheme.Normal.Background));

			Driver.AddStr (ustring.Make (Enumerable.Repeat (Driver.HDLine, padding)));

			Driver.SetAttribute (
			    new Attribute (ColorScheme.Normal.Foreground, ColorScheme.Normal.Background));
			Driver.AddStr (this.title);

			Driver.SetAttribute (
			    new Attribute (ColorScheme.Normal.Foreground, ColorScheme.Normal.Background));
			Driver.AddStr (ustring.Make (Enumerable.Repeat (Driver.HDLine, padding)));
		}

		private void UpdateNavigationVisibility ()
		{
			lblBack.Visible = history.CanBack ();
			lblForward.Visible = history.CanForward ();
			lblUp.Visible = history.CanUp ();
		}

		private void TableView_SelectedCellChanged (TableView.SelectedCellChangedEventArgs obj)
		{
			if (!tableView.HasFocus || obj.NewRow == -1 || obj.Table.Rows.Count == 0) {
				return;
			}

			if (tableView.MultiSelect && tableView.MultiSelectedRegions.Any ()) {
				return;
			}

			var stats = RowToStats (obj.NewRow);

			if (stats == null) {
				return;
			}

			try {
				pushingState = true;

				tbPath.SetTextTo (stats.FileSystemInfo);
				state?.SetSelection (stats);
				tbPath.ClearSuggestions ();

			} finally {

				pushingState = false;
			}
		}

		internal class TextFieldWithAppendAutocomplete : TextField {

			int? currentFragment = null;
			string [] validFragments = new string [0];

			public TextFieldWithAppendAutocomplete ()
			{
				KeyPress += (k) => {
					var key = k.KeyEvent.Key;
					if (key == Key.Tab) {
						k.Handled = AcceptSelectionIfAny ();
					} else
					if (key == Key.CursorUp) {
						k.Handled = CycleSuggestion (1);
					} else
					if (key == Key.CursorDown) {
						k.Handled = CycleSuggestion (-1);
					}
				};

				ColorScheme = new ColorScheme {
					Normal = new Attribute (Color.White, Color.Black),
					HotNormal = new Attribute (Color.White, Color.Black),
					Focus = new Attribute (Color.White, Color.Black),
					HotFocus = new Attribute (Color.White, Color.Black),
				};
			}

			private bool CycleSuggestion (int direction)
			{
				if (currentFragment == null || validFragments.Length <= 1) {
					return false;
				}

				currentFragment = (currentFragment + direction) % validFragments.Length;

				if (currentFragment < 0) {
					currentFragment = validFragments.Length - 1;
				}
				SetNeedsDisplay ();
				return true;
			}

			public override void Redraw (Rect bounds)
			{
				base.Redraw (bounds);

				if (!MakingSuggestion ()) {
					return;
				}

				// draw it like its selected even though its not
				Driver.SetAttribute (new Attribute (Color.Black, Color.White));
				Move (Text.Length, 0);
				Driver.AddStr (validFragments [currentFragment.Value]);
			}

			/// <summary>
			/// Returns true if there is a suggestion that can be made and the control
			/// is in a state where user would expect to see auto-complete (i.e. focused and 
			/// cursor in right place).
			/// </summary>
			/// <returns></returns>
			private bool MakingSuggestion ()
			{
				return currentFragment != null && HasFocus && CursorIsAtEnd ();
			}

			/// <summary>
			/// Accepts the current autocomplete suggestion displaying in the text box.
			/// Returns true if a valid suggestion was being rendered and acceptable or
			/// false if no suggestion was showing.
			/// </summary>
			/// <returns></returns>
			internal bool AcceptSelectionIfAny ()
			{
				if (MakingSuggestion ()) {
					Text += validFragments [currentFragment.Value];
					MoveCursorToEnd ();

					ClearSuggestions ();
					return true;
				}

				return false;
			}

			internal void MoveCursorToEnd ()
			{
				ClearAllSelection ();
				CursorPosition = Text.Length;
			}

			internal void GenerateSuggestions (FileDialogState state, params string [] suggestions)
			{
				if (!CursorIsAtEnd ()) {
					return;
				}

				var path = Text.ToString ();
				var last = path.LastIndexOfAny (FileDialog2.separators);

				if (last == -1 || suggestions.Length == 0 || last >= path.Length - 1) {
					currentFragment = null;
					return;
				}

				var term = path.Substring (last + 1);

				if (term.Equals (state?.Directory?.Name)) {
					ClearSuggestions ();
					return;
				}

				// TODO: Be case insensitive on Windows
				var validSuggestions = suggestions
					.Where (s => s.StartsWith (term))
					.OrderBy (m => m.Length)
					.ToArray ();


				// nothing to suggest 
				if (validSuggestions.Length == 0 || validSuggestions [0].Length == term.Length) {
					ClearSuggestions ();
					return;
				}

				validFragments = validSuggestions.Select (f => f.Substring (term.Length)).ToArray ();
				currentFragment = 0;
			}

			public void ClearSuggestions ()
			{
				currentFragment = null;
				validFragments = new string [0];
				SetNeedsDisplay ();
			}

			internal void GenerateSuggestions (FileDialogState state)
			{
				if (state == null) {
					return;
				}

				var suggestions = state.Children.Select (
					e => e.FileSystemInfo is DirectoryInfo d
						? d.Name + System.IO.Path.DirectorySeparatorChar
						: e.FileSystemInfo.Name)
					.ToArray ();

				GenerateSuggestions (state, suggestions);
			}

			internal void SetTextTo (FileSystemInfo fileSystemInfo)
			{
				var newText = fileSystemInfo.FullName;
				if (fileSystemInfo is DirectoryInfo) {
					newText += System.IO.Path.DirectorySeparatorChar;
				}
				Text = newText;
				MoveCursorToEnd ();
			}

			internal bool CursorIsAtEnd ()
			{
				return CursorPosition == Text.Length;
			}
		}

		/// <inheritdoc/>
		public override void OnLoaded ()
		{
			base.OnLoaded ();

			// if filtering on file type is configured then create the ComboBox and establish
			// initial filtering by extension(s)
			if (AllowedTypes.Any ()) {

				currentFilter = AllowedTypes [0];
				var allowed = AllowedTypes.ToList ();

				if (!AllowedTypesIsStrict) {
					allowed.Insert (0, AllowedType.Any);
				}

				// +2 to allow space for dropdown arrow
				var width = AllowedTypes.Max (a => a.ToString ().Length) + 2;

				var combo = new ComboBox (allowed) {
					Width = width,
					ReadOnly = true,
					Y = 1,
					X = Pos.AnchorEnd (width),
					Height = allowed.Count + 1,
					SelectedItem = AllowedTypesIsStrict ? 0 : 1
				};

				combo.SelectedItemChanged += (e) => Combo_SelectedItemChanged (combo, e);

				Add (combo);
				LayoutSubviews ();
			}

			// if no path has been provided
			if (tbPath.Text.Length <= 0) {
				tbPath.Text = Environment.CurrentDirectory;
			}

			// to streamline user experience and allow direct typing of paths
			// with zero navigation we start with focus in the text box and any
			// default/current path fully selected and ready to be overwritten
			tbPath.FocusFirst ();
			tbPath.SelectAll ();

		}

		private void Combo_SelectedItemChanged (ComboBox combo, ListViewItemEventArgs obj)
		{
			var allow = combo.Source.ToList () [obj.Item] as AllowedType;
			currentFilter = allow == null || allow.IsAny ? null : allow;

			tbPath.ClearAllSelection ();
			tbPath.ClearSuggestions ();

			if (state != null) {
				state.RefreshChildren (this);
				WriteStateToTableView ();
			}
		}

		private bool TableView_KeyUp (KeyEvent keyEvent)
		{
			if (keyEvent.Key == Key.Backspace) {
				return history.Back ();
			}
			if (keyEvent.Key == (Key.ShiftMask | Key.Backspace)) {
				return history.Forward ();
			}

			return false;
		}


		private void SetupColorSchemes ()
		{
			if (ColorSchemeDirectory != null) {
				return;
			}
			ColorSchemeDirectory = new ColorScheme {
				Normal = Driver.MakeAttribute (Color.Blue, Color.Black),
				HotNormal = Driver.MakeAttribute (Color.Blue, Color.Black),
				Focus = Driver.MakeAttribute (Color.Black, Color.Blue),
				HotFocus = Driver.MakeAttribute (Color.Black, Color.Blue),

			};

			ColorSchemeDefault = new ColorScheme {
				Normal = Driver.MakeAttribute (Color.White, Color.Black),
				HotNormal = Driver.MakeAttribute (Color.White, Color.Black),
				Focus = Driver.MakeAttribute (Color.Black, Color.White),
				HotFocus = Driver.MakeAttribute (Color.Black, Color.White),
			};
			ColorSchemeImage = new ColorScheme {
				Normal = Driver.MakeAttribute (Color.Magenta, Color.Black),
				HotNormal = Driver.MakeAttribute (Color.Magenta, Color.Black),
				Focus = Driver.MakeAttribute (Color.Black, Color.Magenta),
				HotFocus = Driver.MakeAttribute (Color.Black, Color.Magenta),
			};
			ColorSchemeExeOrRecommended = new ColorScheme {
				Normal = Driver.MakeAttribute (Color.Green, Color.Black),
				HotNormal = Driver.MakeAttribute (Color.Green, Color.Black),
				Focus = Driver.MakeAttribute (Color.Black, Color.Green),
				HotFocus = Driver.MakeAttribute (Color.Black, Color.Green),
			};
		}

		private void SetupTableColumns ()
		{
			dtFiles = new DataTable ();

			var nameStyle = tableView.Style.GetOrCreateColumnStyle (dtFiles.Columns.Add (HeaderFilename, typeof (int)));
			nameStyle.RepresentationGetter = (i) => state?.Children [(int)i].Name ?? "";
			nameStyle.MinWidth = 50;

			var sizeStyle = tableView.Style.GetOrCreateColumnStyle (dtFiles.Columns.Add (HeaderSize, typeof (int)));
			sizeStyle.RepresentationGetter = (i) => state?.Children [(int)i].HumanReadableLength ?? "";
			nameStyle.MinWidth = 10;

			var dateModifiedStyle = tableView.Style.GetOrCreateColumnStyle (dtFiles.Columns.Add (HeaderModified, typeof (int)));
			dateModifiedStyle.RepresentationGetter = (i) => state?.Children [(int)i].DateModified?.ToString () ?? "";
			dateModifiedStyle.MinWidth = 30;

			var typeStyle = tableView.Style.GetOrCreateColumnStyle (dtFiles.Columns.Add (HeaderType, typeof (int)));
			typeStyle.RepresentationGetter = (i) => state?.Children [(int)i].Type ?? "";
			typeStyle.MinWidth = 6;
			tableView.Style.RowColorGetter = ColorGetter;
		}

		private void CellActivate (TableView.CellActivatedEventArgs obj)
		{
			var multi = MultiRowToStats ();
			if (multi.Any ()) {
				if (multi.All (IsCompatibleWithOpenMode)) {
					Accept (multi);
				} else {
					return;
				}
			}


			var stats = RowToStats (obj.Row);


			if (stats.FileSystemInfo is DirectoryInfo d) {
				PushState (d, false);
				return;
			}

			if (stats.FileSystemInfo is FileInfo f) {
				Accept (f);
			}
		}

		private bool IsCompatibleWithOpenMode (FileSystemInfoStats arg)
		{
			// don't let the user select .. thats just going to be confusing
			if (arg.IsParent) {
				return false;
			}

			switch (OpenMode) {
			case OpenMode.Directory: return arg.IsDir ();
			case OpenMode.File: return !arg.IsDir () && IsCompatibleWithOpenMode (arg.FileSystemInfo);
			case OpenMode.Mixed: return true;
			default: throw new ArgumentOutOfRangeException (nameof (OpenMode));
			}
		}

		private bool IsCompatibleWithOpenMode (FileSystemInfo f)
		{
			switch (OpenMode) {
			case OpenMode.Directory: return f is DirectoryInfo;
			case OpenMode.File:
				if (f is FileInfo file) {
					return IsCompatibleWithAllowedExtensions (file);
				}
				return false;
			case OpenMode.Mixed: return true;
			default: throw new ArgumentOutOfRangeException (nameof (OpenMode));
			};
		}

		private bool IsCompatibleWithAllowedExtensions (FileInfo file)
		{
			// no restrictions
			if (!AllowedTypes.Any () || !AllowedTypesIsStrict) {
				return true;
			}
			return MatchesAllowedTypes (file);
		}

		private bool IsCompatibleWithAllowedExtensions (string path)
		{
			// no restrictions
			if (!AllowedTypes.Any () || !AllowedTypesIsStrict) {
				return true;
			}

			var extension = System.IO.Path.GetExtension (path);

			// There is a requirement to have a particular extension and we have none
			if (string.IsNullOrEmpty (extension)) {
				return false;
			}

			return AllowedTypes.Any (t => t.Matches(extension, false));
		}

		/// <summary>
		/// Returns true if any <see cref="AllowedTypes"/> matches <paramref name="file"/>
		/// regardless of <see cref="AllowedTypesIsStrict"/> status.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		private bool MatchesAllowedTypes (FileInfo file)
		{
			return AllowedTypes.Any (t => t.Matches(file.Extension, true));
		}

		private bool IsCompatibleWithOpenMode (string s)
		{
			if (string.IsNullOrWhiteSpace (s)) {
				return false;
			}

			if (!IsCompatibleWithAllowedExtensions (s)) {
				return false;
			}

			switch (OpenMode) {
			case OpenMode.Directory: return !File.Exists (s);
			case OpenMode.File: return !Directory.Exists (s);
			case OpenMode.Mixed: return true;
			default: throw new ArgumentOutOfRangeException (nameof (OpenMode));
			};
		}

		private void PushState (DirectoryInfo d, bool addCurrentStateToHistory, bool setPathText = true)
		{
			// no change of state
			if (d == state?.Directory) {
				return;
			}


			try {
				pushingState = true;

				// push the old state to history
				if (addCurrentStateToHistory) {
					history.Push (state);
				}

				tbPath.ClearSuggestions ();

				if (setPathText) {
					tbPath.Text = d.FullName;
					tbPath.MoveCursorToEnd ();
				}

				state = new FileDialogState (d, this);
				tbPath.GenerateSuggestions (state);

				WriteStateToTableView ();

				history.ClearForward ();
				tableView.RowOffset = 0;
				tableView.SelectedRow = 0;

				SetNeedsDisplay ();
				UpdateNavigationVisibility ();

			} finally {

				pushingState = false;
			}
		}

		private void WriteStateToTableView ()
		{
			if (state == null) {
				return;
			}

			dtFiles.Rows.Clear ();

			for (int i = 0; i < state.Children.Length; i++) {
				BuildRow (i);
			}

			sorter.ApplySort ();
			tableView.Update ();

		}
		private void BuildRow (int idx)
		{
			tableView.Table.Rows.Add (idx, idx, idx, idx);
		}

		private ColorScheme ColorGetter (TableView.RowColorGetterArgs args)
		{
			var stats = RowToStats (args.RowIndex);

			if (stats.IsDir ()) {
				return ColorSchemeDirectory;
			}
			if (stats.IsImage ()) {
				return ColorSchemeImage;
			}
			if (stats.IsExecutable ()) {
				return ColorSchemeExeOrRecommended;
			}
			if (stats.FileSystemInfo is FileInfo f && MatchesAllowedTypes (f)) {
				return ColorSchemeExeOrRecommended;
			}

			return ColorSchemeDefault;
		}

		/// <summary>
		/// If <see cref="TableView.MultiSelect"/> is on and multiple rows are selected
		/// this returns a union of all <see cref="FileSystemInfoStats"/> in the selection.
		/// </summary>
		/// <remarks>Returns an empty collection if there are not at least 2 rows in the selection</remarks>
		/// <returns></returns>
		private IEnumerable<FileSystemInfoStats> MultiRowToStats ()
		{
			var toReturn = new HashSet<FileSystemInfoStats> ();

			if (AllowsMultipleSelection && tableView.MultiSelectedRegions.Any ()) {

				foreach (var p in tableView.GetAllSelectedCells ()) {

					var add = state?.Children [(int)tableView.Table.Rows [p.Y] [0]];
					if (add != null) {
						toReturn.Add (add);
					}
				}
			}

			return toReturn.Count > 1 ? toReturn : Enumerable.Empty<FileSystemInfoStats> ();
		}
		private FileSystemInfoStats RowToStats (int rowIndex)
		{
			return state?.Children [(int)tableView.Table.Rows [rowIndex] [0]];
		}


		private void PathChanged ()
		{
			// avoid re-entry
			if (pushingState) {
				return;
			}

			var path = tbPath.Text?.ToString ();

			if (string.IsNullOrWhiteSpace (path)) {
				SetupAsClear ();
				return;
			}

			var dir = StringToDirectoryInfo (path);

			if (dir.Exists) {
				PushState (dir, true);
			} else
			if (dir.Parent?.Exists ?? false) {
				PushState (dir.Parent, true, false);
			}
		}

		private DirectoryInfo StringToDirectoryInfo (string path)
		{
			// if you pass new DirectoryInfo("C:") you get a weird object
			// where the FullName is in fact the current working directory.
			// really not what most users would expect

			if (Regex.IsMatch (path, "^\\w:$")) {
				return new DirectoryInfo (path + System.IO.Path.DirectorySeparatorChar);
			}

			return new DirectoryInfo (path);
		}

		private void SetupAsDirectory (DirectoryInfo dir)
		{
			// TODO: Scrap this method
			PushState (dir, true);
		}

		private void SetupAsClear ()
		{

		}

		internal class FileSystemInfoStats {

			public FileSystemInfo FileSystemInfo { get; }
			public string HumanReadableLength { get; }
			public long MachineReadableLength { get; }
			public DateTime? DateModified { get; }
			public string Type { get; }

			/// <summary>
			/// True if this instance represents the parent of the current state (i.e. dot dot)
			/// </summary>
			public bool IsParent { get; internal set; }
			public string Name => IsParent ? ".." : FileSystemInfo.Name;

			/*
			* Blue: Directory
			* Green: Executable or recognized data file
			* Cyan (Sky Blue): Symbolic link file
			* Yellow with black background: Device
			* Magenta (Pink): Graphic image file
			* Red: Archive file
			* Red with black background: Broken link
			*/

			public FileSystemInfoStats (FileSystemInfo fsi)
			{
				FileSystemInfo = fsi;

				if (fsi is FileInfo fi) {
					MachineReadableLength = fi.Length;
					HumanReadableLength = GetHumanReadableFileSize (MachineReadableLength);
					DateModified = FileDialog2.UseUtcDates ? File.GetLastWriteTimeUtc (fi.FullName) : File.GetLastWriteTime (fi.FullName);
					Type = fi.Extension;
				} else {
					HumanReadableLength = "";
					Type = "dir";
				}
			}

			static readonly string [] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
			static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPEG", ".JPE", ".BMP", ".GIF", ".PNG" };
			static readonly List<string> ExecutableExtensions = new List<string> { ".EXE", ".BAT" };

			// TODO: is executable;

			public bool IsDir ()
			{
				return Type == "dir";
			}
			public bool IsImage ()
			{
				return this.FileSystemInfo is FileSystemInfo f &&
					ImageExtensions.Contains (f.Extension,
					StringComparer.InvariantCultureIgnoreCase);
			}
			public bool IsExecutable ()
			{
				// TODO: handle linux executable status
				return this.FileSystemInfo is FileSystemInfo f &&
					ExecutableExtensions.Contains (f.Extension,
					StringComparer.InvariantCultureIgnoreCase);
			}

			const long byteConversion = 1024;
			public static string GetHumanReadableFileSize (long value)
			{

				if (value < 0) { return "-" + GetHumanReadableFileSize (-value); }
				if (value == 0) { return "0.0 bytes"; }

				int mag = (int)Math.Log (value, byteConversion);
				double adjustedSize = (value / Math.Pow (1000, mag));


				return string.Format ("{0:n2} {1}", adjustedSize, SizeSuffixes [mag]);
			}

			internal object GetOrderByValue (string columnName)
			{
				switch (columnName) {
				case HeaderFilename: return FileSystemInfo.Name;
				case HeaderSize: return MachineReadableLength;
				case HeaderModified: return DateModified;
				case HeaderType: return Type;
				default: throw new ArgumentOutOfRangeException (nameof (columnName));
				}
			}

			internal object GetOrderByDefault ()
			{
				if (IsDir ()) {
					return -1;
				}

				return 100;
			}
		}

		/// <summary>
		/// Describes a requirement on what <see cref="FileInfo"/> can be selected
		/// in a <see cref="FileDialog2"/>.
		/// </summary>
		public class AllowedType {
			/// <summary>
			/// Human readable description for the file type
			/// e.g. "Comma Separated Values"
			/// </summary>
			public string Description { get; set; }

			/// <summary>
			/// Permitted extension(s) (e.g. ".csv")
			/// </summary>
			public string[] Extensions { get; set; }

			public bool IsAny => this == Any;

			public static AllowedType Any = new AllowedType ("Any Files", ".*");

			/// <summary>
			/// Creates a new instance of the <see cref="AllowedType"/> class.
			/// </summary>
			/// <param name="description"></param>
			/// <param name="extension"></param>
			public AllowedType (string description, params string[] extensions)
			{
				if(extensions.Length == 0) {
					throw new ArgumentException ("You must supply at least one extension");
				}

				Description = description;
				Extensions = extensions;
			}

			public override string ToString ()
			{
				return $"{Description} ({string.Join(";",Extensions.Select(e=>'*'+e).ToArray())})";
			}

			internal bool Matches (string extension, bool strict)
			{
				if (IsAny) {
					return !strict;
				}

				return Extensions.Any (e => e.Equals (extension));
			}
		}

		internal class FileDialogState {
			public DirectoryInfo Directory { get; }

			public FileSystemInfoStats [] Children;

			public List<FileSystemInfoStats> selected = new List<FileSystemInfoStats> ();
			public IReadOnlyCollection<FileSystemInfoStats> Selected => selected.AsReadOnly ();

			public FileDialogState (DirectoryInfo dir, FileDialog2 parent)
			{
				Directory = dir;

				RefreshChildren (parent);
			}

			internal void RefreshChildren (FileDialog2 parent)
			{
				var dir = Directory;
				

				try {
					List<FileSystemInfoStats> children;

					// if directories only
					if (parent.OpenMode == OpenMode.Directory) {
						children = dir.GetDirectories ().Select (e => new FileSystemInfoStats (e)).ToList ();
					} else {
						children = dir.GetFileSystemInfos ().Select (e => new FileSystemInfoStats (e)).ToList ();
					}

					// if only allowing specific file types
					if (parent.AllowedTypes.Any () && parent.AllowedTypesIsStrict && parent.OpenMode == OpenMode.File) {

						children = children.Where (
							c => c.IsDir () ||
							(c.FileSystemInfo is FileInfo f && parent.IsCompatibleWithAllowedExtensions (f))
							).ToList ();
					}

					// if theres a UI filter in place too
					if(parent.currentFilter != null) {
						children = children.Where (
								c => c.IsDir () ||
								(c.FileSystemInfo is FileInfo f && parent.currentFilter.Matches (f.Extension,true))
								).ToList ();
					}


					// allow navigating up as '..'
					if (dir.Parent != null) {
						children.Add (new FileSystemInfoStats (dir.Parent) { IsParent = true });
					}

					Children = children.ToArray ();
				} catch (Exception) {
					// Access permissions Exceptions, Dir not exists etc
					Children = new FileSystemInfoStats [0];
				}
			}

			internal void SetSelection (FileSystemInfoStats stats)
			{
				selected.Clear ();
				selected.Add (stats);
			}
		}
		class FileDialogHistory {
			private Stack<FileDialogState> back = new Stack<FileDialogState> ();
			private Stack<FileDialogState> forward = new Stack<FileDialogState> ();
			private FileDialog2 dlg;

			public FileDialogHistory (FileDialog2 dlg)
			{
				this.dlg = dlg;
			}

			public bool Back ()
			{

				DirectoryInfo goTo = null;

				if (CanBack ()) {

					goTo = back.Pop ().Directory;
				} else if (CanUp ()) {
					goTo = dlg.state?.Directory.Parent;
				}

				// nowhere to go
				if (goTo == null) {
					return false;
				}

				forward.Push (dlg.state);
				dlg.PushState (goTo, false);
				return true;
			}

			internal bool CanBack ()
			{
				return back.Count > 0;
			}

			internal bool Forward ()
			{
				if (forward.Count > 0) {
					dlg.PushState (forward.Pop ().Directory, false);
					return true;
				}

				return false;
			}
			public bool Up ()
			{
				var parent = dlg.state?.Directory.Parent;
				if (parent != null) {

					back.Push (new FileDialogState (parent, dlg));
					dlg.PushState (parent, true);
					return true;
				}

				return false;
			}

			internal bool CanUp ()
			{
				return dlg.state?.Directory.Parent != null;
			}


			internal void Push (FileDialogState state)
			{
				if (state == null) {
					return;
				}

				// if changing to a new directory push onto the Back history
				if (back.Count == 0 || back.Peek ().Directory != state.Directory) {

					back.Push (state);
					ClearForward ();
				}
			}

			internal bool CanForward ()
			{
				return forward.Count > 0;
			}

			internal void ClearForward ()
			{
				forward.Clear ();
			}
		}
		private class FileDialogSorter {
			readonly FileDialog2 dlg;
			private TableView tableView;

			private DataColumn currentSort = null;
			private bool currentSortIsAsc = true;

			public FileDialogSorter (FileDialog2 dlg, TableView tableView)
			{
				this.dlg = dlg;
				this.tableView = tableView;

				// if user clicks the mouse in TableView
				this.tableView.MouseClick += e => {

					this.tableView.ScreenToCell (e.MouseEvent.X, e.MouseEvent.Y, out DataColumn clickedCol);

					if (clickedCol != null) {
						if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)) {

							// left click in a header
							SortColumn (clickedCol);
						} else if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)) {

							// right click in a header
							ShowHeaderContextMenu (clickedCol, e);
						}
					}
				};

			}
			private void SortColumn (DataColumn clickedCol)
			{
				GetProposedNewSortOrder (clickedCol, out var isAsc);
				SortColumn (clickedCol, isAsc);
			}

			private void SortColumn (DataColumn col, bool isAsc)
			{
				// set a sort order
				currentSort = col;
				currentSortIsAsc = isAsc;

				ApplySort ();
			}

			public void ApplySort ()
			{
				var col = currentSort;

				// TODO: Consider preserving selection
				tableView.Table.Rows.Clear ();

				var colName = col == null ? null : StripArrows (col.ColumnName);

				var stats = dlg.state?.Children ?? new FileSystemInfoStats [0];

				// Do we sort on a column or just use the default sort order?
				Func<FileSystemInfoStats, object> sortAlgorithm;

				if (colName == null) {
					sortAlgorithm = (v) => v.GetOrderByDefault ();
					currentSortIsAsc = true;
				} else {
					sortAlgorithm = (v) => v.GetOrderByValue (colName);
				}

				var ordered =
					currentSortIsAsc ?
					    stats.Select ((v, i) => new { v, i })
						.OrderByDescending (f => f.v.IsParent)
						.ThenBy (f => sortAlgorithm (f.v))
						.ToArray () :
					    stats.Select ((v, i) => new { v, i })
						.OrderByDescending (f => f.v.IsParent)
						.ThenByDescending (f => sortAlgorithm (f.v))
						.ToArray ();

				foreach (var o in ordered) {
					dlg.BuildRow (o.i);
				}

				foreach (DataColumn c in tableView.Table.Columns) {

					// remove any lingering sort indicator
					c.ColumnName = TrimArrows (c.ColumnName);

					// add a new one if this the one that is being sorted
					if (c == col) {
						c.ColumnName += currentSortIsAsc ? '▲' : '▼';
					}
				}

				tableView.Update ();
			}


			private static string TrimArrows (string columnName)
			{
				return columnName.TrimEnd ('▼', '▲');
			}
			private static string StripArrows (string columnName)
			{
				return columnName.Replace ("▼", "").Replace ("▲", "");
			}
			private string GetProposedNewSortOrder (DataColumn clickedCol, out bool isAsc)
			{
				// work out new sort order
				if (currentSort == clickedCol && currentSortIsAsc) {
					isAsc = false;
					return $"{clickedCol.ColumnName} DESC";
				} else {
					isAsc = true;
					return $"{clickedCol.ColumnName} ASC";
				}
			}

			private void ShowHeaderContextMenu (DataColumn clickedCol, View.MouseEventArgs e)
			{
				var sort = GetProposedNewSortOrder (clickedCol, out var isAsc);

				var contextMenu = new ContextMenu (e.MouseEvent.X + 1, e.MouseEvent.Y + 1,
					new MenuBarItem (new MenuItem [] {
					new MenuItem ($"Hide {TrimArrows(clickedCol.ColumnName)}", "", () => HideColumn(clickedCol)),
					new MenuItem ($"Sort {StripArrows(sort)}","",()=>SortColumn(clickedCol,isAsc)),
					})
				);

				contextMenu.Show ();
			}

			private void HideColumn (DataColumn clickedCol)
			{
				var style = tableView.Style.GetOrCreateColumnStyle (clickedCol);
				style.Visible = false;
				tableView.Update ();
			}
		}


		private class FileDialogRootTreeNode {
			public string DisplayName { get; set; }
			public DirectoryInfo Path { get; set; }
			public FileDialogRootTreeNode (string displayName, DirectoryInfo path)
			{
				this.DisplayName = displayName;
				this.Path = path;
			}

			public override string ToString ()
			{
				return DisplayName;
			}
		}
		private class FileDialogTreeBuilder : ITreeBuilder<object> {
			public bool SupportsCanExpand => true;

			public bool CanExpand (object toExpand)
			{
				return TryGetDirectories (NodeToDirectory (toExpand)).Any ();
			}

			private IEnumerable<DirectoryInfo> TryGetDirectories (DirectoryInfo directoryInfo)
			{
				try {
					return directoryInfo.EnumerateDirectories ();
				} catch (Exception) {

					return Enumerable.Empty<DirectoryInfo> ();
				}
			}

			internal static DirectoryInfo NodeToDirectory (object toExpand)
			{
				return toExpand is FileDialogRootTreeNode f ? f.Path : (DirectoryInfo)toExpand;
			}

			public IEnumerable<object> GetChildren (object forObject)
			{
				return TryGetDirectories (NodeToDirectory (forObject));
			}
		}
	}
}