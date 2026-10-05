using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Common_Tests
    {
        [TestMethod]
        public void SingleRequests_SendTheRightMessage()
        {
            var record = new EntityReference("incident", RecordId);
            var process = new EntityReference("workflow", Guid.NewGuid());
            var listId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            service.OnExecute = r => r is SendEmailRequest ? new SendEmailResponse { Results = { ["Subject"] = "Hello" } } : new OrganizationResponse();

            common.ApplyRoutingRule(record);
            common.SetProcess(record, process);
            common.CalculateRollupField(record, "new_total");
            common.AddListToCampaign(listId, campaignId);
            common.CopyListMembers(listId, campaignId);
            common.CopyDynamicListToStatic(listId);
            common.ResolveCase(RecordId, "Fixed", "Details");
            Assert.AreEqual("Hello", common.SendEmail(RecordId));

            var e = service.Executed;
            Assert.AreEqual(record, ((ApplyRoutingRuleRequest)e[0]).Target);
            Assert.AreEqual(process, ((SetProcessRequest)e[1]).NewProcess);
            Assert.AreEqual("new_total", ((CalculateRollupFieldRequest)e[2]).FieldName);
            Assert.AreEqual(campaignId, ((AddItemCampaignRequest)e[3]).CampaignId);
            Assert.AreEqual("list", ((AddItemCampaignRequest)e[3]).EntityName);
            Assert.AreEqual(listId, ((CopyMembersListRequest)e[4]).SourceListId);
            Assert.AreEqual(listId, ((CopyDynamicListToStaticRequest)e[5]).ListId);
            var close = (CloseIncidentRequest)e[6];
            Assert.AreEqual(5, close.Status.Value);
            Assert.AreEqual(RecordId, close.IncidentResolution.GetAttributeValue<EntityReference>("incidentid").Id);
            Assert.IsTrue(((SendEmailRequest)e[7]).IssueSend);
        }

        [TestMethod]
        public void SetLookupAndSetMoney_UpdateTheField()
        {
            var record = new EntityReference("quote", RecordId);
            var account = new EntityReference("account", Guid.NewGuid());

            common.SetLookup(record, "customerid", account);
            common.SetMoney(record, "discountamount", 12.5m);

            Assert.AreEqual(account, service.Updated[0]["customerid"]);
            Assert.AreEqual(12.5m, service.Updated[1].GetAttributeValue<Money>("discountamount").Value);
        }

        [TestMethod]
        public void QuoteRequests_CreateAndWin()
        {
            var quoteId = Guid.NewGuid();
            service.OnExecute = r => r is GenerateQuoteFromOpportunityRequest
                ? new GenerateQuoteFromOpportunityResponse { Results = { ["Entity"] = new Entity("quote", quoteId) } }
                : new OrganizationResponse();

            var quote = common.CreateQuoteFromOpportunity(RecordId);
            common.WinQuote(quote, "Won");

            Assert.AreEqual(quoteId, quote.Id);
            Assert.AreEqual(RecordId, ((GenerateQuoteFromOpportunityRequest)service.Executed[0]).OpportunityId);
            var win = (WinQuoteRequest)service.Executed[1];
            Assert.AreEqual("Won", win.QuoteClose["subject"]);
            Assert.AreEqual(quote, win.QuoteClose["quoteid"]);
        }

        [TestMethod]
        public void DeleteDisassociateAndRetrieveFirst()
        {
            var record = new EntityReference("account", RecordId);
            var related = new EntityReference("contact", Guid.NewGuid());
            var first = new Entity("contact", Guid.NewGuid());
            service.OnRetrieveMultiple = query => Collection(first, new Entity("contact", Guid.NewGuid()));

            common.DeleteRecord(record);
            common.DisassociateEntity(record, "new_account_contact", related);

            Assert.AreEqual(record, service.Deleted.Single());
            var call = service.Disassociated.Single();
            Assert.AreEqual(record, call.Record);
            Assert.AreEqual("new_account_contact", call.Relationship.SchemaName);
            Assert.AreEqual(related, call.Related.Single());
            Assert.AreSame(first, common.RetrieveFirst(new QueryExpression("contact")));
        }

        [TestMethod]
        public void SetUserSettings_UpdatesTheUsersSettings()
        {
            var userId = Guid.NewGuid();

            common.SetUserSettings(userId, 50, 0, 0, 0, 0, -1, true);

            var settings = service.Updated.Single();
            Assert.AreEqual(EntityNames.UserSettings, settings.LogicalName);
            Assert.AreEqual(userId, settings["systemuserid"]);
            Assert.AreEqual(50, settings["paginglimit"]);
        }

        [TestMethod]
        public void CalculatePrice_ExecutesTheRequest()
        {
            var quote = new EntityReference(EntityNames.Quote, RecordId);
            service.OnExecute = r => new CalculatePriceResponse();

            common.CalculatePrice(quote);

            Assert.AreEqual(quote, ((CalculatePriceRequest)service.Executed.Single()).Target);
        }
    }
}
