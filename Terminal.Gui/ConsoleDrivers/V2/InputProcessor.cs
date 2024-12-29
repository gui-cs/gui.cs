﻿#nullable enable
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui;

/// <summary>
/// Processes the queued input buffer contents - which must be of Type <typeparamref name="T"/>.
/// Is responsible for <see cref="ProcessQueue"/> and translating into common Terminal.Gui
/// events and data models.
/// </summary>
public abstract class InputProcessor<T> : IInputProcessor
{
    /// <summary>
    ///     How long after Esc has been pressed before we give up on getting an Ansi escape sequence
    /// </summary>
    private readonly TimeSpan _escTimeout = TimeSpan.FromMilliseconds (50);

    internal AnsiResponseParser<T> Parser { get; } = new ();

    /// <summary>
    /// Input buffer which will be drained from by this class.
    /// </summary>
    public ConcurrentQueue<T> InputBuffer { get; }

    /// <inheritdoc/>
    public IAnsiResponseParser GetParser () { return Parser; }

    private readonly MouseInterpreter _mouseInterpreter = new ();

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

        _mouseInterpreter.Process (a);

        // Pass on
        MouseEvent?.Invoke (this, a);
    }

    /// <summary>
    /// Constructs base instance including wiring all relevant
    /// parser events and setting <see cref="InputBuffer"/> to
    /// the provided thread safe input collection.
    /// </summary>
    /// <param name="inputBuffer"></param>
    protected InputProcessor (ConcurrentQueue<T> inputBuffer)
    {
        InputBuffer = inputBuffer;
        Parser.HandleMouse = true;
        Parser.Mouse += (s, e) => OnMouseEvent (e);

        Parser.HandleKeyboard = true;

        Parser.Keyboard += (s, k) =>
                           {
                               OnKeyDown (k);
                               OnKeyUp (k);
                           };

        // TODO: For now handle all other escape codes with ignore
        Parser.UnexpectedResponseHandler = str =>
                                           {
                                               Logging.Logger.LogInformation ($"{nameof(InputProcessor<T>)} ignored unrecognized response '{new string(str.Select (k=>k.Item1).ToArray ())}'");
                                               return true;
                                           };
    }

    /// <summary>
    ///     Drains the <see cref="InputBuffer"/> buffer, processing all available keystrokes
    /// </summary>
    public void ProcessQueue ()
    {
        while (InputBuffer.TryDequeue (out T? input))
        {
            Process (input);
        }

        foreach (T input in ReleaseParserHeldKeysIfStale ())
        {
            ProcessAfterParsing (input);
        }
    }

    private IEnumerable<T> ReleaseParserHeldKeysIfStale ()
    {
        if (Parser.State == AnsiResponseParserState.ExpectingBracket && DateTime.Now - Parser.StateChangedAt > _escTimeout)
        {
            return Parser.Release ().Select (o => o.Item2);
        }

        return [];
    }

    /// <summary>
    /// Process the provided single input element <paramref name="input"/>. This method
    /// is called sequentially for each value read from <see cref="InputBuffer"/>.
    /// </summary>
    /// <param name="input"></param>
    protected abstract void Process (T input);

    /// <summary>
    /// Process the provided single input element - short-circuiting the <see cref="Parser"/>
    /// stage of the processing.
    /// </summary>
    /// <param name="input"></param>
    protected abstract void ProcessAfterParsing (T input);
}
