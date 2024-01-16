﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Editor", Description: "A Text Editor using the TextView control.")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Text and Formatting")]
	[ScenarioCategory ("Top Level Windows")]
	[ScenarioCategory ("Files and IO")]
	[ScenarioCategory ("TextView")]

	public class Editor : Scenario {
		private string _fileName = "demo.txt";
		private TextView _textView;
		private bool _saved = true;
		private ScrollBarView _scrollBar;
		private byte [] _originalText;
		private string _textToFind;
		private string _textToReplace;
		private bool _matchCase;
		private bool _matchWholeWord;
		private Window _winDialog;
		private TabView _tabView;
		private MenuItem _miForceMinimumPosToZero;
		private bool _forceMinimumPosToZero = true;
		private List<CultureInfo> _cultureInfos;

		public override void Init ()
		{
			Application.Init ();
			_cultureInfos = Application.SupportedCultures;
			ConfigurationManager.Themes.Theme = Theme;
			ConfigurationManager.Apply ();

			Win = new Window () {
				Title = _fileName ?? "Untitled",
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ColorScheme = Colors.ColorSchemes [TopLevelColorScheme],
			};
			Application.Top.Add (Win);

			_textView = new TextView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				BottomOffset = 1,
				RightOffset = 1
			};

			CreateDemoFile (_fileName);

			LoadFile ();

			Win.Add (_textView);

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "", () => New()),
					new MenuItem ("_Open", "", () => Open()),
					new MenuItem ("_Save", "", () => Save()),
					new MenuItem ("_Save As", "", () => SaveAs()),
					new MenuItem ("_Close", "", () => CloseFile()),
					null,
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", () => Copy(),null,null, KeyCode.CtrlMask | KeyCode.C),
					new MenuItem ("C_ut", "", () => Cut(),null,null, KeyCode.CtrlMask | KeyCode.W),
					new MenuItem ("_Paste", "", () => Paste(),null,null, KeyCode.CtrlMask | KeyCode.Y),
					null,
					new MenuItem ("_Find", "", () => Find(),null,null, KeyCode.CtrlMask | KeyCode.S),
					new MenuItem ("Find _Next", "", () => FindNext(),null,null, KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.S),
					new MenuItem ("Find P_revious", "", () => FindPrevious(),null,null, KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.S),
					new MenuItem ("_Replace", "", () => Replace(),null,null, KeyCode.CtrlMask | KeyCode.R),
					new MenuItem ("Replace Ne_xt", "", () => ReplaceNext(),null,null, KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.R),
					new MenuItem ("Replace Pre_vious", "", () => ReplacePrevious(),null,null, KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.R),
					new MenuItem ("Replace _All", "", () => ReplaceAll(),null,null, KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.A),
					null,
					new MenuItem ("_Select All", "", () => SelectAll(),null,null, KeyCode.CtrlMask | KeyCode.T)
				}),
				new MenuBarItem ("_ScrollBarView", CreateKeepChecked ()),
				new MenuBarItem ("_Cursor", CreateCursorRadio ()),
				new MenuBarItem ("Forma_t", new MenuItem [] {
					CreateWrapChecked (),
					CreateAutocomplete(),
					CreateAllowsTabChecked (),
					CreateReadOnlyChecked ()
				}),
				new MenuBarItem ("_Responder", new MenuItem [] {
					CreateCanFocusChecked (),
					CreateEnabledChecked (),
					CreateVisibleChecked ()
				}),
				new MenuBarItem ("Conte_xtMenu", new MenuItem [] {
					_miForceMinimumPosToZero = new MenuItem ("ForceMinimumPosTo_Zero", "", () => {
						_miForceMinimumPosToZero.Checked = _forceMinimumPosToZero = !_forceMinimumPosToZero;
						_textView.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
					}) { CheckType = MenuItemCheckStyle.Checked, Checked = _forceMinimumPosToZero },
					new MenuBarItem ("_Languages", GetSupportedCultures ())
				})
			});

			Application.Top.Add (menu);

			var siCursorPosition = new StatusItem (KeyCode.Null, "", null);

			var statusBar = new StatusBar (new StatusItem [] {
				siCursorPosition,
				new StatusItem(KeyCode.F2, "~F2~ Open", () => Open()),
				new StatusItem(KeyCode.F3, "~F3~ Save", () => Save()),
				new StatusItem(KeyCode.F4, "~F4~ Save As", () => SaveAs()),
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
				new StatusItem(KeyCode.Null, $"OS Clipboard IsSupported : {Clipboard.IsSupported}", null)
			});

			_textView.UnwrappedCursorPosition += (s, e) => {
				siCursorPosition.Title = $"Ln {e.Point.Y + 1}, Col {e.Point.X + 1}";
				statusBar.SetNeedsDisplay ();
			};

			Application.Top.Add (statusBar);

			_scrollBar = new ScrollBarView (_textView, true);

			_scrollBar.ChangedPosition += (s, e) => {
				_textView.TopRow = _scrollBar.Position;
				if (_textView.TopRow != _scrollBar.Position) {
					_scrollBar.Position = _textView.TopRow;
				}
				_textView.SetNeedsDisplay ();
			};

			_scrollBar.OtherScrollBarView.ChangedPosition += (s, e) => {
				_textView.LeftColumn = _scrollBar.OtherScrollBarView.Position;
				if (_textView.LeftColumn != _scrollBar.OtherScrollBarView.Position) {
					_scrollBar.OtherScrollBarView.Position = _textView.LeftColumn;
				}
				_textView.SetNeedsDisplay ();
			};

			_scrollBar.VisibleChanged += (s, e) => {
				if (_scrollBar.Visible && _textView.RightOffset == 0) {
					_textView.RightOffset = 1;
				} else if (!_scrollBar.Visible && _textView.RightOffset == 1) {
					_textView.RightOffset = 0;
				}
			};

			_scrollBar.OtherScrollBarView.VisibleChanged += (s, e) => {
				if (_scrollBar.OtherScrollBarView.Visible && _textView.BottomOffset == 0) {
					_textView.BottomOffset = 1;
				} else if (!_scrollBar.OtherScrollBarView.Visible && _textView.BottomOffset == 1) {
					_textView.BottomOffset = 0;
				}
			};

			_textView.DrawContent += (s, e) => {
				_scrollBar.Size = _textView.Lines;
				_scrollBar.Position = _textView.TopRow;
				if (_scrollBar.OtherScrollBarView != null) {
					_scrollBar.OtherScrollBarView.Size = _textView.Maxlength;
					_scrollBar.OtherScrollBarView.Position = _textView.LeftColumn;
				}
				_scrollBar.LayoutSubviews ();
				_scrollBar.Refresh ();
			};

			Win.KeyDown += (s, e) => {
				if (_winDialog != null && (e.KeyCode == KeyCode.Esc || e == Application.QuitKey)) {
					DisposeWinDialog ();
				} else if (e == Application.QuitKey) {
					Quit ();
					e.Handled = true;
				} else if (_winDialog != null && e.KeyCode == (KeyCode.Tab | KeyCode.CtrlMask)) {
					if (_tabView.SelectedTab == _tabView.Tabs.ElementAt (_tabView.Tabs.Count - 1)) {
						_tabView.SelectedTab = _tabView.Tabs.ElementAt (0);
					} else {
						_tabView.SwitchTabBy (1);
					}
					e.Handled = true;
				} else if (_winDialog != null && e.KeyCode == (KeyCode.Tab | KeyCode.CtrlMask | KeyCode.ShiftMask)) {
					if (_tabView.SelectedTab == _tabView.Tabs.ElementAt (0)) {
						_tabView.SelectedTab = _tabView.Tabs.ElementAt (_tabView.Tabs.Count - 1);
					} else {
						_tabView.SwitchTabBy (-1);
					}
					e.Handled = true;
				}
			};

			Application.Top.Closed += (s, e) => Thread.CurrentThread.CurrentUICulture = new CultureInfo ("en-US");
		}

		private void DisposeWinDialog ()
		{
			_winDialog.Dispose ();
			Win.Remove (_winDialog);
			_winDialog = null;
		}

		public override void Setup ()
		{
		}

		private void New (bool checkChanges = true)
		{
			if (checkChanges && !CanCloseFile ()) {
				return;
			}

			Win.Title = "Untitled.txt";
			_fileName = null;
			_originalText = new System.IO.MemoryStream ().ToArray ();
			_textView.Text = Encoding.Unicode.GetString (_originalText);
		}

		private void LoadFile ()
		{
			if (_fileName != null) {
				// FIXED: BUGBUG: #452 TextView.LoadFile keeps file open and provides no way of closing it
				_textView.Load (_fileName);
				//_textView.Text = System.IO.File.ReadAllText (_fileName);
				_originalText = Encoding.Unicode.GetBytes (_textView.Text);
				Win.Title = _fileName;
				_saved = true;
			}
		}

		private void Paste ()
		{
			if (_textView != null) {
				_textView.Paste ();
			}
		}

		private void Cut ()
		{
			if (_textView != null) {
				_textView.Cut ();
			}
		}

		private void Copy ()
		{
			if (_textView != null) {
				_textView.Copy ();
			}
		}

		private void SelectAll ()
		{
			_textView.SelectAll ();
		}

		private void Find ()
		{
			CreateFindReplace ();
		}

		private void FindNext ()
		{
			ContinueFind ();
		}

		private void FindPrevious ()
		{
			ContinueFind (false);
		}

		private void ContinueFind (bool next = true, bool replace = false)
		{
			if (!replace && string.IsNullOrEmpty (_textToFind)) {
				Find ();
				return;
			} else if (replace && (string.IsNullOrEmpty (_textToFind)
				|| (_winDialog == null && string.IsNullOrEmpty (_textToReplace)))) {
				Replace ();
				return;
			}

			bool found;
			bool gaveFullTurn;

			if (next) {
				if (!replace) {
					found = _textView.FindNextText (_textToFind, out gaveFullTurn, _matchCase, _matchWholeWord);
				} else {
					found = _textView.FindNextText (_textToFind, out gaveFullTurn, _matchCase, _matchWholeWord,
						_textToReplace, true);
				}
			} else {
				if (!replace) {
					found = _textView.FindPreviousText (_textToFind, out gaveFullTurn, _matchCase, _matchWholeWord);
				} else {
					found = _textView.FindPreviousText (_textToFind, out gaveFullTurn, _matchCase, _matchWholeWord,
						_textToReplace, true);
				}
			}
			if (!found) {
				MessageBox.Query ("Find", $"The following specified text was not found: '{_textToFind}'", "Ok");
			} else if (gaveFullTurn) {
				MessageBox.Query ("Find", $"No more occurrences were found for the following specified text: '{_textToFind}'", "Ok");
			}
		}

		private void Replace ()
		{
			CreateFindReplace (false);
		}

		private void ReplaceNext ()
		{
			ContinueFind (true, true);
		}

		private void ReplacePrevious ()
		{
			ContinueFind (false, true);
		}

		private void ReplaceAll ()
		{
			if (string.IsNullOrEmpty (_textToFind) || (string.IsNullOrEmpty (_textToReplace) && _winDialog == null)) {
				Replace ();
				return;
			}

			if (_textView.ReplaceAllText (_textToFind, _matchCase, _matchWholeWord, _textToReplace)) {
				MessageBox.Query ("Replace All", $"All occurrences were replaced for the following specified text: '{_textToReplace}'", "Ok");
			} else {
				MessageBox.Query ("Replace All", $"None of the following specified text was found: '{_textToFind}'", "Ok");
			}
		}

		private bool CanCloseFile ()
		{
			if (_textView.Text == Encoding.Unicode.GetString (_originalText)) {
				//System.Diagnostics.Debug.Assert (!_textView.IsDirty);
				return true;
			}

			System.Diagnostics.Debug.Assert (_textView.IsDirty);

			var r = MessageBox.ErrorQuery ("Save File",
				$"Do you want save changes in {Win.Title}?", "Yes", "No", "Cancel");
			if (r == 0) {
				return Save ();
			} else if (r == 1) {
				return true;
			}

			return false;
		}

		private void Open ()
		{
			if (!CanCloseFile ()) {
				return;
			}
			var aTypes = new List<IAllowedType> () {
				new AllowedType("Text",".txt;.bin;.xml;.json", ".txt", ".bin", ".xml", ".json"),
				new AllowedTypeAny()
			};
			var d = new OpenDialog ("Open", aTypes) { AllowsMultipleSelection = false };
			Application.Run (d);

			if (!d.Canceled && d.FilePaths.Count > 0) {
				_fileName = d.FilePaths [0];
				LoadFile ();
			}
		}

		private bool Save ()
		{
			if (_fileName != null) {
				// FIXED: BUGBUG: #279 TextView does not know how to deal with \r\n, only \r 
				// As a result files saved on Windows and then read back will show invalid chars.
				return SaveFile (Win.Title, _fileName);
			} else {
				return SaveAs ();
			}
		}

		private bool SaveAs ()
		{
			var aTypes = new List<IAllowedType> () {
				new AllowedType("Text Files", ".txt", ".bin", ".xml"),
				new AllowedTypeAny()
			};
			var sd = new SaveDialog ("Save file", aTypes);

			sd.Path = System.IO.Path.Combine (sd.FileName, Win.Title);
			Application.Run (sd);

			if (!sd.Canceled) {
				if (System.IO.File.Exists (sd.Path)) {
					if (MessageBox.Query ("Save File",
						"File already exists. Overwrite any way?", "No", "Ok") == 1) {
						return SaveFile (sd.FileName, sd.Path);
					} else {
						_saved = false;
						return _saved;
					}
				} else {
					return SaveFile (sd.FileName, sd.Path);
				}
			} else {
				_saved = false;
				return _saved;
			}
		}

		private bool SaveFile (string title, string file)
		{
			try {
				Win.Title = title;
				_fileName = file;
				System.IO.File.WriteAllText (_fileName, _textView.Text);
				_originalText = Encoding.Unicode.GetBytes (_textView.Text);
				_saved = true;
				_textView.ClearHistoryChanges ();
				MessageBox.Query ("Save File", "File was successfully saved.", "Ok");

			} catch (Exception ex) {
				MessageBox.ErrorQuery ("Error", ex.Message, "Ok");
				return false;
			}

			return true;
		}

		private void CloseFile ()
		{
			if (!CanCloseFile ()) {
				return;
			}

			try {
				_textView.CloseFile ();
				New (false);
			} catch (Exception ex) {
				MessageBox.ErrorQuery ("Error", ex.Message, "Ok");
			}
		}

		private void Quit ()
		{
			if (!CanCloseFile ()) {
				return;
			}

			Application.RequestStop ();
		}

		private void CreateDemoFile (string fileName)
		{
			var sb = new StringBuilder ();
			// FIXED: BUGBUG: #279 TextView does not know how to deal with \r\n, only \r
			sb.Append ("Hello world.\n");
			sb.Append ("This is a test of the Emergency Broadcast System.\n");

			for (int i = 0; i < 30; i++) {
				sb.Append ($"{i} - This is a test with a very long line and many lines to test the ScrollViewBar against the TextView. - {i}\n");
			}
			var sw = System.IO.File.CreateText (fileName);
			sw.Write (sb.ToString ());
			sw.Close ();
		}

		private MenuItem [] GetSupportedCultures ()
		{
			List<MenuItem> supportedCultures = new List<MenuItem> ();
			var index = -1;

			foreach (var c in _cultureInfos) {
				var culture = new MenuItem {
					CheckType = MenuItemCheckStyle.Checked
				};
				if (index == -1) {
					culture.Title = "_English";
					culture.Help = "en-US";
					culture.Checked = Thread.CurrentThread.CurrentUICulture.Name == "en-US";
					CreateAction (supportedCultures, culture);
					supportedCultures.Add (culture);
					index++;
					culture = new MenuItem {
						CheckType = MenuItemCheckStyle.Checked
					};
				}
				culture.Title = $"_{c.Parent.EnglishName}";
				culture.Help = c.Name;
				culture.Checked = Thread.CurrentThread.CurrentUICulture.Name == c.Name;
				CreateAction (supportedCultures, culture);
				supportedCultures.Add (culture);
			}
			return supportedCultures.ToArray ();

			void CreateAction (List<MenuItem> supportedCultures, MenuItem culture)
			{
				culture.Action += () => {
					Thread.CurrentThread.CurrentUICulture = new CultureInfo (culture.Help);
					culture.Checked = true;
					foreach (var item in supportedCultures) {
						item.Checked = item.Help == Thread.CurrentThread.CurrentUICulture.Name;
					}
				};
			}
		}

		private MenuItem [] CreateKeepChecked ()
		{
			var item = new MenuItem ();
			item.Title = "Keep Content Always In Viewport";
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = true;
			item.Action += () => _scrollBar.KeepContentAlwaysInViewport = (bool)(item.Checked = !item.Checked);

			return new MenuItem [] { item };
		}

		private MenuItem CreateWrapChecked ()
		{
			var item = new MenuItem {
				Title = "Word Wrap"
			};
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = _textView.WordWrap;
			item.Action += () => {
				_textView.WordWrap = (bool)(item.Checked = !item.Checked);
				if (_textView.WordWrap) {
					_scrollBar.OtherScrollBarView.ShowScrollIndicator = false;
					_textView.BottomOffset = 0;
				} else {
					_textView.BottomOffset = 1;
				}
			};

			return item;
		}

		private MenuItem CreateAutocomplete ()
		{
			var singleWordGenerator = new SingleWordSuggestionGenerator ();
			_textView.Autocomplete.SuggestionGenerator = singleWordGenerator;

			var auto = new MenuItem ();
			auto.Title = "Autocomplete";
			auto.CheckType |= MenuItemCheckStyle.Checked;
			auto.Checked = false;
			auto.Action += () => {
				if ((bool)(auto.Checked = !auto.Checked)) {
					// setup autocomplete with all words currently in the editor
					singleWordGenerator.AllSuggestions =

					Regex.Matches (_textView.Text, "\\w+")
					.Select (s => s.Value)
					.Distinct ().ToList ();
				} else {
					singleWordGenerator.AllSuggestions.Clear ();

				}
			};

			return auto;
		}

		private MenuItem CreateAllowsTabChecked ()
		{
			var item = new MenuItem {
				Title = "Allows Tab"
			};
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = _textView.AllowsTab;
			item.Action += () => {
				_textView.AllowsTab = (bool)(item.Checked = !item.Checked);
			};

			return item;
		}

		private MenuItem CreateReadOnlyChecked ()
		{
			var item = new MenuItem {
				Title = "Read Only"
			};
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = _textView.ReadOnly;
			item.Action += () => _textView.ReadOnly = (bool)(item.Checked = !item.Checked);

			return item;
		}

		private MenuItem CreateCanFocusChecked ()
		{
			var item = new MenuItem {
				Title = "CanFocus"
			};
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = _textView.CanFocus;
			item.Action += () => {
				_textView.CanFocus = (bool)(item.Checked = !item.Checked);
				if (_textView.CanFocus) {
					_textView.SetFocus ();
				}
			};

			return item;
		}

		private MenuItem CreateEnabledChecked ()
		{
			var item = new MenuItem {
				Title = "Enabled"
			};
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = _textView.Enabled;
			item.Action += () => {
				_textView.Enabled = (bool)(item.Checked = !item.Checked);
				if (_textView.Enabled) {
					_textView.SetFocus ();
				}
			};

			return item;
		}

		private MenuItem CreateVisibleChecked ()
		{
			var item = new MenuItem {
				Title = "Visible"
			};
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = _textView.Visible;
			item.Action += () => {
				_textView.Visible = (bool)(item.Checked = !item.Checked);
				if (_textView.Visible) {
					_textView.SetFocus ();
				}
			};

			return item;
		}

		MenuItem [] CreateCursorRadio ()
		{
			List<MenuItem> menuItems = new List<MenuItem> ();
			menuItems.Add (new MenuItem ("_Invisible", "", () => SetCursor (CursorVisibility.Invisible)) {
				CheckType = MenuItemCheckStyle.Radio,
				Checked = _textView.DesiredCursorVisibility == CursorVisibility.Invisible
			});
			menuItems.Add (new MenuItem ("_Box", "", () => SetCursor (CursorVisibility.Box)) {
				CheckType = MenuItemCheckStyle.Radio,
				Checked = _textView.DesiredCursorVisibility == CursorVisibility.Box
			});
			menuItems.Add (new MenuItem ("_Underline", "", () => SetCursor (CursorVisibility.Underline)) {
				CheckType = MenuItemCheckStyle.Radio,
				Checked = _textView.DesiredCursorVisibility == CursorVisibility.Underline
			});
			menuItems.Add (new MenuItem ("", "", () => { }, () => false));
			menuItems.Add (new MenuItem ("xTerm :", "", () => { }, () => false));
			menuItems.Add (new MenuItem ("", "", () => { }, () => false));
			menuItems.Add (new MenuItem ("  _Default", "", () => SetCursor (CursorVisibility.Default)) {
				CheckType = MenuItemCheckStyle.Radio,
				Checked = _textView.DesiredCursorVisibility == CursorVisibility.Default
			});
			menuItems.Add (new MenuItem ("  _Vertical", "", () => SetCursor (CursorVisibility.Vertical)) {
				CheckType = MenuItemCheckStyle.Radio,
				Checked = _textView.DesiredCursorVisibility == CursorVisibility.Vertical
			});
			menuItems.Add (new MenuItem ("  V_ertical Fix", "", () => SetCursor (CursorVisibility.VerticalFix)) {
				CheckType = MenuItemCheckStyle.Radio,
				Checked = _textView.DesiredCursorVisibility == CursorVisibility.VerticalFix
			});
			menuItems.Add (new MenuItem ("  B_ox Fix", "", () => SetCursor (CursorVisibility.BoxFix)) {
				CheckType = MenuItemCheckStyle.Radio,
				Checked = _textView.DesiredCursorVisibility == CursorVisibility.BoxFix
			});
			menuItems.Add (new MenuItem ("  U_nderline Fix", "", () => SetCursor (CursorVisibility.UnderlineFix)) {
				CheckType = MenuItemCheckStyle.Radio,
				Checked = _textView.DesiredCursorVisibility == CursorVisibility.UnderlineFix
			});

			void SetCursor (CursorVisibility visibility)
			{
				_textView.DesiredCursorVisibility = visibility;
				var title = "";
				switch (visibility) {
				case CursorVisibility.Default:
					title = "  _Default";
					break;
				case CursorVisibility.Invisible:
					title = "_Invisible";
					break;
				case CursorVisibility.Underline:
					title = "_Underline";
					break;
				case CursorVisibility.UnderlineFix:
					title = "  U_nderline Fix";
					break;
				case CursorVisibility.Vertical:
					title = "  _Vertical";
					break;
				case CursorVisibility.VerticalFix:
					title = "  V_ertical Fix";
					break;
				case CursorVisibility.Box:
					title = "_Box";
					break;
				case CursorVisibility.BoxFix:
					title = "  B_ox Fix";
					break;
				}

				foreach (var menuItem in menuItems) {
					menuItem.Checked = menuItem.Title.Equals (title) && visibility == _textView.DesiredCursorVisibility;
				}
			}

			return menuItems.ToArray ();
		}

		private void CreateFindReplace (bool isFind = true)
		{
			if (_winDialog != null) {
				_winDialog.SetFocus ();
				return;
			}

			_winDialog = new Window () {
				Title = isFind ? "Find" : "Replace",
				X = Win.Bounds.Width / 2 - 30,
				Y = Win.Bounds.Height / 2 - 10,
				ColorScheme = Colors.ColorSchemes ["TopLevel"]
			};

			_tabView = new TabView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			_tabView.AddTab (new Tab () { DisplayText = "Find", View = FindTab () }, isFind);
			var replace = ReplaceTab ();
			_tabView.AddTab (new Tab () { DisplayText = "Replace", View = replace }, !isFind);
			_tabView.SelectedTabChanged += (s, e) => _tabView.SelectedTab.View.FocusFirst ();
			_winDialog.Add (_tabView);

			Win.Add (_winDialog);

			_winDialog.Width = replace.Width + 4;
			_winDialog.Height = replace.Height + 4;

			_winDialog.SuperView.BringSubviewToFront (_winDialog);
			_winDialog.SetFocus ();
		}

		private void SetFindText ()
		{
			_textToFind = !string.IsNullOrEmpty (_textView.SelectedText)
				? _textView.SelectedText
				: string.IsNullOrEmpty (_textToFind) ? "" : _textToFind;

			_textToReplace = string.IsNullOrEmpty (_textToReplace) ? "" : _textToReplace;
		}

		private View FindTab ()
		{
			var d = new View ();
			d.DrawContent += (s, e) => {
				foreach (var v in d.Subviews) {
					v.SetNeedsDisplay ();
				}
			};

			var lblWidth = "Replace:".Length;

			var label = new Label ("Find:") {
				Y = 1,
				Width = lblWidth,
				TextAlignment = TextAlignment.Right,
				AutoSize = false
			};
			d.Add (label);

			SetFindText ();
			var txtToFind = new TextField (_textToFind) {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 20
			};
			txtToFind.Enter += (s, e) => txtToFind.Text = _textToFind;
			d.Add (txtToFind);

			var btnFindNext = new Button () {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (label),
				Width = 20,
				Enabled = !string.IsNullOrEmpty (txtToFind.Text),
				TextAlignment = TextAlignment.Centered,
				IsDefault = true,
				AutoSize = false,
				Text = "Find _Next"
			};
			btnFindNext.Clicked += (s, e) => FindNext ();
			d.Add (btnFindNext);

			var btnFindPrevious = new Button () {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnFindNext) + 1,
				Width = 20,
				Enabled = !string.IsNullOrEmpty (txtToFind.Text),
				TextAlignment = TextAlignment.Centered,
				AutoSize = false,
				Text = "Find _Previous"
			};
			btnFindPrevious.Clicked += (s, e) => FindPrevious ();
			d.Add (btnFindPrevious);

			txtToFind.TextChanged += (s, e) => {
				_textToFind = txtToFind.Text;
				_textView.FindTextChanged ();
				btnFindNext.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
				btnFindPrevious.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
			};

			var btnCancel = new Button () {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnFindPrevious) + 2,
				Width = 20,
				TextAlignment = TextAlignment.Centered,
				AutoSize = false,
				Text = "Cancel"
			};
			btnCancel.Clicked += (s, e) => {
				DisposeWinDialog ();
			};
			d.Add (btnCancel);

			var ckbMatchCase = new CheckBox ("Match c_ase") {
				X = 0,
				Y = Pos.Top (txtToFind) + 2,
				Checked = _matchCase
			};
			ckbMatchCase.Toggled += (s, e) => _matchCase = (bool)ckbMatchCase.Checked;
			d.Add (ckbMatchCase);

			var ckbMatchWholeWord = new CheckBox ("Match _whole word") {
				X = 0,
				Y = Pos.Top (ckbMatchCase) + 1,
				Checked = _matchWholeWord
			};
			ckbMatchWholeWord.Toggled += (s, e) => _matchWholeWord = (bool)ckbMatchWholeWord.Checked;
			d.Add (ckbMatchWholeWord);

			d.Width = label.Width + txtToFind.Width + btnFindNext.Width + 2;
			d.Height = btnFindNext.Height + btnFindPrevious.Height + btnCancel.Height + 4;

			return d;
		}

		private View ReplaceTab ()
		{
			var d = new View ();
			d.DrawContent += (s, e) => {
				foreach (var v in d.Subviews) {
					v.SetNeedsDisplay ();
				}
			};

			var lblWidth = "Replace:".Length;

			var label = new Label ("Find:") {
				Y = 1,
				Width = lblWidth,
				TextAlignment = TextAlignment.Right,
				AutoSize = false
			};
			d.Add (label);

			SetFindText ();
			var txtToFind = new TextField (_textToFind) {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 20
			};
			txtToFind.Enter += (s, e) => txtToFind.Text = _textToFind;
			d.Add (txtToFind);

			var btnFindNext = new Button () {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (label),
				Width = 20,
				Enabled = !string.IsNullOrEmpty (txtToFind.Text),
				TextAlignment = TextAlignment.Centered,
				IsDefault = true,
				AutoSize = false,
				Text = "Replace _Next"
			};
			btnFindNext.Clicked += (s, e) => ReplaceNext ();
			d.Add (btnFindNext);

			label = new Label ("Replace:") {
				X = Pos.Left (label),
				Y = Pos.Top (label) + 1,
				Width = lblWidth,
				TextAlignment = TextAlignment.Right
			};
			d.Add (label);

			SetFindText ();
			var txtToReplace = new TextField (_textToReplace) {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 20
			};
			txtToReplace.TextChanged += (s, e) => _textToReplace = txtToReplace.Text;
			d.Add (txtToReplace);

			var btnFindPrevious = new Button () {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnFindNext) + 1,
				Width = 20,
				Enabled = !string.IsNullOrEmpty (txtToFind.Text),
				TextAlignment = TextAlignment.Centered,
				AutoSize = false,
				Text = "Replace _Previous"
			};
			btnFindPrevious.Clicked += (s, e) => ReplacePrevious ();
			d.Add (btnFindPrevious);

			var btnReplaceAll = new Button () {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnFindPrevious) + 1,
				Width = 20,
				Enabled = !string.IsNullOrEmpty (txtToFind.Text),
				TextAlignment = TextAlignment.Centered,
				AutoSize = false,
				Text = "Replace _All"
			};
			btnReplaceAll.Clicked += (s, e) => ReplaceAll ();
			d.Add (btnReplaceAll);

			txtToFind.TextChanged += (s, e) => {
				_textToFind = txtToFind.Text;
				_textView.FindTextChanged ();
				btnFindNext.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
				btnFindPrevious.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
				btnReplaceAll.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
			};

			var btnCancel = new Button () {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnReplaceAll) + 1,
				Width = 20,
				TextAlignment = TextAlignment.Centered,
				AutoSize = false,
				Text = "Cancel"
			};
			btnCancel.Clicked += (s, e) => {
				DisposeWinDialog ();
			};
			d.Add (btnCancel);

			var ckbMatchCase = new CheckBox ("Match c_ase") {
				X = 0,
				Y = Pos.Top (txtToFind) + 2,
				Checked = _matchCase
			};
			ckbMatchCase.Toggled += (s, e) => _matchCase = (bool)ckbMatchCase.Checked;
			d.Add (ckbMatchCase);

			var ckbMatchWholeWord = new CheckBox ("Match _whole word") {
				X = 0,
				Y = Pos.Top (ckbMatchCase) + 1,
				Checked = _matchWholeWord
			};
			ckbMatchWholeWord.Toggled += (s, e) => _matchWholeWord = (bool)ckbMatchWholeWord.Checked;
			d.Add (ckbMatchWholeWord);

			d.Width = lblWidth + txtToFind.Width + btnFindNext.Width + 2;
			d.Height = btnFindNext.Height + btnFindPrevious.Height + btnCancel.Height + 4;

			return d;
		}
	}
}
