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


            //string result = classObj.AzureTranslateText("9e244b5792ed4d6792b44603801e93cb", "Hola como te va?", "", "en");

            //string jsonresult=classObj.AzureTextAnalyticsSentiment("8c8f3ccfbad44ac4b992901b3df0f797", "Muy malo, desastrozo","en");

            /*classObj.AzureFunctionCall(@"{
                     ""topic"": ""asunto"",
                     ""fullname"": ""Demian Adolfo Raschkovan"",
                     ""email"" :""demian_Rasko@yahoo.com""
                 }",
                 "https://crmsaturday.azurewebsites.net/api/CRMSaturdayGenericWebHook?code=jgOU91LUbxxt/oQko7GRTuezpPWrNJsbOt8Nl1HykRRuOFPyJzQu7Q==");
            */ //"https://crmsaturday990f.queue.core.windows.net/crmsaturdaystoragequeue");

            // classObj.SalesLiteratureToEmail("*.*", "978CE02B-E72D-E711-80F6-5065F38B5621", "9588F65E-EA2D-E711-80F6-5065F38B5621");


            string capitalizedText = "", paddedText = "", replacedText = "", subStringText = "", regexText = "", uppercaseText = "", lowercaseText = "";
            bool regexSuccess = false;

            bool test = classObj.StringFunctions(true, "Demian", "w", true, 150, true,
                "w", "w", 150, 0, true, "w",
                ref capitalizedText, ref paddedText, ref replacedText, ref subStringText, ref regexText,
                ref uppercaseText, ref lowercaseText, ref regexSuccess);


            //classObj.UpdateChildRecords("SalesOrderDetail_Dynamicpropertyinstance", "salesorderdetail", "3BDA2E2D-6C6A-E711-8106-5065F38A1B01", "", "333", "valuestring");

            //classObj.InsertOptionValue(true, "purchaseprocess", "opportunity", "Tipo22", 22, 3082);
            //classObj.InsertOptionValue(false, "cdi_test", "opportunity", "Tipo4", 1, 3082);
            //classObj.DeleteOptionValue(true,"purchaseprocess", "opportunity", 22);
            //classObj.DeleteOptionValue(false, "cdi_test", "opportunity",  1);
            //classObj.AssociateEntity("list", new Guid("F9F76AF5-91DF-E311-B8E5-6C3BE5A8B200"), "cdi_emailsend_list", "cdi_emailsend_list", "cdi_emailsend", "5D84160C-A31C-E711-80FF-5065F38A9A01");
            //classObj.UpdateChildRecords("contact_customer_accounts", "account", "D17BAB26-98BF-E611-810A-3863BB350E28","emailaddress1", "", "emailaddress1");
            //classObj.UpdateChildRecords("contact_customer_accounts", "account", "D17BAB26-98BF-E611-810A-3863BB350E28", "new_campaa", "", "new_campaa");
            //classObj.AssociateEntity("opportunity",new Guid("D9AA2BB3-A8F0-E611-80FA-5065F38A4A21"), "opportunitycompetitors_association", "opportunitycompetitors", "competitor", "C53B2A00-57F0-E611-80FA-5065F38A4A21");
            //classObj.InsertOptionValue(true,"purchaseprocess", "opportunity", "Tipo22",22, 3082);
            // classObj.InsertOptionValue(false,"cdi_test", "opportunity", "Tipo4",1, 3082);
        }
        public static IOrganizationService GetCrmService()
        {

            const string crmServerUrl = "https://demianrasko.crm4.dynamics.com";
            const string userName = "demianrasko@demianrasko.onmicrosoft.com";
            const string password = "XXX";

             var connectionStringCrmOnline = string.Format("Url={0}; Username={1}; Password={2};authtype=Office365;", crmServerUrl, userName, password);

            CrmServiceClient conn = new Microsoft.Xrm.Tooling.Connector.CrmServiceClient(connectionStringCrmOnline);
            
            IOrganizationService _service = (IOrganizationService)conn.OrganizationWebProxyClient != null ? (IOrganizationService)conn.OrganizationWebProxyClient : (IOrganizationService)conn.OrganizationServiceProxy;


           
            return _service;
        }

    }
}
