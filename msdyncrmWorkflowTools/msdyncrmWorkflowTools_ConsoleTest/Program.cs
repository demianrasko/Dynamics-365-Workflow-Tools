using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_ConsoleTest
{
    class Program
    {
        static IOrganizationService service = GetCrmService();

        static void Main(string[] args)
        {
            var classObj = new msdyncrmWorkflowTools_Class(service);

            //classObj.UpdateChildRecords("contact_customer_accounts", "account", "D17BAB26-98BF-E611-810A-3863BB350E28","emailaddress1", "", "emailaddress1");
            classObj.UpdateChildRecords("contact_customer_accounts", "account", "D17BAB26-98BF-E611-810A-3863BB350E28", "new_campaa", "", "new_campaa");
        }
        public static IOrganizationService GetCrmService()
        {

            const string crmServerUrl = "https://demianrasko93.crm4.dynamics.com";
            const string userName = "demianrasko93@demianrasko93.onmicrosoft.com";
            const string password = "XXXX";

             var connectionStringCrmOnline = string.Format("Url={0}; Username={1}; Password={2};authtype=Office365;", crmServerUrl, userName, password);

            CrmServiceClient conn = new Microsoft.Xrm.Tooling.Connector.CrmServiceClient(connectionStringCrmOnline);

            IOrganizationService _service = (IOrganizationService)conn.OrganizationWebProxyClient != null ? (IOrganizationService)conn.OrganizationWebProxyClient : (IOrganizationService)conn.OrganizationServiceProxy;


           
            return _service;
        }

    }
}
