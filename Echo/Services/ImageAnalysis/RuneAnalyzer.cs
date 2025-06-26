using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Echo.Services.ImageAnalysis
{
    public class RuneAnalyzer
    {
        public int CurrentRuneAttempts { get; set; } = 0;
        private readonly ScreenshotProvider _screenshotProvider;
        public RuneAnalyzer(ScreenshotProvider screenshotProvider)
        {
            _screenshotProvider = screenshotProvider;
        }
        public async Task<List<(string, Point)>> FindArrowDirections(CancellationToken ct)
        {
            var fullImg = await _screenshotProvider.GetLatestScreenshot();
            var directions = new List<(string, Point)>();
            List<Point> valid_gradient = new();

            using (var croppedImg = fullImg.Clone(new Rectangle(630, 280, 570, 200), System.Drawing.Imaging.PixelFormat.Format32bppRgb))
            {
                croppedImg. Save($@"{AppDomain.CurrentDomain.BaseDirectory}screenshots\{DateTime.Now.Ticks}.png");

                HSV[,] _hsvMap = new HSV[croppedImg.Width, croppedImg.Height];

                for (int x = 0; x < croppedImg.Width; x++)
                    for (int y = 0; y < croppedImg.Height; y++)
                        _hsvMap[x, y] = ColorToHSV(croppedImg.GetPixel(x, y));

                for (int x = 0; x < croppedImg.Width; x++)
                {
                    for (int y = 0; y < croppedImg.Height; y++)
                    {
                        var direction = string.Empty;
                        //Arrows start at a red-ish color and are around 15 pixels apart.
                        if (hue_is_red(x, y, _hsvMap) && !near_gradient(x, y, valid_gradient))
                            direction = find_direction(x, y, valid_gradient, _hsvMap, croppedImg);
                        if (!string.IsNullOrEmpty(direction))
                            directions.Add((direction, new Point(x, y)));
                    }
                }
            }
            
            return directions;
        }
        private bool hue_is_red(int x, int y, HSV[,] _hsvMap)
        {
            var hsv = _hsvMap[x, y];
            //Returns a boolean value based on whether a certain pixel is a "red" color.



            //if (5 <= hsv.H && hsv.H <= 12 && hsv.S >= 65 && hsv.V >= 128)
            if (((hsv.H >= 0 && hsv.H <= 30) || (hsv.H >= 300 && hsv.H <= 360)) && hsv.S >= 0.8 && hsv.V >= 0.8)
                return true;
            return false;
        }
        private bool hue_is_valid(int x1, int y1, int x2, int y2, int diff, HSV[,] _hsvMap)
        {
            //Returns a boolean value based on whether two pixels are within a certain HSV range of each other.
            //return hsv1.H - hsv2.H <= diff && hsv2.S >= 150 && hsv2.V >= 150 && hsv2.H <= 70
            if ((_hsvMap[x1, y1].H - _hsvMap[x2, y2].H) <= diff && _hsvMap[x2, y2].S >= .6 && _hsvMap[x2, y2].V >= .6 && _hsvMap[x2, y2].H <= 140)
                return true;
            return false;
        }
        private bool near_gradient(int x, int y, List<Point> valid_gradient)
        {
            //Returns a boolean value based on whether or not a certain pixel is around an already discovered gradient.
            foreach (var point in valid_gradient)
                if (Math.Abs(point.X - x) < 25 && Math.Abs(y - point.Y) < 25)
                    return true;
            return false;
        }
        private bool gradient_exists(int x, int y, int deltaX, int deltaY, List<Point> valid_gradient, HSV[,] _hsvMap, Bitmap _image)
        {
            //Given a starting pixel and unit value representing a direction, check if a gradient exists.
            if (near_gradient(x, y, valid_gradient))
                return false;

            var tmpx = x;
            var tmpy = y;
            var rune_gradient = false;
            //The directional arrows that appear in runes are around 30 pixels long.
            for (int i = 0; i < 30; i++)
            {
                var x2 = tmpx + deltaX;
                var y2 = tmpy + deltaY;
                if (0 <= x2 && x2 < _image.Width && 0 <= y2 && y2 < _image.Height)
                {
                    //Check if the next pixel maintains the gradient.
                    if (hue_is_valid(tmpx, tmpy, x2, y2, 20, _hsvMap))
                    {
                        //If the pixel is a green-ish color, it is a possible arrow.
                        if (60 <= _hsvMap[x2, y2].H && _hsvMap[x2, y2].H <= 140)
                        {
                            rune_gradient = true;
                            valid_gradient.Add(new Point(x, y));
                            break;
                        }
                        tmpx = x2;
                        tmpy = y2;
                    }
                    else
                        break;
                }
                else
                    break;
            }
            return rune_gradient;
        }

        private string find_direction(int x, int y, List<Point> valid_gradient, HSV[,] _hsvMap, Bitmap _image)
        {
            //Given a starting pixel, returns the direction of any gradients found to exist.
            if (gradient_exists(x, y, -1, 0, valid_gradient, _hsvMap, _image))
                return "RIGHT";
            else if (gradient_exists(x, y, 1, 0, valid_gradient, _hsvMap, _image))
                return "LEFT";
            else if (gradient_exists(x, y, 0, -1, valid_gradient, _hsvMap, _image))
                return "DOWN";
            else if (gradient_exists(x, y, 0, 1, valid_gradient, _hsvMap, _image))
                return "UP";
            else
                return "";
        }




        private HSV ColorToHSV(System.Drawing.Color color)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            var hue = color.GetHue();
            var saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            var value = max / 255d;

            return new HSV
            {
                H = hue,
                S = saturation,
                V = value
            };
        }
    }
    public class HSV
    {
        public double H { get; set; }
        public double S { get; set; }
        public double V { get; set; }
    }
}
