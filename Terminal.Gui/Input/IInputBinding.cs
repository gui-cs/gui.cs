#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Describes an input binding. Used to bind a set of <see cref="Command"/> objects to a specific input event.
/// </summary>
public interface IInputBinding
{
    /// <summary>
    ///     Gets or sets the commands this input binding will invoke.
    /// </summary>
    Command [] Commands { get; set; }
}
