using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewMouseTests;

[Trait ("Category", "Input")]
public class MouseTests (ITestOutputHelper output) : TestsAllViews
{
   // TODO: Add more tests that ensure the above test works with positive adornments

    // Test drag to move
    [Theory]
    [InlineData (0, 0, 0, 0, false)]
    [InlineData (0, 0, 0, 4, false)]
    [InlineData (1, 0, 0, 4, false)]
    [InlineData (0, 1, 0, 4, true)]
    [InlineData (0, 0, 1, 4, false)]
    [InlineData (1, 1, 0, 3, false)]
    [InlineData (1, 1, 0, 4, false)]
    [InlineData (1, 1, 0, 5, true)]
    [InlineData (1, 1, 0, 6, false)]
    [InlineData (1, 1, 0, 11, false)]
    [InlineData (1, 1, 0, 12, true)]
    [InlineData (1, 1, 0, 13, false)]
    [InlineData (1, 1, 0, 14, false)]
    [AutoInitShutdown]
    public void ButtonPressed_In_Border_Starts_Drag (int marginThickness, int borderThickness, int paddingThickness, int xy, bool expectedMoved)
    {
        var testView = new View
        {
            CanFocus = true,
            X = 4,
            Y = 4,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Movable
        };
        testView.Margin.Thickness = new (marginThickness);
        testView.Border.Thickness = new (borderThickness);
        testView.Padding.Thickness = new (paddingThickness);

        var top = new Toplevel ();
        top.Add (testView);

        RunState rs = Application.Begin (top);
        Assert.Equal (4, testView.Frame.X);

        Assert.Equal (new (4, 4), testView.Frame.Location);
        Application.RaiseMouseEvent (new () { ScreenPosition = new (xy, xy), Flags = MouseFlags.Button1Pressed });

        Application.RaiseMouseEvent (new () { ScreenPosition = new (xy + 1, xy + 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition });
        Application.RunIteration (ref rs);

        Assert.Equal (expectedMoved, new Point (5, 5) == testView.Frame.Location);
        top.Dispose ();
    }

}
