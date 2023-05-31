﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class TreeTableSourceTests {

	readonly ITestOutputHelper output;

	public TreeTableSourceTests (ITestOutputHelper output)
	{
		this.output = output;
	}

	[Fact, AutoInitShutdown]
	public void TestTreeTableSource_BasicExpanding ()
	{
		var tv = GetTreeTable (out _);

		//var wrapper = new CheckBoxTableSourceWrapperByIndex (tv, tv.Table);
		//tv.Table = wrapper;

		tv.Draw ();

		string expected =
			@"
│Name          │Description            │
├──────────────┼───────────────────────┤
│├+Lost Highway│Exciting night road    │
│└+Route 66    │Great race course      │";

		TestHelpers.AssertDriverContentsAre (expected, output);

		Assert.Equal(2, tv.Table.Rows);

		// top left is selected cell
		Assert.Equal (0, tv.SelectedRow);
		Assert.Equal(0, tv.SelectedColumn);

		// when pressing right we should expand the top route
		Application.Top.ProcessHotKey(new KeyEvent (Key.CursorRight, new KeyModifiers ()));


		tv.Draw ();

		expected =
			@"
│Name             │Description         │
├─────────────────┼────────────────────┤
│├-Lost Highway   │Exciting night road │
││ ├─Ford Trans-Am│Talking thunderbird │
││ └─DeLorean     │Time travelling car │
│└+Route 66       │Great race course   │
";

		TestHelpers.AssertDriverContentsAre (expected, output);

		tv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));
		tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));
	}



	interface IDescribedThing {
		string Name { get; }
		string Description { get; }
	}

	class Road : IDescribedThing {
		public string Name { get; set; }
		public string Description { get; set; }

		public List<Car> Traffic { get; set; }
	}

	class Car : IDescribedThing {
		public string Name { get; set; }
		public string Description { get; set; }
	}


	private TableView GetTreeTable (out TreeView<IDescribedThing> tree)
	{
		var tableView = new TableView ();
		tableView.ColorScheme = Colors.TopLevel;
		tableView.ColorScheme = Colors.TopLevel;
		tableView.Bounds = new Rect (0, 0, 40, 6);

		tableView.Style.ShowHorizontalHeaderUnderline = true;
		tableView.Style.ShowHorizontalHeaderOverline = false;
		tableView.Style.AlwaysShowHeaders = true;
		tableView.Style.SmoothHorizontalScrolling = true;

		tree = new TreeView<IDescribedThing> ();
		tree.AspectGetter = (d) => d.Name;

		tree.TreeBuilder = new DelegateTreeBuilder<IDescribedThing> (
			(d) => d is Road r ? r.Traffic : Enumerable.Empty<IDescribedThing> ()
			);

		tree.AddObject (new Road {
			Name = "Lost Highway",
			Description = "Exciting night road",
			Traffic = new List<Car> {
				new Car { Name = "Ford Trans-Am", Description = "Talking thunderbird car"},
				new Car { Name = "DeLorean", Description = "Time travelling car"}
			}
		});

		tree.AddObject (new Road {
			Name = "Route 66",
			Description = "Great race course",
			Traffic = new List<Car> {
				new Car { Name = "Pink Compact", Description = "Penelope Pitstop's car"},
				new Car { Name = "Mean Machine", Description = "Dick Dastardly's car"}
			}
		});

		tableView.Table = new TreeTableSource<IDescribedThing> (tableView,"Name",tree,
			new () {
				{"Description",(d)=>d.Description }
			});

		tableView.BeginInit ();
		tableView.EndInit ();
		tableView.LayoutSubviews ();


		tableView.Style.GetOrCreateColumnStyle (1).MinAcceptableWidth = 1;

		tableView.LayoutSubviews ();

		Application.Top.Add (tableView);
		Application.Top.EnsureFocus ();
		Assert.Equal (tableView, Application.Top.MostFocused);

		return tableView;
	}

}
