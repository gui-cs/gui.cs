﻿using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Terminal.Gui;
using Terminal.Gui.Graphs;
using static Terminal.Gui.OpenDialog;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "FileDialog2", Description: "Demonstrates how to the FileDialog2 class")]
	[ScenarioCategory ("Dialogs")]
	public class FileDialog2Examples : Scenario {
		private CheckBox cbMustExist;
		private CheckBox cbIcons;
		private CheckBox cbUseColors;
		private CheckBox cbCaseSensitive;
		private CheckBox cbAllowMultipleSelection;
		private CheckBox cbShowTreeBranchLines;
		private CheckBox cbAlwaysTableShowHeaders;

		private RadioGroup rgCaption;
		private RadioGroup rgOpenMode;
		private RadioGroup rgAllowedTypes;

		public override void Setup ()
		{
			var y = 0;
			var x = 1;

			cbMustExist = new CheckBox ("Must Exist") { Checked = true, Y = y++, X=x};
			Win.Add (cbMustExist);


			cbIcons = new CheckBox ("Icons") { Checked = true, Y = y++, X=x };
			Win.Add (cbIcons);

			cbUseColors = new CheckBox ("Use Colors") { Checked = false, Y = y++, X=x};
			Win.Add (cbUseColors);

			cbCaseSensitive = new CheckBox ("Case Sensitive Search") { Checked = false, Y = y++, X=x };
			Win.Add (cbCaseSensitive);

			cbAllowMultipleSelection = new CheckBox ("Multiple") { Checked = false, Y = y++, X=x };
			Win.Add (cbAllowMultipleSelection);

			cbShowTreeBranchLines = new CheckBox ("Tree Branch Lines") { Checked = true, Y = y++, X=x };
			Win.Add (cbShowTreeBranchLines);

			cbAlwaysTableShowHeaders = new CheckBox ("Always Show Headers") { Checked = true, Y = y++, X=x };
			Win.Add (cbAlwaysTableShowHeaders);

			y = 0;
			x = 24;

			Win.Add(new LineView(Orientation.Vertical){
				X = x++,
				Y = 1,
				Height = 4
			});
			Win.Add(new Label("Caption"){X=x++,Y=y++});

			rgCaption = new RadioGroup{X = x, Y=y};
			rgCaption.RadioLabels = new NStack.ustring[]{"Ok","Open","Save"};
			Win.Add(rgCaption);

			y = 0;
			x = 37;

			Win.Add(new LineView(Orientation.Vertical){
				X = x++,
				Y = 1,
				Height = 4
			});
			Win.Add(new Label("OpenMode"){X=x++,Y=y++});

			rgOpenMode = new RadioGroup{X = x, Y=y};
			rgOpenMode.RadioLabels = new NStack.ustring[]{"File","Directory","Mixed"};
			Win.Add(rgOpenMode);
			
			y = 5;
			x = 24;

			Win.Add(new LineView(Orientation.Vertical){
				X = x++,
				Y = y+1,
				Height = 4
			});
			Win.Add(new Label("Allowed"){X=x++,Y=y++});

			rgAllowedTypes = new RadioGroup{X = x, Y=y};
			rgAllowedTypes.RadioLabels = new NStack.ustring[]{"Any","Csv (Recommended)","Csv (Strict)"};
			Win.Add(rgAllowedTypes);

			var btn = new Button ($"Run Dialog") {
				X = 1,
				Y = 8
			};

			SetupHandler (btn);
			Win.Add (btn);
		}

		private void SetupHandler (Button btn)
		{
			btn.Clicked += () => {
				var fd = new FileDialog2() {
					OpenMode = Enum.Parse<OpenMode>(
						rgOpenMode.RadioLabels[rgOpenMode.SelectedItem].ToString()),
					MustExist = cbMustExist.Checked ?? false,
					AllowsMultipleSelection = cbAllowMultipleSelection.Checked ?? false,
				};

				fd.Style.OkButtonText = rgCaption.RadioLabels [rgCaption.SelectedItem].ToString ();

				if (cbIcons.Checked ?? false) {
					fd.IconGetter = GetIcon;
				}

				if(cbCaseSensitive.Checked ?? false) {

					fd.SearchMatcher = new CaseSensitiveSearchMatcher ();
				}

				fd.UseColors = cbUseColors.Checked ?? false;
				
				fd.Style.TreeStyle.ShowBranchLines = cbShowTreeBranchLines.Checked ?? false;
				fd.Style.TableStyle.AlwaysShowHeaders = cbAlwaysTableShowHeaders.Checked ?? false;

				if (rgAllowedTypes.SelectedItem > 0) {
					fd.AllowedTypes.Add (new FileDialog2.AllowedType ("Data File", ".csv", ".tsv"));
					fd.AllowedTypesIsStrict = rgAllowedTypes.SelectedItem > 1;
				}

				Application.Run (fd);

				if (fd.Canceled) {
					MessageBox.Query (
						"Canceled",
						"You canceled navigation and did not pick anything",
					"Ok");
				} else if (cbAllowMultipleSelection.Checked ?? false) {
					MessageBox.Query (
						"Chosen!",
						"You chose:" + Environment.NewLine +
						string.Join (Environment.NewLine, fd.MultiSelected.Select (m => m)),
						"Ok");
				} else {
					MessageBox.Query (
						"Chosen!",
						"You chose:" + Environment.NewLine + fd.Path,
						"Ok");
				}
			};
		}

		private class CaseSensitiveSearchMatcher : FileDialog2.ISearchMatcher {
			private string terms;

			public void Initialize (string terms)
			{
				this.terms = terms;
			}

			public bool IsMatch (FileSystemInfo f)
			{
				return f.Name.Contains (terms, StringComparison.CurrentCulture);
			}
		}

		private string GetIcon (FileSystemInfo arg)
		{
			// Typically most windows terminals will not have these unicode characters installed
			// so for the demo lets not bother having any icons on windows
			if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
				return arg is DirectoryInfo ? "\\" : null;
			}

			if (arg is DirectoryInfo) {
				return "\ua909 ";
			}

			return "\u2630 ";
		}
	}
}
