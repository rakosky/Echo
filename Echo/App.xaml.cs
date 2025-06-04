using Echo.Services;
using Echo.Services.GameEventServices;
using Echo.Services.ImageAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Configuration;
using System.Data;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Navigation;

namespace Echo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        [DllImport("Kernel32", SetLastError = true)]
        public static extern void FreeConsole();


        private IServiceProvider _serviceProvider = default!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AllocConsole();

            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder
                    .AddDebug()                     // Output to VS Debug window
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Information);
            });

            services.AddSingleton<Runner>();
            services.AddSingleton<OrcamPlayer>();
            services.AddSingleton<InputSender>();
            services.AddSingleton<ProcessProvider>();
            services.AddSingleton<GameFocusManager>();
            services.AddSingleton<ScreenshotProvider>();

            services.AddSingleton<RuneDetectedEventHandler>();
            services.AddSingleton<PlayerDiedEventHandler>();
            services.AddSingleton<GmMapEventHandler>();
            services.AddSingleton<WrongMapGameEventHandler>();
            services.AddSingleton<RuneFailEventHandler>();

            services.AddSingleton<IGameEventHandler>(sp => sp.GetRequiredService<RuneDetectedEventHandler>());
            services.AddSingleton<IGameEventHandler>(sp => sp.GetRequiredService<PlayerDiedEventHandler>()); 
            services.AddSingleton<IGameEventHandler>(sp => sp.GetRequiredService<GmMapEventHandler>());
            services.AddSingleton<IGameEventHandler>(sp => sp.GetRequiredService<WrongMapGameEventHandler>());
            services.AddSingleton<IGameEventHandler>(sp => sp.GetRequiredService<RuneFailEventHandler>());

            services.AddSingleton<IGameEventChecker>(sp => sp.GetRequiredService<RuneDetectedEventHandler>());
            services.AddSingleton<IGameEventChecker>(sp => sp.GetRequiredService<PlayerDiedEventHandler>());
            services.AddSingleton<IGameEventChecker>(sp => sp.GetRequiredService<GmMapEventHandler>());
            services.AddSingleton<IGameEventChecker>(sp => sp.GetRequiredService<WrongMapGameEventHandler>());
            services.AddSingleton<IGameEventChecker>(sp => sp.GetRequiredService<RuneFailEventHandler>());

            services.AddSingleton<OrcamRecorder>();

            services.AddSingleton<GameAnalyzer>();
            services.AddSingleton<MapAnalyzer>();
            services.AddSingleton<RuneAnalyzer>();

            services.AddSingleton<PlayerController>();
            services.AddSingleton<DiscordBotService>();
            services.AddSingleton(new SoundPlayer("alarm.wav"));
            
            services.AddSingleton<FlagRemovalHookManager>();

            services.AddSingleton<MainWindowViewModel>();


            // Register MainWindow as a singleton (so DI can inject its VM)
            services.AddSingleton<MainWindow>();
            _serviceProvider = services.BuildServiceProvider();

            _ = _serviceProvider.GetRequiredService<FlagRemovalHookManager>().StartHookManagementLoop();
            _ = Task.Run(() => _serviceProvider.GetRequiredService<ScreenshotProvider>().StartScreenCaptureLoop());
            _ = Task.Run(() => _serviceProvider.GetRequiredService<ProcessProvider>().StartProcessCaptureLoop());
            _ = Task.Run(() => _serviceProvider.GetRequiredService<GameFocusManager>().StartFocusCheckLoop());

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();


        }
    }

}
