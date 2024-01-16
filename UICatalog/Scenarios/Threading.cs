﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;

namespace UICatalog.Scenarios;
[ScenarioMetadata (Name: "Threading", Description: "Demonstration of how to use threading in different ways")]
[ScenarioCategory ("Threading")]
public class Threading : Scenario {
	private Action _action;
	private Action _lambda;
	private EventHandler _handler;
	private Action _sync;

	private ListView _itemsList;
	private Button _btnActionCancel;
	List<string> _log = new List<string> ();
	private ListView _logJob;

	public override void Setup ()
	{
		_action = LoadData;
		_lambda = async () => {
			_itemsList.Source = null;
			LogJob ("Loading task lambda");
			var items = await LoadDataAsync ();
			LogJob ("Returning from task lambda");
			_itemsList.SetSource (items);
		};
		_handler = async (s, e) => {
			_itemsList.Source = null;
			LogJob ("Loading task handler");
			var items = await LoadDataAsync ();
			LogJob ("Returning from task handler");
			_itemsList.SetSource (items);

		};
		_sync = () => {
			_itemsList.Source = null;
			LogJob ("Loading task synchronous");
			List<string> items = new List<string> () { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten" };
			LogJob ("Returning from task synchronous");
			_itemsList.SetSource (items);
		};

		_btnActionCancel = new Button ("Cancelable Load Items") { X = 1, Y = 1 };
		_btnActionCancel.Clicked += (s, e) => Application.Invoke (CallLoadItemsAsync);

		Win.Add (new Label ("Data Items:") {
			X = Pos.X (_btnActionCancel),
			Y = Pos.Y (_btnActionCancel) + 4,
		});

		_itemsList = new ListView {
			X = Pos.X (_btnActionCancel),
			Y = Pos.Y (_btnActionCancel) + 6,
			Width = 10,
			Height = 10,
			ColorScheme = Colors.TopLevel
		};

		Win.Add (new Label ("Task Logs:") {
			X = Pos.Right (_itemsList) + 10,
			Y = Pos.Y (_btnActionCancel) + 4,
		});

		_logJob = new ListView (_log) {
			X = Pos.Right (_itemsList) + 10,
			Y = Pos.Y (_itemsList),
			Width = 50,
			Height = Dim.Fill (),
			ColorScheme = Colors.TopLevel
		};

		var text = new TextField ("Type anything after press the button") { X = 1, Y = 3, Width = 100 };

		var _btnAction = new Button ("Load Data Action") { X = 80, Y = 10 };
		_btnAction.Clicked += (s, e) => _action.Invoke ();
		var _btnLambda = new Button ("Load Data Lambda") { X = 80, Y = 12 };
		_btnLambda.Clicked += (s, e) => _lambda.Invoke ();
		var _btnHandler = new Button ("Load Data Handler") { X = 80, Y = 14 };
		_btnHandler.Clicked += (s, e) => _handler.Invoke (null, new EventArgs ());
		var _btnSync = new Button ("Load Data Synchronous") { X = 80, Y = 16 };
		_btnSync.Clicked += (s, e) => _sync.Invoke ();
		var _btnMethod = new Button ("Load Data Method") { X = 80, Y = 18 };
		_btnMethod.Clicked += async (s, e) => await MethodAsync ();
		var _btnClearData = new Button ("Clear Data") { X = 80, Y = 20 };
		_btnClearData.Clicked += (s, e) => { _itemsList.Source = null; LogJob ("Cleaning Data"); };
		var _btnQuit = new Button ("Quit") { X = 80, Y = 22 };
		_btnQuit.Clicked += (s, e) => Application.RequestStop ();

		Win.Add (_itemsList, _btnActionCancel, _logJob, text, _btnAction, _btnLambda, _btnHandler, _btnSync, _btnMethod, _btnClearData, _btnQuit);

		void Top_Loaded (object sender, EventArgs args)
		{
			_btnActionCancel.SetFocus ();
			Application.Top.Loaded -= Top_Loaded;
		}
		Application.Top.Loaded += Top_Loaded;
	}

	private async void LoadData ()
	{
		_itemsList.Source = null;
		LogJob ("Loading task");
		var items = await LoadDataAsync ();
		LogJob ("Returning from task");
		_itemsList.SetSource (items);
	}

	private void LogJob (string job)
	{
		_log.Add (job);
		_logJob.MoveDown ();
	}

	private async Task<List<string>> LoadDataAsync ()
	{
		_itemsList.Source = null;
		LogJob ("Starting delay");
		await Task.Delay (3000);
		LogJob ("Finished delay");
		return new List<string> () { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten" };
	}

	private async Task MethodAsync ()
	{
		_itemsList.Source = null;
		LogJob ("Loading task method");
		List<string> items = new List<string> () { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten" };
		await Task.Delay (3000);
		LogJob ("Returning from task method");
		await _itemsList.SetSourceAsync (items);
		_itemsList.SetNeedsDisplay ();
	}

	private CancellationTokenSource cancellationTokenSource;

	private async void CallLoadItemsAsync ()
	{
		cancellationTokenSource = new CancellationTokenSource ();
		_itemsList.Source = null;
		LogJob ($"Clicked the button");
		if (_btnActionCancel.Text == "Cancel") {
			_btnActionCancel.Text = "Cancelable Load Items";
			cancellationTokenSource.Cancel ();
		} else
			_btnActionCancel.Text = "Cancel";
		try {
			if (cancellationTokenSource.Token.IsCancellationRequested)
				cancellationTokenSource.Token.ThrowIfCancellationRequested ();
			LogJob ($"Calling task Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}");
			var items = await Task.Run (LoadItemsAsync, cancellationTokenSource.Token);
			if (!cancellationTokenSource.IsCancellationRequested) {
				LogJob ($"Returned from task Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}");
				_itemsList.SetSource (items);
				LogJob ($"Finished populate list view Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}");
				_btnActionCancel.Text = "Cancelable Load Items";
			} else {
				LogJob ("Task was canceled!");
			}
		} catch (OperationCanceledException ex) {
			LogJob (ex.Message);
		}
	}

	private async Task<List<string>> LoadItemsAsync ()
	{
		// Do something that takes lot of times.
		LogJob ($"Starting delay Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}");
		await Task.Delay (5000);
		LogJob ($"Finished delay Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}");
		return new List<string> () { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten" };
	}
}
