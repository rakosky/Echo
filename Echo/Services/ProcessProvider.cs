using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Echo.Services
{
    public class ProcessProvider
    {
        readonly ILogger<ProcessProvider> _logger;

        string processName = "maplestory";

        Process? _process;

        public Process? TryGetProcess()
        {
            return _process;
        }
        public Process Process => _process ?? throw new ProcessNotFoundException();

        public ProcessProvider(ILogger<ProcessProvider> logger)
        {
            _logger = logger;
        }

        public async Task StartProcessCaptureLoop()
        {
            while (true)
            {
                try
                {
                    _process = Process.GetProcessesByName(processName).FirstOrDefault()
                        ?? throw new Exception($"Unable to find process: {processName}");

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error getting process {processName}");
                }
                
                await Task.Delay(500);
            }

        }
    }

    public class ProcessNotFoundException : Exception
    {
        public ProcessNotFoundException() : base("Process not found") { }
        public ProcessNotFoundException(string message) : base(message) { }
        public ProcessNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
