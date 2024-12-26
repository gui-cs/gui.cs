﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Terminal.Gui;

/// <inheritdoc/>
public class MainLoop<T> : IMainLoop<T>
{
    /// <inheritdoc/>
    public ITimedEvents TimedEvents { get; private set; }

    public ConcurrentQueue<T> InputBuffer { get; private set; } = new ();

    public IInputProcessor InputProcessor { get; private set; }

    public IOutputBuffer OutputBuffer { get; private set; } = new OutputBuffer();

    public IConsoleOutput Out { get;private set; }
    public AnsiRequestScheduler AnsiRequestScheduler { get; private set; }

    public IWindowSizeMonitor WindowSizeMonitor { get; private set; }

    /// <summary>
    /// Determines how to get the current system type, adjust
    /// in unit tests to simulate specific timings.
    /// </summary>
    public Func<DateTime> Now { get; set; } = () => DateTime.Now;

    static Meter s_meter = new Meter ("Terminal.Gui");


    static Histogram<int> s_iterations = s_meter.CreateHistogram<int> ("Terminal.Gui.iteration");

    public void Initialize (ITimedEvents timedEvents, ConcurrentQueue<T> inputBuffer, IInputProcessor inputProcessor, IConsoleOutput consoleOutput)
    {
        InputBuffer = inputBuffer;
        Out = consoleOutput;
        InputProcessor = inputProcessor;

        TimedEvents = timedEvents;
        AnsiRequestScheduler = new AnsiRequestScheduler (InputProcessor.GetParser ());

        WindowSizeMonitor = new WindowSizeMonitor (Out,OutputBuffer);
    }

    /// <inheritdoc/>
    public void Iteration ()
    {
            var dt = Now();

            IterationImpl ();

            var took = Now() - dt;
            var sleepFor = TimeSpan.FromMilliseconds (50) - took;

            s_iterations.Record (took.Milliseconds);

            if (sleepFor.Milliseconds > 0)
            {
                Task.Delay (sleepFor).Wait ();
            }
    }

    public void IterationImpl ()
    {
        InputProcessor.ProcessQueue ();

        if (Application.Top != null)
        {

            bool needsDrawOrLayout = AnySubviewsNeedDrawn(Application.Top);

            // TODO: throttle this
            WindowSizeMonitor.Poll ();

            if (needsDrawOrLayout)
            {
                // TODO: Test only
                Application.LayoutAndDraw (true);

                Out.Write (OutputBuffer);
            }
        }

        TimedEvents.LockAndRunTimers ();

        TimedEvents.LockAndRunIdles ();
    }


    private bool AnySubviewsNeedDrawn (View v)
    {
        if (v.NeedsDraw || v.NeedsLayout)
        {
            return true;
        }

        foreach(var subview in v.Subviews )
        {
            if (AnySubviewsNeedDrawn (subview))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void Dispose ()
    { // TODO release managed resources here
    }
}