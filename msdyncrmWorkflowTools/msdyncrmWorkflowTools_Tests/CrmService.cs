using Microsoft.Xrm.Sdk;

namespace msdyncrmWorkflowTools_Tests
{
    public class CrmService
    {
        public  CrmService() {
            const string crmServerUrl = "https://demianraskosandbox.crm4.dynamics.com";
            const string userName = "demianrasko@demianrasko.onmicrosoft.com";
            const string password = "xxxx";

            var connectionStringCrmOnline = $"Url={crmServerUrl}; Username={userName}; Password={password};authtype=Office365;";

            var conn = new Microsoft.Xrm.Tooling.Connector.CrmServiceClient(connectionStringCrmOnline);

            var _service = (IOrganizationService)conn.OrganizationWebProxyClient != null ? (IOrganizationService)conn.OrganizationWebProxyClient : (IOrganizationService)conn.OrganizationServiceProxy;
        }

        public  IOrganizationService service;
    }
}
