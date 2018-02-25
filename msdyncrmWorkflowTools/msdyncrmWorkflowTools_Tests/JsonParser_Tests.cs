using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;
using Microsoft.Xrm.Tooling.Connector;

namespace msdyncrmWorkflowTools_Tests
{
    
    [TestClass]
    public class JsonParser_Tests
    {
        CrmService objService = new CrmService();
        

        [TestMethod]
        public void JsonParser1()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);

            string json = @"{""deviceid"":""G7BF20DB2060"",""readingtype"":""Status"",""reading"":""Status"",""eventtoken"":null,""description"":""Engine speed"",""parameters"":{""VehicleName"":""Jeep Wrangler"",""VehicleSerialNumber"":""G7BF20DB2060"",""VIN"":""1J4FA69S74P704699"",""Date"":""10 / 2 / 2017 3:35:48 AM"",""DiagnosticName"":""Engine speed"",""DiagnosticCode"":""107"",""SourceName"":"" * *Go"",""Value"":""1363"",""Unit"":""Engine.UnitOfMeasureRevolutionsPerMinute""},""time"":""2017 - 10 - 02T03: 37:18.863Z""}";
            string jsonpath = "parameters.DiagnosticCode";
            string res=classObj.JsonParser(json, jsonpath);

            Assert.AreEqual(res,"107");
        }
        [TestMethod]
        public void JsonParser2()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            string json = @"{""values"": [{""Author"": ""Lisa Simpson"",""Response Date"": ""2018-02-21T08:13:34.284Z""}	],	""SurveyId"": ""5114FA48-1DE6-E711-80E3-005056B37A5C""}";
            string jsonpath = "values[0].Author";
            string res = classObj.JsonParser(json, jsonpath);

            Assert.AreEqual(res, "Lisa Simpson");
        }
        [TestMethod]
        public void JsonParser3()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            string json = @"{""values"": [{""Author"": ""Lisa Simpson"",""Response Date"": ""2018-02-21T08:13:34.284Z""}	],	""SurveyId"": ""5114FA48-1DE6-E711-80E3-005056B37A5C""}";
            string jsonpath = "values[0]";
            string res = classObj.JsonParser(json, jsonpath);

            Assert.AreEqual(res, "{\r\n  \"Author\": \"Lisa Simpson\",\r\n  \"Response Date\": \"2018-02-21T08:13:34.284Z\"\r\n}");
        }
        [TestMethod]
        public void JsonParser4()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            string json = @"{""values"": [{""Author"": ""Lisa Simpson"",""Response Date"": ""2018-02-21T08:13:34.284Z""}	],	""SurveyId"": ""5114FA48-1DE6-E711-80E3-005056B37A5C""}";
            string jsonpath = "values";
            string res = classObj.JsonParser(json, jsonpath);

            Assert.AreEqual(res, "[\r\n  {\r\n    \"Author\": \"Lisa Simpson\",\r\n    \"Response Date\": \"2018-02-21T08:13:34.284Z\"\r\n  }\r\n]");
        }
        [TestMethod]
        public void JsonParser5()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            string json = @"{""values"": [{""Author"": ""Lisa Simpson"",""Response Date"": ""2018-02-21T08:13:34.284Z""}	],	""SurveyId"": ""5114FA48-1DE6-E711-80E3-005056B37A5C""}";
            string jsonpath = "$";
            string res = classObj.JsonParser(json, jsonpath);

            Assert.AreEqual(res, "{\r\n  \"values\": [\r\n    {\r\n      \"Author\": \"Lisa Simpson\",\r\n      \"Response Date\": \"2018-02-21T08:13:34.284Z\"\r\n    }\r\n  ],\r\n  \"SurveyId\": \"5114FA48-1DE6-E711-80E3-005056B37A5C\"\r\n}");
        }
        [TestMethod]
        public void JsonParser6()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            string json = @"{""values"": [{""Author"": ""Lisa Simpson"",""Response Date"": ""2018-02-21T08:13:34.284Z""}	],	""SurveyId"": ""5114FA48-1DE6-E711-80E3-005056B37A5C""}";
            string jsonpath = "values[0].['Response Date']";
            string res = classObj.JsonParser(json, jsonpath);

            Assert.AreEqual(res, "21/02/2018 8:13:34");
        }

        [TestMethod]
        public void JsonParser7()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            string json = @"{""values"": [{""Author"": ""Lisa Simpson"",""Response Date"": ""2018-02-21T08:13:34.284Z""}	],	""SurveyId"": ""5114FA48-1DE6-E711-80E3-005056B37A5C""}";
            string jsonpath = "";
            string res = classObj.JsonParser(json, jsonpath);

            Assert.AreEqual(res, "{\r\n  \"values\": [\r\n    {\r\n      \"Author\": \"Lisa Simpson\",\r\n      \"Response Date\": \"2018-02-21T08:13:34.284Z\"\r\n    }\r\n  ],\r\n  \"SurveyId\": \"5114FA48-1DE6-E711-80E3-005056B37A5C\"\r\n}");
        }
        [TestMethod]
        public void JsonParser8()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            string json = @"{""values"": [{""Author"": ""Lisa Simpson"",""Response Date"": ""2018-02-21T08:13:34.284Z""}	],	""SurveyId"": ""5114FA48-1DE6-E711-80E3-005056B37A5C""}";
            string jsonpath = null;
            string res = classObj.JsonParser(json, jsonpath);

            Assert.AreEqual(res, "{\r\n  \"values\": [\r\n    {\r\n      \"Author\": \"Lisa Simpson\",\r\n      \"Response Date\": \"2018-02-21T08:13:34.284Z\"\r\n    }\r\n  ],\r\n  \"SurveyId\": \"5114FA48-1DE6-E711-80E3-005056B37A5C\"\r\n}");
        }


    }
}

