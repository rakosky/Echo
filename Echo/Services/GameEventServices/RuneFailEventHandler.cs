using Echo.Models;
using Echo.Services.ImageAnalysis;
using Microsoft.Extensions.Logging;

namespace Echo.Services.GameEventServices
{
    class RuneFailEventHandler : IGameEventHandler, IGameEventChecker
    {
        readonly ILogger<RuneFailEventHandler> _logger;
        readonly RuneAnalyzer _runeAnalyzer;
        readonly InputSender _inputSender;
        readonly PlayerController _playerController;

        public RuneFailEventHandler( RuneAnalyzer runeAnalyzer, InputSender inputSender, ILogger<RuneFailEventHandler> logger, PlayerController playerController)
        {
            _runeAnalyzer = runeAnalyzer;
            _inputSender = inputSender;
            _logger = logger;
            _playerController = playerController;
        }

        public GameEventType EventType => GameEventType.RuneFail;

        public async Task<GameEventType> EventDetected(PixelSnapshot pixelSnapshot)
        {
            return _runeAnalyzer.CurrentRuneAttempts >= 3 ? GameEventType.RuneFail : GameEventType.None;
        }

        public async Task HandleEvent(CancellationToken ct)
        {
            var nTries = 0;
            var firstTry = true;
            do
            {
                _inputSender.ReleaseAllPressed();
                //initial CC should try to attack incase there is a flying mob
                if (firstTry)
                {
                    if (await _playerController.CC(true, true, true, ct))
                        break;
                    firstTry = false;
                }
                if (nTries++ == 10)
                {
                    break;
                }
            }
            while (await _playerController.CC(true, false, false, ct) != true);//if initial CC fails, no need to catch breath or attack if fail
            await Task.Delay(6000, ct);
            //now go back to original ch.  No need to catch breath or attack
            nTries = 0;
            while (await _playerController.CC(false, false, false, ct) != true)
            {
                if (nTries++ == 10)
                {
                    break;
                }
            }

            await Task.Delay(3000, ct);

            _runeAnalyzer.CurrentRuneAttempts = 0;
            _logger.LogInformation($"Channel reset @ {DateTime.Now}.");
        }

    }
}
