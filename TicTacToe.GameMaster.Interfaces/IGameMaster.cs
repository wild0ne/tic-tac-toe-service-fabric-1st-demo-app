using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting;
using TicTacToe.Contracts;

[assembly: FabricTransportActorRemotingProvider(RemotingListenerVersion = RemotingListenerVersion.V2_1, RemotingClientVersion = RemotingClientVersion.V2_1)]
namespace TicTacToe.GameMaster.Interfaces
{
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IGameMaster : IActor
    {
        Task<(bool, string, Game?)> StartGame(Guid gameId, Guid player1, Guid player2);

        Task<Game?> GetGame(Guid gameId);

        Task<(bool, string, Game?)> MakeMove(Guid gameId, Guid playerId, int position);

        // TODO: add garbage collector for actor states
    }
}
