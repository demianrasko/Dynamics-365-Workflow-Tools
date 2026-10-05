using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Common_Tests
    {
        [TestMethod]
        public void GetOptionSetValue_ReturnsTheValueOrZero()
        {
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["industrycode"] = new OptionSetValue(7) };

            Assert.AreEqual(7, common.GetOptionSetValue(new EntityReference("account", RecordId), "industrycode"));
            Assert.AreEqual("industrycode", service.Retrieved.Single().Columns.Single());
            Assert.AreEqual(0, common.GetOptionSetValue(new EntityReference("account", RecordId), "missingcode"));
        }

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
        public void CloneRecord_SetsTheReplacementValuesOnCreate()
        {
            var oldParent = new EntityReference("salesorder", Guid.NewGuid());
            var newParent = new EntityReference("salesorder", Guid.NewGuid());
            service.OnExecute = r => new RetrieveEntityResponse
            {
                Results = { ["EntityMetadata"] = EntityWithAttributes("salesorderdetailid", Attribute<StringAttributeMetadata>("productdescription"), Attribute<LookupAttributeMetadata>("salesorderid")) }
            };
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["productdescription"] = "Widget", ["salesorderid"] = oldParent };

            common.CloneRecord("salesorderdetail", RecordId, null, null, new Dictionary<string, object> { ["salesorderid"] = newParent, ["new_oldorderid"] = null });

            var copy = service.Created.Single();
            Assert.AreEqual("Widget", copy["productdescription"]);
            Assert.AreEqual(newParent, copy["salesorderid"]);
            Assert.IsTrue(copy.Contains("new_oldorderid") && copy["new_oldorderid"] == null);
            Assert.AreEqual(0, service.Updated.Count, "the copy is never created under the old parent and updated afterwards");
        }

        [TestMethod]
        public void CreateTeam_SendsTheTeamTypeAsAnOptionSetValue()
        {
            common.CreateTeam("Sales", 1, new EntityReference("systemuser", UserId), new EntityReference("businessunit", Guid.NewGuid()));

            var team = service.Created.Single();
            Assert.AreEqual("Sales", team["name"]);
            Assert.AreEqual(1, team.GetAttributeValue<OptionSetValue>("teamtype").Value);
        }

        [TestMethod]
        public void InsertOptionValue_GlobalOptionSetUsesTheOptionSetName()
        {
            service.OnExecute = r => new InsertOptionValueResponse();

            common.InsertOptionValue(true, "new_size", "account", "Large", 3, 1033);

            var request = (InsertOptionValueRequest)service.Executed.Single();
            Assert.AreEqual("new_size", request.OptionSetName);
            Assert.IsNull(request.EntityLogicalName);
            Assert.AreEqual(3, request.Value);
            Assert.AreEqual("Large", request.Label.LocalizedLabels[0].Label);
        }

        [TestMethod]
        public void InsertOptionValue_LocalOptionSetUsesTheAttributeAndEntity()
        {
            service.OnExecute = r => new InsertOptionValueResponse();

            common.InsertOptionValue(false, "new_size", "account", "Large", 3, 1033);

            var request = (InsertOptionValueRequest)service.Executed.Single();
            Assert.IsNull(request.OptionSetName);
            Assert.AreEqual("new_size", request.AttributeLogicalName);
            Assert.AreEqual("account", request.EntityLogicalName);
        }

        [TestMethod]
        public void DeleteOptionValue_LocalOptionSetUsesTheAttributeAndEntity()
        {
            service.OnExecute = r => new OrganizationResponse();

            common.DeleteOptionValue(false, "new_size", "account", 3);

            var request = (DeleteOptionValueRequest)service.Executed.Single();
            Assert.AreEqual("new_size", request.AttributeLogicalName);
            Assert.AreEqual("account", request.EntityLogicalName);
            Assert.AreEqual(3, request.Value);
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
        public void SalesLiteratureToEmail_AttachesEachItemToTheEmail()
        {
            var emailId = Guid.NewGuid();
            service.OnRetrieveMultiple = query => Collection(
                new Entity("salesliteratureitem") { ["title"] = "Brochure", ["filename"] = "brochure.pdf", ["documentbody"] = "QUJD", ["mimetype"] = "application/pdf" },
                new Entity("salesliteratureitem") { ["filename"] = "price list.xlsx" });

            common.SalesLiteratureToEmail("*", Guid.NewGuid(), emailId);

            Assert.AreEqual(2, service.Created.Count);
            Assert.IsTrue(service.Created.All(a => a.LogicalName == "activitymimeattachment" && a.GetAttributeValue<EntityReference>("objectid").Id == emailId));
            CollectionAssert.AreEqual(new[] { 1, 2 }, service.Created.Select(a => a.GetAttributeValue<int>("attachmentnumber")).ToArray());
            Assert.AreEqual("Brochure", service.Created[0]["subject"]);
            Assert.AreEqual("QUJD", service.Created[0]["body"]);
            Assert.IsFalse(service.Created[1].Contains("subject"));
        }

        [TestMethod]
        public void SalesLiteratureToEmail_FiltersOnTheFileNamePattern()
        {
            var salesLiteratureId = Guid.NewGuid();

            common.SalesLiteratureToEmail("price*", salesLiteratureId, Guid.NewGuid());

            var query = (QueryExpression)service.Queries.Single();
            var conditions = query.Criteria.Conditions.Concat(query.LinkEntities.SelectMany(l => l.LinkCriteria.Conditions)).ToList();
            Assert.IsTrue(conditions.Any(c => c.Values.Contains("%price%%")), "file name pattern");
            Assert.IsTrue(conditions.Any(c => c.Values.Contains(salesLiteratureId)), "sales literature id");
            Assert.AreEqual(0, service.Created.Count, "no items, nothing attached");
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
        public void GetAbsoluteUrlFromLocation_NoLocationIsNotFound()
        {
            Assert.AreEqual("URL Not found", common.GetAbsoluteUrlFromLocation(new EntityCollection()));
            Assert.AreEqual(0, service.Executed.Count);
        }

        [TestMethod]
        public void GetAbsoluteUrlFromLocation_ReturnsTheFirstLocationsUrl()
        {
            var location = new Entity("sharepointdocumentlocation", Guid.NewGuid());
            service.OnExecute = r => new RetrieveAbsoluteAndSiteCollectionUrlResponse { Results = { ["AbsoluteUrl"] = "https://sp/site/doc", ["SiteCollectionUrl"] = "https://sp" } };

            Assert.AreEqual("https://sp/site/doc", common.GetAbsoluteUrlFromLocation(Collection(location)));
            Assert.AreEqual(location.Id, ((RetrieveAbsoluteAndSiteCollectionUrlRequest)service.Executed.Single()).Target.Id);
        }
    }
}
