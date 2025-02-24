using System;
using System.Linq;
using System.Text;
using Terminal.Gui;
using UICatalog;
using UICatalog.Scenarios;

/// <summary>
///     Demonstrates creating and drawing regions through mouse dragging.
/// </summary>
[ScenarioMetadata ("Regions", "Region Tester")]
[ScenarioCategory ("Mouse and Keyboard")]
[ScenarioCategory ("Drawing")]
public class RegionScenario : Scenario
{
    private readonly Region _region = new ();
    private Point? _dragStart;
    private bool _isDragging;

    public class ToolsView : Window
    {
        //private Button _addLayerBtn;
        private readonly AttributeView _colors;
        //private RadioGroup _stylePicker;

        public Attribute CurrentColor
        {
            get => _colors.Value;
            set => _colors.Value = value;
        }

        public ToolsView ()
        {
            BorderStyle = LineStyle.Dotted;
            Border.Thickness = new (1, 2, 1, 1);
            Height = Dim.Auto ();
            Width = Dim.Auto ();

            _colors = new ();
            Add (_colors);

        }

        public event Action AddLayer;

        public override void BeginInit ()
        {
            base.BeginInit ();

            _colors.ValueChanged += (s, e) => ColorChanged?.Invoke (this, e);

            //_stylePicker = new ()
            //{
            //    X = 0, Y = Pos.Bottom (_colors), RadioLabels = Enum.GetNames (typeof (LineStyle)).ToArray ()
            //};
            //_stylePicker.SelectedItemChanged += (s, a) => { SetStyle?.Invoke ((LineStyle)a.SelectedItem); };
            //_stylePicker.SelectedItem = 1;

            //_addLayerBtn = new () { Text = "New Layer", X = Pos.Center (), Y = Pos.Bottom (_stylePicker) };

            //_addLayerBtn.Accepting += (s, a) => AddLayer?.Invoke ();
            //Add (_colors, _stylePicker, _addLayerBtn);
        }

        public event EventHandler<Attribute> ColorChanged;
        public event Action<LineStyle> SetStyle;
    }

    public Attribute? _attribute;

    public Rune? _previewFillRune = CM.Glyphs.Stipple;

    public Rune? _fillRune = CM.Glyphs.Dot;

    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),
            TabStop = TabBehavior.TabGroup
        };

        _attribute = app.ColorScheme.Normal;

        var tools = new ToolsView { Title = "Tools", X = Pos.AnchorEnd (), Y = 2 };

        tools.ColorChanged += (s, e) => _attribute = e;
        //tools.SetStyle += b => canvas.CurrentTool = new DrawLineTool { LineStyle = b };
        //tools.AddLayer += () => canvas.AddLayer ();

        app.Add (tools);

        // Add drag handling to window
        app.MouseEvent += (s, e) =>
                          {
                              if (e.Flags.HasFlag (MouseFlags.Button1Pressed))
                              {
                                  if (!e.Flags.HasFlag (MouseFlags.ReportMousePosition))
                                  { // Start drag
                                      _dragStart = e.ScreenPosition;
                                      _isDragging = true;
                                  }
                                  else
                                  {
                                      // Drag
                                      if (_isDragging && _dragStart.HasValue)
                                      {
                                          app.SetNeedsDraw ();
                                      }
                                  }
                              }

                              if (e.Flags.HasFlag (MouseFlags.Button1Released))
                              {
                                  if (_isDragging && _dragStart.HasValue)
                                  {
                                      // Add the new region
                                      AddRectangleFromPoints (_dragStart.Value, e.ScreenPosition);
                                      _isDragging = false;
                                      _dragStart = null;
                                  }

                                  app.SetNeedsDraw ();
                              }
                          };

        // Draw the regions
        app.DrawingContent += (s, e) =>
                              {
                                  // Draw all regions with single line style
                                  //_region.FillRectangles (_attribute.Value, _fillRune);
                                  if (_outer)
                                  {
                                      _region.DrawOuterBoundary (app.LineCanvas, LineStyle.Single, _attribute);
                                  }
                                  else
                                  {
                                      _region.FillRectangles (_attribute.Value, _previewFillRune);
                                  }

                                  // If currently dragging, draw preview rectangle
                                  if (_isDragging && _dragStart.HasValue)
                                  {
                                      Point currentMousePos = Application.GetLastMousePosition ()!.Value;
                                      var previewRect = GetRectFromPoints (_dragStart.Value, currentMousePos);
                                      var previewRegion = new Region (previewRect);

                                      previewRegion.FillRectangles (_attribute.Value, _previewFillRune);

                                      // previewRegion.DrawBoundaries(app.LineCanvas, LineStyle.Dashed, _attribute);
                                  }
                              };

        app.KeyDown += (s, e) =>
                       {
                           if (e.KeyCode == KeyCode.B)
                           {
                               _outer = !_outer;
                               app.SetNeedsDraw ();
                           }
                       };

        Application.Run (app);

        // Clean up
        app.Dispose ();
        Application.Shutdown ();
    }

    private bool _outer;

    private void AddRectangleFromPoints (Point start, Point end)
    {
        var rect = GetRectFromPoints (start, end);
        var region = new Region (rect);
        _region.Combine (region, RegionOp.MinimalUnion); // Or RegionOp.MinimalUnion if you want minimal rectangles
    }

    private Rectangle GetRectFromPoints (Point start, Point end)
    {
        int left = Math.Min (start.X, end.X);
        int top = Math.Min (start.Y, end.Y);
        int right = Math.Max (start.X, end.X);
        int bottom = Math.Max (start.Y, end.Y);

        // Ensure minimum width and height of 1
        int width = Math.Max (1, right - left + 1);
        int height = Math.Max (1, bottom - top + 1);

        return new Rectangle (left, top, width, height);
    }
}
