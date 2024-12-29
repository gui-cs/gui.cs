﻿#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui;

/// <summary>
///     Implementation of <see cref="IApplication"/> that boots the new 'v2'
///     main loop architecture.
/// </summary>
public class ApplicationV2 : ApplicationImpl
{
    private readonly Func<INetInput> _netInputFactory;
    private readonly Func<IConsoleOutput> _netOutputFactory;
    private readonly Func<IWindowsInput> _winInputFactory;
    private readonly Func<IConsoleOutput> _winOutputFactory;
    private IMainLoopCoordinator? _coordinator;
    private string? _driverName;

    private readonly ITimedEvents _timedEvents = new TimedEvents ();

    /// <summary>
    /// Creates anew instance of the Application backend. The provided
    /// factory methods will be used on Init calls to get things booted.
    /// </summary>
    public ApplicationV2 () : this (
                                    () => new NetInput (),
                                    () => new NetOutput (),
                                    () => new WindowsInput (),
                                    () => new WindowsOutput ()
                                   )
    { }

    internal ApplicationV2 (
        Func<INetInput> netInputFactory,
        Func<IConsoleOutput> netOutputFactory,
        Func<IWindowsInput> winInputFactory,
        Func<IConsoleOutput> winOutputFactory
    )
    {
        _netInputFactory = netInputFactory;
        _netOutputFactory = netOutputFactory;
        _winInputFactory = winInputFactory;
        _winOutputFactory = winOutputFactory;
        IsLegacy = false;
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public override void Init (IConsoleDriver? driver = null, string? driverName = null)
    {
        if (!string.IsNullOrWhiteSpace (driverName))
        {
            _driverName = driverName;
        }

        Application.Navigation = new ();

        Application.AddKeyBindings ();

        // This is consistent with Application.ForceDriver which magnetically picks up driverName
        // making it use custom driver in future shutdown/init calls where no driver is specified
        CreateDriver (driverName ?? _driverName);

        Application.Initialized = true;

        Application.SubscribeDriverEvents ();
    }

    private void CreateDriver (string? driverName)
    {
        PlatformID p = Environment.OSVersion.Platform;

        bool definetlyWin = driverName?.Contains ("win") ?? false;
        bool definetlyNet = driverName?.Contains ("net") ?? false;

        if (definetlyWin)
        {
            _coordinator = CreateWindowsSubcomponents ();
        }
        else if (definetlyNet)
        {
            _coordinator = CreateNetSubcomponents ();
        }
        else if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            _coordinator = CreateWindowsSubcomponents();
        }
        else
        {
            _coordinator = CreateNetSubcomponents ();
        }

        _coordinator.StartAsync ().Wait ();

        if (Application.Driver == null)
        {
            throw new ("Application.Driver was null even after booting MainLoopCoordinator");
        }
    }

    private IMainLoopCoordinator CreateWindowsSubcomponents ()
    {
        ConcurrentQueue<WindowsConsole.InputRecord> inputBuffer = new ConcurrentQueue<WindowsConsole.InputRecord> ();
        MainLoop<WindowsConsole.InputRecord> loop = new MainLoop<WindowsConsole.InputRecord> ();

        return new MainLoopCoordinator<WindowsConsole.InputRecord> (
                                                                            _timedEvents,
                                                                            _winInputFactory,
                                                                            inputBuffer,
                                                                            new WindowsInputProcessor (inputBuffer),
                                                                            _winOutputFactory,
                                                                            loop);
    }

    private IMainLoopCoordinator CreateNetSubcomponents ()
    {
        ConcurrentQueue<ConsoleKeyInfo> inputBuffer = new ConcurrentQueue<ConsoleKeyInfo> ();
        MainLoop<ConsoleKeyInfo> loop = new MainLoop<ConsoleKeyInfo> ();

        return new MainLoopCoordinator<ConsoleKeyInfo> (
                                                                _timedEvents,
                                                                _netInputFactory,
                                                                inputBuffer,
                                                                new NetInputProcessor (inputBuffer),
                                                                _netOutputFactory,
                                                                loop);
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public override T Run<T> (Func<Exception, bool>? errorHandler = null, IConsoleDriver? driver = null)
    {
        var top = new T ();

        Run (top, errorHandler);

        return top;
    }

    /// <inheritdoc/>
    public override void Run (Toplevel view, Func<Exception, bool>? errorHandler = null)
    {
        Logging.Logger.LogInformation ($"Run '{view}'");
        ArgumentNullException.ThrowIfNull (view);

        if (!Application.Initialized)
        {
            throw new ("App not Initialized");
        }

        Application.Top = view;

        Application.Begin (view);

        // TODO : how to know when we are done?
        while (Application.TopLevels.TryPeek (out Toplevel? found) && found == view)
        {
            if (_coordinator is null)
            {
                throw new Exception ($"{nameof (IMainLoopCoordinator)}inexplicably became null during Run");
            }
            _coordinator.RunIteration ();
        }
    }

    /// <inheritdoc/>
    public override void Shutdown ()
    {
        _coordinator?.Stop ();
        base.Shutdown ();
        Application.Driver = null;
    }

    /// <inheritdoc/>
    public override void RequestStop (Toplevel? top)
    {
        Logging.Logger.LogInformation ($"RequestStop '{top}'");

        // TODO: This definition of stop seems sketchy
        Application.TopLevels.TryPop (out _);

        if (Application.TopLevels.Count > 0)
        {
            Application.Top = Application.TopLevels.Peek ();
        }
        else
        {
            Application.Top = null;
        }
    }

    /// <inheritdoc/>
    public override void Invoke (Action action)
    {
        _timedEvents.AddIdle (
                             () =>
                             {
                                 action ();

                                 return false;
                             }
                            );
    }

    /// <inheritdoc/>
    public override void AddIdle (Func<bool> func) { _timedEvents.AddIdle (func); }

    /// <inheritdoc/>
    public override object AddTimeout (TimeSpan time, Func<bool> callback) { return _timedEvents.AddTimeout (time, callback); }

    /// <inheritdoc/>
    public override bool RemoveTimeout (object token) { return _timedEvents.RemoveTimeout (token); }
}
