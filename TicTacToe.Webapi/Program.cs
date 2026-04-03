using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Threading;
using TicTacToe.Common;
using TicTacToe.Webapi;

namespace TictacToe.Webapi
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            var fm = new FailureMaker();
            try
            {
                // The ServiceManifest.XML file defines one or more service type names.
                // Registering a service maps a service type name to a .NET type.
                // When Service Fabric creates an instance of this service type,
                // an instance of the class is created in this host process.

                ServiceRuntime.RegisterServiceAsync(
                    "TicTacToe.WebapiType",
                    context => new SFService(context)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Environment.ProcessId, typeof(SFService).Name);

                // fm.PutBomb();

                // Prevents this host process from terminating so services keeps running. 
                Thread.Sleep(Timeout.Infinite); // commenting this out will lead to a failure
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
