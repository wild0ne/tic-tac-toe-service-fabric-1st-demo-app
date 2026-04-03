
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TicTacToe.Contracts;
using TicTacToe.Contracts.DTO;
using CommandLine;

namespace TicTacToe.PlayerApp
{
    public class Options
    {
        [Option('p', "players", Required = false, HelpText = "Players count", Default = 1)]
        public int PlayersCount { get; set; }

        [Option('g', "games", Required = true, HelpText = "Games count", Default = 10)]
        public int GamesCount { get; set; }
    }

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length > 0)
            {
                var parsed = Parser.Default.ParseArguments<Options>(args).Value;
                var impl = new PlayerImpl(NullLogger.Instance);
                var players = Enumerable.Range(0, parsed.PlayersCount).Select(_ => new PlayerImpl(NullLogger.Instance).Go(parsed.GamesCount)).ToList();
                await Task.WhenAll(players);

                var gameStats = players.Aggregate(new GameStats(), (gs, p) =>
                {
                    gs.Append(p.Result);
                    return gs;
                });

                Console.WriteLine($"Game stats: {gameStats.Display()}");
                return;
            }

            while (true)
            {
                using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                ILogger logger = loggerFactory.CreateLogger<PlayerImpl>();
                var impl = new PlayerImpl(logger);

                Console.WriteLine("Press a key to proceed: Q for quit");
                var k = Console.ReadKey();
                if (k.Key == ConsoleKey.Q)
                    return;

                var gameStats = await impl.Go(1, 3);
                logger.LogInformation($"Game stats: {gameStats.Display()}");
            }
        }
    }
}