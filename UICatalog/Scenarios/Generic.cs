#nullable enable
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Generic", "Generic sample - A template for creating new Scenarios")]
[ScenarioCategory ("Controls")]
public sealed class Generic : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
        };

        var button = new Button { Id = "button", X = Pos.Center (), Y = 1, Text = "_Press me!" };
        button.CanFocus = false;

        button.Accepting += (s, e) =>
                            {
                                e.Cancel = false;
                                MessageBox.ErrorQuery ("Error", "You pressed the button!", "_Ok");
                            };

        var label = new Label ()
        {
            Title = "Press the _button or use Alt+P to press it.",
            X = Pos.Center (),
            Y = Pos.Bottom (button) + 1,
        };
        appWindow.Add (label, button);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
