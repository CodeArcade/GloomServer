using GloomServer.Gloom.Models;
using GloomServer.Gloom.Models.PlayerInfoRepository;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public Game JoinGame(PlayerRequest request, RequestHeader header)
        {
            Game newGame = new();

            // add the game if it does not exist
            if (!Games.ContainsKey(request.GameId))
                if (!Games.TryAdd(request.GameId, newGame)) throw new Exception("The game could not be created.");

            // get the game to join
            if (!Games.TryGetValue(request.GameId, out Game newOrFoundGame)) throw new Exception("The game could not be found.");

            if (newOrFoundGame == newGame) // if new game add player to it and set elements
            {
                newOrFoundGame.Players.Add(request.Player);
                newOrFoundGame.Elements = new List<Element>() {
                    new()
                        {
                            Name = ElementName.Fire,
                            Stage = ElementStages.Empty
                        },
                    new()
                        {
                            Name = ElementName.Ice,
                            Stage = ElementStages.Empty
                        },
                    new()
                        {
                            Name = ElementName.Ground,
                            Stage = ElementStages.Empty
                        },
                    new()
                        {
                            Name = ElementName.Air,
                            Stage = ElementStages.Empty
                        },
                    new()
                        {
                            Name = ElementName.Light,
                            Stage = ElementStages.Empty
                        },
                    new()
                        {
                            Name = ElementName.Dark,
                            Stage = ElementStages.Empty
                        }
                };
            }
            else // if found a game check the player count
            {
                Player existingPlayer = newOrFoundGame.Players.FirstOrDefault(x => x.Id == request.Player.Id);

                if (existingPlayer is null)
                    if (newOrFoundGame.Players.Count == 4) throw new Exception("The game is already full.");
            }

            // set new player socket id for communication
            newOrFoundGame.Players.First(x => x.Id == request.Player.Id).SocketId = header.SocketId;

            return UpdateGame(newOrFoundGame);
        }

        public Game LeaveGame(PlayerRequest request, RequestHeader header)
        {
            // get game by id
            if (!Games.TryGetValue(request.GameId, out Game foundGame)) throw new Exception("The game could not be found.");
            // remove request player from game
            foundGame.Players = foundGame.Players.Where(x => x.SocketId != header.SocketId).ToList();

            // remove game if no player remains
            if (!foundGame.Players.Any())
                if (!Games.TryRemove(request.GameId, out foundGame)) throw new Exception("The game could not be removed.");

            // return updated game to all players except the request player
            return UpdateGame(foundGame);
        }

        public Game UpdatePlayer(PlayerRequest request)
        {
            if (!Games.TryGetValue(request.GameId, out Game foundGame)) throw new Exception("The game could not be found.");

            Player foundPlayer = foundGame.Players.First(x => x.Id == request.Player.Id);
            foundGame.Players.Remove(foundPlayer);
            foundGame.Players.Add(request.Player);

            return UpdateGame(foundGame);
        }

        public Game UpdateElements(ElementsRequest request)
        {
            if (!Games.TryGetValue(request.GameId, out Game foundGame)) throw new Exception("The game could not be found.");

            foundGame.Elements = request.Elements;

            return UpdateGame(foundGame);
        }

        private static Game UpdateGame(Game game) => Games.AddOrUpdate(game.Id, game, (key, oldGame) => oldGame = game);

        /// <summary>
        /// Loops through all games and removes empty ones in case a player does not leave properly
        /// </summary>
        private static void RemoveGamesIfEmpty()
        {
            foreach (string key in Games.Keys)
            {
                if (Games.TryGetValue(key, out Game game))
                {
                    foreach (string socketId in game.Players.Select(x => x.SocketId))
                        if (!WebSocketMiddleware.ConnectedSockets.Contains(socketId))
                            game.Players = game.Players.Where(x => x.SocketId != socketId).ToList();

                    if (!game.Players.Any()) Games.TryRemove(key, out _);
                }
            }
        }
    }
}
