using Echo.Extern;
using Echo.Models;
using Echo.Services;
using Echo.Services.GameEventServices;
using Echo.Services.ImageAnalysis;
using Echo.Util;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;

namespace Echo
{
    public class MainWindowViewModel
    {
        readonly ILogger<MainWindowViewModel> _logger;
        readonly Runner _runner;
        readonly OrcamPlayer _orcamPlayer;
        readonly OrcamRecorder _orcamRecorder;
        readonly ProcessProvider _processProvider;
        readonly GameFocusManager _gameFocusManager;
        readonly MapAnalyzer _mapAnalyzer;

        public string NewOrcamName { get; set; }
        public Orcam? CurrentOrcam { get; set; }

        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            Runner runner,
            OrcamPlayer orcamPlayer,
            OrcamRecorder orcamRecorder,
            ProcessProvider processProvider,
            GameFocusManager gameFocusManager,
            MapAnalyzer mapAnalyzer)
        {
            _logger = logger;
            _runner = runner;
            _orcamPlayer = orcamPlayer;
            _orcamRecorder = orcamRecorder;
            _processProvider = processProvider;
            _gameFocusManager = gameFocusManager;
            _mapAnalyzer = mapAnalyzer;
        }

        public async Task Start()
        {
            if (CurrentOrcam is null)
                return;

            await _mapAnalyzer.UpdateMapBounds();

            _gameFocusManager.SetFocus();

            _orcamPlayer.SetOrcam(CurrentOrcam);
            _runner.Start();
            _orcamPlayer.Play();

        }
        public async Task Stop()
        {
            _orcamPlayer.Stop();
            _runner.Stop();
        }

        public async Task Record()
        {
            var gameProc = _processProvider.Process;

            _gameFocusManager.SetFocus();

            CurrentOrcam = await _orcamRecorder.Record();

        }

        public void Save()
        {
            if (CurrentOrcam == null)
                throw new Exception("No macro to save");


            CurrentOrcam.Name = NewOrcamName ?? Guid.NewGuid().ToString();
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cultivation3", "Macros", $"{CurrentOrcam.Name}.json");
            SerializationHelper.SerializeToFile(CurrentOrcam, filePath);
        }
    }
}
