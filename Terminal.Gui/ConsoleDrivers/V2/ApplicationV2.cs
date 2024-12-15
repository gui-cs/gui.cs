﻿using System.Collections.Concurrent;

namespace Terminal.Gui.ConsoleDrivers.V2;


public class ApplicationV2 : ApplicationImpl
{
    private readonly Func<INetInput> _netInputFactory;
    private readonly Func<IConsoleOutput> _netOutputFactory;
    private readonly Func<IWindowsInput> _winInputFactory;
    private readonly Func<IConsoleOutput> _winOutputFactory;
    private IMainLoopCoordinator _coordinator;
    public ITimedEvents TimedEvents { get; } = new TimedEvents ();
    public ApplicationV2 () : this (
                                    ()=>new NetInput (),
                                    ()=>new NetOutput (),
                                    ()=>new WindowsInput (),
                                    ()=>new WindowsOutput ()
                                    )
    {
    }
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

    /// <inheritdoc />
    public override void Init (IConsoleDriver driver = null, string driverName = null)
    {
        Application.Navigation = new ();

        Application.AddKeyBindings ();

        CreateDriver (driverName);

        Application.Initialized = true;

        Application.SubscribeDriverEvents ();
    }

    private void CreateDriver (string driverName)
    {

        PlatformID p = Environment.OSVersion.Platform;

        var definetlyWin = driverName?.Contains ("win") ?? false;
        var definetlyNet = driverName?.Contains ("net") ?? false;

        if (definetlyWin)
        {
            CreateWindowsSubcomponents ();

        }
        else if (definetlyNet)
        {
            CreateNetSubcomponents ();
        }
        else
        if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            CreateWindowsSubcomponents ();
        }
        else
        {
            CreateNetSubcomponents ();
        }

        _coordinator.StartAsync().Wait();

        if (Application.Driver == null)
        {
            throw new Exception ("Application.Driver was null even after booting MainLoopCoordinator");
        }
    }


    private void CreateWindowsSubcomponents ()
    {
        var inputBuffer = new ConcurrentQueue<WindowsConsole.InputRecord> ();
        var loop = new MainLoop<WindowsConsole.InputRecord> ();
        _coordinator = new MainLoopCoordinator<WindowsConsole.InputRecord> (TimedEvents,
                                                                            _winInputFactory,
                                                                            inputBuffer,
                                                                            new WindowsInputProcessor (inputBuffer),
                                                                            _winOutputFactory,
                                                                            loop);
    }
    private void CreateNetSubcomponents ()
    {
        var inputBuffer = new ConcurrentQueue<ConsoleKeyInfo> ();
        var loop = new MainLoop<ConsoleKeyInfo> ();
        _coordinator = new MainLoopCoordinator<ConsoleKeyInfo> (TimedEvents,
                                                                _netInputFactory,
                                                                inputBuffer,
                                                                new NetInputProcessor (inputBuffer),
                                                                _netOutputFactory,
                                                                loop);
    }

    /// <inheritdoc />
    public override T Run<T> (Func<Exception, bool> errorHandler = null, IConsoleDriver driver = null)
    {
        var top = new T ();

        Run (top, errorHandler);

        return top;
    }

    /// <inheritdoc />
    public override void Run (Toplevel view, Func<Exception, bool> errorHandler = null)
    {
        ArgumentNullException.ThrowIfNull (view);

        if (!Application.Initialized)
        {
            throw new Exception ("App not Initialized");
        }

        Application.Top = view;

        Application.Begin (view);

        // TODO : how to know when we are done?
        while (Application.TopLevels.TryPeek (out var found) && found == view)
        {
            Task.Delay (100).Wait ();
        }
    }

    /// <inheritdoc />
    public override void Shutdown ()
    {
        _coordinator.Stop ();
        base.Shutdown ();
        Application.Driver = null;
    }

    /// <inheritdoc />
    public override void RequestStop (Toplevel top)
    {
        Application.TopLevels.Pop ();

        if(Application.TopLevels.Count>0)
        {
            Application.Top = Application.TopLevels.Peek ();
        }
        else
        {
            Application.Top = null;
        }
    }

    /// <inheritdoc />
    public override void Invoke (Action action)
    {
        TimedEvents.AddIdle (() =>
                               {
                                   action ();

                                   return false;
                               }
                              );
    }

    /// <inheritdoc />
    public override void AddIdle (Func<bool> func)
    {
        TimedEvents.AddIdle (func);
    }

    /// <inheritdoc />
    public override object AddTimeout (TimeSpan time, Func<bool> callback)
    {
        return TimedEvents.AddTimeout(time,callback);
    }

    /// <inheritdoc />
    public override bool RemoveTimeout (object token)
    {
        return TimedEvents.RemoveTimeout (token);
    }
}
