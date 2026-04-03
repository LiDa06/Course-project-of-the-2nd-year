using Balda.Features.Game.Domain;

namespace Balda.Features.Game.Bot
{
    public interface IBotMoveProvider
    {
        bool TryFindMove(GameSession session, out BotMove move);
    }
}
