using GloomServer.Gloom.Models;
using GloomServer.Gloom.Models.PlayerInfoRepository;
using GloomServer.Gloom.Repositories;
using System.Collections.Generic;
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
        public Game JoinGame(PlayerRequest request, RequestHeader header)
        {
            Game game = PlayerInfoRepository.JoinGame(request, header);

            ReturnToAllPlayers(game, header);
            return game;
        }

        [Function("leave")]
        public Game LeaveGame(PlayerRequest request, RequestHeader header)
        {
            Game game = PlayerInfoRepository.LeaveGame(request, header);

            ReturnToAllPlayers(game, header);
            return game;
        }

        [Function("update-player")]
        public Game UpdatePlayer(PlayerRequest request, RequestHeader header)
        {
            Game game = PlayerInfoRepository.UpdatePlayer(request);

            ReturnToAllPlayers(game, header);
            return game;
        }


        [Function("update-elements")]
        public Game UpdateElements(ElementsRequest request, RequestHeader header)
        {
            Game game = PlayerInfoRepository.UpdateElements(request);

            ReturnToAllPlayers(game, header);
            return game;
        }

        private static void ReturnToAllPlayers(Game game, RequestHeader header) => header.TargetSockets = game.Players.Select(x => x.SocketId);
    }
}
