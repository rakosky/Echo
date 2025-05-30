using Echo.Models;
using Microsoft.Extensions.Logging;

namespace Echo.Services
{
    public class OrcamPlayer
    {
        CancellationTokenSource _playbackCTS;
        CancellationToken _playbackCT;

        readonly ILogger<OrcamPlayer> _logger;
        readonly InputSender _inputSender;
        readonly GameFocusManager _gameFocusManager;

        public bool IsPlaying { get; private set; } = false;

        public bool StoppedByFocusLoss { get; set; }
        public Orcam Macro { get; private set; }
        private IEnumerator<OrcamCommand> _macroEnumerator;

        public OrcamPlayer(
            ILogger<OrcamPlayer> logger,
            InputSender inputSender,
            GameFocusManager gameFocusManager)
        {
            _logger = logger;
            _inputSender = inputSender;
            _gameFocusManager = gameFocusManager;

            _gameFocusManager.OnFocusChanged += HandleFocusChange;
        }

        public void SetOrcam(Orcam macro)
        {
            Macro = macro ?? throw new ArgumentNullException(nameof(macro));
            _macroEnumerator = Macro.Commands.GetEnumerator();
        }

        public void Play()
        {
            if (Macro is null)
            {
                _logger.LogError("Macro is null");
                return;
            }

            if (IsPlaying)
            {
                _logger.LogWarning("Macro is already playing");
                return;
            }


            _ = Task.Run(PlayInternal);
        }

        public void HandleFocusChange(bool isFocused)
        {
            if (!IsPlaying && isFocused && StoppedByFocusLoss)
            {
                _logger.LogInformation("Game window gained focus, resuming macro playback");
                _ = Task.Run(PlayInternal);
                return;
            }

            if (IsPlaying && !isFocused)
            {
                _logger.LogInformation("Game window lost focus, stopping macro playback");
                Stop(stoppedByFocusLoss:true);
            }
        }

        public void Restart()
        {
            _logger.LogInformation("Restarting macro playback");

            if (IsPlaying)
            {
                Stop();
            }
            _macroEnumerator.Reset();
        }

        public void Stop(bool stoppedByFocusLoss = false)
        {
            StoppedByFocusLoss = stoppedByFocusLoss;
            if (!IsPlaying)
            {
                return;
            }

            _logger.LogInformation("Stopping macro playback");
            _inputSender.ReleaseAllPressed();
            IsPlaying = false;
            _playbackCTS.Cancel();
        }

        private async Task PlayInternal()
        {
            _logger.LogInformation($"Playing {Macro.Name}");
            StoppedByFocusLoss = false;
            _playbackCTS?.Dispose();
            _playbackCTS = new CancellationTokenSource();
            _playbackCT = _playbackCTS.Token;
            IsPlaying = true;
            
            while (!_playbackCT.IsCancellationRequested)
            {
                if (!_macroEnumerator.MoveNext())
                {
                    _macroEnumerator.Reset();
                    _macroEnumerator.MoveNext();
                }
                var command = _macroEnumerator.Current;
                await Task.Delay(command.Delay, _playbackCT);
                _inputSender.SendKey(command.Key, command.Type);
            }
        }
    }
}
