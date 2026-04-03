using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicTacToe.Contracts;
using TicTacToe.Contracts.DTO;

namespace TicTacToe.PlayerApp
{
    internal sealed class PlayerImpl
    {
        const string BASE_ADDRESS = "http://localhost:19081/TicTacToe/TicTacToe.Webapi/";
     
        private readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri(BASE_ADDRESS) };
        private readonly Random _random = new Random();
        private readonly ILogger _logger;

        public PlayerImpl(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GameStats> Go(int gamesCount, int maxJoinAttempts = 3)
        {
            GameStats gameStats = new GameStats();
            while (++gameStats.TotalGames <= gamesCount)
            {
                try
                {
                    Guid PlayerId = Guid.NewGuid();
                    _logger.LogInformation($"Now I'm player {PlayerId}");

                    int joinAttempts = 0;
                    Guid? gameId = null;
                    while (++joinAttempts <= maxJoinAttempts)
                    {
                        gameId = await JoinGame(PlayerId);
                        if (gameId != null)
                            break;

                        _logger.LogInformation($"Join attempt {joinAttempts} failed. Retrying...");
                        await Task.Delay(3000 * joinAttempts);
                    }

                    if (gameId == null)
                    {
                        _logger.LogInformation("Failed to join a game after 3 attempts. Now exit...");
                        gameStats.FailedJoins++;
                        gameStats.Reason = "J";
                        return gameStats;
                    }

                    Game game = await GetGame(gameId.Value, 10) ?? throw new Exception("Failed to retrieve game after joining.");

                    int playAttempts = 0;
                    int turnWaits = 0;
                    while (playAttempts++ < 100)
                    {
                        game = await GetGame(gameId.Value, 10) ?? throw new Exception("Failed to retrieve game while playing.");

                        if (game.Board.IsFinished)
                            break;

                        if (game.TurnOf(PlayerId))
                        {
                            turnWaits = 0;

                            var (ok, updatedGame) = await MakeMove(game, PlayerId);
                            if (ok && updatedGame != null)
                            {
                                game = updatedGame;
                            }
                        }
                        else
                        {
                            turnWaits++;
                        }

                        if (turnWaits == 5)
                        {
                            _logger.LogInformation("Game is idle, now quit");
                            gameStats.FailedGames++;
                            gameStats.Reason = "I";
                            break;
                        }

                        await Task.Delay(1000); // Simulate thinking time
                    }

                    switch(game.Board.GetWinner())
                    {
                        case FieldValue.X:
                            gameStats.XWins++;
                            break;
                        case FieldValue.O:
                            gameStats.OWins++;
                            break;
                        case FieldValue.Empty:
                            gameStats.Draws++;
                            break;
                    }

                    _logger.LogInformation($"Game finished with result: {game.Board.GetWinnerText()}");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogInformation($"HTTP error: {ex.Message}");
                }
            }

            if (gameStats.TotalGames > gamesCount)
                gameStats.TotalGames--; // Adjust for the last increment

            return gameStats;
        }

        private async Task<Guid?> JoinGame(Guid playerId)
        {
            var newGameDto = new NewGameRequestDTO { PlayerId = playerId };
            using var content = new StringContent(JsonSerializer.Serialize(newGameDto), Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await _httpClient.PostAsync("api/game/join", content);
            if (response.IsSuccessStatusCode)
            {
                var dto = await response.Content.ReadFromJsonAsync<NewGameResponseDTO>();

                _logger.LogInformation($"Game created: {dto?.Created}");
                _logger.LogInformation($"Game: {dto?.Game?.Display()}");

                if (dto?.Game?.Id != null)
                    return dto.Game.Id;

                return null;
            }
            else
            {
                _logger.LogInformation($"Request failed: {response.StatusCode}");
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(error);

                return null;
            }
        }

        private async Task<(bool, Game?)> MakeMove(Game game, Guid playerId)
        {
            int epmtyCount = game.Board.Fields.Count(f => f == FieldValue.Empty);
            int pick = _random.Next(epmtyCount);
            int pos = game.Board.Fields.Select((f, i) => (f, i)).Where(t => t.f == FieldValue.Empty).ElementAt(pick).i;

            var moveRequestDto = new MakeMoveRequestDTO { PlayerId = playerId, GameId = game.Id, Position = pos };
            using StringContent content = new StringContent(JsonSerializer.Serialize(moveRequestDto), Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await _httpClient.PostAsync("api/game/move", content);

            if (response.IsSuccessStatusCode)
            {
                Game? updatedGame = await response.Content.ReadFromJsonAsync<Game>();
                _logger.LogInformation($"Game after move:{Environment.NewLine}{updatedGame?.Board?.Display()}");
                return (true, updatedGame);
            }
            else
            {
                _logger.LogInformation($"Request failed: {response.StatusCode}");
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(error);
                return (false, null);
            }
        }

        private async Task<Game?> GetGame(Guid gameId, int maxAttempts)
        {
            int attempts = 0;
            while (++attempts <= maxAttempts)
            {
                try
                {
                    using HttpResponseMessage response = await _httpClient.GetAsync($"api/game/{gameId}");
                    if (response.IsSuccessStatusCode)
                    {
                        Game? dto = await response.Content.ReadFromJsonAsync<Game>();
                        return dto;
                    }
                    else
                    {
                        _logger.LogInformation($"Get Game Request failed: {response.StatusCode}");
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation(error);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"HTTP error: {ex.Message}");
                    await Task.Delay(1000 * attempts);
                }
            }

            throw new Exception("Failed to get game after multiple attempts.");
        }
    }
}
