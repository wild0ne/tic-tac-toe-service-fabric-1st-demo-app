using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using TicTacToe.GameMaster.Interfaces;
using TicTacToe.Contracts;

namespace TicTacToe.GameMaster
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class GameMaster : Actor, IGameMaster
    {
        const string GAME = "game";

        public GameMaster(ActorService actorService, ActorId actorId) 
            : base(actorService, actorId)
        {
        }

        public async Task<(bool, string, Game?)> StartGame(Guid gameId, Guid player1, Guid player2)
        {
            try
            {
                EnsureGameId(gameId);

                var game = new Game { Id = gameId }.SetRandom(player1, player2);
                game = await this.StateManager.GetOrAddStateAsync<Game>(GAME, game);
                ActorEventSource.Current.Message("Game started: {}", gameId);
                return (true, string.Empty, game);
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Message("Game creation failed: {}", ex.ToString());
                return (false, ex.ToString(), null);
            }
        }

        public async Task<Game?> GetGame(Guid gameId)
        {
            try
            {
                EnsureGameId(gameId);

                var game = await this.StateManager.TryGetStateAsync<Game>(GAME);
                return game.HasValue ? game.Value! : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool, string, Game?)> MakeMove(Guid gameId, Guid playerId, int position)
        {
            try
            {
                EnsureGameId(gameId);

                var game = await GetGame(gameId);
                if (game == null)
                    throw new Exception("No game found");

                game.MakeMove(playerId, position);
                await this.StateManager.SetStateAsync<Game>(GAME, game);
                ActorEventSource.Current.Message("A move is made in the game {}", gameId);
                return (true, string.Empty, game);
            }
            catch (Exception ex)
            {
                return (false, ex.ToString(), null);
            }
        }

        private void EnsureGameId(Guid gameId)
        {
            if (this.GetActorId().GetGuidId() != gameId)
                throw new InvalidOperationException();
        }
    }
}
