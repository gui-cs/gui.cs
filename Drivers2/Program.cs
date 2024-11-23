﻿using System.Collections.Concurrent;
using Terminal.Gui;
using Terminal.Gui.ConsoleDrivers.V2;
using static Terminal.Gui.WindowsConsole;

namespace Drivers2;

class Program
{
    static void Main (string [] args)
    {
        bool win = false;

        if (args.Length > 0)
        {
            if (args [0] == "net")
            {
                // default
            }
            else if(args [0] == "win")
            {
                win = true;
            }
            else
            {
                Console.WriteLine("Arg must be 'win' or 'net' or blank to use default");
            }
        }

        // Required to set up colors etc?
        Application.Init ();
        IMainLoopCoordinator coordinator;
        if (win)
        {
            // TODO: We will need a nice factory for this constructor, it's getting a bit epic

            var inputBuffer = new ConcurrentQueue<InputRecord> ();
            var loop = new MainLoop<InputRecord> ();
            coordinator = new MainLoopCoordinator<InputRecord> (
                                                                ()=>new WindowsInput (),
                                                                inputBuffer,
                                                                new WindowsInputProcessor (inputBuffer),
                                                                ()=>new WindowsOutput (),
                                                                loop);
        }
        else
        {

            var inputBuffer = new ConcurrentQueue<ConsoleKeyInfo> ();
            var loop = new MainLoop<ConsoleKeyInfo> ();
            coordinator = new MainLoopCoordinator<ConsoleKeyInfo> (()=>new NetInput (),
                                                                   inputBuffer,
                                                                   new NetInputProcessor (inputBuffer),
                                                                   ()=>new NetOutput (),
                                                                   loop);
        }

        // Register the event handler for Ctrl+C
        Console.CancelKeyPress += (s,e)=>
                                  {
                                      e.Cancel = true;
                                      coordinator.Stop ();
                                  };

        coordinator.StartBlocking ();
    }
}
