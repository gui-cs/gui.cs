#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

/// <summary>
///     An event log that automatically shows the events that are raised.
/// </summary>
/// <remarks>
/// </remarks>
/// </example>
public class EventLog : ListView
{
    public EventLog ()
    {
        Title = "Event Log";
        CanFocus = true;

        X = Pos.AnchorEnd ();
        Y = 0;
        Width = Dim.Func (() =>
                          {
                              if (!IsInitialized)
                              {
                                  return 0;
                              }
                              return Math.Min (SuperView!.Viewport.Width / 3, MaxLength + GetAdornmentsThickness ().Horizontal);
                          });
        Height = Dim.Fill ();

        ExpandButton = new ()
        {
            Orientation = Orientation.Horizontal
        };

        Initialized += EventLog_Initialized;

        HorizontalScrollBar.AutoShow = true;
        VerticalScrollBar.AutoShow = true;

        AddCommand (Command.DeleteAll,
                   () =>
                   {
                       _eventSource.Clear ();

                       return true;
                   });

        KeyBindings.Add (Key.Delete, Command.DeleteAll);

    }
    public ExpanderButton? ExpandButton { get; }

    private readonly ObservableCollection<string> _eventSource = [];

    private View? _viewToLog;

    public View? ViewToLog
    {
        get => _viewToLog;
        set
        {
            if (_viewToLog == value)
            {
                return;
            }

            _viewToLog = value;

            if (_viewToLog is { })
            {
                _viewToLog.Initialized += (s, args) =>
                                             {
                                                 View? sender = s as View;
                                                 Log ($"Initialized: {GetIdentifyingString (sender)}");
                                             };

                _viewToLog.MouseClick += (s, args) =>
                {
                    Log ($"MouseClick: {args}");
                };

                _viewToLog.HandlingHotKey += (s, args) =>
                                        {
                                            Log ($"HandlingHotKey: {args.Context.Command} {args.Context.Data}");
                                        };
                _viewToLog.Selecting += (s, args) =>
                                        {
                                            Log ($"Selecting: {args.Context.Command} {args.Context.Data}");
                                        };
                _viewToLog.Accepting += (s, args) =>
                                        {
                                            Log ($"Accepting: {args.Context.Command} {args.Context.Data}");
                                        };
            }
        }
    }

    public void Log (string text)
    {
        _eventSource.Add (text);
        MoveEnd ();
    }

    private void EventLog_Initialized (object? _, EventArgs e)
    {
        Border?.Add (ExpandButton!);
        Source = new ListWrapper<string> (_eventSource);
    }
    private string GetIdentifyingString (View? view)
    {
        if (view is null)
        {
            return "null";
        }

        if (!string.IsNullOrEmpty (view.Title))
        {
            return view.Title;
        }

        if (!string.IsNullOrEmpty (view.Text))
        {
            return view.Text;
        }

        return view.GetType ().Name;
    }
}
