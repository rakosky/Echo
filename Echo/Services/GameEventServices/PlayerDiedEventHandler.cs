using Echo.Models;
using Echo.Services.ImageAnalysis;
using Echo.Util;
using System.Drawing;

namespace Echo.Services.GameEventServices
{
    internal class PlayerDiedEventHandler : IGameEventHandler, IGameEventChecker
    {
        public GameEventType EventType => GameEventType.PlayerDied;

        readonly GameAnalyzer _gameAnalyzer;
        readonly ScreenshotProvider _screenshotProvider;
        readonly InputSender _inputSender;

        private Point? _lastRespawnBoxCoords = null;

        public PlayerDiedEventHandler(GameAnalyzer gameAnalyzer, ScreenshotProvider screenshotProvider, InputSender inputSender)
        {
            _gameAnalyzer = gameAnalyzer;
            _screenshotProvider = screenshotProvider;
            _inputSender = inputSender;
        }

        public async Task<GameEventType> EventDetected(PixelSnapshot pixelSnapshot)
        {
            var respawnBoxCoords = ImageFunctions.FindSubImageCoords(
                pixelSnapshot,
                StaticImages.RespawnImg)
                .LocationOrDefault();

            if (respawnBoxCoords is not null)
            {
                _lastRespawnBoxCoords = respawnBoxCoords.Value;
                return GameEventType.PlayerDied;
            }
            return GameEventType.None;
        }

        public async Task HandleEvent(CancellationToken ct)
        {
            await Task.Delay(1000, ct);
            _inputSender.ClickOnPoint(_lastRespawnBoxCoords!.Value);
            await Task.Delay(1000, ct);
        }
    }
}
