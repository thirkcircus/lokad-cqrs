#region 
// Homepage: http://dylanvester.com/post/Creating-Heat-Maps-with-NET-20-(C-Sharp).aspx
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Snippets.HttpEndpoint
{
    public class Heatmap
    {
        public static Bitmap CreateIntensityMask(Bitmap bSurface, IEnumerable<HeatPoint> aHeatPoints)
        {
            // Create new graphics surface from memory bitmap
            var drawSurface = Graphics.FromImage(bSurface);

            // Set background color to white so that pixels can be correctly colorized
            drawSurface.Clear(Color.White);

            // Traverse heat point data and draw masks for each heat point
            foreach (var dataPoint in aHeatPoints)
            {
                // Render current heat point on draw surface
                DrawHeatPoint(drawSurface, dataPoint, 15);
            }

            return bSurface;
        }

        private static void DrawHeatPoint(Graphics canvas, HeatPoint heatPoint, int radius)
        {
            // Create points generic list of points to hold circumference points
            var circumferencePointsList = new List<Point>();

            const float fRatio = 1F / Byte.MaxValue;
            // Precalulate half of byte max value
            const byte bHalf = Byte.MaxValue / 2;
            // Flip intensity on it's center value from low-high to high-low
            int iIntensity = (byte)(heatPoint.Intensity - ((heatPoint.Intensity - bHalf) * 2));
            // Store scaled and flipped intensity value for use with gradient center location
            var fIntensity = iIntensity * fRatio;

            // Loop through all angles of a circle
            // Define loop variable as a double to prevent casting in each iteration
            // Iterate through loop on 10 degree deltas, this can change to improve performance
            for (double i = 0; i <= 360; i += 10)
            {
                // Replace last iteration point with new empty point struct
                var circumferencePoint = new Point
                {
                    X =
                        Convert.ToInt32(heatPoint.X +
                                        radius * Math.Cos(ConvertDegreesToRadians(i))),
                    Y =
                        Convert.ToInt32(heatPoint.Y +
                                        radius * Math.Sin(ConvertDegreesToRadians(i)))
                };

                // Add newly plotted circumference point to generic point list
                circumferencePointsList.Add(circumferencePoint);
            }

            // Populate empty points system array from generic points array list
            // Do this to satisfy the datatype of the PathGradientBrush and FillPolygon methods
            var circumferencePointsArray = circumferencePointsList.ToArray();

            // Create new PathGradientBrush to create a radial gradient using the circumference points
            var gradientShaper = new PathGradientBrush(circumferencePointsArray);

            // Create new color blend to tell the PathGradientBrush what colors to use and where to put them
            var gradientSpecifications = new ColorBlend(3)
            {
                Positions = new[] { 0, fIntensity, 1 },
                Colors = new[]
                                                              {
                                                                  Color.FromArgb(0, Color.White),
                                                                  Color.FromArgb(heatPoint.Intensity, Color.Black),
                                                                  Color.FromArgb(heatPoint.Intensity, Color.Black)
                                                              }
            };

            // Pass off color blend to PathGradientBrush to instruct it how to generate the gradient
            gradientShaper.InterpolationColors = gradientSpecifications;

            // Draw polygon (circle) using our point array and gradient brush
            canvas.FillPolygon(gradientShaper, circumferencePointsArray);
        }

        private static double ConvertDegreesToRadians(double degrees)
        {
            var radians = (Math.PI / 180) * degrees;
            return (radians);
        }

        public static Bitmap Colorize(Bitmap mask, byte alpha)
        {
            // Create new bitmap to act as a work surface for the colorization process
            var output = new Bitmap(mask.Width, mask.Height, PixelFormat.Format32bppArgb);

            // Create a graphics object from our memory bitmap so we can draw on it and clear it's drawing surface
            var surface = Graphics.FromImage(output);
            surface.Clear(Color.Transparent);

            // Build an array of color mappings to remap our greyscale mask to full color
            // Accept an alpha byte to specify the transparancy of the output image
            var colors = CreatePaletteIndex(alpha);

            // Create new image attributes class to handle the color remappings
            // Inject our color map array to instruct the image attributes class how to do the colorization
            var remapper = new ImageAttributes();
            remapper.SetRemapTable(colors);

            // Draw our mask onto our memory bitmap work surface using the new color mapping scheme
            surface.DrawImage(mask, new Rectangle(0, 0, mask.Width, mask.Height), 0, 0, mask.Width, mask.Height,
                              GraphicsUnit.Pixel, remapper);

            // Send back newly colorized memory bitmap
            return output;
        }

        private static ColorMap[] CreatePaletteIndex(byte alpha)
        {
            var outputMap = new ColorMap[256];

            // Change this path to wherever you saved the palette image.
            var palette = DrawGradient();

            // Loop through each pixel and create a new color mapping
            for (var x = 0; x <= 255; x++)
            {
                outputMap[x] = new ColorMap
                {
                    OldColor = Color.FromArgb(x, x, x),
                    NewColor = Color.FromArgb(alpha, palette.GetPixel(x, 0))
                };
            }

            return outputMap;
        }

        public static Bitmap DrawGradient()
        {
            const int width = 256;
            var spectre = new Bitmap(width, 10);
            var canvas = Graphics.FromImage(spectre);

            var brush = new LinearGradientBrush(new Point(0, 0), new Point(width, 10), Color.Red, Color.Yellow);
            var colorBlend = new ColorBlend(4)
            {
                Colors = new[] { Color.White, Color.White, Color.Red, Color.Yellow, Color.Green, Color.DeepSkyBlue, Color.DarkBlue },
                Positions = new[] { 0, .1f, .2f, .3f, .6f, .8f, 1f }
            };
            brush.InterpolationColors = colorBlend;

            for (var i = 0; i < 10; i++)
            {
                canvas.DrawLine(new Pen(brush), 0, i, width, i);
            }

            return spectre;
        }
    }
}