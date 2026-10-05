using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Common_Tests
    {
        [TestMethod]
        public void AIClassify_SendsTextAndCategories()
        {
            service.OnExecute = r => new OrganizationResponse { Results = { ["Classification"] = "Billing" } };

            Assert.AreEqual("Billing", common.AIClassify("Invoice is wrong", new[] { "Billing", "Support" }));

            var request = service.Executed.Single();
            Assert.AreEqual("AIClassify", request.RequestName);
            Assert.AreEqual("Invoice is wrong", request["Text"]);
            CollectionAssert.AreEqual(new[] { "Billing", "Support" }, (string[])request["Categories"]);
            Assert.AreEqual(false, request["AllowMultipleCategories"]);
        }

        [TestMethod]
        public void AIClassify_RetriesWithoutAllowMultipleCategoriesWhereItIsUnknown()
        {
            service.OnExecute = r => r.Parameters.Contains("AllowMultipleCategories")
                ? throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault { Message = "Unrecognized request parameter: AllowMultipleCategories" },
                    "Unrecognized request parameter: AllowMultipleCategories")
                : new OrganizationResponse { Results = { ["Classification"] = "Billing" } };

            Assert.AreEqual("Billing", common.AIClassify("Invoice is wrong", new[] { "Billing", "Support" }));
            Assert.AreEqual(2, service.Executed.Count);
            Assert.IsFalse(service.Executed.Last().Parameters.Contains("AllowMultipleCategories"));
        }

        [TestMethod]
        public void AIFunctions_UseTheirMessageAndResult()
        {
            var results = new Dictionary<string, string> { ["AIReply"] = "PreparedResponse", ["AISentiment"] = "AnalyzedSentiment", ["AISummarize"] = "SummarizedText" };
            service.OnExecute = r => new OrganizationResponse { Results = { [results[r.RequestName]] = $"{r.RequestName} result" } };

            Assert.AreEqual("AIReply result", common.AIReply("text"));
            Assert.AreEqual("AISentiment result", common.AISentiment("text"));
            Assert.AreEqual("AISummarize result", common.AISummarize("text"));
            Assert.IsTrue(service.Executed.All(r => (string)r["Text"] == "text"));
        }

        [TestMethod]
        public void AITranslate_SendsTheTargetLanguageOnlyWhenGiven()
        {
            service.OnExecute = r => new OrganizationResponse { Results = { ["TranslatedText"] = "Bonjour" } };

            Assert.AreEqual("Bonjour", common.AITranslate("Hello", " fr "));
            Assert.AreEqual("fr", service.Executed[0]["TargetLanguage"]);

            common.AITranslate("Hello", string.Empty);
            Assert.IsFalse(service.Executed[1].Parameters.Contains("TargetLanguage"));
        }

        [TestMethod]
        public void AISummarizeRecord_SendsTheRecordAndOptions()
        {
            service.OnExecute = r => new OrganizationResponse { Results = { ["SummarizedText"] = "Summary" } };

            Assert.AreEqual("Summary", common.AISummarizeRecord(new EntityReference("opportunity", RecordId), true, "{\"channel\":\"email\"}"));

            var request = service.Executed.Single();
            Assert.AreEqual("opportunity", request["EntityLogicalName"]);
            Assert.AreEqual(RecordId.ToString(), request["Id"]);
            Assert.AreEqual(true, request["IsMergedCatchupAndSummary"]);
            Assert.AreEqual("{\"channel\":\"email\"}", request["RecordContext"]);
        }

        [TestMethod]
        public void AIFunctions_MissingResultThrows()
        {
            service.OnExecute = r => new OrganizationResponse();

            try
            {
                common.AISummarize("text");
                Assert.Fail("Expected InvalidPluginExecutionException");
            }
            catch (InvalidPluginExecutionException ex)
            {
                Assert.AreEqual("AISummarize response missing 'SummarizedText'.", ex.Message);
            }
        }
    }
}
