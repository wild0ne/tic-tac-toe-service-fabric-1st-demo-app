using System.Fabric;
using System.Fabric.Health;

namespace TicTacToe.Common
{
    public class HealthReportHelper
    {
        private Uri _applicationName;
        private string _serviceManifestName;
        private string? _nodeName = null;

        public HealthReportHelper(Uri applicationName, string serviceManifestName)
        {
            _applicationName = applicationName;
            _serviceManifestName = serviceManifestName;
        }

        public void SendReport(HealthInformation healthInfo)
        {
            _nodeName ??= FabricRuntime.GetNodeContext().NodeName;

            FabricRuntime.GetNodeContext();

            using var fc = new FabricClient(new FabricClientSettings() { HealthReportSendInterval = TimeSpan.FromSeconds(0) });

            var deployedServicePackageHealthReport = new DeployedServicePackageHealthReport(
                _applicationName,
                _serviceManifestName,
                _nodeName,
                healthInfo 
                );

            fc.HealthManager.ReportHealth(deployedServicePackageHealthReport);
        }
    }
}
