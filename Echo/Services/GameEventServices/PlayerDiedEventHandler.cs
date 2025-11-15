using Echo.Extern;
using Echo.Models;
using Echo.Models.Settings;
using Echo.Services.ImageAnalysis;
using Echo.Util;
using System.Drawing;
using System.IO.Pipes;

namespace Echo.Services.GameEventServices
{
    internal class PlayerDiedEventHandler : IGameEventHandler, IGameEventChecker
    {
        public GameEventType EventType => GameEventType.PlayerDied;

        readonly GameAnalyzer _gameAnalyzer;
        readonly ScreenshotProvider _screenshotProvider;
        readonly InputSender _inputSender;
        readonly AppSettings _settings;

        private Point? _lastRespawnBoxCoords = null;

        public PlayerDiedEventHandler(GameAnalyzer gameAnalyzer, ScreenshotProvider screenshotProvider, InputSender inputSender, AppSettings settings)
        {
            _gameAnalyzer = gameAnalyzer;
            _screenshotProvider = screenshotProvider;
            _inputSender = inputSender;
            _settings = settings;
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
            _inputSender.SendKey(_settings.Hotkeys.NpcChatKey, KeyPressType.DOWN);
            await Task.Delay(4000, ct);
            _inputSender.SendKey(_settings.Hotkeys.NpcChatKey, KeyPressType.UP);
            await Task.Delay(1000, ct);
        }
    }
}