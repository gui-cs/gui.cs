#nullable enable
using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
public class DrawTests (ITestOutputHelper _output)
{

    [Fact]
    [SetupFakeDriver]
    public void Move_Is_Not_Constrained_To_Viewport ()
    {
        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 3, Height = 3
        };
        view.Margin!.Thickness = new (1);

        view.Move (0, 0);
        Assert.Equal (new (2, 2), new Point (Application.Driver!.Col, Application.Driver!.Row));

        view.Move (-1, -1);
        Assert.Equal (new (1, 1), new Point (Application.Driver!.Col, Application.Driver!.Row));

        view.Move (1, 1);
        Assert.Equal (new (3, 3), new Point (Application.Driver!.Col, Application.Driver!.Row));
    }

    [Fact]
    [SetupFakeDriver]
    public void AddRune_Is_Constrained_To_Viewport ()
    {
        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 3, Height = 3
        };
        view.Padding!.Thickness = new (1);
        view.Padding.Diagnostics = ViewDiagnosticFlags.Thickness;
        view.BeginInit ();
        view.EndInit ();
        view.Draw ();

        // Only valid location w/in Viewport is 0, 0 (view) - 2, 2 (screen)
        Assert.Equal ((Rune)' ', Application.Driver?.Contents! [2, 2].Rune);

        // When we exit Draw, the view is excluded from the clip. So drawing at 0,0, is not valid and is clipped.
        view.AddRune (0, 0, Rune.ReplacementChar);
        Assert.Equal ((Rune)' ', Application.Driver?.Contents! [2, 2].Rune);

        view.AddRune (-1, -1, Rune.ReplacementChar);
        Assert.Equal ((Rune)'P', Application.Driver?.Contents! [1, 1].Rune);

        view.AddRune (1, 1, Rune.ReplacementChar);
        Assert.Equal ((Rune)'P', Application.Driver?.Contents! [3, 3].Rune);
    }

    [Theory]
    [InlineData (0, 0, 1, 1)]
    [InlineData (0, 0, 2, 2)]
    [InlineData (-1, -1, 2, 2)]
    [SetupFakeDriver]
    public void FillRect_Fills_HonorsClip (int x, int y, int width, int height)
    {
        var superView = new View { Width = Dim.Fill (), Height = Dim.Fill () };

        var view = new View
        {
            Text = "X",
            X = 1, Y = 1,
            Width = 3, Height = 3,
            BorderStyle = LineStyle.Single
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();
        superView.LayoutSubviews ();

        superView.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      _output);

        Rectangle toFill = new (x, y, width, height);
        View.SetClipToScreen ();
        view.FillRect (toFill);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      _output);

        // Now try to clear beyond Viewport (invalid; clipping should prevent)
        superView.SetNeedsDraw ();
        superView.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      _output);
        toFill = new (-width, -height, width, height);
        view.FillRect (toFill);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      _output);

        // Now try to clear beyond Viewport (valid)
        superView.SetNeedsDraw ();
        superView.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      _output);
        toFill = new (-1, -1, width + 1, height + 1);

        View.SetClipToScreen ();
        view.FillRect (toFill);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      _output);

        // Now clear too much size
        superView.SetNeedsDraw ();
        superView.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      _output);
        toFill = new (0, 0, width * 2, height * 2);
        View.SetClipToScreen ();
        view.FillRect (toFill);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      _output);
    }

    [Fact]
    [SetupFakeDriver]
    public void Clear_ClearsEntireViewport ()
    {
        var superView = new View { Width = Dim.Fill (), Height = Dim.Fill () };

        var view = new View
        {
            Text = "X",
            X = 1, Y = 1,
            Width = 3, Height = 3,
            BorderStyle = LineStyle.Single
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();
        superView.LayoutSubviews ();

        superView.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      _output);

        // On Draw exit the view is excluded from the clip, so this will do nothing.
        view.ClearViewport ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      _output);

        View.SetClipToScreen ();

        view.ClearViewport ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      _output);
    }

    [Fact]
    [SetupFakeDriver]
    public void Clear_WithClearVisibleContentOnly_ClearsVisibleContentOnly ()
    {
        var superView = new View { Width = Dim.Fill (), Height = Dim.Fill () };

        var view = new View
        {
            Text = "X",
            X = 1, Y = 1,
            Width = 3, Height = 3,
            BorderStyle = LineStyle.Single,
            ViewportSettings = ViewportSettings.ClearContentOnly
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();
        superView.LayoutSubviews ();

        superView.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      _output);
        View.SetClipToScreen ();
        view.ClearViewport ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      _output);
    }

    [Fact]
    [AutoInitShutdown]
    [Trait ("Category", "Unicode")]
    public void CJK_Compatibility_Ideographs_ConsoleWidth_ColumnWidth_Equal_Two ()
    {
        const string us = "\U0000f900";
        var r = (Rune)0xf900;

        Assert.Equal ("豈", us);
        Assert.Equal ("豈", r.ToString ());
        Assert.Equal (us, r.ToString ());

        Assert.Equal (2, us.GetColumns ());
        Assert.Equal (2, r.GetColumns ());

        var win = new Window { Title = us };
        var view = new View { Text = r.ToString (), Height = Dim.Fill (), Width = Dim.Fill () };
        var tf = new TextField { Text = us, Y = 1, Width = 3 };
        win.Add (view, tf);
        Toplevel top = new ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (10, 4);

        const string expectedOutput = """

                                      ┌┤豈├────┐
                                      │豈      │
                                      │豈      │
                                      └────────┘
                                      """;
        TestHelpers.AssertDriverContentsWithFrameAre (expectedOutput, _output);

        TestHelpers.AssertDriverContentsAre (expectedOutput, _output);

        // This test has nothing to do with color - removing as it is not relevant and fragile
        top.Dispose ();
    }

    // TODO: Simplify this test to just use AddRune directly
    [Fact]
    [SetupFakeDriver]
    [Trait ("Category", "Unicode")]
    public void Clipping_Wide_Runes ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (30, 1);

        var top = new View ()
        {
            Id = "top",
            Width = Dim.Fill (),
            Height = Dim.Fill (),
        };
        var frameView = new View ()
        {
            Id = "frameView",
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = """
                   これは広いルーンラインです。
                   """,
        };
        frameView.Border!.LineStyle = LineStyle.Single;
        frameView.Border.Thickness = new (1, 0, 0, 0);

        top.Add (frameView);
        View.SetClipToScreen ();
        top.Layout ();
        top.Draw ();

        string expectedOutput = """
                                      │これは広いルーンラインです。
                                      """;

        TestHelpers.AssertDriverContentsWithFrameAre (expectedOutput, _output);

        var view = new View
        {
            Text = "0123456789",
            //Text = "ワイドルー。",
            X = 2,
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            BorderStyle = LineStyle.Single
        };
        view.Border!.Thickness = new (1, 0, 1, 0);

        top.Add (view);
        top.Layout ();
        View.SetClipToScreen ();
        top.Draw ();
        //                            012345678901234567890123456789012345678
        //                            012 34 56 78 90 12 34 56 78 90 12 34 56 78
        //                            │こ れ  は 広 い  ル ー ン  ラ イ ン で  す 。
        //                            01 2345678901234 56 78 90 12 34 56 
        //                            │� |0123456989│� ン  ラ イ ン で  す 。
        expectedOutput = """
                         │�│0123456789│�ンラインです。
                         """;

        TestHelpers.AssertDriverContentsWithFrameAre (expectedOutput, _output);
    }

    // TODO: Add more AddRune tests to cover all the cases where wide runes are clipped

    [Fact]
    [AutoInitShutdown]
    [Trait ("Category", "Output")]
    public void Colors_On_TextAlignment_Right_And_Bottom ()
    {
        var viewRight = new View
        {
            Text = "Test",
            Width = 6,
            Height = 1,
            TextAlignment = Alignment.End,
            ColorScheme = Colors.ColorSchemes ["Base"]
        };

        var viewBottom = new View
        {
            Text = "Test",
            TextDirection = TextDirection.TopBottom_LeftRight,
            Y = 1,
            Width = 1,
            Height = 6,
            VerticalTextAlignment = Alignment.End,
            ColorScheme = Colors.ColorSchemes ["Base"]
        };
        Toplevel top = new ();
        top.Add (viewRight, viewBottom);

        var rs = Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (7, 7);
        Application.RunIteration (ref rs);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                        Test
                                                            
                                                            
                                                      T     
                                                      e     
                                                      s     
                                                      t     
                                                      """,
                                                      _output
                                                     );

        TestHelpers.AssertDriverAttributesAre (
                                               """

                                               000000
                                               0
                                               0
                                               0
                                               0
                                               0
                                               0
                                               """,
                                               _output,
                                               Application.Driver,
                                               Colors.ColorSchemes ["Base"]!.Normal
                                              );
        top.Dispose ();
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsLayout);
        Assert.True (view.NeedsDraw);
        view.Layout ();

        Assert.Equal (new (0, 0, 2, 2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        Assert.True (view.NeedsDraw);
        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                      ┌┐
                                                      └┘
                                                      """,
                                                      _output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Bottom ()
    {
        var view = new View { Width = 2, Height = 1, BorderStyle = LineStyle.Single };
        view.Border!.Thickness = new (1, 1, 1, 0);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (0, 0, 2, 1), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre ("──", _output);
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Left ()
    {
        var view = new View { Width = 1, Height = 2, BorderStyle = LineStyle.Single };
        view.Border!.Thickness = new (0, 1, 1, 1);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (0, 0, 1, 2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                      │
                                                      │
                                                      """,
                                                      _output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Right ()
    {
        var view = new View { Width = 1, Height = 2, BorderStyle = LineStyle.Single };
        view.Border!.Thickness = new (1, 1, 0, 1);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (0, 0, 1, 2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                      │
                                                      │
                                                      """,
                                                      _output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Top ()
    {
        var view = new View { Width = 2, Height = 1, BorderStyle = LineStyle.Single };
        view.Border!.Thickness = new (1, 0, 1, 1);

        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (0, 0, 2, 1), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      "││",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Negative_Viewport_Horizontal_With_New_Lines ()
    {
        var subView = new View
        {
            Id = "subView",
            X = 1,
            Width = 1,
            Height = 7,
            Text = """
                   s
                   u
                   b
                   V
                   i
                   e
                   w
                   """
        };

        var view = new View
        {
            Id = "view", Width = 2, Height = 20, Text = """
                                                        0
                                                        1
                                                        2
                                                        3
                                                        4
                                                        5
                                                        6
                                                        7
                                                        8
                                                        9
                                                        0
                                                        1
                                                        2
                                                        3
                                                        4
                                                        5
                                                        6
                                                        7
                                                        8
                                                        9
                                                        """
        };
        view.Add (subView);
        var content = new View { Id = "content", Width = 20, Height = 20 };
        content.Add (view);

        var container = new View
        {
            Id = "container",
            X = 1,
            Y = 1,
            Width = 5,
            Height = 5
        };
        container.Add (content);
        Toplevel top = new ();
        top.Add (container);
        var rs = Application.Begin (top);

        top.Draw ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       0s
                                                       1u
                                                       2b
                                                       3V
                                                       4i
                                                      """,
                                                      _output
                                                     );

        content.X = -1;
        Application.LayoutAndDraw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       s
                                                       u
                                                       b
                                                       V
                                                       i
                                                      """,
                                                      _output
                                                     );

        content.X = -2;
        Application.LayoutAndDraw ();
        TestHelpers.AssertDriverContentsWithFrameAre (@"", _output);

        content.X = 0;
        content.Y = -1;
        Application.LayoutAndDraw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       1u
                                                       2b
                                                       3V
                                                       4i
                                                       5e
                                                      """,
                                                      _output
                                                     );

        content.Y = -6;
        Application.LayoutAndDraw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       6w
                                                       7 
                                                       8 
                                                       9 
                                                       0 
                                                      """,
                                                      _output
                                                     );

        content.Y = -19;
        Application.LayoutAndDraw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       9
                                                      """,
                                                      _output
                                                     );

        content.Y = -20;
        Application.LayoutAndDraw ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);

        content.X = -2;
        content.Y = 0;
        Application.LayoutAndDraw ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Negative_Viewport_Horizontal_Without_New_Lines ()
    {
        // BUGBUG: This previously assumed the default height of a View was 1.
        var subView = new View
        {
            Id = "subView",
            Y = 1,
            Width = 7,
            Height = 1,
            Text = "subView"
        };
        var view = new View { Id = "view", Width = 20, Height = 2, Text = "01234567890123456789" };
        view.Add (subView);
        var content = new View { Id = "content", Width = 20, Height = 20 };
        content.Add (view);

        var container = new View
        {
            Id = "container",
            X = 1,
            Y = 1,
            Width = 5,
            Height = 5
        };
        container.Add (content);
        Toplevel top = new ();
        top.Add (container);

        // BUGBUG: v2 - it's bogus to reference .Frame before BeginInit. And why is the clip being set anyway???

        top.SubviewsLaidOut += Top_LayoutComplete;
        Application.Begin (top);

        Application.LayoutAndDraw ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       01234
                                                       subVi
                                                      """,
                                                      _output
                                                     );

        content.X = -1;
        Application.LayoutAndDraw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       12345
                                                       ubVie
                                                      """,
                                                      _output
                                                     );

        content.Y = -1;
        Application.LayoutAndDraw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       ubVie
                                                      """,
                                                      _output
                                                     );

        content.Y = -2;
        Application.LayoutAndDraw ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);

        content.X = -20;
        content.Y = 0;
        Application.LayoutAndDraw ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);
        top.Dispose ();

        return;

        void Top_LayoutComplete (object? sender, LayoutEventArgs e) { Application.Driver!.Clip = new (container.Frame); }
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Negative_Viewport_Vertical ()
    {
        var subView = new View
        {
            Id = "subView",
            X = 1,
            Width = 1,
            Height = 7,
            Text = "subView",
            TextDirection = TextDirection.TopBottom_LeftRight
        };

        var view = new View
        {
            Id = "view",
            Width = 2,
            Height = 20,
            Text = "01234567890123456789",
            TextDirection = TextDirection.TopBottom_LeftRight
        };
        view.Add (subView);
        var content = new View { Id = "content", Width = 20, Height = 20 };
        content.Add (view);

        var container = new View
        {
            Id = "container",
            X = 1,
            Y = 1,
            Width = 5,
            Height = 5
        };
        container.Add (content);
        Toplevel top = new ();
        top.Add (container);
        Application.Begin (top);

        Application.LayoutAndDraw ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       0s
                                                       1u
                                                       2b
                                                       3V
                                                       4i
                                                      """,
                                                      _output
                                                     );

        content.X = -1;
        Application.LayoutAndDraw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       s
                                                       u
                                                       b
                                                       V
                                                       i
                                                      """,
                                                      _output
                                                     );

        content.X = -2;
        Application.LayoutAndDraw ();
        TestHelpers.AssertDriverContentsWithFrameAre (@"", _output);

        content.X = 0;
        content.Y = -1;
        Application.LayoutAndDraw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       1u
                                                       2b
                                                       3V
                                                       4i
                                                       5e
                                                      """,
                                                      _output
                                                     );

        content.Y = -6;
        Application.LayoutAndDraw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       6w
                                                       7 
                                                       8 
                                                       9 
                                                       0 
                                                      """,
                                                      _output
                                                     );

        content.Y = -19;
        Application.LayoutAndDraw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       9
                                                      """,
                                                      _output
                                                     );

        content.Y = -20;
        Application.LayoutAndDraw ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);

        content.X = -2;
        content.Y = 0;
        Application.LayoutAndDraw ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);
        top.Dispose ();
    }

    [Theory]
    [SetupFakeDriver]
    [InlineData ("𝔽𝕆𝕆𝔹𝔸R")]
    [InlineData ("a𐐀b")]
    public void DrawHotString_NonBmp (string expected)
    {
        var view = new View { Width = 10, Height = 1 };
        view.DrawHotString (expected, Attribute.Default, Attribute.Default);

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    // TODO: The tests below that use Label should use View instead.
    [Fact]
    [AutoInitShutdown]
    public void Non_Bmp_ConsoleWidth_ColumnWidth_Equal_Two ()
    {
        var us = "\U0001d539";
        var r = (Rune)0x1d539;

        Assert.Equal ("𝔹", us);
        Assert.Equal ("𝔹", r.ToString ());
        Assert.Equal (us, r.ToString ());

        Assert.Equal (1, us.GetColumns ());
        Assert.Equal (1, r.GetColumns ());

        var win = new Window { Title = us };
        var view = new Label { Text = r.ToString () };
        var tf = new TextField { Text = us, Y = 1, Width = 3 };
        win.Add (view, tf);
        Toplevel top = new ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (10, 4);

        var expected = """

                       ┌┤𝔹├─────┐
                       │𝔹       │
                       │𝔹       │
                       └────────┘
                       """;
        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        TestHelpers.AssertDriverContentsAre (expected, _output);
        top.Dispose ();

        // This test has nothing to do with color - removing as it is not relevant and fragile
    }

    [Fact]
    [SetupFakeDriver]
    public void SetClip_ClipVisibleContentOnly_VisibleContentIsClipped ()
    {
        // Screen is 25x25
        // View is 25x25
        // Viewport is (0, 0, 23, 23)
        // ContentSize is (10, 10)
        // ViewportToScreen is (1, 1, 23, 23)
        // Visible content is (1, 1, 10, 10)
        // Expected clip is (1, 1, 10, 10) - same as visible content
        Rectangle expectedClip = new (1, 1, 10, 10);

        // Arrange
        var view = new View
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ViewportSettings = ViewportSettings.ClipContentOnly
        };
        view.SetContentSize (new Size (10, 10));
        view.Border!.Thickness = new (1);
        view.BeginInit ();
        view.EndInit ();
        Assert.Equal (view.Frame, View.GetClip ()!.GetBounds ());

        // Act
        view.AddViewportToClip ();

        // Assert
        Assert.Equal (expectedClip, View.GetClip ()!.GetBounds ());
        view.Dispose ();
    }

    [Fact]
    [SetupFakeDriver]
    public void SetClip_Default_ClipsToViewport ()
    {
        // Screen is 25x25
        // View is 25x25
        // Viewport is (0, 0, 23, 23)
        // ContentSize is (10, 10)
        // ViewportToScreen is (1, 1, 23, 23)
        // Visible content is (1, 1, 10, 10)
        // Expected clip is (1, 1, 23, 23) - same as Viewport
        Rectangle expectedClip = new (1, 1, 23, 23);

        // Arrange
        var view = new View
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        view.SetContentSize (new Size (10, 10));
        view.Border!.Thickness = new (1);
        view.BeginInit ();
        view.EndInit ();
        Assert.Equal (view.Frame, View.GetClip ()!.GetBounds ());
        view.Viewport = view.Viewport with { X = 1, Y = 1 };

        // Act
        view.AddViewportToClip ();

        // Assert
        Assert.Equal (expectedClip, View.GetClip ()!.GetBounds ());
        view.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void Draw_Throws_IndexOutOfRangeException_With_Negative_Bounds ()
    {
        Application.Init (new FakeDriver ());

        Toplevel top = new ();

        var view = new View { X = -2, Text = "view" };
        top.Add (view);

        Application.Iteration += (s, a) =>
                                 {
                                     Assert.Equal (-2, view.X);

                                     Application.RequestStop ();
                                 };

        try
        {
            Application.Run (top);
        }
        catch (IndexOutOfRangeException ex)
        {
            // After the fix this exception will not be caught.
            Assert.IsType<IndexOutOfRangeException> (ex);
        }

        top.Dispose ();

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }
}
