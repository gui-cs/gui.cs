﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Terminal.Gui.Color;

namespace UnitTests.Drawing;

public class SixelEncoderTests
{

    [Fact]
    public void EncodeSixel_RedSquare12x12_ReturnsExpectedSixel ()
    {

        var expected = "\u001bP" + // Start sixel sequence
                            "0;0;0" + // Defaults for aspect ratio and grid size
                            "q" + // Signals beginning of sixel image data
                            "\"1;1;12;2" + // no scaling factors (1x1) and filling 12px width with 2 'sixel' height = 12 px high

                            /*
                             * Definition of the color palette
                             */
                            "#0;2;100;0;0" + // Red color definition in the format "#<index>;<type>;<R>;<G>;<B>" - 2 means RGB.  The values range 0 to 100

                            /*
                             * Start of the Pixel data
                             *   We draw 6 rows at once, so end up with 2 'lines'
                             *   Both are basically the same and terminate with dollar hyphen (except last row)
                             *   Format is:
                             *       #0 (selects to use color palette index 0 i.e. red)
                             *       !12 (repeat next byte 12 times i.e. the whole length of the row)
                             *       ~ (the byte 111111 i.e. fill completely)
                             *       $ (return to start of line)
                             *       - (move down to next line)
                             */
                            "#0!12~$-" +
                            "#0!12~$" + // Next 6 rows of red pixels

                            "\u001b\\"; // End sixel sequence

        // Arrange: Create a 12x12 bitmap filled with red
        var pixels = new Color [12, 12];
        for (int x = 0; x < 12; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                pixels [x, y] = new Color(255,0,0);
            }
        }

        // Act: Encode the image
        var encoder = new SixelEncoder (); // Assuming SixelEncoder is the class that contains the EncodeSixel method
        string result = encoder.EncodeSixel (pixels);

        // Since image is only red we should only have 1 color definition
        Color c1 = Assert.Single (encoder.Quantizer.Palette);

        Assert.Equal (new Color(255,0,0),c1);

        Assert.Equal (expected, result);
    }
}
