#nullable enable
using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
public class TransparentTests (ITestOutputHelper _output)
{
    [Fact]
    [SetupFakeDriver]

    public void Transparent_Text_Occludes ()
    {
        var super = new View
        {
            Id = "super",
            Width = 20,
            Height = 5,
        };
        super.DrawingContent += (sender, args) =>
                                {
                                    var s = sender as View;
                                    s!.FillRect(s!.Viewport, Glyphs.Stipple);
                                    args.Cancel = true;
                                };

        var sub = new View
        {
            X = 1,
            Y = 1,
            Width = 15,
            Height = 3,
            Id = "sub",
            Text = "Sub",
            ViewportSettings = ViewportSettings.Transparent,
            BorderStyle = LineStyle.Single
        };

        super.Add (sub);

        super.Layout ();
        super.Draw ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (
                                                        @"
░░░░░░░░░░░░░░░░░░░░
░┌─────────────┐░░░░
░│Sub░░░░░░░░░░│░░░░
░└─────────────┘░░░░
░░░░░░░░░░░░░░░░░░░░", _output);
    }

    [Fact]
    [SetupFakeDriver]

    public void Transparent_Subview_Occludes ()
    {
        var super = new View
        {
            Id = "super",
            Width = 20,
            Height = 5,
        };
        super.DrawingContent += (sender, args) =>
                                {
                                    var s = sender as View;
                                    s!.FillRect (s!.Viewport, Glyphs.Stipple);
                                    args.Cancel = true;
                                };

        var sub = new View
        {
            X = 1,
            Y = 1,
            Width = 15,
            Height = 3,
            Id = "sub",
            ViewportSettings = ViewportSettings.Transparent,
            BorderStyle = LineStyle.Single
        };

        var subSub = new View
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Auto(),
            Height = Dim.Auto(),
            Id = "subSub",
            Text = "subSub",
        };
        sub.Add (subSub);

        super.Add (sub);

        super.Layout ();
        super.Draw ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (
                                                          @"
░░░░░░░░░░░░░░░░░░░░
░┌─────────────┐░░░░
░│░░░subSub░░░░│░░░░
░└─────────────┘░░░░
░░░░░░░░░░░░░░░░░░░░", _output);
    }
}
