using System;
using System.Diagnostics.Tracing;
using System.Fabric;
using System.Threading.Tasks;

namespace TicTacToe.Webapi
{
    // Event keywords can be used to categorize events. 
    // Each keyword is a bit flag. A single event can be associated with multiple keywords (via EventAttribute.Keywords property).
    // Keywords must be defined as a public class named 'Keywords' inside EventSource that uses them.
    //internal static class Keywords
    //{
    //    public const EventKeywords Requests = (EventKeywords)0x1L;
    //    public const EventKeywords ServiceInitialization = (EventKeywords)0x2L;
    //}

    [EventSource(Name = "Microsoft-SFSerbiaTeam-TicTacToe-WebapiService")]
    internal class ServiceEventSource : EventSource
    {
        private const int ServiceMessageEventId = 2;

        public static readonly ServiceEventSource Current = new();

        static ServiceEventSource()
        {
            // A workaround for the problem where ETW activities do not get tracked until Tasks infrastructure is initialized.
            // This problem will be fixed in .NET Framework 4.6.2.
            Task.Run(() => { });
        }

        // Instance constructor is private to enforce singleton semantics
        private ServiceEventSource() : base()
        {
        }

        [NonEvent]
        public void ServiceMessage(ServiceContext serviceContext, string message, params object[] args)
        {
            if (IsEnabled())
            {
                string finalMessage = string.Format(message, args);
                ServiceMessage(
                    serviceContext.ServiceName.ToString(),
                    serviceContext.ServiceTypeName,
                    GetReplicaOrInstanceId(serviceContext),
                    serviceContext.PartitionId,
                    serviceContext.CodePackageActivationContext.ApplicationName,
                    serviceContext.CodePackageActivationContext.ApplicationTypeName,
                    serviceContext.NodeContext.NodeName,
                    finalMessage);
            }
        }

        [Event(ServiceMessageEventId, Level = EventLevel.Informational, Message = "{7}")]
        private void ServiceMessage(
            string serviceName,
            string serviceTypeName,
            long replicaOrInstanceId,
            Guid partitionId,
            string applicationName,
            string applicationTypeName,
            string nodeName,
            string message)
        {

            WriteEvent(
                ServiceMessageEventId,
                serviceName,
                serviceTypeName,
                replicaOrInstanceId,
                partitionId,
                applicationName,
                applicationTypeName,
                nodeName,
                message);
        }

        private const int ServiceHostInitializationFailedEventId = 4;

        // , Keywords = Keywords.ServiceInitialization)
        [Event(ServiceHostInitializationFailedEventId, Level = EventLevel.Error, Message = "Service host initialization failed")]
        public void ServiceHostInitializationFailed(string exception)
        {
            WriteEvent(ServiceHostInitializationFailedEventId, exception);
        }

        private static long GetReplicaOrInstanceId(ServiceContext context)
        {
            StatelessServiceContext stateless = context as StatelessServiceContext;
            if (stateless != null)
            {
                return stateless.InstanceId;
            }

            StatefulServiceContext stateful = context as StatefulServiceContext;
            if (stateful != null)
            {
                return stateful.ReplicaId;
            }

            throw new NotSupportedException("Context type not supported.");
        }


        private const int ServiceTypeRegisteredEventId = 3;

        // Keywords = Keywords.ServiceInitialization
        [Event(ServiceTypeRegisteredEventId, Level = EventLevel.Informational, Message = "Service host process {0} registered service type {1}")]
        public void ServiceTypeRegistered(int hostProcessId, string serviceType)
        {
            WriteEvent(ServiceTypeRegisteredEventId, hostProcessId, serviceType);
        }
    }
}
