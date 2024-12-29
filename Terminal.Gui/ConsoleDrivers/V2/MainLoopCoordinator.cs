﻿using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui;

/// <summary>
///<para>
/// Handles creating the input loop thread and bootstrapping the
/// <see cref="MainLoop{T}"/> that handles layout/drawing/events etc.
/// </para>
/// <para>This class is designed to be managed by <see cref="ApplicationV2"/></para>
/// </summary>
/// <typeparam name="T"></typeparam>
internal class MainLoopCoordinator<T> : IMainLoopCoordinator
{
    private readonly Func<IConsoleInput<T>> _inputFactory;
    private readonly ConcurrentQueue<T> _inputBuffer;
    private readonly IInputProcessor _inputProcessor;
    private readonly IMainLoop<T> _loop;
    private readonly CancellationTokenSource _tokenSource = new ();
    private readonly Func<IConsoleOutput> _outputFactory;
    private IConsoleInput<T> _input;
    private IConsoleOutput _output;
    private readonly object _oLockInitialization = new ();
    private ConsoleDriverFacade<T> _facade;
    private Task _inputTask;
    private readonly ITimedEvents _timedEvents;

    private readonly SemaphoreSlim _startupSemaphore = new (0, 1);

    /// <summary>
    ///     Creates a new coordinator
    /// </summary>
    /// <param name="timedEvents"></param>
    /// <param name="inputFactory">
    ///     Function to create a new input. This must call <see langword="new"/>
    ///     explicitly and cannot return an existing instance. This requirement arises because Windows
    ///     console screen buffer APIs are thread-specific for certain operations.
    /// </param>
    /// <param name="inputBuffer"></param>
    /// <param name="inputProcessor"></param>
    /// <param name="outputFactory">
    ///     Function to create a new output. This must call <see langword="new"/>
    ///     explicitly and cannot return an existing instance. This requirement arises because Windows
    ///     console screen buffer APIs are thread-specific for certain operations.
    /// </param>
    /// <param name="loop"></param>
    public MainLoopCoordinator (
        ITimedEvents timedEvents,
        Func<IConsoleInput<T>> inputFactory,
        ConcurrentQueue<T> inputBuffer,
        IInputProcessor inputProcessor,
        Func<IConsoleOutput> outputFactory,
        IMainLoop<T> loop
    )
    {
        _timedEvents = timedEvents;
        _inputFactory = inputFactory;
        _inputBuffer = inputBuffer;
        _inputProcessor = inputProcessor;
        _outputFactory = outputFactory;
        _loop = loop;
    }

    /// <summary>
    ///     Starts the input loop thread in separate task (returning immediately).
    /// </summary>
    public async Task StartAsync ()
    {
        Logging.Logger.LogInformation ("Main Loop Coordinator booting...");

        // TODO: if crash on boot then semaphore never finishes
        _inputTask = Task.Run (RunInput);

        // Main loop is now booted on same thread as rest of users application
        BootMainLoop ();

        // Use asynchronous semaphore waiting.
        await _startupSemaphore.WaitAsync ().ConfigureAwait (false);

        Logging.Logger.LogInformation ("Main Loop Coordinator booting complete");
    }

    private void RunInput ()
    {
        try
        {
            lock (_oLockInitialization)
            {
                // Instance must be constructed on the thread in which it is used.
                _input = _inputFactory.Invoke ();
                _input.Initialize (_inputBuffer);

                BuildFacadeIfPossible ();
            }

            try
            {
                _input.Run (_tokenSource.Token);
            }
            catch (OperationCanceledException)
            { }

            _input.Dispose ();
        }
        catch (Exception e)
        {
            Logging.Logger.LogCritical (e, "Input loop crashed");
        }
    }

    /// <inheritdoc/>
    public void RunIteration () { _loop.Iteration (); }

    private void BootMainLoop ()
    {
        lock (_oLockInitialization)
        {
            // Instance must be constructed on the thread in which it is used.
            _output = _outputFactory.Invoke ();
            _loop.Initialize (_timedEvents, _inputBuffer, _inputProcessor, _output);

            BuildFacadeIfPossible ();
        }
    }

    private void BuildFacadeIfPossible ()
    {
        if (_input != null && _output != null)
        {
            _facade = new (
                           _inputProcessor,
                           _loop.OutputBuffer,
                           _output,
                           _loop.AnsiRequestScheduler,
                           _loop.WindowSizeMonitor);
            Application.Driver = _facade;

            _startupSemaphore.Release ();
        }
    }

    /// <inheritdoc/>
    public void Stop ()
    {
        _tokenSource.Cancel ();

        // Wait for input infinite loop to exit
        Task.WhenAll (_inputTask).Wait ();
    }
}
