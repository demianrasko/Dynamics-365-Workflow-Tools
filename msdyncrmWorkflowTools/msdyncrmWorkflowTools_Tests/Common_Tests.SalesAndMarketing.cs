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
        public void CreateOpportunityProduct_CreatesTheLineWithTheGivenValues()
        {
            var opportunity = new EntityReference("opportunity", Guid.NewGuid());
            var product = new EntityReference("product", Guid.NewGuid());
            var unit = new EntityReference("uom", Guid.NewGuid());

            var id = common.CreateOpportunityProduct(opportunity, product, unit, 2.5m);

            var line = service.Created.Single();
            Assert.AreNotEqual(Guid.Empty, id);
            Assert.AreEqual("opportunityproduct", line.LogicalName);
            Assert.AreEqual(opportunity, line.GetAttributeValue<EntityReference>("opportunityid"));
            Assert.AreEqual(product, line.GetAttributeValue<EntityReference>("productid"));
            Assert.AreEqual(unit, line.GetAttributeValue<EntityReference>("uomid"));
            Assert.AreEqual(2.5m, line.GetAttributeValue<decimal>("quantity"));
        }

        [TestMethod]
        public void CreateOpportunityProduct_SendsOnlyTheLookupIds()
        {
            var opportunity = new EntityReference("opportunity", Guid.NewGuid()) { Name = "Big deal" };

            common.CreateOpportunityProduct(opportunity, new EntityReference("product", Guid.NewGuid()) { Name = "Widget" }, new EntityReference("uom", Guid.NewGuid()), 1m);

            var line = service.Created.Single();
            Assert.IsNull(line.GetAttributeValue<EntityReference>("opportunityid").Name);
            Assert.IsNull(line.GetAttributeValue<EntityReference>("productid").Name);
            Assert.AreNotSame(opportunity, line["opportunityid"]);
        }

        [TestMethod]
        public void RecalculateGoal_TargetsTheGoal()
        {
            var goalId = Guid.NewGuid();
            service.OnExecute = r => new OrganizationResponse();

            common.RecalculateGoal(goalId);

            var request = (RecalculateRequest)service.Executed.Single();
            Assert.AreEqual(new EntityReference("goal", goalId), request.Target);
        }

        [TestMethod]
        public void IsMemberOfMarketingList_DependsOnTheMembership()
        {
            Assert.IsFalse(common.IsMemberOfMarketingList(Guid.NewGuid(), RecordId));

            service.OnRetrieveMultiple = query => Collection(new Entity("listmember", Guid.NewGuid()));

            Assert.IsTrue(common.IsMemberOfMarketingList(Guid.NewGuid(), RecordId));
        }

        [TestMethod]
        public void AddToMarketingList_AddsTheMemberToTheList()
        {
            var listId = Guid.NewGuid();
            service.OnExecute = r => new OrganizationResponse();

            common.AddToMarketingList(listId, new EntityReference("lead", RecordId));

            var request = (AddMemberListRequest)service.Executed.Single();
            Assert.AreEqual(listId, request.ListId);
            Assert.AreEqual(RecordId, request.EntityId);
        }

        [TestMethod]
        public void RemoveFromAllMarketingLists_RemovesTheRecordFromEachList()
        {
            var lists = new[] { Guid.NewGuid(), Guid.NewGuid() };
            service.OnExecute = r => new OrganizationResponse();
            service.OnRetrieveMultiple = query => Collection(lists.Select(l => new Entity("listmember", Guid.NewGuid()) { ["listid"] = new EntityReference("list", l) }).ToArray());

            var removed = common.RemoveFromAllMarketingLists(new EntityReference("contact", RecordId));

            Assert.AreEqual(2, removed);
            CollectionAssert.AreEqual(lists, service.Executed.Cast<RemoveMemberListRequest>().Select(r => r.ListId).ToArray());
            Assert.IsTrue(service.Executed.Cast<RemoveMemberListRequest>().All(r => r.EntityId == RecordId));
        }

        [TestMethod]
        public void RemoveFromAllMarketingLists_NoMembershipsRemovesNothing()
        {
            Assert.AreEqual(0, common.RemoveFromAllMarketingLists(new EntityReference("lead", RecordId)));
            Assert.AreEqual(0, service.Executed.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void RemoveFromAllMarketingLists_OtherRecordTypesThrow()
        {
            common.RemoveFromAllMarketingLists(new EntityReference("incident", RecordId));
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
        public void CalculatePrice_ExecutesTheRequest()
        {
            var quote = new EntityReference(EntityNames.Quote, RecordId);
            service.OnExecute = r => new CalculatePriceResponse();

            common.CalculatePrice(quote);

            Assert.AreEqual(quote, ((CalculatePriceRequest)service.Executed.Single()).Target);
        }

        [TestMethod]
        public void QualifyLead_UsesTheBaseCurrencyAndTheExistingAccount()
        {
            var currency = new EntityReference("transactioncurrency", Guid.NewGuid());
            var account = new EntityReference("account", Guid.NewGuid());
            service.OnRetrieveMultiple = query => Collection(new Entity("organization", Guid.NewGuid()) { ["basecurrencyid"] = currency });
            service.OnExecute = r => new OrganizationResponse();

            common.QualifyLead(new EntityReference("lead", RecordId), false, false, true, account, new EntityReference("contact", Guid.NewGuid()), 3);

            var request = (QualifyLeadRequest)service.Executed.Single();
            Assert.AreEqual(RecordId, request.LeadId.Id);
            Assert.IsTrue(request.CreateOpportunity);
            Assert.AreEqual(currency, request.OpportunityCurrencyId);
            Assert.AreEqual(account.Id, request.OpportunityCustomerId.Id);
            Assert.AreEqual(3, request.Status.Value);
        }

        [TestMethod]
        public void MarketingListMemberships_FiltersOnTheMember()
        {
            var query = Common.MarketingListMembershipsQuery(IdA);

            Assert.AreEqual("listmember", query.EntityName);
            CollectionAssert.AreEqual(new[] { "listid" }, query.ColumnSet.Columns.ToArray());
            AssertCondition(query.Criteria.Conditions.Single(), "entityid", ConditionOperator.Equal, IdA);
        }

        [TestMethod]
        public void CreateOpportunityProduct_NeedsTheOpportunityProductAndUnit()
        {
            var lookup = new EntityReference("product", Guid.NewGuid());

            Assert.AreEqual("Opportunity, Existing Product and Unit are required.",
                Assert.ThrowsException<InvalidPluginExecutionException>(() => common.CreateOpportunityProduct(lookup, null, lookup, 1)).Message);
            Assert.AreEqual(0, service.Created.Count);
        }

        [TestMethod]
        public void RecalculateGoal_UsesTheLookupOrTheGuid()
        {
            var goal = new EntityReference(EntityNames.Goal, Guid.NewGuid());
            var guid = Guid.NewGuid();
            service.OnExecute = r => new OrganizationResponse();

            common.RecalculateGoal(goal, guid.ToString());
            common.RecalculateGoal(null, $" {guid} ");

            CollectionAssert.AreEqual(new[] { goal.Id, guid }, service.Executed.Cast<RecalculateRequest>().Select(r => r.Target.Id).ToArray());
            Assert.AreEqual("Goal or Goal Guid is required.", Assert.ThrowsException<InvalidPluginExecutionException>(() => common.RecalculateGoal(null, " ")).Message);
            Assert.AreEqual("Goal Guid 'x' is not a valid GUID.", Assert.ThrowsException<InvalidPluginExecutionException>(() => common.RecalculateGoal(null, "x")).Message);
        }
    }
}
