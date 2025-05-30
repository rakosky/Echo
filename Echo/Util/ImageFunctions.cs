using Echo.Services;
using System.Drawing;
using System.Drawing.Imaging;

namespace Echo.Util
{
    public static class ImageFunctions
    {
        public static bool IsEqualTo(this Color color1, Color color2, double colorClosenessThreshold = 1)
        {
            return color1.R == color2.R
                && color1.G == color2.G
                && color1.B == color2.B;
        }

        public static bool IsColorClose(Color color1, Color color2, double percentageThreshold)
        {
            // Calculate color differences for each RGB component
            int deltaR = Math.Abs(color1.R - color2.R);
            int deltaG = Math.Abs(color1.G - color2.G);
            int deltaB = Math.Abs(color1.B - color2.B);

            // Calculate the total color difference
            double totalDifference = Math.Sqrt(deltaR * deltaR + deltaG * deltaG + deltaB * deltaB);

            // Calculate the maximum possible color difference (diagonal across RGB cube)
            double maxDifference = Math.Sqrt(255 * 255 + 255 * 255 + 255 * 255);

            // Calculate the percentage difference
            double percentageDifference = (totalDifference / maxDifference) * 100.0;

            // Compare the percentage difference to the threshold
            return percentageDifference <= percentageThreshold;
        }

        public static bool CheckForTotalColor(
        PixelSnapshot snap, Color target, double percentageRequired,
        double tolerance = 0.10)                 // default = 10 % of 255 per channel
        {
            int hits = 0, total = 0;
            int w = snap.Width, h = snap.Height;
            int stride = Math.Abs(snap.Stride);      // use absolute stride
            int xStep = Math.Max(1, w / 20);
            int yStep = Math.Max(1, h / 20);
            int margin = (int)(255 * tolerance);

            unsafe
            {
                fixed (byte* basePtr = snap.Buffer)
                {
                    for (int y = 0; y < h; y += yStep)
                    {
                        int srcY = snap.Stride > 0 ? y : h - 1 - y; // flip if bottom-up
                        byte* row = basePtr + srcY * stride;

                        for (int x = 0; x < w; x += xStep)
                        {
                            byte* px = row + x * 3;
                            if (Math.Abs(px[2] - target.R) <= margin &&
                                Math.Abs(px[1] - target.G) <= margin &&
                                Math.Abs(px[0] - target.B) <= margin)
                                hits++;

                            total++;
                        }
                    }
                }
            }

            return hits >= total * percentageRequired;
        }


        public static Rectangle FindSubImageCoords(
            PixelSnapshot bigSnap,      // ← new: managed copy of the full screen
            Bitmap smallBmp,
            double tolerance = .10,
            Rectangle? searchArea = null)
        {
            BitmapData smallData = null;
            try
            {

                // 1) default search area = whole big image
                var area = searchArea ?? new Rectangle(0, 0, bigSnap.Width, bigSnap.Height);

                // 2) clamp to valid region
                area.Intersect(new Rectangle(0, 0, bigSnap.Width, bigSnap.Height));
                if (area.IsEmpty ||
                    area.Width < smallBmp.Width ||
                    area.Height < smallBmp.Height)
                    return Rectangle.Empty;

                /* ------------------------------------------------------------------ */
                /*  Lock the small (template) bitmap                                  */
                /* ------------------------------------------------------------------ */
                smallData = smallBmp.LockBits(
                    new Rectangle(0, 0, smallBmp.Width, smallBmp.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);

                int smallStride = smallData.Stride;
                int bigStride = bigSnap.Stride;
                int smallWidth3 = smallBmp.Width * 3;
                int smallHeight = smallBmp.Height;
                int margin = (int)(255.0 * tolerance);

                Rectangle result = Rectangle.Empty;

                unsafe
                {
                    byte* pSmallBase = (byte*)smallData.Scan0;

                    // pin the big snapshot buffer so we can use a raw pointer
                    fixed (byte* pBigBase = bigSnap.Buffer)
                    {
                        for (int y = area.Top; y <= area.Bottom - smallHeight; y++)
                        {
                            byte* rowStartBig = pBigBase + y * bigStride;

                            for (int x = area.Left; x <= area.Right - smallBmp.Width; x++)
                            {
                                byte* pBig = rowStartBig + x * 3;
                                byte* pSmall = pSmallBase;
                                bool match = true;

                                for (int row = 0; row < smallHeight; row++)
                                {
                                    byte* scanBig = pBig + row * bigStride;
                                    byte* scanSmall = pSmall + row * smallStride;

                                    for (int col = 0; col < smallWidth3; col++)
                                    {
                                        if (Math.Abs(scanBig[col] - scanSmall[col]) > margin)
                                        {
                                            match = false;
                                            break;
                                        }
                                    }
                                    if (!match) break;
                                }

                                if (match)
                                {
                                    return result = new Rectangle(x, y, smallBmp.Width, smallBmp.Height);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (smallData is not null)
                    smallBmp.UnlockBits(smallData);
            }
            return Rectangle.Empty;
        }


        public static int CalculateLevenshteinDistance(string s1, string s2)
        {
            int[,] distance = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
            {
                for (int j = 0; j <= s2.Length; j++)
                {
                    if (i == 0)
                    {
                        distance[i, j] = j;
                    }
                    else if (j == 0)
                    {
                        distance[i, j] = i;
                    }
                    else
                    {
                        int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                        distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
                    }
                }
            }

            return distance[s1.Length, s2.Length];
        }

        public static Point? LocationOrDefault(this Rectangle rect)
        {
            if (rect.IsEmpty)
                return null;
            return new Point(rect.X, rect.Y);
        }
    }
}
