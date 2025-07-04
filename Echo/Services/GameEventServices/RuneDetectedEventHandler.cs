using Echo.Models;
using Echo.Services.ImageAnalysis;
using Microsoft.Extensions.Logging;
using System.Drawing;
using static Echo.Extern.User32;

namespace Echo.Services.GameEventServices
{
    internal class RuneDetectedEventHandler : IGameEventHandler, IGameEventChecker
    {
        RuneAnalyzer _runeAnalyzer;
        MapAnalyzer _mapAnalyzer;
        PlayerController _playerController;
        InputSender _inputSender;
        HotkeySettings _hotkeys;
        ILogger<RuneDetectedEventHandler> _logger;

        Point? _activeRuneLoc = null;
        DateTime _lastRuneTime = DateTime.MinValue;
        int _consecutiveRuneFails = 0;

        public RuneDetectedEventHandler(
            RuneAnalyzer runeAnalyzer,
            MapAnalyzer mapAnalyzer,
            Settings settings,
            PlayerController playerController,
            InputSender inputSender,
            ILogger<RuneDetectedEventHandler> logger)
        {
            _runeAnalyzer = runeAnalyzer;
            _mapAnalyzer = mapAnalyzer;
            _playerController = playerController;
            _hotkeys = settings.Hotkeys;
            _inputSender = inputSender;
            _logger = logger;

        }

        public GameEventType EventType => GameEventType.Rune;

        public async Task<GameEventType> EventDetected(PixelSnapshot pixelSnapshot)
        {
            if (_mapAnalyzer.GetRuneLocation(pixelSnapshot) is Point point)
            {
                _activeRuneLoc = point;
                return GameEventType.Rune;
            }

            return GameEventType.None;
        }

        public async Task HandleEvent(CancellationToken ct)
        {
            await Task.Delay(1000, ct);
            _inputSender.ReleaseAllPressed();
            await _playerController.GoTo(_activeRuneLoc!.Value, ct);
            _logger.LogInformation("Arrived at rune");
            await Task.Delay(200, ct);
            _inputSender.SendKey(_hotkeys.NpcChatKey);
            await Task.Delay(2000, ct);

            var dirs = await _runeAnalyzer.FindArrowDirections(ct);
            
            string logMessage = $"Determined {dirs.Count} directions. {string.Join(',', dirs)}";
            if (dirs.Count == 4)
            {
                _logger.LogInformation(logMessage);
                foreach (var dir in dirs)
                {
                    ScanCodeShort keyCode = ScanCodeShort.DOWN;
                    switch (dir.Item1)
                    {
                        case "RIGHT": keyCode = ScanCodeShort.RIGHT; break;
                        case "LEFT": keyCode = ScanCodeShort.LEFT; break;
                        case "UP": keyCode = ScanCodeShort.UP; break;
                        case "DOWN": keyCode = ScanCodeShort.DOWN; break;
                    }

                    _inputSender.SendKey(keyCode);
                    await Task.Delay(70, ct);
                }
                _lastRuneTime = DateTime.Now;
                _runeAnalyzer.CurrentRuneAttempts = 0;
            }
            else
            {
                _logger.LogError(logMessage);
                _runeAnalyzer.CurrentRuneAttempts++;
                // Spicy lil attack to clear potential floating mob if we are failing runes?
                _inputSender.SendKey(_hotkeys.AttackKey);
            }

            await Task.Delay(1000, ct);
        }
    }
}
