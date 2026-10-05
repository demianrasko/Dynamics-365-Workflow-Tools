using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;
using System;
using System.ServiceModel;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    /// <summary>
    /// The Dataverse AI functions (AI Builder / Copilot prompts). They use AI credits and need the functions turned on
    /// in the environment; when they aren't available the tests are inconclusive.
    /// </summary>
    public abstract partial class IntegrationTestBase
    {
        private const string AiText = "The new order arrived two days late and the box was damaged, but support replaced it quickly.";

        [TestMethod]
        public void AI_ClassifySentimentSummarizeReplyAndTranslate()
        {
            var category = AiCall(() => Common.AIClassify(AiText, new[] { "Delivery", "Billing", "Product question" }));
            Assert.IsFalse(string.IsNullOrWhiteSpace(category));

            Assert.IsFalse(string.IsNullOrWhiteSpace(AiCall(() => Common.AISentiment(AiText))));
            Assert.IsFalse(string.IsNullOrWhiteSpace(AiCall(() => Common.AISummarize(AiText))));
            Assert.IsFalse(string.IsNullOrWhiteSpace(AiCall(() => Common.AIReply(AiText))));
            Assert.IsFalse(string.IsNullOrWhiteSpace(AiCall(() => Common.AITranslate("Good morning", "es"))));
        }

        [TestMethod]
        public void AI_SummarizeRecord()
        {
            var account = Create(new Entity(EntityNames.Account)
            {
                [AttributeNames.Name] = UniqueName("summary"),
                [AttributeNames.Description] = AiText
            });

            Assert.IsFalse(string.IsNullOrWhiteSpace(AiCall(() => Common.AISummarizeRecord(account, false, null))));
        }

        /// <summary>Runs an AI function; inconclusive when the environment can't run AI functions.</summary>
        private string AiCall(Func<string> call)
        {
            try
            {
                var result = call();
                TestContext.WriteLine(result);

                return result;
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                Assert.Inconclusive($"The AI functions aren't available here: {ex.Detail.Message}");

                return null;
            }
        }
    }
}
