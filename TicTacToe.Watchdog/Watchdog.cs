using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Health;
using System.Fabric.Query;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TicTacToe.Watchdog
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Watchdog : Microsoft.ServiceFabric.Services.Runtime.StatelessService
    {
        private readonly HttpClient _httpClient;

        public Watchdog(StatelessServiceContext context, HttpClient httpClient)
            : base(context)
        { 
            _httpClient = httpClient;
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            long iteration = 0;
            while (true)
            {
                iteration++;
                cancellationToken.ThrowIfCancellationRequested();

                var serviceName = new Uri("fabric:/TicTacToe/TicTacToe.Matching");

                var fabricClient = new FabricClient();

                ServicePartitionList partitions = await fabricClient.QueryManager.GetPartitionListAsync(serviceName);

                foreach (var partition in partitions)
                {
                    Debug.Assert(partition.PartitionInformation.Kind == ServicePartitionKind.Int64Range);
                    var int64Info = (Int64RangePartitionInformation)partition.PartitionInformation;

                    long lowKey = int64Info.LowKey;
                    long highKey = int64Info.HighKey;
                    Guid partitionId = partition.PartitionInformation.Id;

                    bool partitionOk = await ProbePartition(lowKey);

                    ReportHealth(fabricClient, partitionOk, partitionId, iteration);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }

        private async Task<bool> ProbePartition(long partitionKey)
        {
            Uri serviceName = new Uri("fabric:/TicTacToe/TicTacToe.Matching");
            Uri proxyAddress = GetProxyAddress(serviceName);
            string proxyUrl = $"{proxyAddress}/api/probe?PartitionKey={partitionKey}&PartitionKind=Int64Range";
            using HttpResponseMessage response = await _httpClient.GetAsync(proxyUrl);
            return response.IsSuccessStatusCode;
        }

        private static Uri GetProxyAddress(Uri serviceName)
        {
            return new Uri($"http://localhost:19081{serviceName.AbsolutePath}");
        }

        private void ReportHealth(FabricClient fabricClient, bool ok, Guid partitionId, long iteration)
        {
            var OK = HealthState.Ok;

            var healthInfo = new HealthInformation(
                sourceId: "Watchdog",
                property: "ReliableStateAvailability",
                healthState: ok ? OK : HealthState.Warning)
            {
                Description = "Reliable state probe failed (transaction timeout)",
                TimeToLive = TimeSpan.FromMinutes(2),
                RemoveWhenExpired = true
            };

            var report = new PartitionHealthReport(
                partitionId: partitionId,
                healthInformation: healthInfo);

            fabricClient.HealthManager.ReportHealth(report);
        }
    }
}
