using Echo.Models;
using Echo.Models.Settings;
using System.Diagnostics;
using System.Threading;
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
        readonly AppSettings _settings;
        const int MaxDelayVarianceMs = 0;

        const double InjectionChance = .00; // one in every 100 commands will inject a key

        HashSet<byte> _releasedKeys = new HashSet<byte>();

        public bool IsPlaying { get; private set; } = false;

        public bool StoppedByFocusLoss { get; set; }
        public Orcam Ocram { get; private set; }
        private IEnumerator<OrcamCommand> _enumerator;

        public OrcamPlayer(
            ILogger<OrcamPlayer> logger,
            InputSender inputSender,
            AppSettings settings,
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
            // Use a high-resolution stopwatch to schedule commands by absolute ticks
            var playbackStopwatch = Stopwatch.StartNew();

            // loopStartTicks anchors the recorded absolute command timestamps to the
            // current playback loop. When we wrap the enumerator we reset this so
            // recorded timestamps are applied relative to each loop start.
            long loopStartTicks = playbackStopwatch.ElapsedTicks;

            while (!_playbackCT.IsCancellationRequested)
            {
                if (!_enumerator.MoveNext())
                {
                    // restart loop: reset enumerator and anchor timestamps to now
                    _enumerator.Reset();
                    _enumerator.MoveNext();
                    loopStartTicks = playbackStopwatch.ElapsedTicks;
                }

                var command = _enumerator.Current;

                // recorded command.Delay is ticks since recording start; apply loopStartTicks
                long targetTicks = loopStartTicks + command.Delay;

                long remainingTicks = targetTicks - playbackStopwatch.ElapsedTicks;
                if (remainingTicks > 0)
                {
                    double remainingMs = remainingTicks * 1000.0 / Stopwatch.Frequency;

                    // If there's a coarse wait, use Task.Delay to yield the thread first
                    if (remainingMs > 12)
                    {
                        int delayMs = Math.Max(0, (int)(remainingMs - 8)); // leave ~8ms for tight waiting
                        try
                        {
                            await Task.Delay(delayMs, _playbackCT);
                        }
                        catch (TaskCanceledException) { break; }
                    }

                    // Spin-wait for the remaining short interval to improve precision
                    while (playbackStopwatch.ElapsedTicks < targetTicks)
                    {
                        // small yield to avoid full busy-wait; still tight
                        Thread.SpinWait(10);
                        if (_playbackCT.IsCancellationRequested) break;
                    }
                }

                // execute command
                var before = playbackStopwatch.ElapsedTicks;
                _inputSender.SendKey(command.Key, command.Type);
                var after = playbackStopwatch.ElapsedTicks;

                // diagnostic logging: report skew in ms between intended target and actual send time
                var actualMs = after * 1000.0 / Stopwatch.Frequency;
                var targetMs = targetTicks * 1000.0 / Stopwatch.Frequency;
                var skewMs = actualMs - targetMs;
                _logger.LogTrace($"Played key {command.Key} type {command.Type}: targetMs={targetMs:F3}, actualMs={actualMs:F3}, skewMs={skewMs:F3}");
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
