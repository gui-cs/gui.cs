#nullable enable
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Transparent", "Testing Transparency")]
public sealed class Transparent : Scenario
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
        appWindow.BorderStyle = LineStyle.None;
        appWindow.ColorScheme = Colors.ColorSchemes ["Error"];

        appWindow.Text = "Transparent Tester";

        appWindow.Add (
                       new Button ()
                       {
                           X = 15,
                           Y = 4,
                           Title = "_AppButton"
                       });

        var tv = new TransparentView ()
        {
            X = -1,
            Y = -1,
            Width = 25,
            Height = 10
        };
        appWindow.Add (tv);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    public class TransparentView : FrameView
    {
        public TransparentView ()
        {
            Title = "Transparent";
            //base.Text = "Text. This should be visible.";
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Resizable | ViewArrangement.Movable;
            ViewportSettings |= Terminal.Gui.ViewportSettings.Transparent | Terminal.Gui.ViewportSettings.ClipContentOnly | Terminal.Gui.ViewportSettings.ClearContentOnly;

            ColorScheme = Colors.ColorSchemes ["Base"];

            var transparentSubView = new View ()
            {
                //Title = "SubView",
                Text = "01234",
                Id = "transparentSubView",
                X = 0,
                Y = 0,
                Width = 5,
                Height = 2,
                BorderStyle = LineStyle.Dashed,
                Arrangement = ViewArrangement.Movable,
                //ViewportSettings = Terminal.Gui.ViewportSettings.Transparent
            };
            transparentSubView.Border.Thickness = new (0, 1, 0, 0);
            transparentSubView.ColorScheme = Colors.ColorSchemes ["Dialog"];
            base.Add (transparentSubView);
            base.Add (
                      new Button ()
                      {
                          Title = "_Ok",
                          X = 5,
                          Y = 1,
                          ShadowStyle = ShadowStyle.None,
                          ColorScheme = Colors.ColorSchemes ["Toplevel"],
                      });
        }

        /// <inheritdoc />
        protected override bool OnClearingViewport () { return false; }

        /// <inheritdoc />
        protected override bool OnMouseEvent (MouseEventArgs mouseEvent) { return false; }
    }

}
