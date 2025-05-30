using Echo.Models;

namespace Echo.Services.GameEventServices
{
    public interface IGameEventChecker
    {
        GameEventType EventType { get; }
        Task<GameEventType> EventDetected(PixelSnapshot pixelSnapshot);
    }
}
