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

        appWindow.Text = "Transparent Tester.\n2nd Line of Text.\n3rd Line of Text.";
        appWindow.TextAlignment = Alignment.Center;
        appWindow.VerticalTextAlignment = Alignment.Center;
        appWindow.ClearingViewport += (s, e) =>
                                    {
                                        appWindow!.FillRect (appWindow!.Viewport, CM.Glyphs.Dot);
                                        e.Cancel = true;
                                    };

        appWindow.Add (
                       new Button ()
                       {
                           X = 15,
                           Y = 4,
                           Title = "_AppButton"
                       });

        var tv = new TransparentView ()
        {
            X = 0,
            Y = 0,
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
            base.Text = "Text. This should be visible.";
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Resizable | ViewArrangement.Movable;
            ViewportSettings |= Terminal.Gui.ViewportSettings.Transparent ;
            base.TextAlignment = Alignment.Center;
            base.VerticalTextAlignment = Alignment.Center;

            ColorScheme = Colors.ColorSchemes ["Base"];

            // Create 4 sub-views with borders and different colors. Each 3 high/wide. One aligned top, one bottom, one left, one right.
            View topView = new ()
            {
                Title = "Top",
                Text = "Top View",
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 3,
                BorderStyle = LineStyle.Single,
                Arrangement = ViewArrangement.Movable,
                ColorScheme = Colors.ColorSchemes ["Error"]
            };

            View leftView = new ()
            {
                Title = "Left",
                Text = "Left View",
                X = 0,
                Y = Pos.Bottom(topView),
                Width = 3,
                Height = Dim.Fill (),
                BorderStyle = LineStyle.Single,
                Arrangement = ViewArrangement.Movable,
                ColorScheme = Colors.ColorSchemes ["Base"]
            };

            View bottomView = new ()
            {
                Title = "Bottom",
                Text = "Bottom View",
                X = 0,
                Y = Pos.AnchorEnd(),
                Width = Dim.Fill (),
                Height = 3,
                BorderStyle = LineStyle.Single,
                Arrangement = ViewArrangement.Movable,
                ColorScheme = Colors.ColorSchemes ["Dialog"]
            };

            View rightView = new () {
                Title = "Right",
                Text = "Right View",
                X = Pos.AnchorEnd (),
                Y = 0,
                Width = 3,
                Height = Dim.Fill (),
                BorderStyle = LineStyle.Single,
                Arrangement = ViewArrangement.Movable,
                ColorScheme = Colors.ColorSchemes ["Toplevel"]
            };

            base.Add (topView, leftView, bottomView, rightView);




            //var transparentSubView = new View ()
            //{
            //    Title = "TS",
            //    Text = "0123456989",
            //    Id = "transparentSubView",
            //    X = -5,
            //    Y = 0,
            //    Width = 10,
            //    Height = 3,
            //    BorderStyle = LineStyle.Dashed,
            //    Arrangement = ViewArrangement.Movable,
            //    //ViewportSettings = Terminal.Gui.ViewportSettings.Transparent
            //};
            //transparentSubView.Border.Thickness = new (0, 1, 0, 0);
            //transparentSubView.ColorScheme = Colors.ColorSchemes ["Dialog"];

            //base.Add (
            //          new Label ()
            //          {
            //              X = 4, 
            //              Y = 0,
            //              Width = 15,
            //              Height = 1,
            //              Title = "Label..!"
            //          });
            //base.Add (transparentSubView);  
            //base.Add (
            //          new Button ()
            //          {
            //              Title = "_Okay!",
            //              X = 4,
            //              Y = 3,
            //              ShadowStyle = ShadowStyle.None,
            //              ColorScheme = Colors.ColorSchemes ["Toplevel"],
            //          });
            //base.Add (
            //          new Label ()
            //          {
            //              X = 12,
            //              Y = 0,
            //              Width = 1,
            //              Height = 10,
            //              Title = "Vert..."
            //          });
        }

        /// <inheritdoc />
        protected override bool OnClearingViewport () { return false; }

        /// <inheritdoc />
        protected override bool OnMouseEvent (MouseEventArgs mouseEvent) { return false; }
    }

}
