﻿using System;
using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata (Name: "Dialogs", Description: "Demonstrates how to the Dialog class")]
[ScenarioCategory ("Dialogs")]
public class Dialogs : Scenario {
	static int CODE_POINT = '你'; // We know this is a wide char

	public override void Setup ()
	{
		var frame = new FrameView {
			X = Pos.Center (),
			Y = 1,
			Width = Dim.Percent (75),
			Title = "Dialog Options"
		};

		var label = new Label { X = 0, Y = 0, Width = 15, Height = 1, TextAlignment = Terminal.Gui.TextAlignment.Right, Text = "Width:" };
		frame.Add (label);

		var widthEdit = new TextField ("0") {
			X = Pos.Right (label) + 1,
			Y = Pos.Top (label),
			Width = 5,
			Height = 1
		};
		frame.Add (widthEdit);

		label = new Label {
			X = 0,
			Y = Pos.Bottom (label),
			Width = Dim.Width (label),
			Height = 1,
			TextAlignment = Terminal.Gui.TextAlignment.Right,
			Text = "Height:"
		};
		frame.Add (label);

		var heightEdit = new TextField ("0") {
			X = Pos.Right (label) + 1,
			Y = Pos.Top (label),
			Width = 5,
			Height = 1
		};
		frame.Add (heightEdit);

		frame.Add (new Label {
			X = Pos.Right (widthEdit) + 2,
			Y = Pos.Top (widthEdit),
			Text = "If height & width are both 0,"
		});
		frame.Add (new Label { X = Pos.Right(heightEdit) + 2, Y = Pos.Top(heightEdit), Text = "the Dialog will size to 80% of container." });

		label = new Label { X = 0, Y = Pos.Bottom(label), Width = Dim.Width(label), Height = 1, TextAlignment = Terminal.Gui.TextAlignment.Right, Text = "Title:" };
		frame.Add (label);

		var titleEdit = new TextField ("Title") {
			X = Pos.Right (label) + 1,
			Y = Pos.Top (label),
			Width = Dim.Fill (),
			Height = 1
		};
		frame.Add (titleEdit);

		label = new Label { X = 0, Y = Pos.Bottom(label), Width = Dim.Width(label), Height = 1, TextAlignment = Terminal.Gui.TextAlignment.Right, Text = "Num Buttons:" };
		frame.Add (label);

		var numButtonsEdit = new TextField ("3") {
			X = Pos.Right (label) + 1,
			Y = Pos.Top (label),
			Width = 5,
			Height = 1
		};
		frame.Add (numButtonsEdit);

		var glyphsNotWords = new CheckBox {
			X = Pos.Left (numButtonsEdit),
			Y = Pos.Bottom (label),
			TextAlignment = Terminal.Gui.TextAlignment.Right,
			Text = $"Add {Char.ConvertFromUtf32 (CODE_POINT)} to button text to stress wide char support",
			Checked = false
		};
		frame.Add (glyphsNotWords);

		label = new Label { X = 0, Y = Pos.Bottom(glyphsNotWords), TextAlignment = Terminal.Gui.TextAlignment.Right, Text = "Button Style:" };
		frame.Add (label);

		var styleRadioGroup = new RadioGroup (new string [] { "_Center", "_Justify", "_Left", "_Right" }) {
			X = Pos.Right (label) + 1,
			Y = Pos.Top (label),
		};
		frame.Add (styleRadioGroup);

		frame.ValidatePosDim = true;
		void Top_LayoutComplete (object sender, EventArgs args)
		{
			frame.Height =
				widthEdit.Frame.Height +
				heightEdit.Frame.Height +
				titleEdit.Frame.Height +
				numButtonsEdit.Frame.Height +
				glyphsNotWords.Frame.Height +
				styleRadioGroup.Frame.Height + frame.GetAdornmentsThickness ().Vertical;
		}
		Application.Top.LayoutComplete += Top_LayoutComplete;

		Win.Add (frame);

		label = new Label {
			X = Pos.Center (),
			Y = Pos.Bottom (frame) + 4,
			Height = 1,
			TextAlignment = Terminal.Gui.TextAlignment.Right,
			Text = "Button Pressed:"
		};
		Win.Add (label);

		var buttonPressedLabel = new Label { X = Pos.Center(), Y = Pos.Bottom(frame) + 5, Width = 25, Height = 1, ColorScheme = Colors.ColorSchemes["Error"], Text = " " };
		// glyphsNotWords
		// false:var btnText = new [] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
		// true: var btnText = new [] { "0", "\u2780", "➁", "\u2783", "\u2784", "\u2785", "\u2786", "\u2787", "\u2788", "\u2789" };
		// \u2781 is ➁ dingbats \ufb70 is	

		var showDialogButton = new Button {
			X = Pos.Center (),
			Y = Pos.Bottom (frame) + 2,
			IsDefault = true,
			Text = "_Show Dialog"
		};
		showDialogButton.Clicked += (s, e) => {
			var dlg = CreateDemoDialog (widthEdit, heightEdit, titleEdit, numButtonsEdit, glyphsNotWords, styleRadioGroup, buttonPressedLabel);
			Application.Run (dlg);
		};

		Win.Add (showDialogButton);

		Win.Add (buttonPressedLabel);
	}

	Dialog CreateDemoDialog (TextField widthEdit, TextField heightEdit, TextField titleEdit, TextField numButtonsEdit, CheckBox glyphsNotWords, RadioGroup styleRadioGroup, Label buttonPressedLabel)
	{
		Dialog dialog = null;
		try {

			int width = 0;
			int.TryParse (widthEdit.Text, out width);
			int height = 0;
			int.TryParse (heightEdit.Text, out height);
			int numButtons = 3;
			int.TryParse (numButtonsEdit.Text, out numButtons);

			var buttons = new List<Button> ();
			var clicked = -1;
			for (int i = 0; i < numButtons; i++) {
				int buttonId = i;
				Button button = null;
				if (glyphsNotWords.Checked == true) {
					buttonId = i;
					button = new Button {
						Text = NumberToWords.Convert (buttonId) + " " + Char.ConvertFromUtf32 (buttonId + CODE_POINT),
						IsDefault = buttonId == 0
					};
				} else {
					button = new Button {
						Text = NumberToWords.Convert (buttonId),
						IsDefault = buttonId == 0
					};
				}
				button.Clicked += (s, e) => {
					clicked = buttonId;
					Application.RequestStop ();
				};
				buttons.Add (button);
			}
			//if (buttons.Count > 1) {
			//	buttons [1].Text = "Accept";
			//	buttons [1].IsDefault = true;
			//	buttons [0].Visible = false;
			//	buttons [0].Text = "_Back";
			//	buttons [0].IsDefault = false;
			//}

			// This tests dynamically adding buttons; ensuring the dialog resizes if needed and 
			// the buttons are laid out correctly
			dialog = new Dialog (buttons.ToArray ()) {
				Title = titleEdit.Text,
				ButtonAlignment = (Dialog.ButtonAlignments)styleRadioGroup.SelectedItem
			};
			if (height != 0 || width != 0) {
				dialog.Height = height;
				dialog.Width = width;
			}

			var add = new Button {
				X = Pos.Center (),
				Y = Pos.Center (),
				Text = "Add a button"
			};
			add.Clicked += (s, e) => {
				var buttonId = buttons.Count;
				Button button;
				if (glyphsNotWords.Checked == true) {
					button = new Button {
						Text = NumberToWords.Convert (buttonId) + " " + Char.ConvertFromUtf32 (buttonId + CODE_POINT),
						IsDefault = buttonId == 0
					};
				} else {
					button = new Button {
						Text = NumberToWords.Convert (buttonId),
						IsDefault = buttonId == 0
					};
				}
				button.Clicked += (s, e) => {
					clicked = buttonId;
					Application.RequestStop ();

				};
				buttons.Add (button);
				dialog.AddButton (button);
				if (buttons.Count > 1) {
					button.TabIndex = buttons [buttons.Count - 2].TabIndex + 1;
				}
			};
			dialog.Add (add);

			var addChar = new Button {
				X = Pos.Center (),
				Y = Pos.Center () + 1,
				Text = $"Add a {Char.ConvertFromUtf32 (CODE_POINT)} to each button"
			};
			addChar.Clicked += (s, e) => {
				foreach (var button in buttons) {
					button.Text += Char.ConvertFromUtf32 (CODE_POINT);
				}
				dialog.LayoutSubviews ();
			};
			dialog.Closed += (s, e) => {
				buttonPressedLabel.Text = $"{clicked}";
			};
			dialog.Add (addChar);

		} catch (FormatException) {
			buttonPressedLabel.Text = "Invalid Options";
		}
		return dialog;
	}
}