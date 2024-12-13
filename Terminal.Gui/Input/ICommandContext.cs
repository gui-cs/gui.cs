#nullable enable
namespace Terminal.Gui;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
/// <summary>
///     Describes the context in which a <see cref="Command"/> is being invoked. <see cref="CommandContext{TBindingType}"/> inherits from this interface.
///     When a <see cref="Command"/> is invoked,
///     a context object is passed to Command handlers as an <see cref="ICommandContext"/> reference.
/// </summary>
/// <seealso cref="View.AddCommand(Command, View.CommandImplementation)"/>.
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
public interface ICommandContext
{
    /// <summary>
    ///     The <see cref="Command"/> that is being invoked.
    /// </summary>
    public Command Command { get; set; }
}
