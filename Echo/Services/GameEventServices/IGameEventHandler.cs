using Echo.Models;
using System.Drawing;

namespace Echo.Services.GameEventServices
{
    public interface IGameEventHandler
    {
        GameEventType EventType { get; }

        Task HandleEvent(CancellationToken ct);

    }
}
