﻿#nullable enable

namespace Terminal.Gui;

public class MouseInterpreter
{
    /// <summary>
    /// Function for returning the current time. Use in unit tests to
    /// ensure repeatable tests.
    /// </summary>
    private Func<DateTime> Now { get; set; }

    /// <summary>
    /// How long to wait for a second click after the first before giving up and
    /// releasing event as a 'click'
    /// </summary>
    public TimeSpan DoubleClickThreshold { get; set; }

    /// <summary>
    /// How long to wait for a third click after the second before giving up and
    /// releasing event as a 'double click'
    /// </summary>
    public TimeSpan TripleClickThreshold { get; set; }

    /// <summary>
    /// How far between a mouse down and mouse up before it is considered a 'drag' rather
    /// than a 'click'. Console row counts for 2 units while column counts for only 1. Distance is
    /// measured in Euclidean distance.
    /// </summary>
    public double DragThreshold { get; set; }

    public MouseState CurrentState { get; private set; }

    private ButtonNarrative? [] _ongoingNarratives = new ButtonNarrative? [4];

    public Action<ButtonNarrative> Click { get; set; }

    public MouseInterpreter (
        Func<DateTime>? now = null,
        TimeSpan? doubleClickThreshold = null,
        TimeSpan? tripleClickThreshold = null,
        int dragThreshold = 5
    )
    {
        Now = now ?? (() => DateTime.Now);
        DoubleClickThreshold = doubleClickThreshold ?? TimeSpan.FromMilliseconds (500);
        TripleClickThreshold = tripleClickThreshold ?? TimeSpan.FromMilliseconds (1000);
        DragThreshold = dragThreshold;
    }

    public IEnumerable<ButtonNarrative> Process (MouseEventArgs e)
    {
        // Update narratives
       if (e.Flags.HasFlag (MouseFlags.Button1Pressed))
       {
           if (_ongoingNarratives [0] == null)
           {
               _ongoingNarratives [0] = BeginPressedNarrative (0,e);
           }
           else
           {
               _ongoingNarratives [0]?.Process (0,e.Position,true);
           }
       }
       else
       {
           _ongoingNarratives [0]?.Process (0,e.Position,false);
       }

       for (var i = 0; i < _ongoingNarratives.Length; i++)
       {
           ButtonNarrative? narrative = _ongoingNarratives [i];

           if (narrative != null)
           {
               if (ShouldRelease (narrative))
               {
                   yield return narrative;
                   _ongoingNarratives [i] = null;
               }
           }
       }
    }

    private bool ShouldRelease (ButtonNarrative narrative)
    {
        // TODO: needs to be way smarter
        if (narrative.NumberOfClicks > 0)
        {
            return true;
        }

        return false;
    }

    private ButtonNarrative BeginPressedNarrative (int buttonIdx, MouseEventArgs e)
    {
        return new ButtonNarrative
        {
            NumberOfClicks = 0,
            Now = Now,
            MouseStates =
            [
                new ButtonState
                {
                    Button = buttonIdx,
                    At = Now(),
                    Pressed = true,
                    Position = e.ScreenPosition,

                    /* TODO: Do these too*/
                    View = null,
                    Shift = false,
                    Ctrl = false,
                    Alt = false
                }
            ]
        };
    }

    /* TODO: Probably need this at some point
    public static double DistanceTo (Point p1, Point p2)
    {
        int deltaX = p2.X - p1.X;
        int deltaY = p2.Y - p1.Y;
        return Math.Sqrt (deltaX * deltaX + deltaY * deltaY);
    }*/
}

/// <summary>
/// Describes a completed narrative e.g. 'user triple clicked'.
/// </summary>
/// <remarks>Internally we can have a double click narrative that becomes
/// a triple click narrative. But we will not release both i.e. we don't say
/// user clicked then user double-clicked then user triple clicked</remarks>
public class ButtonNarrative
{
    public int NumberOfClicks { get; set; }

    /// <summary>
    /// Mouse states during which click was generated.
    /// N = 2x<see cref="NumberOfClicks"/>
    /// </summary>
    public List<ButtonState> MouseStates { get; set; } = new ();

    /// <summary>
    /// Function for returning the current time. Use in unit tests to
    /// ensure repeatable tests.
    /// </summary>
    public Func<DateTime> Now { get; set; }

    public void Process (int buttonIdx, Point position, bool pressed)
    {
        var last = MouseStates.Last ();

        // Still pressed
        if (last.Pressed && pressed)
        {
            // No change
            return;
        }

        NumberOfClicks++;
        MouseStates.Add (new ButtonState
        {
            Button = buttonIdx,
            At = Now(),
            Pressed = false,
            Position = position,

            /* TODO: Need these probably*/
            View = null,
            Shift = false,
            Ctrl = false,
            Alt = false,

        });
    }
}

public class MouseState
{
    public ButtonState[] ButtonStates = new ButtonState? [4];

    public Point Position;
}

public class ButtonState
{
    public required int Button { get; set; }
    /// <summary>
    ///     When the button entered its current state.
    /// </summary>
    public DateTime At { get; set; }

    /// <summary>
    /// <see langword="true"/> if the button is currently down
    /// </summary>
    public bool Pressed { get; set; }

    /// <summary>
    /// The screen location when the mouse button entered its current state
    /// (became pressed or was released)
    /// </summary>
    public Point Position { get; set; }

    /// <summary>
    /// The <see cref="View"/> (if any) that was at the <see cref="Position"/>
    /// when the button entered its current state.
    /// </summary>
    public View? View { get; set; }

    /// <summary>
    /// True if shift was provided by the console at the time the mouse
    ///  button entered its current state.
    /// </summary>
    public bool Shift { get; set; }

    /// <summary>
    /// True if control was provided by the console at the time the mouse
    /// button entered its current state.
    /// </summary>
    public bool Ctrl { get; set; }

    /// <summary>
    /// True if alt was held down at the time the mouse
    /// button entered its current state.
    /// </summary>
    public bool Alt { get; set; }
}