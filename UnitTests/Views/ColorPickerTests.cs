﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.ViewsTests {
	public class ColorPickerTests {
		[Fact]
		public void Constructors ()
		{
			var colorPicker = new ColorPicker ();
			Assert.Equal (ColorNames.Black, colorPicker.SelectedColor);
			Assert.Equal (new Point (0, 0), colorPicker.Cursor);
			Assert.True (colorPicker.CanFocus);

			colorPicker.BeginInit ();
			colorPicker.EndInit ();
			colorPicker.LayoutSubviews ();
			Assert.Equal (new Rect (0, 0, 32, 4), colorPicker.Frame);
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var colorPicker = new ColorPicker ();
			Assert.Equal (ColorNames.Black, colorPicker.SelectedColor);

			Assert.True (colorPicker.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (ColorNames.Blue, colorPicker.SelectedColor);

			Assert.True (colorPicker.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (ColorNames.BrightBlue, colorPicker.SelectedColor);

			Assert.True (colorPicker.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (ColorNames.DarkGray, colorPicker.SelectedColor);

			Assert.True (colorPicker.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (ColorNames.Black, colorPicker.SelectedColor);

			Assert.True (colorPicker.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (ColorNames.Black, colorPicker.SelectedColor);

			Assert.True (colorPicker.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (ColorNames.Black, colorPicker.SelectedColor);
		}

		[Fact]
		[AutoInitShutdown]
		public void MouseEvents ()
		{
			var colorPicker = new ColorPicker ();
			Assert.Equal (ColorNames.Black, colorPicker.SelectedColor);

			Assert.False (colorPicker.MouseEvent (new MouseEvent ()));

			Assert.True (colorPicker.MouseEvent (new MouseEvent () { Flags = MouseFlags.Button1Clicked, X = 4, Y = 0 }));
			Assert.Equal (ColorNames.Blue, colorPicker.SelectedColor);
		}

		[Fact]
		[AutoInitShutdown]
		public void SelectedColorAndCursor ()
		{
			var colorPicker = new ColorPicker ();
			colorPicker.SelectedColor = ColorNames.White;
			Assert.Equal (7, colorPicker.Cursor.X);
			Assert.Equal (1, colorPicker.Cursor.Y);

			colorPicker.SelectedColor = Color.Black;
			Assert.Equal (0, colorPicker.Cursor.X);
			Assert.Equal (0, colorPicker.Cursor.Y);

			colorPicker.Cursor = new Point (7, 1);
			Assert.Equal (ColorNames.White, colorPicker.SelectedColor);

			colorPicker.Cursor = new Point (0, 0);
			Assert.Equal (ColorNames.Black, colorPicker.SelectedColor);
		}
	}
}
