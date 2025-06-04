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
        public Orcam Ocram { get; private set; }
        private IEnumerator<OrcamCommand> _enumerator;

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

        public void SetOrcam(Orcam ocram)
        {
            Ocram = ocram ?? throw new ArgumentNullException(nameof(ocram));
            _enumerator = Ocram.Commands.GetEnumerator();
        }

        public void Play()
        {
            if (Ocram is null)
            {
                _logger.LogError("Ocram is null");
                return;
            }

            if (IsPlaying)
            {
                _logger.LogWarning("Ocram is already playing");
                return;
            }


            _ = Task.Run(PlayInternal);
        }

        public void HandleFocusChange(bool isFocused)
        {
            if (!IsPlaying && isFocused && StoppedByFocusLoss)
            {
                _logger.LogInformation("Game window gained focus, resuming playback");
                _ = Task.Run(PlayInternal);
                return;
            }

            if (IsPlaying && !isFocused)
            {
                _logger.LogInformation("Game window lost focus, stopping playback");
                Stop(stoppedByFocusLoss:true);
            }
        }

        public void Restart()
        {
            _logger.LogInformation("Restarting playback");

            if (IsPlaying)
            {
                Stop();
            }
            _enumerator.Reset();
        }

        public void Stop(bool stoppedByFocusLoss = false)
        {
            StoppedByFocusLoss = stoppedByFocusLoss;
            if (!IsPlaying)
            {
                return;
            }

            _logger.LogInformation("Stopping playback");
            _inputSender.ReleaseAllPressed();
            IsPlaying = false;
            _playbackCTS.Cancel();
        }

        private async Task PlayInternal()
        {
            _logger.LogInformation($"Playing {Ocram.Name}");
            StoppedByFocusLoss = false;
            _playbackCTS?.Dispose();
            _playbackCTS = new CancellationTokenSource();
            _playbackCT = _playbackCTS.Token;
            IsPlaying = true;
            
            while (!_playbackCT.IsCancellationRequested)
            {
                if (!_enumerator.MoveNext())
                {
                    _enumerator.Reset();
                    _enumerator.MoveNext();
                }
                var command = _enumerator.Current;
                await Task.Delay(command.Delay, _playbackCT);
                _inputSender.SendKey(command.Key, command.Type);
            }
        }
    }
}
