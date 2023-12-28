﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class ListViewTests {
		readonly ITestOutputHelper output;

		public ListViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Constructors_Defaults ()
		{
			var lv = new ListView ();
			Assert.Null (lv.Source);
			Assert.True (lv.CanFocus);
			Assert.Equal (-1, lv.SelectedItem);

			lv = new ListView (new List<string> () { "One", "Two", "Three" });
			Assert.NotNull (lv.Source);
			Assert.Equal (-1, lv.SelectedItem);

			lv = new ListView (new NewListDataSource ());
			Assert.NotNull (lv.Source);
			Assert.Equal (-1, lv.SelectedItem);

			lv = new ListView (new Rect (0, 1, 10, 20), new List<string> () { "One", "Two", "Three" });
			Assert.NotNull (lv.Source);
			Assert.Equal (-1, lv.SelectedItem);
			Assert.Equal (new Rect (0, 1, 10, 20), lv.Frame);

			lv = new ListView (new Rect (0, 1, 10, 20), new NewListDataSource ());
			Assert.NotNull (lv.Source);
			Assert.Equal (-1, lv.SelectedItem);
			Assert.Equal (new Rect (0, 1, 10, 20), lv.Frame);
		}

		[Fact]
		public void ListViewSelectThenDown ()
		{
			var lv = new ListView (new List<string> () { "One", "Two", "Three" });
			lv.AllowsMarking = true;

			Assert.NotNull (lv.Source);

			// first item should be deselected by default
			Assert.Equal (-1, lv.SelectedItem);

			// nothing is ticked
			Assert.False (lv.Source.IsMarked (0));
			Assert.False (lv.Source.IsMarked (1));
			Assert.False (lv.Source.IsMarked (2));

			lv.KeyBindings.Add (KeyCode.Space | KeyCode.ShiftMask, Command.ToggleChecked, Command.LineDown);

			var ev = new Key (KeyCode.Space | KeyCode.ShiftMask);

			// view should indicate that it has accepted and consumed the event
			Assert.True (lv.NewKeyDownEvent (ev));

			// first item should now be selected
			Assert.Equal (0, lv.SelectedItem);

			// none of the items should be ticked
			Assert.False (lv.Source.IsMarked (0));
			Assert.False (lv.Source.IsMarked (1));
			Assert.False (lv.Source.IsMarked (2));

			// Press key combo again
			Assert.True (lv.NewKeyDownEvent (ev));

			// second item should now be selected
			Assert.Equal (1, lv.SelectedItem);

			// first item only should be ticked
			Assert.True (lv.Source.IsMarked (0));
			Assert.False (lv.Source.IsMarked (1));
			Assert.False (lv.Source.IsMarked (2));

			// Press key combo again
			Assert.True (lv.NewKeyDownEvent (ev));
			Assert.Equal (2, lv.SelectedItem);
			Assert.True (lv.Source.IsMarked (0));
			Assert.True (lv.Source.IsMarked (1));
			Assert.False (lv.Source.IsMarked (2));

			// Press key combo again
			Assert.True (lv.NewKeyDownEvent (ev));
			Assert.Equal (2, lv.SelectedItem); // cannot move down any further
			Assert.True (lv.Source.IsMarked (0));
			Assert.True (lv.Source.IsMarked (1));
			Assert.True (lv.Source.IsMarked (2)); // but can toggle marked

			// Press key combo again 
			Assert.True (lv.NewKeyDownEvent (ev));
			Assert.Equal (2, lv.SelectedItem); // cannot move down any further
			Assert.True (lv.Source.IsMarked (0));
			Assert.True (lv.Source.IsMarked (1));
			Assert.False (lv.Source.IsMarked (2)); // untoggle toggle marked
		}
		[Fact]
		public void SettingEmptyKeybindingThrows ()
		{
			var lv = new ListView (new List<string> () { "One", "Two", "Three" });
			Assert.Throws<ArgumentException> (() => lv.KeyBindings.Add (KeyCode.Space));
		}

		/// <summary>
		/// Tests that when none of the Commands in a chained keybinding are possible
		/// the <see cref="View.NewKeyDownEvent"/> returns the appropriate result
		/// </summary>
		[Fact]
		public void ListViewProcessKeyReturnValue_WithMultipleCommands ()
		{
			var lv = new ListView (new List<string> () { "One", "Two", "Three", "Four" });

			Assert.NotNull (lv.Source);

			// first item should be deselected by default
			Assert.Equal (-1, lv.SelectedItem);

			// bind shift down to move down twice in control
			lv.KeyBindings.Add (KeyCode.CursorDown | KeyCode.ShiftMask, Command.LineDown, Command.LineDown);

			var ev = new Key (KeyCode.CursorDown | KeyCode.ShiftMask);

			Assert.True (lv.NewKeyDownEvent (ev), "The first time we move down 2 it should be possible");

			// After moving down twice from -1 we should be at 'Two'
			Assert.Equal (1, lv.SelectedItem);

			// clear the items
			lv.SetSource (null);

			// Press key combo again - return should be false this time as none of the Commands are allowable
			Assert.False (lv.NewKeyDownEvent (ev), "We cannot move down so will not respond to this");
		}

		private class NewListDataSource : IListDataSource {
			public int Count => throw new NotImplementedException ();

			public int Length => throw new NotImplementedException ();

			public bool IsMarked (int item)
			{
				throw new NotImplementedException ();
			}

			public void Render (ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
			{
				throw new NotImplementedException ();
			}

			public void SetMark (int item, bool value)
			{
				throw new NotImplementedException ();
			}

			public IList ToList ()
			{
				return new List<string> () { "One", "Two", "Three" };
			}
		}

		[Fact]
		public void KeyBindings_Command ()
		{
			List<string> source = new List<string> () { "One", "Two", "Three" };
			ListView lv = new ListView (source) { Height = 2, AllowsMarking = true };
			lv.BeginInit (); lv.EndInit ();
			Assert.Equal (-1, lv.SelectedItem);
			Assert.True (lv.NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.Equal (0, lv.SelectedItem);
			Assert.True (lv.NewKeyDownEvent (new (KeyCode.CursorUp)));
			Assert.Equal (0, lv.SelectedItem);
			Assert.True (lv.NewKeyDownEvent (new (KeyCode.PageDown)));
			Assert.Equal (2, lv.SelectedItem);
			Assert.Equal (2, lv.TopItem);
			Assert.True (lv.NewKeyDownEvent (new (KeyCode.PageUp)));
			Assert.Equal (0, lv.SelectedItem);
			Assert.Equal (0, lv.TopItem);
			Assert.False (lv.Source.IsMarked (lv.SelectedItem));
			Assert.True (lv.NewKeyDownEvent (new (KeyCode.Space)));
			Assert.True (lv.Source.IsMarked (lv.SelectedItem));
			var opened = false;
			lv.OpenSelectedItem += (s, _) => opened = true;
			Assert.True (lv.NewKeyDownEvent (new (KeyCode.Enter)));
			Assert.True (opened);
			Assert.True (lv.NewKeyDownEvent (new (KeyCode.End)));
			Assert.Equal (2, lv.SelectedItem);
			Assert.True (lv.NewKeyDownEvent (new (KeyCode.Home)));
			Assert.Equal (0, lv.SelectedItem);
		}

		[Fact]
		[AutoInitShutdown]
		public void RowRender_Event ()
		{
			var rendered = false;
			var source = new List<string> () { "one", "two", "three" };
			var lv = new ListView () { Width = Dim.Fill (), Height = Dim.Fill () };
			lv.RowRender += (s, _) => rendered = true;
			Application.Top.Add (lv);
			Application.Begin (Application.Top);
			Assert.False (rendered);

			lv.SetSource (source);
			lv.Draw ();
			Assert.True (rendered);
		}

		[Fact]
		[AutoInitShutdown]
		public void EnsureSelectedItemVisible_Top ()
		{
			var source = new List<string> () { "First", "Second" };
			ListView lv = new ListView (source) { Width = Dim.Fill (), Height = 1 };
			lv.SelectedItem = 1;
			Application.Top.Add (lv);
			Application.Begin (Application.Top);

			Assert.Equal ("Second ", GetContents (0));
			Assert.Equal (new (' ', 7), GetContents (1));

			lv.MoveUp ();
			lv.Draw ();

			Assert.Equal ("First  ", GetContents (0));
			Assert.Equal (new (' ', 7), GetContents (1));

			string GetContents (int line)
			{
				var item = "";
				for (int i = 0; i < 7; i++) {
					item += Application.Driver.Contents [line, i].Rune;
				}
				return item;
			}
		}

		[Fact]
		[AutoInitShutdown]
		public void Ensures_Visibility_SelectedItem_On_MoveDown_And_MoveUp ()
		{
			var source = new List<string> ();
			for (int i = 0; i < 20; i++) {
				source.Add ($"Line{i}");
			}
			var lv = new ListView (source) { Width = Dim.Fill (), Height = Dim.Fill () };
			var win = new Window ();
			win.Add (lv);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (12, 12);
			Application.Refresh ();

			Assert.Equal (-1, lv.SelectedItem);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘", output);

			Assert.True (lv.ScrollDown (10));
			lv.Draw ();
			Assert.Equal (-1, lv.SelectedItem);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘", output);

			Assert.True (lv.MoveDown ());
			lv.Draw ();
			Assert.Equal (0, lv.SelectedItem);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘", output);

			Assert.True (lv.MoveEnd ());
			lv.Draw ();
			Assert.Equal (19, lv.SelectedItem);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line19    │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
└──────────┘", output);

			Assert.True (lv.ScrollUp (20));
			lv.Draw ();
			Assert.Equal (19, lv.SelectedItem);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘", output);

			Assert.True (lv.MoveDown ());
			lv.Draw ();
			Assert.Equal (19, lv.SelectedItem);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘", output);

			Assert.True (lv.ScrollUp (20));
			lv.Draw ();
			Assert.Equal (19, lv.SelectedItem);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘", output);

			Assert.True (lv.MoveDown ());
			lv.Draw ();
			Assert.Equal (19, lv.SelectedItem);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘", output);

			Assert.True (lv.MoveHome ());
			lv.Draw ();
			Assert.Equal (0, lv.SelectedItem);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘", output);

			Assert.True (lv.ScrollDown (20));
			lv.Draw ();
			Assert.Equal (0, lv.SelectedItem);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line19    │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
└──────────┘", output);

			Assert.True (lv.MoveUp ());
			lv.Draw ();
			Assert.Equal (0, lv.SelectedItem);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘", output);
		}

		[Fact]
		public void SetSource_Preserves_ListWrapper_Instance_If_Not_Null ()
		{
			var lv = new ListView (new List<string> { "One", "Two" });

			Assert.NotNull (lv.Source);

			lv.SetSource (null);
			Assert.NotNull (lv.Source);

			lv.Source = null;
			Assert.Null (lv.Source);

			lv = new ListView (new List<string> { "One", "Two" });
			Assert.NotNull (lv.Source);

			lv.SetSourceAsync (null);
			Assert.NotNull (lv.Source);
		}

		[Fact]
		public void ListWrapper_StartsWith ()
		{
			var lw = new ListWrapper (new List<string> { "One", "Two", "Three" });

			Assert.Equal (1, lw.StartsWith ("t"));
			Assert.Equal (1, lw.StartsWith ("tw"));
			Assert.Equal (2, lw.StartsWith ("th"));
			Assert.Equal (1, lw.StartsWith ("T"));
			Assert.Equal (1, lw.StartsWith ("TW"));
			Assert.Equal (2, lw.StartsWith ("TH"));

			lw = new ListWrapper (new List<string> { "One", "Two", "Three" });

			Assert.Equal (1, lw.StartsWith ("t"));
			Assert.Equal (1, lw.StartsWith ("tw"));
			Assert.Equal (2, lw.StartsWith ("th"));
			Assert.Equal (1, lw.StartsWith ("T"));
			Assert.Equal (1, lw.StartsWith ("TW"));
			Assert.Equal (2, lw.StartsWith ("TH"));
		}

		[Fact, AutoInitShutdown]
		public void EnsureSelectedItemVisible_SelectedItem ()
		{
			var source = new List<string> ();
			for (int i = 0; i < 10; i++) {
				source.Add ($"Item {i}");
			}
			var lv = new ListView (source) {
				Width = 10,
				Height = 5
			};
			Application.Top.Add (lv);
			Application.Begin (Application.Top);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
Item 0
Item 1
Item 2
Item 3
Item 4", output);

			// EnsureSelectedItemVisible is auto enabled on the OnSelectedChanged
			lv.SelectedItem = 6;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Item 2
Item 3
Item 4
Item 5
Item 6", output);
		}

		[Fact]
		public void SelectedItem_Get_Set ()
		{
			var lv = new ListView (new List<string> { "One", "Two", "Three" });
			Assert.Equal (-1, lv.SelectedItem);
			Assert.Throws<ArgumentException> (() => lv.SelectedItem = 3);
			var exception = Record.Exception (() => lv.SelectedItem = -1);
			Assert.Null (exception);
		}

		[Fact]
		public void OnEnter_Does_Not_Throw_Exception ()
		{
			var lv = new ListView ();
			var top = new View ();
			top.Add (lv);
			var exception = Record.Exception (lv.SetFocus);
			Assert.Null (exception);
		}

// No longer needed given PR #2920
//		[Fact, AutoInitShutdown]
//		public void Clicking_On_Border_Is_Ignored ()
//		{
//			var selected = "";
//			var lv = new ListView {
//				Height = 5,
//				Width = 7,
//				BorderStyle = LineStyle.Single
//			};
//			lv.SetSource (new List<string> { "One", "Two", "Three", "Four" });
//			lv.SelectedItemChanged += (s, e) => selected = e.Value.ToString ();
//			Application.Top.Add (lv);
//			Application.Begin (Application.Top);

//			Assert.Equal (new Thickness (1), lv.Border.Thickness);
//			Assert.Equal (-1, lv.SelectedItem);
//			Assert.Equal ("", lv.Text);
//			TestHelpers.AssertDriverContentsWithFrameAre (@"
//┌─────┐
//│One  │
//│Two  │
//│Three│
//└─────┘", output);

//			Assert.True (lv.MouseEvent (new MouseEvent {
//				X = 0,
//				Y = 0,
//				Flags = MouseFlags.Button1Clicked
//			}));
//			Assert.Equal ("", selected);
//			Assert.Equal (-1, lv.SelectedItem);

//			Assert.True (lv.MouseEvent (new MouseEvent {
//				X = 0,
//				Y = 1,
//				Flags = MouseFlags.Button1Clicked
//			}));
//			Assert.Equal ("One", selected);
//			Assert.Equal (0, lv.SelectedItem);

//			Assert.True (lv.MouseEvent (new MouseEvent {
//				X = 0,
//				Y = 2,
//				Flags = MouseFlags.Button1Clicked
//			}));
//			Assert.Equal ("Two", selected);
//			Assert.Equal (1, lv.SelectedItem);

//			Assert.True (lv.MouseEvent (new MouseEvent {
//				X = 0,
//				Y = 3,
//				Flags = MouseFlags.Button1Clicked
//			}));
//			Assert.Equal ("Three", selected);
//			Assert.Equal (2, lv.SelectedItem);

//			Assert.True (lv.MouseEvent (new MouseEvent {
//				X = 0,
//				Y = 4,
//				Flags = MouseFlags.Button1Clicked
//			}));
//			Assert.Equal ("Three", selected);
//			Assert.Equal (2, lv.SelectedItem);
//		}
	}
}
