using System;
using Terminal.Gui;
using UICatalog;

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

    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),
            TabStop = TabBehavior.TabGroup
        };

        // Add drag handling to window
        app.MouseEvent += (s, e) =>
                          {
                              if (e.Flags.HasFlag (MouseFlags.Button1Pressed))
                              {
                                  if (!e.Flags.HasFlag (MouseFlags.ReportMousePosition))
                                  { // Start drag
                                      _dragStart = e.Position;
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
                                      AddRectangleFromPoints (_dragStart.Value, e.Position);
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
                                  _region.Draw (app.LineCanvas, LineStyle.Single);

                                  // If currently dragging, draw preview rectangle
                                  if (_isDragging && _dragStart.HasValue)
                                  {
                                      Point currentMousePos = app.ScreenToViewport (Application.GetLastMousePosition ()!.Value);
                                      var previewRegion = new Region (GetRectFromPoints (_dragStart.Value, currentMousePos));
                                      previewRegion.Draw (app.LineCanvas, LineStyle.Dashed);
                                  }
                              };

        Application.Run (app);

        // Clean up
        app.Dispose ();
        Application.Shutdown ();
    }

    private void AddRectangleFromPoints (Point start, Point end)
    {
        Rectangle rect = GetRectFromPoints (start, end);

        if (!rect.IsEmpty)
        {
            _region.Combine (rect, RegionOp.Union);
        }
    }

    private static Rectangle GetRectFromPoints (Point start, Point end)
    {
        int x = Math.Min (start.X, end.X);
        int y = Math.Min (start.Y, end.Y);
        int width = Math.Abs (end.X - start.X);
        int height = Math.Abs (end.Y - start.Y);

        return new (x, y, width, height);
    }
}
