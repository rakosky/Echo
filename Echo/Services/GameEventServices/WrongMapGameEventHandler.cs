using Echo.Models;

namespace Echo.Services.GameEventServices
{
    public class WrongMapGameEventHandler : IGameEventHandler, IGameEventChecker
    {
        public GameEventType EventType => GameEventType.WrongMap;
        public async Task<GameEventType> EventDetected(PixelSnapshot pixelSnapshot)
        {
            return GameEventType.None;
            //if (_macroPlayer.Macro.MapNameImg != null && Functions.FindImageCoords(mapleImg, _macroPlayer.Macro.MapNameImg, .4) == null)
            //{
            //    _macroPlayer.Pause();
            //    Console.WriteLine("Not in correct map.");
            //    Thread.Sleep(3000);
            //    _player.TeleToMacroMap(_macroPlayer.Macro.MapImg);
            //    Thread.Sleep(5000);
            //    _macroPlayer.Resume();
            //}.None;
        }
        public async Task HandleEvent(CancellationToken ct)
        {
            await Task.Delay(10000, ct);
        }
    }

}
