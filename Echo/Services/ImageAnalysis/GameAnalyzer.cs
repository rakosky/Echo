using Echo.Util;
using System.Drawing;

namespace Echo.Services.ImageAnalysis
{
    public class GameAnalyzer
    {
        ScreenshotProvider _screenshotProvider;

        public GameAnalyzer(ScreenshotProvider screenshotProvider)
        {
            _screenshotProvider = screenshotProvider;
        }
        public async Task<bool> CheckForMapOrChannelChange()
        {
            var imageData = await _screenshotProvider.GetLatestPixels();

            return ImageFunctions.CheckForTotalColor(imageData, Color.Black, .8);
        }

        public async Task<bool> CheckForGMMap()
        {
            var imageData = await _screenshotProvider.GetLatestPixels();

            return ImageFunctions.CheckForTotalColor(imageData, Color.White, .4);
        }

        public async Task<bool> CheckForRuneCooldownIcon()
        {
            var imageData = await _screenshotProvider.GetLatestPixels();

            var runeCdSearchBounds = new Rectangle(
                x: imageData.Width - 400,
                y: 0,
                width: 400,
                height: 75);
            
            var topCdPoint = ImageFunctions.FindSubImageCoords(imageData, StaticImages.RuneCdImgTop, .3, runeCdSearchBounds)
                .LocationOrDefault();

            var botCdPoint = ImageFunctions.FindSubImageCoords(imageData, StaticImages.RuneCdImgBot, .3, runeCdSearchBounds)
                .LocationOrDefault();

            var isOnCd = topCdPoint is not null || botCdPoint is not null;
            return isOnCd;
        }
    }
}
