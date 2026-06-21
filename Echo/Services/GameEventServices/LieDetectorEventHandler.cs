using Echo.Models;
using Echo.Services.ImageAnalysis;
using Echo.Util;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Media;

namespace Echo.Services.GameEventServices
{
    class LieDetectorEventHandler : IGameEventHandler, IGameEventChecker
    {
        readonly ILogger<LieDetectorEventHandler> _logger;
        readonly SoundPlayer _soundPlayer;
        readonly DiscordBotService _discordBotService;
        readonly InputSender _inputSender;
        readonly PlayerController _playerController;



        public LieDetectorEventHandler( SoundPlayer soundPlayer, InputSender inputSender, ILogger<LieDetectorEventHandler> logger, PlayerController playerController, DiscordBotService discordBotService)
        {
            _soundPlayer = soundPlayer;
            _inputSender = inputSender;
            _logger = logger;
            _playerController = playerController;
            _discordBotService = discordBotService;
        }

        public GameEventType EventType => GameEventType.LieDetected;

        public async Task<GameEventType> EventDetected(PixelSnapshot pixelSnapshot)
        {
            Rectangle ldBounds = new Rectangle(pixelSnapshot.Width / 2, pixelSnapshot.Height / 2, pixelSnapshot.Width / 2, pixelSnapshot.Height / 2);

            if(ImageFunctions.FindSubImageCoords(pixelSnapshot, StaticImages.LdImage, .2, ldBounds) == Rectangle.Empty)
            {
                return GameEventType.None;
            }

            return EventType;
        }

        public async Task HandleEvent(CancellationToken ct)
        {
            _soundPlayer.Play();
            await _discordBotService.SendMessageToChannel("Lie detected!");

            _logger.LogInformation($"Lie detected @ {DateTime.Now}.");
            await Task.Delay(8000, ct);
        }

    }
}
