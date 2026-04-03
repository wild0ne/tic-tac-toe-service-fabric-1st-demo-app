using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using System;
using System.Fabric;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicTacToe.Contracts;
using TicTacToe.Contracts.DTO;
using TicTacToe.GameMaster.Interfaces;
using TicTacToe.Webapi;

namespace webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly ILogger<GameController> _logger;
        private readonly StatelessServiceContext _serviceContext;
        private readonly HttpClient _httpClient;

        private ServiceProxyFactory _serviceProxyFactory = new ServiceProxyFactory((c) =>
        {
            return new FabricTransportServiceRemotingClientFactory(
                serializationProvider: new ServiceRemotingDataContractSerializationProvider(),
                servicePartitionResolver: new ServicePartitionResolver("localhost:19000", "localhost:19001")
                );
        });

        public GameController(ILogger<GameController> logger, StatelessServiceContext serviceContext, HttpClient httpClient)
        {
            _logger = logger;
            _serviceContext = serviceContext;
            _httpClient = httpClient;
        }

        [HttpPost("join")]
        public async Task<IActionResult> Join([FromBody] NewGameRequestDTO newGameDto)
        {
            Uri serviceName = GetServiceName(_serviceContext);
            Uri proxyAddress = GetProxyAddress(serviceName);

            string proxyUrl =
                $"{proxyAddress}/api/join?PartitionKey={GetPartitionKeyFromPlayerId(newGameDto.PlayerId)}&PartitionKind=Int64Range";

            using var content = new StringContent(JsonSerializer.Serialize(newGameDto), Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await _httpClient.PostAsync(proxyUrl, content);
            if (response.IsSuccessStatusCode)
            {
                string s1 = await response.Content.ReadAsStringAsync();
                var resp = JsonSerializer.Deserialize<NewGameResponseDTO>(s1,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return Ok(resp);
            }
            else
            {
                string s1 = await response.Content.ReadAsStringAsync();
                var resp = JsonSerializer.Deserialize<NewGameResponseDTO>(s1);
                return BadRequest(resp);
            }

        }

        public static long GetPartitionKeyFromPlayerId(Guid id)
        {
            return 0;
        }

        public static Uri GetServiceName(ServiceContext context)
        {
            return new Uri($"{context.CodePackageActivationContext.ApplicationName}/TicTacToe.Matching");
        }

        public static Uri GetProxyAddress(Uri serviceName)
        {
            return new Uri($"http://localhost:19081{serviceName.AbsolutePath}");
        }

        [HttpGet("{gameId}")]
        public async Task<IActionResult> GetGame(Guid gameId)
        {
            var actorProxy = ActorProxy.Create<IGameMaster>(new ActorId(gameId));
            Game? game = await actorProxy.GetGame(gameId);
            if (game != null)
                return Ok(game);

            return NotFound();
        }

        [HttpPost("move")]
        public async Task<IActionResult> Move([FromBody] MakeMoveRequestDTO request)
        {
            var actorProxy = ActorProxy.Create<IGameMaster>(new ActorId(request.GameId));
            var (ok, message, game) = await actorProxy.MakeMove(request.GameId, request.PlayerId, request.Position);
            if (ok)
            {
                return Ok(game);
            }

            return BadRequest(message);
        }
    }
}