using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using msdyncrmWorkflowTools;
using System;

namespace msdyncrmWorkflowTools_ConsoleTest
{
    class Program
    {
        static IOrganizationService service = GetCrmService();

        static void Main(string[] args)
        {
            var classObj = new Common(service);

            var createdTeam=classObj.CreateTeam("PruebaTeam2", 1, new EntityReference("systemuser", new Guid("8fe5fd89-f447-4a38-90f1-1180617fcbc5")), new EntityReference("businessunit", new Guid("6025BC19-2E34-EA11-A812-000D3ABAAFE7")));
        }
        public static IOrganizationService GetCrmService()
        {
            const string crmServerUrl = "https://XXX.crm4.dynamics.com";
            const string userName = "XXX@XXX.com";
            const string password = "XXX";

            var connectionStringCrmOnline = $"Url={crmServerUrl}; Username={userName}; Password={password};AuthType=Office365";

            var conn = new CrmServiceClient(connectionStringCrmOnline);

            var _service = (IOrganizationService)conn.OrganizationWebProxyClient != null ? (IOrganizationService)conn.OrganizationWebProxyClient : (IOrganizationService)conn.OrganizationServiceProxy;

            return _service;
        }
    }
}
