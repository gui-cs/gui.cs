﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rune = System.Rune;

namespace Terminal.Gui {
	/// <summary>
	/// Renders an overlay on another view at a given point that allows selecting
	/// from a range of 'autocomplete' options.
	/// </summary>
	public class Autocomplete {

		/// <summary>
		/// The maximum width of the autocomplete dropdown
		/// </summary>
		public int MaxWidth { get; set; } = 10;

		/// <summary>
		/// The maximum number of visible rows in the autocomplete dropdown to render
		/// </summary>
		public int MaxHeight { get; set; } = 6;

		/// <summary>
		/// True if the autocomplete should be considered open and visible
		/// </summary>
		public bool Visible { get; set; } = true;

		/// <summary>
		/// The strings that form the current list of suggestions to render
		/// </summary>
		public string [] Suggestions { get; set; } = new string [0];

		/// <summary>
		/// The currently selected index into <see cref="Suggestions"/> that the user has highlighted
		/// </summary>
		public int SelectedIdx { get; set; }

		/// <summary>
		/// The colors to use to render the overlay
		/// </summary>
		public ColorScheme ColorScheme { get; set; }

		/// <summary>
		/// The key that the user must press to accept the currently selected autocomplete suggestion
		/// </summary>
		public Key SelectionKey { get; set; } = Key.Enter;

		public Autocomplete ()
		{
			ColorScheme = new ColorScheme () {
				Normal = Application.Driver.MakeAttribute(Color.White,Color.Blue),
				HotNormal = Application.Driver.MakeAttribute (Color.Black, Color.BrightBlue),
			};
		}

		/// <summary>
		/// Renders the autocomplete dialog inside the given <paramref name="view"/> at the
		/// given point.
		/// </summary>
		/// <param name="view">The view the overlay should be rendered into</param>
		/// <param name="renderAt"></param>
		public void RenderOverlay (View view, Point renderAt)
		{
			if (!Visible || !view.HasFocus) {
				return;
			}

			view.Move (renderAt.X, renderAt.Y);
			for(int i=0;i<Math.Min(Suggestions.Length,MaxHeight); i++) {

				if(i== SelectedIdx) {
					Application.Driver.SetAttribute (ColorScheme.HotNormal);
				}
				else {
					Application.Driver.SetAttribute (ColorScheme.Normal);
				}

				view.Move (renderAt.X, renderAt.Y+i);
				Application.Driver.AddStr (Suggestions[i]);
			}
		}

		public bool ProcessKey (TextView hostControl, KeyEvent kb)
		{
			if(!Visible || Suggestions.Length == 0) {
				return false;
			}

			if (kb.Key == Key.CursorDown) {
				SelectedIdx = Math.Min (Suggestions.Length - 1, SelectedIdx + 1);
				hostControl.SetNeedsDisplay ();
				return true;
			}

			if (kb.Key == Key.CursorUp) {
				SelectedIdx = Math.Max (0, Math.Min(SelectedIdx,Suggestions.Length-1) - 1);
				hostControl.SetNeedsDisplay ();
				return true;
			}

			if(kb.Key == SelectionKey && SelectedIdx >=0 && SelectedIdx < Suggestions.Length) {

				var accepted = Suggestions [SelectedIdx];
								
				var typedSoFar = GetCurrentWord (hostControl) ?? "";
				
				if(typedSoFar.Length < accepted.Length) {
					accepted = accepted.Substring (typedSoFar.Length);
					hostControl.InsertText (accepted);
					return true;
				}

				return false;
			}

			return false;
		}


		/// <summary>
		/// Populates <see cref="Suggestions"/> with all strings in <paramref name="options"/> that
		/// match with the current cursor position/text in the <paramref name="hostControl"/>
		/// </summary>
		/// <param name="hostControl">The text view that you want suggestions for</param>
		/// <param name="options">All options that could ever be offered to the user</param>
		public void GenerateSuggestions (TextView hostControl, IEnumerable<string> options)
		{
			var currentWord = GetCurrentWord (hostControl); 

			if(string.IsNullOrWhiteSpace(currentWord)) {
				Suggestions = new string [0];
			}
			else {
				Suggestions = options.Where (o => 
				o.StartsWith (currentWord, StringComparison.CurrentCultureIgnoreCase) &&
				!o.Equals(currentWord,StringComparison.CurrentCultureIgnoreCase)
				).ToArray ();
			}
		}

		private string GetCurrentWord (TextView hostControl)
		{
			var currentLine = hostControl.GetCurrentLine ();
			var cursorPosition = Math.Min (hostControl.CurrentColumn, currentLine.Count);
			return IdxToWord (currentLine, cursorPosition);
		}

		private string IdxToWord (List<Rune> line, int idx)
		{
			StringBuilder sb = new StringBuilder ();

			// do not generate suggestions if the cursor is positioned in the middle of a word
			bool areMidWord;

			if(idx == line.Count) {
				// the cursor positioned at the very end of the line
				areMidWord = false;
			}
			else {
				// we are in the middle of a word if the cursor is over a letter/number
				areMidWord = IsWordChar (line [idx]);
			}

			// if we are in the middle of a word then there is no way to autocomplete that word
			if(areMidWord) {
				return null;
			}

			// we are at the end of a word.  Work out what has been typed so far
			while(idx-- > 0) {

				if(IsWordChar(line [idx])) {
					sb.Insert(0,(char)line [idx]);
				}
				else {
					break;
				}
			}
			return sb.ToString ();
		}

		/// <summary>
		/// Return true if the given symbol should be considered part of a word
		/// and can be contained in matches.  Base behaviour is to use <see cref="char.IsLetterOrDigit(char)"/>
		/// </summary>
		/// <param name="rune"></param>
		/// <returns></returns>
		protected virtual bool IsWordChar (Rune rune)
		{
			return Char.IsLetterOrDigit ((char)rune);
		}
	}
}
