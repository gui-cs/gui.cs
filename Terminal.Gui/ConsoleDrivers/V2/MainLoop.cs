﻿using System.Collections.Concurrent;

namespace Terminal.Gui;
class MainLoop<T> : IMainLoop<T>
{
    public ConcurrentQueue<T> InputBuffer { get; private set; } = new ();

    public AnsiResponseParser<T> Parser
    {
        get;
        private set;
    }

    /// <inheritdoc />
    public void Initialize (ConcurrentQueue<T> inputBuffer, AnsiResponseParser<T> parser)
    {
        InputBuffer = inputBuffer;
        Parser = parser;
    }


    public void Run (CancellationToken token)
    {
        do
        {
            var dt = DateTime.Now;

            Iteration ();

            var took = DateTime.Now - dt;
            var sleepFor = TimeSpan.FromMilliseconds (50) - took;

            if (sleepFor.Milliseconds > 0)
            {
                Task.Delay (sleepFor, token).Wait (token);
            }
        }
        while (!token.IsCancellationRequested);
    }
    /// <inheritdoc />
    public void Iteration ()
    {

    }
    /// <inheritdoc />
    public void Dispose ()
    { // TODO release managed resources here
    }
}