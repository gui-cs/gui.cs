#nullable enable

namespace Terminal.Gui;

/// <summary>
///     Represents a region composed of one or more rectangles, providing methods for union, intersection, exclusion, and
///     complement operations.
/// </summary>
public class Region : IDisposable
{
    private List<Rectangle>? _rectangles;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Region"/> class.
    /// </summary>
    public Region () { _rectangles = []; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Region"/> class with the specified rectangle.
    /// </summary>
    /// <param name="rectangle">The initial rectangle for the region.</param>
    public Region (Rectangle rectangle) { _rectangles = [rectangle]; }

    /// <summary>
    ///     Adds the specified rectangle to the region. Merges all rectangles into a minimal bounding shape.
    /// </summary>
    /// <param name="rectangle">The rectangle to add to the region.</param>
    public void Union (Rectangle rectangle)
    {
        if (rectangle.IsEmpty)
        {
            return;
        }

        _rectangles!.Add (rectangle);
        _rectangles = MergeRectangles (_rectangles);
    }

    /// <summary>
    ///     Adds the specified region to this region. Merges all rectangles into a minimal bounding shape.
    /// </summary>
    /// <param name="region">The region to add to this region.</param>
    public void Union (Region? region)
    {
        if (region is { })
        {
            _rectangles!.AddRange (region._rectangles!);
            _rectangles = MergeRectangles (_rectangles);
        }
    }

    /// <summary>
    ///     Updates the region to be the intersection of itself with the specified rectangle.
    /// </summary>
    /// <param name="rectangle">The rectangle to intersect with the region.</param>
    public void Intersect (Rectangle rectangle)
    {
        _rectangles = _rectangles!.Select (r => Rectangle.Intersect (r, rectangle)).Where (r => !r.IsEmpty).ToList ();
    }

    /// <summary>
    ///     Updates the region to be the intersection of itself with the specified region.
    /// </summary>
    /// <param name="region">The region to intersect with this region.</param>
    public void Intersect (Region? region)
    {
        List<Rectangle> intersections = [];

        // Null is same as empty region
        region ??= new ();

        foreach (Rectangle rect1 in _rectangles!)
        {
            foreach (Rectangle rect2 in region!._rectangles!)
            {
                Rectangle intersected = Rectangle.Intersect (rect1, rect2);

                if (!intersected.IsEmpty)
                {
                    intersections.Add (intersected);
                }
            }
        }

        _rectangles = intersections;
    }

    /// <summary>
    ///     Removes the specified rectangle from the region.
    /// </summary>
    /// <param name="rectangle">The rectangle to exclude from the region.</param>
    public void Exclude (Rectangle rectangle) { _rectangles = _rectangles!.SelectMany (r => SubtractRectangle (r, rectangle)).ToList (); }

    /// <summary>
    ///     Removes the portion of the specified region from this region.
    /// </summary>
    /// <param name="region">The region to exclude from this region.</param>
    public void Exclude (Region? region)
    {
        // Null is same as empty region
        region ??= new ();

        foreach (Rectangle rect in region._rectangles!)
        {
            _rectangles = _rectangles!.SelectMany (r => SubtractRectangle (r, rect)).ToList ();
        }
    }

    /// <summary>
    ///     Updates the region to be the complement of itself within the specified bounds.
    /// </summary>
    /// <param name="bounds">The bounding rectangle to use for complementing the region.</param>
    public void Complement (Rectangle bounds)
    {
        if (bounds.IsEmpty || _rectangles!.Count == 0)
        {
            _rectangles!.Clear ();

            return;
        }

        List<Rectangle> complementRectangles = [bounds];

        foreach (Rectangle rect in _rectangles)
        {
            complementRectangles = complementRectangles.SelectMany (r => SubtractRectangle (r, rect)).ToList ();
        }

        _rectangles = complementRectangles;
    }

    /// <summary>
    ///     Creates an exact copy of the region.
    /// </summary>
    /// <returns>A new <see cref="Region"/> that is a copy of this instance.</returns>
    public Region Clone ()
    {
        var clone = new Region ();
        clone._rectangles = [.. _rectangles!];

        return clone;
    }

    /// <summary>
    ///     Gets a bounding rectangle for the entire region.
    /// </summary>
    /// <returns>A <see cref="Rectangle"/> that bounds the region.</returns>
    public Rectangle GetBounds ()
    {
        if (_rectangles!.Count == 0)
        {
            return Rectangle.Empty;
        }

        int left = _rectangles.Min (r => r.Left);
        int top = _rectangles.Min (r => r.Top);
        int right = _rectangles.Max (r => r.Right);
        int bottom = _rectangles.Max (r => r.Bottom);

        return new (left, top, right - left, bottom - top);
    }

    /// <summary>
    ///     Determines whether the region is empty.
    /// </summary>
    /// <returns><c>true</c> if the region is empty; otherwise, <c>false</c>.</returns>
    public bool IsEmpty () { return !_rectangles!.Any (); }

    /// <summary>
    ///     Determines whether the specified point is contained within the region.
    /// </summary>
    /// <param name="x">The x-coordinate of the point.</param>
    /// <param name="y">The y-coordinate of the point.</param>
    /// <returns><c>true</c> if the point is contained within the region; otherwise, <c>false</c>.</returns>
    public bool Contains (int x, int y) { return _rectangles!.Any (r => r.Contains (x, y)); }

    /// <summary>
    ///     Determines whether the specified rectangle is contained within the region.
    /// </summary>
    /// <param name="rectangle">The rectangle to check for containment.</param>
    /// <returns><c>true</c> if the rectangle is contained within the region; otherwise, <c>false</c>.</returns>
    public bool Contains (Rectangle rectangle) { return _rectangles!.Any (r => r.Contains (rectangle)); }

    /// <summary>
    ///     Returns an array of rectangles that represent the region.
    /// </summary>
    /// <returns>An array of <see cref="Rectangle"/> objects that make up the region.</returns>
    public Rectangle [] GetRectangles () { return _rectangles!.ToArray (); }

    /// <summary>
    ///     Offsets all rectangles in the region by the specified amounts.
    /// </summary>
    /// <param name="offsetX">The amount to offset along the x-axis.</param>
    /// <param name="offsetY">The amount to offset along the y-axis.</param>
    public void Offset (int offsetX, int offsetY)
    {
        for (var i = 0; i < _rectangles!.Count; i++)
        {
            Rectangle rect = _rectangles [i];
            _rectangles [i] = new (rect.Left + offsetX, rect.Top + offsetY, rect.Width, rect.Height);
        }
    }

    /// <summary>
    ///     Merges overlapping rectangles into a minimal set of non-overlapping rectangles with a minimal bounding shape.
    /// </summary>
    /// <param name="rectangles">The list of rectangles to merge.</param>
    /// <returns>A list of merged rectangles.</returns>
    internal static List<Rectangle> MergeRectangles (List<Rectangle> rectangles)
    {
        if (rectangles is not { Count: not 0 })
        {
            return [];
        }

        // Create events for the left and right edges of each rectangle
        List<(int x, int y1, int y2, bool isStart)> events = [];

        foreach (Rectangle rect in rectangles)
        {
            events.Add ((rect.Left, rect.Top, rect.Bottom, true));
            events.Add ((rect.Right, rect.Top, rect.Bottom, false));
        }

        // Sort events by x-coordinate, and by start before end if x-coordinates are equal
        events.Sort ((a, b) => a.x != b.x ? a.x.CompareTo (b.x) : a.isStart.CompareTo (b.isStart));

        List<(int y1, int y2)> activeIntervals = [];
        List<Rectangle> mergedRectangles = [];
        int currentX = events [0].x;

        foreach ((int x, int y1, int y2, bool isStart) in events)
        {
            if (x != currentX)
            {
                // Merge active intervals and create rectangles
                mergedRectangles.AddRange (MergeIntervals (activeIntervals, currentX, x));
                currentX = x;
            }

            if (isStart)
            {
                activeIntervals.Add ((y1, y2));
            }
            else
            {
                activeIntervals.Remove ((y1, y2));
            }
        }

        // Handle the last set of active intervals
        mergedRectangles.AddRange (MergeIntervals (activeIntervals, currentX, events [^1].x));

        return mergedRectangles;
    }

    /// <summary>
    ///     Merges overlapping intervals into a minimal set of non-overlapping intervals and creates rectangles from them.
    /// </summary>
    /// <param name="intervals">The list of y-coordinate intervals to merge.</param>
    /// <param name="startX">The starting x-coordinate for the rectangles.</param>
    /// <param name="endX">The ending x-coordinate for the rectangles.</param>
    /// <returns>A list of rectangles created from the merged intervals.</returns>
    private static List<Rectangle> MergeIntervals (List<(int y1, int y2)> intervals, int startX, int endX)
    {
        if (intervals.Count == 0)
        {
            return [];
        }

        // Sort intervals by their starting y-coordinate
        intervals.Sort ((a, b) => a.y1.CompareTo (b.y1));

        List<(int y1, int y2)> mergedIntervals = [];
        (int y1, int y2) currentInterval = intervals [0];

        for (var i = 1; i < intervals.Count; i++)
        {
            (int y1, int y2) nextInterval = intervals [i];

            if (currentInterval.y2 >= nextInterval.y1)
            {
                // Merge overlapping intervals
                currentInterval = (currentInterval.y1, Math.Max (currentInterval.y2, nextInterval.y2));
            }
            else
            {
                mergedIntervals.Add (currentInterval);
                currentInterval = nextInterval;
            }
        }

        mergedIntervals.Add (currentInterval);

        // Create rectangles from merged intervals
        List<Rectangle> rectangles = [];

        foreach ((int y1, int y2) in mergedIntervals)
        {
            rectangles.Add (new (startX, y1, endX - startX, y2 - y1));
        }

        return rectangles;
    }

    /// <summary>
    ///     Subtracts the specified rectangle from the original rectangle, returning the resulting rectangles.
    /// </summary>
    /// <param name="original">The original rectangle.</param>
    /// <param name="subtract">The rectangle to subtract from the original.</param>
    /// <returns>An enumerable collection of resulting rectangles after subtraction.</returns>
    private static IEnumerable<Rectangle> SubtractRectangle (Rectangle original, Rectangle subtract)
    {
        if (!original.IntersectsWith (subtract))
        {
            yield return original;

            yield break;
        }

        // Top segment
        if (original.Top < subtract.Top)
        {
            yield return new (original.Left, original.Top, original.Width, subtract.Top - original.Top);
        }

        // Bottom segment
        if (original.Bottom > subtract.Bottom)
        {
            yield return new (original.Left, subtract.Bottom, original.Width, original.Bottom - subtract.Bottom);
        }

        // Left segment
        if (original.Left < subtract.Left)
        {
            int top = Math.Max (original.Top, subtract.Top);
            int bottom = Math.Min (original.Bottom, subtract.Bottom);

            if (bottom > top)
            {
                yield return new (original.Left, top, subtract.Left - original.Left, bottom - top);
            }
        }

        // Right segment
        if (original.Right > subtract.Right)
        {
            int top = Math.Max (original.Top, subtract.Top);
            int bottom = Math.Min (original.Bottom, subtract.Bottom);

            if (bottom > top)
            {
                yield return new (subtract.Right, top, original.Right - subtract.Right, bottom - top);
            }
        }
    }

    /// <summary>
    ///     Releases all resources used by the <see cref="Region"/>.
    /// </summary>
    public void Dispose () { _rectangles!.Clear (); }
}
