using Echo.Util;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Imaging;

namespace Echo.Services.ImageAnalysis
{
    public class MapAnalyzer
    {

        //private Rectangle MAP_BOUNDS = new Rectangle(0, 50, 200, 85);
        private Color PLAYER_COLOR = Color.FromArgb(255, 0xFF, 0xDD, 0x44);
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

            var tl = ImageFunctions.FindSubImageCoords(imageData, StaticImages.MapImageTopLeft, .4, mapSearchBounds)
                .LocationOrDefault()
                ?? throw new Exception("Unable to determine minimap top left");
            
            var br = ImageFunctions.FindSubImageCoords(imageData, StaticImages.MapImageBottomRight, .4, mapSearchBounds)
                .LocationOrDefault()
                ?? throw new Exception("Unable to determine minimap bottom right"); ;

            br.X += StaticImages.MapImageBottomRight.Width;
            br.Y += StaticImages.MapImageBottomRight.Height;

            _mapBounds = new Rectangle(
                x: tl.X,
                y: tl.Y,
                width: br.X - tl.X+10,
                height: br.Y - tl.Y+10);

            Console.WriteLine($"Found map bounds at {_mapBounds}");
        }

        public static List<Point?> Locate(
             PixelSnapshot snap,
             Color targetColor,
             Rectangle bounds,
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

            unsafe
            {
                fixed (byte* pBase = snap.Buffer)
                {
                    for (int y = area.Top; y < area.Bottom; y++)
                    {
                        // if the snapshot came from a bottom-up DIB (negative stride),
                        // flip the row index so we still scan image-top to image-bottom
                        int srcY = snap.Stride > 0 ? y : snap.Height - 1 - y;
                        byte* row = pBase + srcY * strideAbs;

                        for (int x = area.Left; x < area.Right; x++)
                        {
                            byte* px = row + x * BGR;
                            if (px[2] == targetColor.R &&
                                px[1] == targetColor.G &&
                                px[0] == targetColor.B)
                            {
                                sumX += x;
                                sumY += y;
                                nHits++;
                            }
                        }
                    }
                }
            }

            // if we saw "enough" matching pixels, return the centroid
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
