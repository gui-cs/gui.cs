﻿#region

using Xunit.Abstractions;

#endregion

namespace Terminal.Gui.ViewTests;

/// <summary>
/// Tests for view coordinate mapping (e.g. <see cref="View.ScreenToFrame"/> etc...).
/// </summary>
public class CoordinateTests {
    readonly ITestOutputHelper _output;
    public CoordinateTests (ITestOutputHelper output) => _output = output;

    /// <summary>
    /// Tests that screen to view mapping works correctly when the view has no superview and there are no Frames on the view.
    /// </summary>
    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 0, 1, 1, 1, 1)]
    [InlineData (0, 0, 9, 9, 9, 9)]
    [InlineData (0, 0, 11, 11, 11, 11)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -1, -1)]
    [InlineData (1, 1, 1, 1, 0, 0)]
    [InlineData (1, 1, 9, 9, 8, 8)]
    [InlineData (1, 1, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToView_NoSuper_NoFrames (int viewX, int viewY, int x, int y, int expectedX, int expectedY) {
        var view = new View {
                                X = viewX,
                                Y = viewY,
                                Width = 10,
                                Height = 10
                            };

        var actual = view.ScreenToFrame (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>
    /// Tests that screen to view mapping works correctly when the view has no superview and there ARE Frames on the view.
    /// </summary>
    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 0, 1, 1, 1, 1)]
    [InlineData (0, 0, 9, 9, 9, 9)]
    [InlineData (0, 0, 11, 11, 11, 11)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -1, -1)]
    [InlineData (1, 1, 1, 1, 0, 0)]
    [InlineData (1, 1, 9, 9, 8, 8)]
    [InlineData (1, 1, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToView_NoSuper_HasFrames (int viewX, int viewY, int x, int y, int expectedX, int expectedY) {
        var view = new View {
                                X = viewX,
                                Y = viewY,
                                Width = 10,
                                Height = 10,
                                BorderStyle = LineStyle.Single
                            };

        var actual = view.ScreenToFrame (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>
    /// Tests that screen to view mapping works correctly when the view has as superview it does not have Frames
    /// </summary>
    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 0, 1, 1, 1, 1)]
    [InlineData (0, 0, 9, 9, 9, 9)]
    [InlineData (0, 0, 11, 11, 11, 11)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -1, -1)]
    [InlineData (1, 1, 1, 1, 0, 0)]
    [InlineData (1, 1, 9, 9, 8, 8)]
    [InlineData (1, 1, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToView_SuperHasNoFrames (int viewX, int viewY, int x, int y, int expectedX, int expectedY) {
        var super = new View {
                                 X = 0,
                                 Y = 0,
                                 Width = 10,
                                 Height = 10
                             };
        var view = new View {
                                X = viewX,
                                Y = viewY,
                                Width = 5,
                                Height = 5
                            };
        super.Add (view);

        var actual = view.ScreenToFrame (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>
    /// Tests that screen to view mapping works correctly when the view has as superview it DOES have Frames
    /// </summary>
    [Theory]
    [InlineData (0, 0, 0, 0, -1, -1)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (0, 0, 1, 1, 0, 0)]
    [InlineData (0, 0, 9, 9, 8, 8)]
    [InlineData (0, 0, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -2, -2)]
    [InlineData (1, 1, 1, 1, -1, -1)]
    [InlineData (1, 1, 9, 9, 7, 7)]
    [InlineData (1, 1, 11, 11, 9, 9)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToView_SuperHasFrames (int viewX, int viewY, int x, int y, int expectedX, int expectedY) {
        var super = new View {
                                 X = 0,
                                 Y = 0,
                                 Width = 10,
                                 Height = 10,
                                 BorderStyle = LineStyle.Single
                             };
        var view = new View {
                                X = viewX,
                                Y = viewY,
                                Width = 5,
                                Height = 5
                            };
        super.Add (view);

        var actual = view.ScreenToFrame (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>
    /// Tests that screen to bounds mapping works correctly when the view has no superview and there are no Frames on the view.
    /// </summary>
    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 0, 1, 1, 1, 1)]
    [InlineData (0, 0, 9, 9, 9, 9)]
    [InlineData (0, 0, 11, 11, 11, 11)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -1, -1)]
    [InlineData (1, 1, 1, 1, 0, 0)]
    [InlineData (1, 1, 9, 9, 8, 8)]
    [InlineData (1, 1, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToBounds_NoSuper_NoFrames (int viewX, int viewY, int x, int y, int expectedX, int expectedY) {
        var view = new View {
                                X = viewX,
                                Y = viewY,
                                Width = 10,
                                Height = 10
                            };

        var actual = view.ScreenToBounds (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>
    /// Tests that screen to bounds mapping works correctly when the view has no superview and there ARE Frames on the view.
    /// </summary>
    [Theory]
    [InlineData (0, 0, 0, 0, -1, -1)]
    [InlineData (0, 0, 1, 1, 0, 0)]
    [InlineData (0, 0, 9, 9, 8, 8)]
    [InlineData (0, 0, 11, 11, 10, 10)]
    [InlineData (1, 1, 0, 0, -2, -2)]
    [InlineData (1, 1, 1, 1, -1, -1)]
    [InlineData (1, 1, 9, 9, 7, 7)]
    [InlineData (1, 1, 11, 11, 9, 9)]
    public void ScreenToBounds_NoSuper_HasFrames (int viewX, int viewY, int x, int y, int expectedX, int expectedY) {
        var view = new View {
                                X = viewX,
                                Y = viewY,
                                Width = 10,
                                Height = 10,
                                BorderStyle = LineStyle.Single
                            };

        var actual = view.ScreenToBounds (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>
    /// Tests that screen to bounds mapping works correctly when the view has as superview it does not have Frames
    /// </summary>
    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 0, 1, 1, 1, 1)]
    [InlineData (0, 0, 9, 9, 9, 9)]
    [InlineData (0, 0, 11, 11, 11, 11)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -1, -1)]
    [InlineData (1, 1, 1, 1, 0, 0)]
    [InlineData (1, 1, 9, 9, 8, 8)]
    [InlineData (1, 1, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToBounds_SuperHasNoFrames (int viewX, int viewY, int x, int y, int expectedX, int expectedY) {
        var super = new View {
                                 X = 0,
                                 Y = 0,
                                 Width = 10,
                                 Height = 10
                             };
        var view = new View {
                                X = viewX,
                                Y = viewY,
                                Width = 5,
                                Height = 5
                            };
        super.Add (view);

        var actual = view.ScreenToBounds (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>
    /// Tests that screen to bounds mapping works correctly when the view has as superview it DOES have Frames
    /// </summary>
    [Theory]
    [InlineData (0, 0, 0, 0, -1, -1)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (0, 0, 1, 1, 0, 0)]
    [InlineData (0, 0, 9, 9, 8, 8)]
    [InlineData (0, 0, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -2, -2)]
    [InlineData (1, 1, 1, 1, -1, -1)]
    [InlineData (1, 1, 9, 9, 7, 7)]
    [InlineData (1, 1, 11, 11, 9, 9)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToBounds_SuperHasFrames (int viewX, int viewY, int x, int y, int expectedX, int expectedY) {
        var super = new View {
                                 X = 0,
                                 Y = 0,
                                 Width = 10,
                                 Height = 10,
                                 BorderStyle = LineStyle.Single
                             };
        var view = new View {
                                X = viewX,
                                Y = viewY,
                                Width = 5,
                                Height = 5
                            };
        super.Add (view);

        var actual = view.ScreenToFrame (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }
}
