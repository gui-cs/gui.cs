﻿using System.Collections.Concurrent;

namespace Terminal.Gui;

public abstract class InputProcessor<T> : IInputProcessor
{
    public AnsiResponseParser<T> Parser { get; } = new ();
    public ConcurrentQueue<T> InputBuffer { get; }

    public IAnsiResponseParser GetParser () => Parser;

    public MouseInterpreter MouseInterpreter { get; } = new ();

    /// <summary>Event fired when a key is pressed down. This is a precursor to <see cref="KeyUp"/>.</summary>
    public event EventHandler<Key>? KeyDown;

    /// <summary>
    ///     Called when a key is pressed down. Fires the <see cref="KeyDown"/> event. This is a precursor to
    ///     <see cref="OnKeyUp"/>.
    /// </summary>
    /// <param name="a"></param>
    public void OnKeyDown (Key a) { KeyDown?.Invoke (this, a); }

    /// <summary>Event fired when a key is released.</summary>
    /// <remarks>
    ///     Drivers that do not support key release events will fire this event after <see cref="KeyDown"/> processing is
    ///     complete.
    /// </remarks>
    public event EventHandler<Key>? KeyUp;

    /// <summary>Called when a key is released. Fires the <see cref="KeyUp"/> event.</summary>
    /// <remarks>
    ///     Drivers that do not support key release events will call this method after <see cref="OnKeyDown"/> processing
    ///     is complete.
    /// </remarks>
    /// <param name="a"></param>
    public void OnKeyUp (Key a) { KeyUp?.Invoke (this, a); }

    /// <summary>Event fired when a mouse event occurs.</summary>
    public event EventHandler<MouseEventArgs>? MouseEvent;

    /// <summary>Called when a mouse event occurs. Fires the <see cref="MouseEvent"/> event.</summary>
    /// <param name="a"></param>
    public void OnMouseEvent (MouseEventArgs a)
    {
        // Ensure ScreenPosition is set
        a.ScreenPosition = a.Position;

        foreach (var narrative in MouseInterpreter.Process (a))
        {
            ResolveNarrative (narrative);
        }
    }

    private void ResolveNarrative (ButtonNarrative narrative)
    {
        if (narrative.NumberOfClicks == 1)
        {
            // its a click
            MouseEvent?.Invoke (this, new MouseEventArgs
            {
                Handled = false,
                Flags = MouseFlags.Button1Clicked,
                ScreenPosition = narrative.MouseStates.Last().Position
            });
        }
    }

    public InputProcessor (ConcurrentQueue<T> inputBuffer)
    {
        InputBuffer = inputBuffer;
        Parser.HandleMouse = true;
        Parser.Mouse += (s, e) => OnMouseEvent (e);
        // TODO: For now handle all other escape codes with ignore
        Parser.UnexpectedResponseHandler = str => { return true; };
    }

    /// <summary>
    ///     Drains the <see cref="InputBuffer"/> buffer, processing all available keystrokes
    /// </summary>
    public void ProcessQueue ()
    {
        // TODO: Esc timeout etc

        while (InputBuffer.TryDequeue (out T input))
        {
            Process (input);
        }
    }

    protected abstract void Process (T result);
}
