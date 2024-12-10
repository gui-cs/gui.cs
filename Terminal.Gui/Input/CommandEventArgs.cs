#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     Event arguments for <see cref="Command"/> events.
/// </summary>
public class CommandEventArgs : CancelEventArgs
{
    /// <summary>
    ///     The context for the command, if any.
    /// </summary>
    /// <remarks>
    ///     If <see langword="null"/> the command was invoked without context.
    /// </remarks>
    public required ICommandContext? Context { get; init; }
}
