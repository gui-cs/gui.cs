﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ProcessTable", "Demonstrates TableView with the currently running processes.")]
[ScenarioCategory ("TableView")]
public class ProcessTable : Scenario
{
    private TableView tableView;

    public override void Setup ()
    {
        Win.Title = GetName ();
        Win.Y = 1; // menu
        Win.Height = Dim.Fill (1); // status bar

        tableView = new TableView { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
        tableView.Padding.EnableScrollBars = true;

        // First time
        CreateProcessTable ();

        // Then every second
        Application.AddTimeout (
                                TimeSpan.FromSeconds (1),
                                () =>
                                {
                                    CreateProcessTable ();

                                    return true;
                                }
                               );

        Win.Add (tableView);
    }

    private void CreateProcessTable ()
    {
        int ro = tableView.RowOffset;
        int co = tableView.ColumnOffset;

        tableView.Table = new EnumerableTableSource<Process> (
                                                              Process.GetProcesses (),
                                                              new Dictionary<string, Func<Process, object>>
                                                              {
                                                                  { "ID", p => p.Id },
                                                                  { "Name", p => p.ProcessName },
                                                                  { "Threads", p => p.Threads.Count },
                                                                  { "Virtual Memory", p => p.VirtualMemorySize64 },
                                                                  { "Working Memory", p => p.WorkingSet64 }
                                                              }
                                                             );

        tableView.RowOffset = ro;
        tableView.ColumnOffset = co;
        tableView.EnsureValidScrollOffsets ();
    }
}
