using GloomServer.Gloom.Models;
using GloomServer.Gloom.Repositories;
using System.Linq;

namespace GloomServer.Gloom.Controller
{
    public class GameController : WebSocketController
    {
        public override string Name => "game";

        private PlayerInfoRepository PlayerInfoRepository { get; set; }

        public GameController()
        {
            PlayerInfoRepository = new();
        }

        [Function("join")]
        public Game JoinGame(Game game, RequestHeader header)
        {
            game = PlayerInfoRepository.JoinGame(game, header);

            ReturnToAllPlayers(game, header);
            return game;
        }

        [Function("leave")]
        public Game LeaveGame(Game game, RequestHeader header)
        {
            game = PlayerInfoRepository.LeaveGame(game, header);

            ReturnToAllPlayers(game, header);
            return game;
        }

        [Function("sync")]
        public Game Sync(Game game, RequestHeader header)
        {
            game = PlayerInfoRepository.Sync(game);

            ReturnToAllPlayers(game, header);
            return game;
        }

        private void ReturnToAllPlayers(Game game, RequestHeader header) => header.TargetSockets = game.Players.Select(x => x.SocketId);
    }
}
