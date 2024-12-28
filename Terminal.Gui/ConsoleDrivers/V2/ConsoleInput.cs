﻿#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Terminal.Gui;

public abstract class ConsoleInput<T> : IConsoleInput<T>
{
    private ConcurrentQueue<T>? _inputBuffer;

    /// <summary>
    ///     Determines how to get the current system type, adjust
    ///     in unit tests to simulate specific timings.
    /// </summary>
    public Func<DateTime> Now { get; set; } = () => DateTime.Now;

    private readonly Histogram<int> drainInputStream = Logging.Meter.CreateHistogram<int> ("Drain Input (ms)");

    /// <inheritdoc/>
    public virtual void Dispose () { }

    /// <inheritdoc/>
    public void Initialize (ConcurrentQueue<T> inputBuffer) { _inputBuffer = inputBuffer; }

    /// <inheritdoc/>
    public void Run (CancellationToken token)
    {
        try
        {
            if (_inputBuffer == null)
            {
                throw new ("Cannot run input before Initialization");
            }

            do
            {
                DateTime dt = Now ();

                while (Peek ())
                {
                    foreach (T r in Read ())
                    {
                        _inputBuffer.Enqueue (r);
                    }
                }

                TimeSpan took = Now () - dt;
                TimeSpan sleepFor = TimeSpan.FromMilliseconds (20) - took;

                drainInputStream.Record (took.Milliseconds);

                if (sleepFor.Milliseconds > 0)
                {
                    Task.Delay (sleepFor, token).Wait (token);
                }

                token.ThrowIfCancellationRequested ();
            }
            while (!token.IsCancellationRequested);
        }
        catch (OperationCanceledException)
        { }
    }

    /// <summary>
    ///     When implemented in a derived class, returns true if there is data available
    ///     to read from console.
    /// </summary>
    /// <returns></returns>
    protected abstract bool Peek ();

    /// <summary>
    ///     Returns the available data without blocking, called when <see cref="Peek"/>
    ///     returns <see langword="true"/>.
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerable<T> Read ();
}
