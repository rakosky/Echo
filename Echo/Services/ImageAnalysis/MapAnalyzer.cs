using Echo.Util;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Imaging;

namespace Echo.Services.ImageAnalysis
{
    public class MapAnalyzer
    {

        //private Rectangle MAP_BOUNDS = new Rectangle(0, 50, 200, 85);
        private Color PLAYER_COLOR = Color.FromArgb(255, 0xFE, 0xEF, 0x00);
        private Color RUNE_COLOR = Color.FromArgb(255, 0xDD, 0x66, 0xFF);
        private Color ENEMY_COLOR = Color.FromArgb(255, 255, 0, 0);
        private Color GUILD_COLOR = Color.FromArgb(255, 102, 102, 255);
        private Color BUDDY_COLOR = Color.FromArgb(255, 17, 221, 225);

        private Rectangle _mapBounds;

        readonly ScreenshotProvider _screenshotProvider;
        readonly ILogger<MapAnalyzer> _logger;

        public MapAnalyzer(ScreenshotProvider screenshotProvider, ILogger<MapAnalyzer> logger)
        {
            _screenshotProvider = screenshotProvider;
            _logger = logger;
        }

        public async Task UpdateMapBounds()
        {
            var mapSearchBounds = new Rectangle(0,0, 700, 500);

            var imageData = await _screenshotProvider.GetLatestPixels();

            var tl = ImageFunctions.FindSubImageCoords(imageData, StaticImages.MapImageTopLeft, tolerance: .4, searchArea: mapSearchBounds)
                .LocationOrDefault()
                ?? throw new Exception("Unable to determine minimap top left");

            var br = ImageFunctions.FindSubImageCoords(imageData, StaticImages.MapImageBottomRight, tolerance: .4, searchArea: mapSearchBounds)
                .LocationOrDefault()
                ?? throw new Exception("Unable to determine minimap bottom right"); ;

            br.X += StaticImages.MapImageBottomRight.Width;
            br.Y += StaticImages.MapImageBottomRight.Height;

            _mapBounds = new Rectangle(
                x: tl.X,
                y: tl.Y,
                width: br.X - tl.X + 10,
                height: br.Y - tl.Y + 10);

            //_mapBounds = new Rectangle(
            //    x:20,
            //    y:60,
            //    width: 300,
            //    height: 250);

            Console.WriteLine($"Found map bounds at {_mapBounds}");
        }

        /// <summary>
        /// Scans snap.Buffer (24-bpp BGR) within bounds for pixels close to targetColor.
        /// Returns the centroid of all “hits” if at least minHitsForCentroid are found.
        /// </summary>
        /// <param name="snap">Raw 24bpp BGR snapshot.</param>
        /// <param name="targetColor">Color to look for.</param>
        /// <param name="bounds">Search area.</param>
        /// <param name="pixelTolerance">
        /// Maximum allowed per-pixel difference, normalized 0–1:
        /// (|R₁–R₂|+|G₁–G₂|+|B₁–B₂|)/(3×255) ≤ pixelTolerance counts as a match.
        /// </param>
        /// <param name="minHitsForCentroid">
        /// Minimum number of matching pixels before returning a centroid.
        /// </param>
        public static List<Point?> Locate(
            PixelSnapshot snap,
            Color targetColor,
            Rectangle bounds,
            double pixelTolerance = 0.0,
            int minHitsForCentroid = 8)
        {
            var points = new List<Point?>();

            // restrict search to the valid portion of the snapshot
            var area = Rectangle.Intersect(
                bounds,
                new Rectangle(0, 0, snap.Width, snap.Height));

            if (area.IsEmpty)
                return points;

            int sumX = 0, sumY = 0, nHits = 0;

            // absolute stride is the distance (in bytes) between consecutive rows
            int strideAbs = Math.Abs(snap.Stride);
            const int BGR = 3;           // 24-bpp: 3 bytes per pixel, B-G-R order
            const double norm = 3.0 * 255.0;

            unsafe
            {
                fixed (byte* pBase = snap.Buffer)
                {
                    for (int y = area.Top; y < area.Bottom; y++)
                    {
                        // handle bottom-up DIBs
                        int srcY = snap.Stride > 0 ? y : snap.Height - 1 - y;
                        byte* row = pBase + srcY * strideAbs;

                        for (int x = area.Left; x < area.Right; x++)
                        {
                            byte* px = row + x * BGR;
                            // compute normalized difference
                            double diff =
                                (Math.Abs(px[2] - targetColor.R)
                               + Math.Abs(px[1] - targetColor.G)
                               + Math.Abs(px[0] - targetColor.B))
                                / norm;

                            if (diff <= pixelTolerance)
                            {
                                sumX += x;
                                sumY += y;
                                nHits++;
                            }
                        }
                    }
                }
            }
            // if enough hits, return the centroid
            if (nHits >= minHitsForCentroid)
                points.Add(new Point(sumX / nHits, sumY / nHits));

            return points;
        }



        public Point? GetPlayerLocation(PixelSnapshot pixelSnapshot)
        {
            var location = Locate(pixelSnapshot, PLAYER_COLOR, _mapBounds);
            return location.FirstOrDefault();
        }


        public Point? GetRuneLocation(PixelSnapshot pixelSnapshot)
        {
            var location = Locate(pixelSnapshot, RUNE_COLOR, _mapBounds);
            return location.FirstOrDefault();
        }


        //public Point GetOtherLocation()
        //{
        //    location = self.locate(ENEMY_COLOR, GUILD_COLOR, BUDDY_COLOR)
        //    return len(location) > 0
        //    }
    }
}
