﻿// These classes use a keybinding system based on the design implemented in Scintilla.Net which is an MIT licensed open source project https://github.com/jacobslusser/ScintillaNET/blob/master/src/ScintillaNET/Command.cs

using System;

namespace Terminal.Gui {

	/// <summary>
	/// Actions which can be performed by the application or bound to keys in a <see cref="View"/> control.
	/// </summary>
	public enum Command {

		/// <summary>
		/// Moves the caret down one line.
		/// </summary>
		LineDown,

		/// <summary>
		/// Extends the selection down one line.
		/// </summary>
		LineDownExtend,

		/// <summary>
		/// Scrolls down one line.
		/// </summary>
		LineScrollDown,

		// --------------------------------------------------------------------

		/// <summary>
		/// Moves the caret up one line.
		/// </summary>
		LineUp,

		/// <summary>
		/// Extends the selection up one line.
		/// </summary>
		LineUpExtend,

		/// <summary>
		/// Scrolls up one line.
		/// </summary>
		LineScrollUp,

		/// <summary>
		/// Moves the caret left one character.
		/// </summary>
		CharLeft,

		/// <summary>
		/// Extends the selection left one character.
		/// </summary>
		CharLeftExtend,

		/// <summary>
		/// Moves the caret right one character.
		/// </summary>
		CharRight,

		/// <summary>
		/// Extends the selection right one character.
		/// </summary>
		CharRightExtend,

		/// <summary>
		/// Moves the caret to the start of the previous word.
		/// </summary>
		WordLeft,

		/// <summary>
		/// Extends the selection to the start of the previous word.
		/// </summary>
		WordLeftExtend,

		/// <summary>
		/// Moves the caret to the start of the next word.
		/// </summary>
		WordRight,

		/// <summary>
		/// Extends the selection to the start of the next word.
		/// </summary>
		WordRightExtend,

		/// <summary>
		/// Move the page down.
		/// </summary>
		PageDown,

		/// <summary>
		/// Move the page up.
		/// </summary>
		PageUp,

		/// <summary>
		/// Move to begin.
		/// </summary>
		Home,

		/// <summary>
		/// Move to end.
		/// </summary>
		End,

		/// <summary>
		/// Open selected item.
		/// </summary>
		OpenSelectedItem,

		/// <summary>
		/// Toggle the checked state.
		/// </summary>
		ToggleChecked,

		/// <summary>
		/// Executes a hot key event.
		/// </summary>
		ExecuteHotKey,

		/// <summary>
		/// Executes a cold key event.
		/// </summary>
		ExecuteColdKey,

		/// <summary>
		/// Accepts the current state (e.g. selection, button press etc)
		/// </summary>
		Accept,

		/// <summary>
		/// Toggles the Expanded or collapsed state of a a list or item (with subitems)
		/// </summary>
		ToggleExpandCollapse,

		/// <summary>
		/// Expands a list or item (with subitems)
		/// </summary>
		Expand,

		/// <summary>
		/// Collapses a list or item (with subitems)
		/// </summary>
		Collapse,

		/// <summary>
		/// Cancels any current temporary states on the control e.g. expanding
		/// a combo list
		/// </summary>
		Cancel,

		/// <summary>
		/// Unix emulation
		/// </summary>
		UnixEmulation
	}
}