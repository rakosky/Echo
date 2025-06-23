using Echo.Models;
using Echo.Util;
using System.Drawing;

namespace Echo.Services.GameEventServices
{
    public class BuffRefresher : IGameEventChecker, IGameEventHandler
    {
        readonly ScreenshotProvider _screenshotProvider;
        readonly InputSender _inputSender;

        public GameEventType EventType => GameEventType.BuffRefresh;

        public BuffRefresher(ScreenshotProvider screenshotProvider, InputSender inputSender)
        {
            _screenshotProvider = screenshotProvider;
            _inputSender = inputSender;
        }

        public async Task<GameEventType> EventDetected(PixelSnapshot pixelSnapshot)
        {
            // Check if the buff refresh image is present in the screenshot
            var gameImage = await _screenshotProvider.GetLatestPixels();
            return ImageFunctions.FindSubImageCoords(gameImage, StaticImages.LotdIcon, .30) == Rectangle.Empty && ImageFunctions.FindSubImageCoords(gameImage, StaticImages.LotdCDIcon, .1) != Rectangle.Empty
                ? GameEventType.BuffRefresh
                : GameEventType.None;
        }

        public async Task HandleEvent(CancellationToken ct)
        {
            await Task.Delay(1000, ct);
            _inputSender.SendKey(Extern.User32.ScanCodeShort.KEY_L, KeyPressType.PRESS);
            await Task.Delay(400, ct);
        }
    }
}
