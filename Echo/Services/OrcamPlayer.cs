using Echo.Models;
using Microsoft.Extensions.Logging;
using static Echo.Extern.User32;

namespace Echo.Services
{
    public class OrcamPlayer
    {
        CancellationTokenSource _playbackCTS;
        CancellationToken _playbackCT;

        readonly ILogger<OrcamPlayer> _logger;
        readonly InputSender _inputSender;
        readonly GameFocusManager _gameFocusManager;
        readonly Settings _settings;
        const int MaxDelayVarianceMs = 10;

        const double InjectionChance = .01; // one in every 100 commands will inject a key

        HashSet<byte> _releasedKeys = new HashSet<byte>();

        public bool IsPlaying { get; private set; } = false;

        public bool StoppedByFocusLoss { get; set; }
        public Orcam Ocram { get; private set; }
        private IEnumerator<OrcamCommand> _enumerator;

        public OrcamPlayer(
            ILogger<OrcamPlayer> logger,
            InputSender inputSender,
            Settings settings,
            GameFocusManager gameFocusManager)
        {
            _logger = logger;
            _inputSender = inputSender;
            _settings = settings;
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
            _releasedKeys = _inputSender.ReleaseAllPressed();
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

            // repress all release arrow keys if applicible
            if (_releasedKeys is not null && _releasedKeys.Count > 0)
                _inputSender.RepressAll(_releasedKeys);

            while (!_playbackCT.IsCancellationRequested)
            {
                if (!_enumerator.MoveNext())
                {
                    _enumerator.Reset();
                    _enumerator.MoveNext();
                }

                var random = Random.Shared.NextDouble();
                var command = _enumerator.Current;
                
                if (command.Delay > 30)
                {
                    var dir = Random.Shared.NextDouble() > .5 ? 1 : -1;
                    await Task.Delay(command.Delay + (int)(random * MaxDelayVarianceMs * dir), _playbackCT);
                }
                else
                {
                    await Task.Delay(command.Delay, _playbackCT);
                }
                
                _inputSender.SendKey(command.Key, command.Type);

                if (random <= InjectionChance)
                    await HandleRandomKeyInjection();
            }
        }

        private async Task HandleRandomKeyInjection()
        {
            var key = _settings.InjectableKeys[Random.Shared.Next(_settings.InjectableKeys.Length - 1)];

            await Task.Delay(MaxDelayVarianceMs + (int)(Random.Shared.NextDouble() * MaxDelayVarianceMs), _playbackCT);
            _inputSender.SendKey(key, KeyPressType.DOWN);
            await Task.Delay(MaxDelayVarianceMs + (int)(Random.Shared.NextDouble() * MaxDelayVarianceMs), _playbackCT);
            _inputSender.SendKey(key, KeyPressType.UP);

        }
    }
}
