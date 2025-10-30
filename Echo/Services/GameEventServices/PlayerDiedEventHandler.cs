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
            // NOTE: applying an offset here because the way we take screenshots now means the extra space above messes up the absolute coordinates of the game
            int yOffset = -40;
            var pointToClick = new Point(_lastRespawnBoxCoords.Value.X, _lastRespawnBoxCoords.Value.Y + yOffset);
            await Task.Delay(1000, ct);
            _inputSender.ClickOnPoint(pointToClick);
            await Task.Delay(1000, ct);
        }
    }
}