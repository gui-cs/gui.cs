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
        CanFocus = false;

        X = Pos.AnchorEnd ();
        Y = 0;
        Width = Dim.Func (() => Math.Min (SuperView!.Viewport.Width / 3, MaxLength + GetAdornmentsThickness ().Horizontal));
        Height = Dim.Fill ();

        ExpandButton = new ()
        {
            Orientation = Orientation.Horizontal
        };

        Initialized += EventLog_Initialized;
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
                                                 _eventSource.Add ($"Initialized: {GetIdentifyingString (sender)}");
                                                 MoveEnd ();
                                             };

                _viewToLog.MouseClick += (s, args) =>
                {
                    View? sender = s as View;
                    _eventSource.Add ($"MouseClick: {args}");
                    MoveEnd ();
                };

                _viewToLog.HandlingHotKey += (s, args) =>
                                        {
                                            View? sender = s as View;
                                            _eventSource.Add ($"HandlingHotKey: {args.Context.Command} {args.Context.Data}");
                                            MoveEnd ();
                                        };
                _viewToLog.Selecting += (s, args) =>
                                        {
                                            View? sender = s as View;
                                            _eventSource.Add ($"Selecting: {args.Context.Command} {args.Context.Data}");
                                            MoveEnd ();
                                        };
                _viewToLog.Accepting += (s, args) =>
                                        {
                                            View? sender = s as View;
                                            _eventSource.Add ($"Accepting: {args.Context.Command} {args.Context.Data}");
                                            MoveEnd ();
                                        };
            }
        }
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
