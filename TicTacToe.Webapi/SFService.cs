using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Fabric;
using System.Runtime.Versioning;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace TicTacToe.Webapi
{
    internal sealed class SFService(StatelessServiceContext context) : StatelessService(context)
    {
        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        [SupportedOSPlatform("windows")]
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return
            [
                new(
                    serviceContext =>
                        new HttpSysCommunicationListener(
                            serviceContext,
                            "ServiceEndpoint",
                            (url, listener) =>
                            {
                                ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting HttpSysListener on {url}");

                                return new WebHostBuilder()
                                    .UseKestrel()
                                    // .UseHttpSys()
                                    .ConfigureLogging(logging =>
                                    {
                                        logging.ClearProviders();
                                        logging.AddConsole();
                                        logging.AddDebug();
                                    })
                                    .ConfigureServices(
                                        services => services
                                            // .AddSingleton(new ConfigSettings(serviceContext))
                                            .AddSingleton(new HttpClient())
                                            .AddSingleton(new FabricClient())
                                            .AddSingleton(serviceContext)
                                     )
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseStartup<Startup>()
                                    .UseUrls(url)
                                    .Build();
                            }))
            ];
        }
    }
}
