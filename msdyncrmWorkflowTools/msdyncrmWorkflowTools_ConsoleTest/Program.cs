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
using System.Security;
using System.Net;

namespace msdyncrmWorkflowTools_ConsoleTest
{
    class Program
    {
        static IOrganizationService service = GetCrmService();

       // static ITracingService tracingService;
        static void Main(string[] args)
        {
            var classObj = new msdyncrmWorkflowTools_Class(service);


            classObj.DeleteRecordAuditHistory("account", "475B158C-541C-E511-80D3-3863BB347BA8");
            /*classObj.QRCode("account", "7DF24294-9EC4-E711-8116-5065F38A3A01", "www.demianrasko.com", "Demian QR Code", "www.demianrasko.com", "QrDemian.bmp");
            classObj.QRCode("account", "7DF24294-9EC4-E711-8116-5065F38A3A01", "www.demianrasko.com", "Demian QR Code", "www.demianrasko.com", "QrDemian.gif");
            classObj.QRCode("account", "7DF24294-9EC4-E711-8116-5065F38A3A01", "www.demianrasko.com", "Demian QR Code", "www.demianrasko.com", "QrDemian.png");
            */

            //EntityReference team=classObj.retrieveUserBUDefaultTeam("A292B22E-C957-4B97-BED1-EA0A504954C7");
            //string jsonresult=classObj.AzureTextAnalyticsSentiment("8c8f3ccfbad44ac4b992901b3df0f797", "Muy malo, desastrozo","en");

            /*classObj.AzureFunctionCall(@"{
                     ""topic"": ""asunto"",
                     ""fullname"": ""Demian Adolfo Raschkovan"",
                     ""email"" :""demian_Rasko@yahoo.com""
                 }",
                 "https://crmsaturday.azurewebsites.net/api/CRMSaturdayGenericWebHook?code=jgOU91LUbxxt/oQko7GRTuezpPWrNJsbOt8Nl1HykRRuOFPyJzQu7Q==");
            */ //"https://crmsaturday990f.queue.core.windows.net/crmsaturdaystoragequeue");

            // classObj.SalesLiteratureToEmail("*.*", "978CE02B-E72D-E711-80F6-5065F38B5621", "9588F65E-EA2D-E711-80F6-5065F38B5621");

            //string capitalizedText = "", paddedText = "", replacedText = "", subStringText = "", regexText = "", uppercaseText = "", lowercaseText = "";
            //bool regexSuccess = false;
            //classObj.StringFunctions(false, "Lead subject", "", false, 0, false, "", "", 50, 0, false, "", ref capitalizedText, ref paddedText, ref replacedText, ref subStringText, ref regexText, ref uppercaseText, ref lowercaseText, ref regexSuccess);


            //string json = @"{""values"": [{""Author"": ""Lisa Simpson"",""Response Date"": ""2018-02-21T08:13:34.284Z""}	],	""SurveyId"": ""5114FA48-1DE6-E711-80E3-005056B37A5C""}";
            //string jsonpath = "values[0].Autho";
            //string res = classObj.JsonParser(json, jsonpath);

            //EntityReference email = new EntityReference("email", new Guid("756EB11C-214E-E811-812A-5065F38A1B01"));
            //classObj.EntityAttachmentToEmail("%.%", "4E6F2D8F-204E-E811-812A-5065F38A1B01", email, true,false);
            //

            //classObj.UpdateChildRecords("SalesOrderDetail_Dynamicpropertyinstance", "salesorderdetail", "3BDA2E2D-6C6A-E711-8106-5065F38A1B01", "", "333", "valuestring");
            /*  EntityReference securityRoleLookup = new EntityReference("role", new Guid("EC013771-633B-E711-8104-5065F38B2601"));
              EntityReference emailTemplateLookup = new EntityReference("template", new Guid("e3b8956e-bab0-e711-810f-5065f38bf4a1"));

              classObj.SendEmailFromTemplateToUsersInRole(securityRoleLookup, emailTemplateLookup);
              */
            // classObj.SendEmailToUsersInRole(securityRoleLookup, new EntityReference("email",new Guid("B96825B7-CCB0-E711-810F-5065F38BF4A1")));

            //classObj.InsertOptionValue(true, "purchaseprocess", "opportunity", "Tipo22", 22, 3082);
            //classObj.InsertOptionValue(false, "cdi_test", "opportunity", "Tipo4", 1, 3082);
            //classObj.DeleteOptionValue(true,"purchaseprocess", "opportunity", 22);
            //classObj.DeleteOptionValue(false, "cdi_test", "opportunity",  1);
            // classObj.AssociateEntity("new_test", new Guid("612F10EE-32DB-E711-8116-5065F38BF4A1"), "new_new_test_new_test", "new_test", "new_test", "F1F924DC-32DB-E711-8116-5065F38BF4A1");
            // classObj.UpdateChildRecords("Quote_Tasks", "quote", "D19A670C-5EF1-E711-811B-5065F38A3A01", "", "2", "statecode", true);
            //classObj.UpdateChildRecords("Quote_Tasks", "quote", "D19A670C-5EF1-E711-811B-5065F38A3A01", "", "6", "statuscode", true);

            //classObj.UpdateChildRecords("account_parent_account", "account", "68665897-352C-E811-8121-5065F38A1B01", "primarycontactid", "", "primarycontactid", false);
            //classObj.AssociateEntity("opportunity",new Guid("D9AA2BB3-A8F0-E611-80FA-5065F38A4A21"), "opportunitycompetitors_association", "opportunitycompetitors", "competitor", "C53B2A00-57F0-E611-80FA-5065F38A4A21");
            //classObj.InsertOptionValue(true,"purchaseprocess", "opportunity", "Tipo22",22, 3082);
            //https://demianrasko.crm4.dynamics.com/main.aspx?etc=1&extraqs=formid%3d8448b78f-8f42-454e-8e2a-f8196b0419af&id=%7b
            // classObj.InsertOptionValue(false,"cdi_test", "opportunity", "Tipo4",1, 3082);
        }
        public static IOrganizationService GetCrmService()
        {

            const string crmServerUrl = "https://demianrasko222.crm4.dynamics.com";
            const string userName = "demianrasko222@demianrasko222.onmicrosoft.com";
            const string password = "XXXX";
            SecureString theSecureString = new NetworkCredential("", password).SecurePassword;


            var connectionStringCrmOnline = string.Format("Url={0}; Username={1}; Password={2};AuthType=Office365;", crmServerUrl, userName, password);


            CrmServiceClient crmSvc = new CrmServiceClient(userName, theSecureString, "EMEA", "org1c3835c3");

            CrmServiceClient conn = new CrmServiceClient(connectionStringCrmOnline);
            
            
            IOrganizationService _service = (IOrganizationService)conn.OrganizationWebProxyClient != null ? (IOrganizationService)conn.OrganizationWebProxyClient : (IOrganizationService)conn.OrganizationServiceProxy;


           
            return _service;
        }

    }
}
