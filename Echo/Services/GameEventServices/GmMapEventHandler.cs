using Echo.Services;
using Echo.Util;
using Echo.Models;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Media;

namespace Echo.Services.GameEventServices
{
    internal class GmMapEventHandler : IGameEventChecker, IGameEventHandler
    {
        public GameEventType EventType => GameEventType.GMWarp;
        readonly ScreenshotProvider _screenshotProvider;
        readonly DiscordBotService _discordBotService;
        readonly InputSender _inputSender;
        readonly PlayerController _playerController;
        readonly SoundPlayer _soundPlayer;
        readonly ILogger<GmMapEventHandler> _logger;

        public GmMapEventHandler(
            ScreenshotProvider screenshotProvider,
            DiscordBotService discordBotService,
            InputSender inputSender,
            PlayerController playerController,
            SoundPlayer soundPlayer,
            ILogger<GmMapEventHandler> logger)
        {
            _screenshotProvider = screenshotProvider;
            _discordBotService = discordBotService;
            _inputSender = inputSender;
            _playerController = playerController;
            _soundPlayer = soundPlayer;
            _logger = logger;
        }

        public async Task<GameEventType> EventDetected(PixelSnapshot pixelSnapshot)
        {
            return ImageFunctions.CheckForTotalColor(pixelSnapshot, Color.White, .4)
                ? GameEventType.GMWarp
                : GameEventType.None;
        }

        public async Task HandleEvent(CancellationToken ct)
        {
            _logger.LogInformation("handling GM Warp event detected.");
            _soundPlayer.Play();
            _ = _discordBotService.SendMessageToChannel($"GM WARP at {DateTime.Now}!");
            _inputSender.ReleaseAllPressed();
            await Task.Delay(1500, ct);
            var rng = new Random();
            var next = rng.Next(0, 3);
            var msg = "";
            switch (next)
            {
                case 0:
                    msg = "wtf";
                    break;
                case 1:
                    msg = "uhhh";
                    break;
                case 2:
                    msg = "hello";
                    break;
                case 3:
                    msg = "huh";
                    break;

                default: break;
            }
            //send intro message
            await _playerController.SendMessage(msg, ct);
            
            await Task.Delay(4000, ct);
        }
    }
}
