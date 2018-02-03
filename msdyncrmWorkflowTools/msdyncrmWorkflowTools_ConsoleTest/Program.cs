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

       // static ITracingService tracingService;
        static void Main(string[] args)
        {
            var classObj = new msdyncrmWorkflowTools_Class(service);

            /*classObj.QRCode("account", "7DF24294-9EC4-E711-8116-5065F38A3A01", "www.demianrasko.com", "Demian QR Code", "www.demianrasko.com", "QrDemian.bmp");
            classObj.QRCode("account", "7DF24294-9EC4-E711-8116-5065F38A3A01", "www.demianrasko.com", "Demian QR Code", "www.demianrasko.com", "QrDemian.gif");
            classObj.QRCode("account", "7DF24294-9EC4-E711-8116-5065F38A3A01", "www.demianrasko.com", "Demian QR Code", "www.demianrasko.com", "QrDemian.png");
            */
            /*
            TimeSpan difference=new TimeSpan();
            int DayOfWeek = 0;
            int DayOfYear = 0;
            int Day = 0;
            int Month = 0;
            int Year = 0;
            int WeekOfYear = 0;
            classObj.DateFunctions(DateTime.Now, new DateTime(2018, 01, 01), ref difference,
                ref DayOfWeek, ref  DayOfYear, ref  Day, ref  Month, ref  Year, ref  WeekOfYear);
                */

            //var details = JObject.Parse(jsonData);

            //string json = @"{""deviceid"":""G7BF20DB2060"",""readingtype"":""Status"",""reading"":""Status"",""eventtoken"":null,""description"":""Engine speed"",""parameters"":{""VehicleName"":""Jeep Wrangler"",""VehicleSerialNumber"":""G7BF20DB2060"",""VIN"":""1J4FA69S74P704699"",""Date"":""10 / 2 / 2017 3:35:48 AM"",""DiagnosticName"":""Engine speed"",""DiagnosticCode"":""107"",""SourceName"":"" * *Go"",""Value"":""1363"",""Unit"":""Engine.UnitOfMeasureRevolutionsPerMinute""},""time"":""2017 - 10 - 02T03: 37:18.863Z""}";
            //string jsonpath = "parameters.DiagnosticCode";
            //string res=classObj.JsonParser(json, jsonpath);

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
            /*

                        string capitalizedText = "", paddedText = "", replacedText = "", subStringText = "", regexText = "", uppercaseText = "", lowercaseText = "";
                        bool regexSuccess = false;

                        bool test = classObj.StringFunctions(true, "Demian", "w", true, 150, true,
                            "w", "w", 150, 0, true, "w",
                            ref capitalizedText, ref paddedText, ref replacedText, ref subStringText, ref regexText,
                            ref uppercaseText, ref lowercaseText, ref regexSuccess);
                            */


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
            //classObj.AssociateEntity("list", new Guid("F9F76AF5-91DF-E311-B8E5-6C3BE5A8B200"), "cdi_emailsend_list", "cdi_emailsend_list", "cdi_emailsend", "5D84160C-A31C-E711-80FF-5065F38A9A01");
            classObj.UpdateChildRecords("contact_customer_accounts", "account", "6F1BD95D-DEBE-E711-8114-5065F38A3A01", "", "100000000", "new_quotestatus");
            //classObj.UpdateChildRecords("contact_customer_accounts", "account", "D17BAB26-98BF-E611-810A-3863BB350E28", "new_campaa", "", "new_campaa");
            //classObj.AssociateEntity("opportunity",new Guid("D9AA2BB3-A8F0-E611-80FA-5065F38A4A21"), "opportunitycompetitors_association", "opportunitycompetitors", "competitor", "C53B2A00-57F0-E611-80FA-5065F38A4A21");
            //classObj.InsertOptionValue(true,"purchaseprocess", "opportunity", "Tipo22",22, 3082);
            //https://demianrasko.crm4.dynamics.com/main.aspx?etc=1&extraqs=formid%3d8448b78f-8f42-454e-8e2a-f8196b0419af&id=%7b
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
