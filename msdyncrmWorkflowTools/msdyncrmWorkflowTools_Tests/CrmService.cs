using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools_Tests
{
    public class CrmService
    {

        public  CrmService() {
            const string crmServerUrl = "https://demianraskosandbox.crm4.dynamics.com";
            const string userName = "demianrasko@demianrasko.onmicrosoft.com";
            const string password = "xxxx";

            var connectionStringCrmOnline = string.Format("Url={0}; Username={1}; Password={2};authtype=Office365;", crmServerUrl, userName, password);

            CrmServiceClient conn = new Microsoft.Xrm.Tooling.Connector.CrmServiceClient(connectionStringCrmOnline);

            IOrganizationService _service = (IOrganizationService)conn.OrganizationWebProxyClient != null ? (IOrganizationService)conn.OrganizationWebProxyClient : (IOrganizationService)conn.OrganizationServiceProxy;

        }

        public  IOrganizationService service;
        
        
    }
}
