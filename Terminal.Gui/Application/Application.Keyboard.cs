﻿#nullable enable
using System.Text.Json.Serialization;
using static System.Formats.Asn1.AsnWriter;

namespace Terminal.Gui;

public static partial class Application // Keyboard handling
{
    private static Key _alternateForwardKey = Key.Empty; // Defined in config.json

    /// <summary>Alternative key to navigate forwards through views. Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    [JsonConverter (typeof (KeyJsonConverter))]
    public static Key AlternateForwardKey
    {
        get => _alternateForwardKey;
        set
        {
            if (_alternateForwardKey != value)
            {
                Key oldKey = _alternateForwardKey;
                _alternateForwardKey = value;

                if (_alternateForwardKey == Key.Empty)
                {
                    KeyBindings.Remove (_alternateForwardKey);
                }
                else
                {
                    KeyBindings.ReplaceKey (oldKey, _alternateForwardKey);
                }
                OnAlternateForwardKeyChanged (new (oldKey, value));
            }
        }
    }

    private static void OnAlternateForwardKeyChanged (KeyChangedEventArgs e)
    {
        // TODO: The fact Top has it's own AlternateForwardKey and events is needlessly complex. Remove it.
        foreach (Toplevel top in _topLevels.ToArray ())
        {
            top.OnAlternateForwardKeyChanged (e);
        }
    }

    private static Key _alternateBackwardKey = Key.Empty; // Defined in config.json

    /// <summary>Alternative key to navigate backwards through views. Shift+Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    [JsonConverter (typeof (KeyJsonConverter))]
    public static Key AlternateBackwardKey
    {
        get => _alternateBackwardKey;
        set
        {
            if (_alternateBackwardKey != value)
            {
                Key oldKey = _alternateBackwardKey;
                _alternateBackwardKey = value;

                if (_alternateBackwardKey == Key.Empty)
                {
                    KeyBindings.Remove (_alternateBackwardKey);
                }
                else
                {
                    KeyBindings.ReplaceKey (oldKey, _alternateBackwardKey);
                }

                OnAlternateBackwardKeyChanged (new (oldKey, value));
            }
        }
    }

    private static void OnAlternateBackwardKeyChanged (KeyChangedEventArgs oldKey)
    {
        // TODO: The fact Top has it's own AlternateBackwardKey and events is needlessly complex. Remove it.
        foreach (Toplevel top in _topLevels.ToArray ())
        {
            top.OnAlternateBackwardKeyChanged (oldKey);
        }
    }

    private static Key _quitKey = Key.Empty; // Defined in config.json

    /// <summary>Gets or sets the key to quit the application.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    [JsonConverter (typeof (KeyJsonConverter))]
    public static Key QuitKey
    {
        get => _quitKey;
        set
        {
            if (_quitKey != value)
            {
                Key oldKey = _quitKey;
                _quitKey = value;
                if (_quitKey == Key.Empty)
                {
                    KeyBindings.Remove (_quitKey);
                }
                else
                {
                    KeyBindings.ReplaceKey (oldKey, _quitKey);
                }
                OnQuitKeyChanged (new (oldKey, value));
            }
        }
    }

    private static void OnQuitKeyChanged (KeyChangedEventArgs e)
    {
        // TODO: The fact Top has it's own QuitKey and events is needlessly complex. Remove it.
        // Duplicate the list so if it changes during enumeration we're safe
        foreach (Toplevel top in _topLevels.ToArray ())
        {
            top.OnQuitKeyChanged (e);
        }
    }

    /// <summary>
    ///     Event fired when the user presses a key. Fired by <see cref="OnKeyDown"/>.
    ///     <para>
    ///         Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and to prevent
    ///         additional processing.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     All drivers support firing the <see cref="KeyDown"/> event. Some drivers (Curses) do not support firing the
    ///     <see cref="KeyDown"/> and <see cref="KeyUp"/> events.
    ///     <para>Fired after <see cref="KeyDown"/> and before <see cref="KeyUp"/>.</para>
    /// </remarks>
    public static event EventHandler<Key> KeyDown;

    /// <summary>
    ///     Called by the <see cref="ConsoleDriver"/> when the user presses a key. Fires the <see cref="KeyDown"/> event
    ///     then calls <see cref="View.NewKeyDownEvent"/> on all top level views. Called after <see cref="OnKeyDown"/> and
    ///     before <see cref="OnKeyUp"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key press events.</remarks>
    /// <param name="keyEvent"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool OnKeyDown (Key keyEvent)
    {
        if (!_initialized)
        {
            return true;
        }

        KeyDown?.Invoke (null, keyEvent);

        if (keyEvent.Handled)
        {
            return true;
        }

        foreach (Toplevel topLevel in _topLevels.ToList ())
        {
            if (topLevel.NewKeyDownEvent (keyEvent))
            {
                return true;
            }

            if (topLevel.Modal)
            {
                break;
            }
        }

        // Invoke any Application-scoped KeyBindings.
        // The first view that handles the key will stop the loop.
        foreach (var binding in KeyBindings.Bindings.Where (b => b.Key == keyEvent.KeyCode))
        {
            if (binding.Value.BoundView is { })
            {
                bool? handled = binding.Value.BoundView?.InvokeCommands (binding.Value.Commands, binding.Key, binding.Value);

                if (handled != null && (bool)handled)
                {
                    return true;
                }
            }
            else
            {
                if (!KeyBindings.TryGet (keyEvent, KeyBindingScope.Application, out KeyBinding appBinding))
                {
                    continue;
                }

                bool? toReturn = null;

                foreach (Command command in appBinding.Commands)
                {
                    if (!CommandImplementations.ContainsKey (command))
                    {
                        throw new NotSupportedException (
                                                         @$"A KeyBinding was set up for the command {command} ({keyEvent}) but that command is not supported by Application."
                                                        );
                    }

                    if (CommandImplementations.TryGetValue (command, out Func<CommandContext, bool?>? implementation))
                    {
                        var context = new CommandContext (command, keyEvent, appBinding); // Create the context here
                        toReturn = implementation (context);
                    }

                    // if ever see a true then that's what we will return
                    if (toReturn ?? false)
                    {
                        toReturn = true;
                    }
                }

                return toReturn ?? true;
            }
        }


        return false;
    }

    /// <summary>
    ///     Event fired when the user releases a key. Fired by <see cref="OnKeyUp"/>.
    ///     <para>
    ///         Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and to prevent
    ///         additional processing.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     All drivers support firing the <see cref="KeyDown"/> event. Some drivers (Curses) do not support firing the
    ///     <see cref="KeyDown"/> and <see cref="KeyUp"/> events.
    ///     <para>Fired after <see cref="KeyDown"/>.</para>
    /// </remarks>
    public static event EventHandler<Key> KeyUp;

    /// <summary>
    ///     Called by the <see cref="ConsoleDriver"/> when the user releases a key. Fires the <see cref="KeyUp"/> event
    ///     then calls <see cref="View.NewKeyUpEvent"/> on all top level views. Called after <see cref="OnKeyDown"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key press events.</remarks>
    /// <param name="a"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool OnKeyUp (Key a)
    {
        if (!_initialized)
        {
            return true;
        }

        KeyUp?.Invoke (null, a);

        if (a.Handled)
        {
            return true;
        }

        foreach (Toplevel topLevel in _topLevels.ToList ())
        {
            if (topLevel.NewKeyUpEvent (a))
            {
                return true;
            }

            if (topLevel.Modal)
            {
                break;
            }
        }

        return false;
    }

    /// <summary>Gets the key bindings for this view.</summary>
    public static KeyBindings KeyBindings { get; internal set; } = new ();

    /// <summary>
    /// Commands for Application.
    /// </summary>
    private static Dictionary<Command, Func<CommandContext, bool?>> CommandImplementations { get; } = new ();

    /// <summary>
    ///     <para>
    ///         Sets the function that will be invoked for a <see cref="Command"/>. 
    ///     </para>
    ///     <para>
    ///         If AddCommand has already been called for <paramref name="command"/> <paramref name="f"/> will
    ///         replace the old one.
    ///     </para>
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This version of AddCommand is for commands that do not require a <see cref="CommandContext"/>.
    /// </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="f">The function.</param>
    private static void AddCommand (Command command, Func<bool?> f)
    {
        CommandImplementations [command] = ctx => f ();
    }

    ///// <summary>
    /////     The <see cref="KeyBindingScope.Application"/> key bindings.
    ///// </summary>
    //private static readonly Dictionary<Key, List<View?>> _keyBindings = new ();

    ///// <summary>
    ///// Gets the list of <see cref="KeyBindingScope.Application"/> key bindings.
    ///// </summary>
    //public static Dictionary<Key, List<View?>> GetKeyBindings () { return _keyBindings; }

    ///// <summary>
    /////     Adds an  <see cref="KeyBindingScope.Application"/> scoped key binding.
    ///// </summary>
    ///// <remarks>
    /////     This is an internal method used by the <see cref="View"/> class to add Application key bindings.
    ///// </remarks>
    ///// <param name="key">The key being bound.</param>
    ///// <param name="view">The view that is bound to the key. If <see langword="null"/>, <see cref="Application.Current"/> will be used.</param>
    //internal static void AddKeyBinding (Key key, View? view)
    //{
    //    if (!_keyBindings.ContainsKey (key))
    //    {
    //        _keyBindings [key] = [];
    //    }

    //    _keyBindings [key].Add (view);
    //}

    internal static void AddApplicationKeyBindings ()
    {
        // Things this view knows how to do
        AddCommand (
                    Command.QuitToplevel,  // TODO: IRunnable: Rename to Command.Quit to make more generic.
                    () =>
                    {
                        if (OverlappedTop is { })
                        {
                            RequestStop (Current);
                        }
                        else
                        {
                            Application.RequestStop ();
                        }

                        return true;
                    }
                   );

        AddCommand (
                    Command.Suspend,
                    () =>
                    {
                        Driver?.Suspend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.NextView,    
                    () =>
                    {
                        // TODO: Move this method to Application.Navigation.cs
                        Current.MoveNextView ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.PreviousView,
                    () =>
                    {
                        // TODO: Move this method to Application.Navigation.cs
                        Current.MovePreviousView ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.NextViewOrTop,
                    () =>
                    {
                        // TODO: Move this method to Application.Navigation.cs
                        Current.MoveNextViewOrTop ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.PreviousViewOrTop,
                    () =>
                    {
                        // TODO: Move this method to Application.Navigation.cs
                        Current.MovePreviousViewOrTop ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Refresh,
                    () =>
                    {
                        Refresh ();

                        return true;
                    }
                   );


        KeyBindings.Add (Application.QuitKey, KeyBindingScope.Application, Command.QuitToplevel);

        KeyBindings.Add (Key.CursorRight, KeyBindingScope.Application, Command.NextView);
        KeyBindings.Add (Key.CursorDown, KeyBindingScope.Application, Command.NextView);
        KeyBindings.Add (Key.CursorLeft, KeyBindingScope.Application, Command.PreviousView);
        KeyBindings.Add (Key.CursorUp, KeyBindingScope.Application, Command.PreviousView);

        KeyBindings.Add (Key.Tab, KeyBindingScope.Application, Command.NextView);
        KeyBindings.Add (Key.Tab.WithShift, KeyBindingScope.Application, Command.PreviousView);
        KeyBindings.Add (Key.Tab.WithCtrl, KeyBindingScope.Application, Command.NextViewOrTop);
        KeyBindings.Add (Key.Tab.WithShift.WithCtrl, KeyBindingScope.Application, Command.PreviousViewOrTop);

        // TODO: Refresh Key should be configurable
        KeyBindings.Add (Key.F5, KeyBindingScope.Application, Command.Refresh);
        KeyBindings.Add (Application.AlternateForwardKey, KeyBindingScope.Application, Command.NextViewOrTop); // Needed on Unix
        KeyBindings.Add (Application.AlternateBackwardKey, KeyBindingScope.Application, Command.PreviousViewOrTop); // Needed on Unix

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            KeyBindings.Add (Key.Z.WithCtrl, KeyBindingScope.Application, Command.Suspend);
        }

#if UNIX_KEY_BINDINGS
        KeyBindings.Add (Key.L.WithCtrl, Command.Refresh); // Unix
        KeyBindings.Add (Key.F.WithCtrl, Command.NextView); // Unix
        KeyBindings.Add (Key.I.WithCtrl, Command.NextView); // Unix
        KeyBindings.Add (Key.B.WithCtrl, Command.PreviousView); // Unix
#endif
    }

    /// <summary>
    ///     Gets the list of Views that have <see cref="KeyBindingScope.Application"/> key bindings.
    /// </summary>
    /// <remarks>
    ///     This is an internal method used by the <see cref="View"/> class to add Application key bindings.
    /// </remarks>
    /// <returns>The list of Views that have Application-scoped key bindings.</returns>
    internal static List<KeyBinding> GetViewKeyBindings ()
    {
        // Get the list of views that do not have Application-scoped key bindings
        return KeyBindings.Bindings
                          .Where (kv => kv.Value.Scope != KeyBindingScope.Application)
                          .Select (kv => kv.Value)
                          .Distinct ()
                          .ToList ();
    }

    ///// <summary>
    /////     Gets the list of Views that have <see cref="KeyBindingScope.Application"/> key bindings for the specified key.
    ///// </summary>
    ///// <remarks>
    /////     This is an internal method used by the <see cref="View"/> class to add Application key bindings.
    ///// </remarks>
    ///// <param name="key">The key to check.</param>
    ///// <param name="views">Outputs the list of views bound to <paramref name="key"/></param>
    ///// <returns><see langword="True"/> if successful.</returns>
    //internal static bool TryGetKeyBindings (Key key, out List<View> views) { return _keyBindings.TryGetValue (key, out views); }

    /// <summary>
    ///     Removes all <see cref="KeyBindingScope.Application"/> scoped key bindings for the specified view.
    /// </summary>
    /// <remarks>
    ///     This is an internal method used by the <see cref="View"/> class to remove Application key bindings.
    /// </remarks>
    /// <param name="view">The view that is bound to the key.</param>
    internal static void RemoveKeyBindings (View view)
    {
        var list = KeyBindings.Bindings
                          .Where (kv => kv.Value.Scope != KeyBindingScope.Application)
                          .Select (kv => kv.Value)
                          .Distinct ()
                          .ToList ();
    }
}
