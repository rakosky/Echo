using Echo.Extern;
using Echo.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Echo.Services.GameEventServices
{
    public class Runner
    {
        ILogger<Runner> _logger;
        List<IGameEventHandler> _gameEventHandlers;
        List<IGameEventChecker> _gameEventCheckers;
        OrcamPlayer _orcamPlayer;
        ScreenshotProvider _screenshotProvider;

        CancellationTokenSource _eventCheckCTS;
        CancellationToken _eventCheckCT;

        CancellationTokenSource _eventHandlerCTS;
        CancellationToken _eventHandlerCT;

        GameEventType _currentEvent = GameEventType.None;
        Task? _eventLoopTask = null;
        Task? _currentHandlerTask = null;
        public Runner(
            ILogger<Runner> logger,
            IEnumerable<IGameEventHandler> gameEventHandlers,
            IEnumerable<IGameEventChecker> gameEventCheckers,
            OrcamPlayer orcamPlayer,
            ScreenshotProvider screenshotProvider)
        {
            _logger = logger;
            _gameEventHandlers = gameEventHandlers.ToList();
            _gameEventCheckers = gameEventCheckers.ToList();
            _orcamPlayer = orcamPlayer;
            _screenshotProvider = screenshotProvider;
        }


        public void Start()
        {
            _eventCheckCTS = new CancellationTokenSource();
            _eventHandlerCTS = new CancellationTokenSource();
            _eventCheckCT = _eventCheckCTS.Token;
            _eventHandlerCT = _eventHandlerCTS.Token;

            _logger.LogInformation("Starting event check loop...");

            _eventLoopTask = Task.Run(() => RunEventLoop());
        }

        public async Task Stop()
        {
            // 1) signal both loops to stop
            _eventCheckCTS.Cancel();
            _eventHandlerCTS.Cancel();

            if (_currentHandlerTask is not null)
                await _currentHandlerTask;       // any active handler

            // 2) wait for graceful completion
            if (_eventLoopTask is not null)
                await _eventLoopTask;            // event-check loop

            // 3) NOW it’s safe to dispose the CTS objects
            _eventCheckCTS.Dispose();
            _eventHandlerCTS.Dispose();
        }

        async Task RunEventLoop()
        {
            while (!_eventCheckCT.IsCancellationRequested)
            {
                try
                {
                    var detectedEvents = await CheckForEvents();
                    var priorityEvent = detectedEvents
                        .OrderByDescending(e => (int)e)
                        .FirstOrDefault();

                    if (priorityEvent is GameEventType.None)
                        continue;

                    if (priorityEvent <= _currentEvent)
                    {
                        _logger.LogInformation($"Lower priority event detected: {priorityEvent} (current: {_currentEvent}). Skipping...");
                        continue;
                    }

                    await CheckForAndStopCurrentEvent();

                    _orcamPlayer.Stop();

                    _logger.LogInformation($"Starting new handler for event {priorityEvent}");

                    _currentHandlerTask = StartEventHandlerAsync(priorityEvent, _eventHandlerCT);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running event loop: {Message}", ex.Message);
                }
                await Task.Delay(1000, _eventCheckCT);
            }
            _logger.LogInformation("Finished event check loop...");
        }


        async Task<List<GameEventType>> CheckForEvents()
        {
            var pixelSnapshot = await _screenshotProvider.GetLatestPixels();
            var detectedEvents = new List<GameEventType>();

            foreach (var handler in _gameEventCheckers.Where(h => h.EventType != _currentEvent))
            {
                var @event = await handler.EventDetected(pixelSnapshot);
                if (@event != GameEventType.None)
                {
                    _logger.LogInformation($"Event detected: {@event}");
                    detectedEvents.Add(@event);
                }
            }

            return detectedEvents;
        }

        async Task CheckForAndStopCurrentEvent()
        {
            if (_currentHandlerTask != null && !_currentHandlerTask.IsCompleted)
            {
                _eventHandlerCTS.Cancel();
                try
                {
                    await _currentHandlerTask;
                }
                catch (OperationCanceledException) { /* swallow */ }
                _logger.LogInformation($"Handler for previous event event {_currentEvent} successfully cancelled.");
                // dispose the old CTS and make a brand-new one
                _eventHandlerCTS.Dispose();
                _eventHandlerCTS = new CancellationTokenSource();
                _eventHandlerCT = _eventHandlerCTS.Token;
            }
        }

        async Task StartEventHandlerAsync(GameEventType e, CancellationToken ct)
        {
            try
            {
                var handler = _gameEventHandlers
                    .FirstOrDefault(h => h.EventType == e);

                if (handler == null)
                {
                    _logger.LogError($"No handler found for event {e}");
                    return;
                }

                _currentEvent = handler.EventType;
                await handler.HandleEvent(ct);
                _logger.LogInformation($"Handler for event {e} completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Handler for event {e} failed");
            }
            finally
            {
                _currentEvent = GameEventType.None;
                _orcamPlayer.Play();
            }
        }


    }
}
