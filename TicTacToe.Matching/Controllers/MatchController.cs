using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using TicTacToe.Contracts;
using TicTacToe.Contracts.DTO;
using TicTacToe.GameMaster.Interfaces;

namespace TicTacToe.Matching.Controllers
{
    [ApiController]
    [Route("api/")]
    public class MatchController : Controller
    {
        const string PLAYERS_WHITELIST = "PlayersWhitelist";
        const string PLAYERS_QUEUE = "PlayersQueue";
        const string PLAYER_TO_GAME_MAP = "PlayerToGameMap";

        private readonly IReliableStateManager _stateManager;

        public MatchController(IReliableStateManager stateManager) : base()
        {
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        [HttpGet("probe")]
        public async Task<IActionResult> ProbeAsync()
        {
            TimeSpan TIMEOUT_THRESHOLD = TimeSpan.FromSeconds(2);

            try
            {
                var playersQueue = await _stateManager.GetOrAddAsync<IReliableQueue<Guid>>(PLAYERS_QUEUE);
                var playersWaitlist = await _stateManager.GetOrAddAsync<IReliableDictionary<Guid, bool>>(PLAYERS_WHITELIST);
                var player2game = await _stateManager.GetOrAddAsync<IReliableDictionary<Guid, Guid>>(PLAYER_TO_GAME_MAP);

                using (var tx = _stateManager.CreateTransaction())
                {
                    await playersQueue.TryPeekAsync(tx, TIMEOUT_THRESHOLD, CancellationToken.None);
                    await playersWaitlist.TryGetValueAsync(tx, Guid.Empty, TIMEOUT_THRESHOLD, CancellationToken.None);
                    await player2game.TryGetValueAsync(tx, Guid.Empty, TIMEOUT_THRESHOLD, CancellationToken.None);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message}");
            }
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinGame([FromBody] NewGameRequestDTO gameRequest)
        {
            var playersQueue = await _stateManager.GetOrAddAsync<IReliableQueue<Guid>>(PLAYERS_QUEUE);
            var playersWaitlist = await _stateManager.GetOrAddAsync<IReliableDictionary<Guid, bool>>(PLAYERS_WHITELIST);
            var player2game = await _stateManager.GetOrAddAsync<IReliableDictionary<Guid, Guid>>(PLAYER_TO_GAME_MAP);

            using (ITransaction tx = _stateManager.CreateTransaction())
            {
                await ProcessQueue(tx, playersQueue, playersWaitlist, player2game);
            }

            using (ITransaction tx = _stateManager.CreateTransaction())
            {
                if (await PlayerIsInTheWaitList(tx, playersWaitlist, gameRequest.PlayerId))
                    return Ok(new NewGameResponseDTO { Created = false });

                var existingGameId = await PlayerIsInTheGame(tx, player2game, gameRequest.PlayerId);
                if (existingGameId != null)
                    return Ok(new NewGameResponseDTO { Created = true, Game = await GetGame(existingGameId.Value) });

                var playerInQueue = await playersQueue.TryDequeueAsync(tx);
                if (playerInQueue.HasValue)
                {
                    // start a game
                    var (ok, message, game) = await StartGame(tx, playerInQueue.Value, gameRequest.PlayerId, player2game, playersWaitlist);
                    if (!ok)
                    {
                        tx.Abort();
                        return BadRequest(message);
                    }

                    // TODO: add (signalR) notification when the game has been started
                    await tx.CommitAsync();
                    return Ok(new NewGameResponseDTO { Created = true, Game = game });
                }
                else
                {
                    AddToWaitlistAndQueue(tx, playersQueue, playersWaitlist, gameRequest.PlayerId).GetAwaiter().GetResult();

                    await tx.CommitAsync();
                    return Ok(new NewGameResponseDTO { Created = false });
                }
            }
        }

        private async Task ProcessQueue(ITransaction tx, IReliableQueue<Guid> playersQueue, IReliableDictionary<Guid, bool> playersWaitlist,
            IReliableDictionary<Guid, Guid> player2game)
        {
            if (await playersQueue.GetCountAsync(tx) >= 2)
            {
                var p1 = await playersQueue.TryDequeueAsync(tx);
                var p2 = await playersQueue.TryDequeueAsync(tx);

                if (p1.HasValue && p2.HasValue)
                {
                    var (ok, message, game) = await StartGame(tx, p1.Value, p2.Value, player2game, playersWaitlist);
                    if (ok)
                        await tx.CommitAsync();
                    else
                        tx.Abort();
                }
            }
        }

        private async Task<bool> PlayerIsInTheWaitList(ITransaction tx, IReliableDictionary<Guid, bool> waitlist, Guid playerId)
        {
            var alreadyInQueue = await waitlist.TryGetValueAsync(tx, playerId);
            return alreadyInQueue.HasValue && alreadyInQueue.Value == true;
        }

        private async Task<Guid?> PlayerIsInTheGame(ITransaction tx, IReliableDictionary<Guid, Guid> player2game, Guid playerId)
        {
            var existingGame = await player2game.TryGetValueAsync(tx, playerId);
            return existingGame.HasValue ? existingGame.Value : null;
        }

        private async Task<Game> GetGame(Guid gameId)
        {
            var actorProxy = ActorProxy.Create<IGameMaster>(new ActorId(gameId));
            Game? game = await actorProxy.GetGame(gameId);
            if (game != null)
                return game;

            throw new Exception("Game not found");
        }

        private async Task<(bool, string, Game?)> StartGame(ITransaction tx, Guid player1, Guid player2, IReliableDictionary<Guid, Guid> player2game,
            IReliableDictionary<Guid, bool> playersWaitlist)
        {
            // start a game
            Guid newGameId = Guid.NewGuid();

            await player2game.TryAddAsync(tx, player1, newGameId);
            await player2game.TryAddAsync(tx, player2, newGameId);

            await playersWaitlist.TryRemoveAsync(tx, player1);
            await playersWaitlist.TryRemoveAsync(tx, player2);

            var actorProxy = ActorProxy.Create<IGameMaster>(new ActorId(newGameId));
            var (ok, message, game) = await actorProxy.StartGame(newGameId, player1, player2);
            if (ok)
                ServiceEventSource.Current.Message("New game started");

            return (ok, message, game);
        }

        private async Task AddToWaitlistAndQueue(ITransaction tx, IReliableQueue<Guid> queue, IReliableDictionary<Guid, bool> playersWaitlist, Guid playerId)
        {
            await queue.EnqueueAsync(tx, playerId);
            await playersWaitlist.TryAddAsync(tx, playerId, true);
        }
    }
}
