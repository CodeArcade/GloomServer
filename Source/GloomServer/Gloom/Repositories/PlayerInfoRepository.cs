using GloomServer.Gloom.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace GloomServer.Gloom.Repositories
{
    public class PlayerInfoRepository
    {
        private static ConcurrentDictionary<string, Game> Games { get; set; } = new();

        public PlayerInfoRepository()
        {
            WebSocketMiddleware.OnBroadcast += (sender, e) => RemoveGamesIfEmpty();
        }

        public Game JoinGame(Game game, RequestHeader header)
        {
            Game newGame = new();

            // add the game if it does not exist
            if (!Games.ContainsKey(game.Id))
                if (!Games.TryAdd(game.Id, newGame)) throw new Exception("The game could not be created.");

            // get the game to join
            if (!Games.TryGetValue(game.Id, out Game newOrFoundGame)) throw new Exception("The game could not be found.");

            if (newOrFoundGame == newGame) // if new game add player to it and set elements
            {
                newOrFoundGame.Players = newOrFoundGame.Players.Append(game.Players.First());
                newOrFoundGame.Elements = game.Elements;
            }
            else // if found a game check the player count
                if (newOrFoundGame.Players.Count() == 4) throw new Exception("The game is already full.");

            // set new player socket id for communication
            newOrFoundGame.Players.First(x => x.Name == game.Players.First().Name).SocketId = header.SocketId;

            return UpdateGame(newOrFoundGame);
        }

        public Game LeaveGame(Game game, RequestHeader header)
        {
            // get game by id
            if (!Games.TryGetValue(game.Id, out Game foundGame)) throw new Exception("The game could not be found.");
            // remove request player from game
            game.Players = game.Players.Where(x => x.SocketId != header.SocketId);

            // remove game if no player remains
            if (!game.Players.Any())
                if (!Games.TryRemove(game.Id, out foundGame)) throw new Exception("The game could not be removed.");

            // return updated game to all players except the request player
            return UpdateGame(game);
        }

        public Game Sync(Game game) => UpdateGame(game);

        private Game UpdateGame(Game game) => Games.AddOrUpdate(game.Id, game, (key, oldGame) => oldGame = game);

        /// <summary>
        /// Loops through all games and removes empty ones in case a player does not leave properly
        /// </summary>
        private static void RemoveGamesIfEmpty()
        {
            foreach (string key in Games.Keys)
            {
                if (Games.TryGetValue(key, out Game game))
                    if (!game.Players.Any()) Games.TryRemove(key, out _);
            }
        }
    }
}
