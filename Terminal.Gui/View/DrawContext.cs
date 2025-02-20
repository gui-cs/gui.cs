#nullable enable
namespace Terminal.Gui;

/// <summary>
/// Provides a context for drawing operations, allowing views to report areas they draw.
/// </summary>
public class DrawContext
{
    private readonly Region _drawnRegion = new Region ();

    /// <summary>
    /// Gets the region drawn so far in this context.
    /// </summary>
    public Region DrawnRegion
    {
        get { return _drawnRegion.Clone (); }
    }

    /// <summary>
    /// Reports that a rectangle has been drawn.
    /// </summary>
    /// <param name="rect">The rectangle that was drawn.</param>
    public void AddDrawnRectangle (Rectangle rect)
    {
        _drawnRegion.Union (rect);
    }

    /// <summary>
    /// Reports that a region has been drawn.
    /// </summary>
    /// <param name="region">The region that was drawn.</param>
    public void AddDrawnRegion (Region region)
    {
        if (region != null)
        {
            _drawnRegion.Union (region);
        }
    }
}
