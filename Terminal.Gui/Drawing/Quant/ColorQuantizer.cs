﻿using System.Collections.ObjectModel;

namespace Terminal.Gui.Drawing.Quant;

/// <summary>
/// Translates colors in an image into a Palette of up to 256 colors.
/// </summary>
public class ColorQuantizer
{
    /// <summary>
    /// Gets the current colors in the palette based on the last call to
    /// <see cref="BuildPalette"/>.
    /// </summary>
    public IReadOnlyCollection<Color> Palette { get; private set; } = new List<Color> ();

    /// <summary>
    /// Gets or sets the maximum number of colors to put into the <see cref="Palette"/>.
    /// Defaults to 256 (the maximum for sixel images).
    /// </summary>
    public int MaxColors { get; set; } = 256;

    /// <summary>
    /// Gets or sets the algorithm used to map novel colors into existing
    /// palette colors (closest match). Defaults to <see cref="CIE94ColorDistance"/>
    /// </summary>
    public IColorDistance DistanceAlgorithm { get; set; } = new CIE94ColorDistance ();

    /// <summary>
    /// Gets or sets the algorithm used to build the <see cref="Palette"/>.
    /// Defaults to <see cref="MedianCutPaletteBuilder"/>
    /// </summary>
    public IPaletteBuilder PaletteBuildingAlgorithm { get; set; } = new MedianCutPaletteBuilder ();

    public void BuildPalette (Color [,] pixels)
    {
        List<Color> allColors = new List<Color> ();
        int width = pixels.GetLength (0);
        int height = pixels.GetLength (1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                allColors.Add (pixels [x, y]);
            }
        }

        Palette = PaletteBuildingAlgorithm.BuildPalette (allColors, MaxColors);
    }

    public int GetNearestColor (Color toTranslate)
    {
        // Simple nearest color matching based on Euclidean distance in RGB space
        double minDistance = double.MaxValue;
        int nearestIndex = 0;

        for (var index = 0; index < Palette.Count; index++)
        {
            Color color = Palette.ElementAt (index);
            double distance = DistanceAlgorithm.CalculateDistance (color, toTranslate);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = index;
            }
        }

        return nearestIndex;
    }
}