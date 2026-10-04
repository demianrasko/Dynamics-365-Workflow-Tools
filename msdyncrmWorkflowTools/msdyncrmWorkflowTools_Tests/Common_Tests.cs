using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class Common_Tests
    {
        private static readonly Guid RecordId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly Guid TeamId = Guid.NewGuid();

        private FakeOrganizationService service;
        private TraceRecorder trace;
        private Common common;

        [TestInitialize]
        public void Setup()
        {
            service = new FakeOrganizationService();
            trace = new TraceRecorder();
            common = new Common(service, trace);
        }

        #region Record URLs

        [TestMethod]
        public void ParseRecordUrl_UsesEtnWithoutAMetadataCall()
        {
            var parsedUrl = common.ParseRecordUrl($"https://org.crm.dynamics.com/main.aspx?etn=account&id={RecordId}&pagetype=entityrecord");

            Assert.AreEqual("account", parsedUrl.EntityName);
            Assert.AreEqual(0, service.Executed.Count);
        }

        [TestMethod]
        public void ParseRecordUrl_LooksUpTheEntityNameFromTheTypeCode()
        {
            service.OnExecute = r => MetadataResponse("Account");

            var parsedUrl = common.ParseRecordUrl($"https://org.crm.dynamics.com/main.aspx?etc=1&id={RecordId}");

            Assert.AreEqual("account", parsedUrl.EntityName);
            Assert.AreEqual(RecordId, parsedUrl.Id);
            Assert.IsInstanceOfType(service.Executed.Single(), typeof(RetrieveMetadataChangesRequest));
        }

        [TestMethod]
        public void GetEntityTypeCode_ReadsTheObjectTypeCode()
        {
            service.OnExecute = r => new RetrieveMetadataChangesResponse
            {
                Results = { ["EntityMetadata"] = new EntityMetadataCollection { MetadataWithTypeCode(2) } }
            };

            Assert.AreEqual(2, common.GetEntityTypeCode("contact"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void GetEntityTypeCode_UnknownEntityThrows()
        {
            service.OnExecute = r => new RetrieveMetadataChangesResponse { Results = { ["EntityMetadata"] = new EntityMetadataCollection() } };

            common.GetEntityTypeCode("new_missing");
        }

        [TestMethod]
        public void GetRecordReference_BuildsTheReference()
        {
            service.OnExecute = r => MetadataResponse("Contact");

            var reference = common.GetRecordReference($"https://org.crm.dynamics.com/main.aspx?etc=2&id={RecordId}");

            Assert.AreEqual("contact", reference.LogicalName);
            Assert.AreEqual(RecordId, reference.Id);
        }

        #endregion

        #region Sharing

        [TestMethod]
        public void ShareRecord_GrantsAccessToThePrincipal()
        {
            service.OnExecute = r => new OrganizationResponse();
            var user = new EntityReference("systemuser", UserId);

            common.ShareRecord(UrlFor("account"), user, AccessRights.ReadAccess | AccessRights.WriteAccess);

            var grant = (GrantAccessRequest)service.Executed.Single();
            Assert.AreEqual(RecordId, grant.Target.Id);
            Assert.AreEqual(user, grant.PrincipalAccess.Principal);
            Assert.AreEqual(AccessRights.ReadAccess | AccessRights.WriteAccess, grant.PrincipalAccess.AccessMask);
        }

        [TestMethod]
        public void ShareRecord_WithoutAPrincipalGrantsNothing()
        {
            common.ShareRecord(UrlFor("account"), null, AccessRights.ReadAccess);

            Assert.AreEqual(0, service.Executed.Count);
        }

        [TestMethod]
        public void UnshareRecord_RevokesThePrincipal()
        {
            service.OnExecute = r => new OrganizationResponse();
            var team = new EntityReference("team", TeamId);

            common.UnshareRecord(UrlFor("account"), team);

            var revoke = (RevokeAccessRequest)service.Executed.Single();
            Assert.AreEqual(team, revoke.Revokee);
            Assert.AreEqual(RecordId, revoke.Target.Id);
        }

        [TestMethod]
        public void ShareSecuredField_NotSecuredDoesNothing()
        {
            service.OnExecute = r => AttributeResponse(isSecured: false);

            common.ShareSecuredField(new EntityReference("account", RecordId), "name", true, true, new EntityReference("systemuser", UserId));

            Assert.AreEqual(0, service.Queries.Count);
            Assert.AreEqual(0, service.Created.Count);
            Assert.IsTrue(trace.Messages.Any(m => m.Contains("not a secured field")));
        }

        [TestMethod]
        public void ShareSecuredField_CreatesASharePerPrincipalAndSkipsNulls()
        {
            service.OnExecute = r => AttributeResponse(isSecured: true);
            var user = new EntityReference("systemuser", UserId);
            var team = new EntityReference("team", TeamId);

            common.ShareSecuredField(new EntityReference("account", RecordId), "creditlimit", true, false, user, null, team);

            Assert.AreEqual(1, service.Executed.Count, "the field metadata is read once");
            CollectionAssert.AreEqual(new[] { user, team }, service.Created.Select(e => e.GetAttributeValue<EntityReference>("principalid")).ToArray());
            Assert.IsTrue(service.Created.All(e => e.GetAttributeValue<bool>("readaccess") && !e.GetAttributeValue<bool>("updateaccess")));
            Assert.AreEqual(RecordId, service.Created[0].GetAttributeValue<EntityReference>("objectid").Id);
        }

        [TestMethod]
        public void ShareSecuredField_UpdatesAnExistingShare()
        {
            var existing = new Entity("principalobjectattributeaccess", Guid.NewGuid());
            service.OnExecute = r => AttributeResponse(isSecured: true);
            service.OnRetrieveMultiple = query => Collection(existing);

            common.ShareSecuredField(new EntityReference("account", RecordId), "creditlimit", true, true, new EntityReference("systemuser", UserId));

            Assert.AreEqual(0, service.Created.Count);
            Assert.AreSame(existing, service.Updated.Single());
            Assert.IsTrue(existing.GetAttributeValue<bool>("updateaccess"));
        }

        [TestMethod]
        public void ShareSecuredField_RemovesTheShareWhenNoAccessIsAllowed()
        {
            var existing = new Entity("principalobjectattributeaccess", Guid.NewGuid());
            service.OnExecute = r => AttributeResponse(isSecured: true);
            service.OnRetrieveMultiple = query => Collection(existing);

            common.ShareSecuredField(new EntityReference("account", RecordId), "creditlimit", false, false, new EntityReference("systemuser", UserId));

            Assert.AreEqual(existing.Id, service.Deleted.Single().Id);
            Assert.AreEqual(0, service.Updated.Count);
        }

        #endregion

        #region Multi-select option sets

        [TestMethod]
        public void GetMultiSelectOptionSet_ReturnsTheValuesOrAnEmptyCollection()
        {
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["new_colors"] = Options(2, 5) };

            CollectionAssert.AreEqual(new[] { 2, 5 }, common.GetMultiSelectOptionSet(new EntityReference("account", RecordId), "new_colors").Select(v => v.Value).ToArray());
            Assert.AreEqual(0, common.GetMultiSelectOptionSet(new EntityReference("account", RecordId), "new_sizes").Count);
        }

        [TestMethod]
        public void GetOptionSetNames_UsesTheOptionLabels()
        {
            var options = new OptionMetadataCollection { new OptionMetadata(new Label("Red", 1033), 1), new OptionMetadata(new Label("Blue", 1033), 2) };
            service.OnExecute = r => new RetrieveAttributeResponse
            {
                Results = { ["AttributeMetadata"] = new MultiSelectPicklistAttributeMetadata { OptionSet = new OptionSetMetadata(options) } }
            };

            Assert.AreEqual("Blue,Red,3", common.GetOptionSetNames("account", "new_colors", Options(2, 1, 3)));
        }

        [TestMethod]
        public void SetMultiSelectOptionSets_ReplacesTheValues()
        {
            var values = Options(1, 2);

            common.SetMultiSelectOptionSet(new EntityReference("account", RecordId), "new_colors", values, false);

            Assert.AreEqual(0, service.Retrieved.Count);
            CollectionAssert.AreEqual(new[] { 1, 2 }, Values(service.Updated.Single(), "new_colors"));
        }

        [TestMethod]
        public void SetMultiSelectOptionSets_KeepsExistingValuesWithOneRetrieve()
        {
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["new_colors"] = Options(1, 3), ["new_sizes"] = Options(7) };
            var values = new Dictionary<string, OptionSetValueCollection>
            {
                ["new_colors"] = Options(3, 4),
                ["new_sizes"] = Options(8)
            };

            common.SetMultiSelectOptionSets(new EntityReference("account", RecordId), values, true);

            Assert.AreEqual(1, service.Retrieved.Count);
            var update = service.Updated.Single();
            CollectionAssert.AreEqual(new[] { 1, 3, 4 }, Values(update, "new_colors"));
            CollectionAssert.AreEqual(new[] { 7, 8 }, Values(update, "new_sizes"));
        }

        [TestMethod]
        public void SetMultiSelectOptionSets_NothingToSetSkipsTheUpdate()
        {
            common.SetMultiSelectOptionSets(new EntityReference("account", RecordId), new Dictionary<string, OptionSetValueCollection>(), false);

            Assert.AreEqual(0, service.Updated.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void MapMultiSelectOptionSets_MismatchedAttributeCountsThrow()
        {
            common.MapMultiSelectOptionSets(new EntityReference("lead", RecordId), new[] { "a", "b" }, new EntityReference("account", RecordId), new[] { "a" }, false);
        }

        [TestMethod]
        public void MapMultiSelectOptionSets_CopiesMultiSelectFieldsAndSkipsOthers()
        {
            var target = new EntityReference("account", Guid.NewGuid());
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["new_colors"] = Options(5), ["name"] = "text" };

            common.MapMultiSelectOptionSets(new EntityReference("lead", RecordId), new[] { "new_colors", "name", "new_missing" }, target, new[] { "new_tint", "new_x", "new_y" }, false);

            var update = service.Updated.Single();
            Assert.AreEqual(target.Id, update.Id);
            CollectionAssert.AreEqual(new[] { "new_tint" }, update.Attributes.Keys.ToArray());
            CollectionAssert.AreEqual(new[] { 5 }, Values(update, "new_tint"));
        }

        #endregion

        #region Business process flows

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void GetProcessStageId_UnknownStageThrows()
        {
            common.GetProcessStageId(Guid.NewGuid(), "Close");
        }

        [TestMethod]
        public void GetProcessInstance_PicksTheInstanceOfTheProcess()
        {
            var processId = Guid.NewGuid();
            var other = Instance(Guid.NewGuid());
            var wanted = Instance(processId);
            service.OnExecute = r => InstancesResponse(other, wanted);

            Assert.AreSame(wanted, common.GetProcessInstance(new EntityReference("opportunity", RecordId), processId));
        }

        [TestMethod]
        public void GetProcessInstance_UsesTheFirstInstanceWhenInstancesHaveNoProcessId()
        {
            var first = new Entity("opportunitysalesprocess", Guid.NewGuid());
            service.OnExecute = r => InstancesResponse(first, new Entity("opportunitysalesprocess", Guid.NewGuid()));

            Assert.AreSame(first, common.GetProcessInstance(new EntityReference("opportunity", RecordId), Guid.NewGuid()));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void GetProcessInstance_NoInstanceOfTheProcessThrows()
        {
            service.OnExecute = r => InstancesResponse(Instance(Guid.NewGuid()));

            common.GetProcessInstance(new EntityReference("opportunity", RecordId), Guid.NewGuid());
        }

        [TestMethod]
        public void SetProcessStage_MovesTheInstanceToTheStage()
        {
            var processId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var instance = Instance(processId);
            service.OnRetrieveMultiple = query => Collection(new Entity("processstage", stageId));
            service.OnExecute = r => InstancesResponse(instance);
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["uniquename"] = "new_bpf" };

            common.SetProcessStage(new EntityReference("opportunity", RecordId), new EntityReference("workflow", processId), "Close");

            var update = service.Updated.Single();
            Assert.AreEqual("new_bpf", update.LogicalName);
            Assert.AreEqual(instance.Id, update.Id);
            Assert.AreEqual(stageId, update.GetAttributeValue<EntityReference>("activestageid").Id);
        }

        #endregion

        #region Paging

        [TestMethod]
        public void RetrieveAllWithFetchXml_ReadsEveryPage()
        {
            service.OnRetrieveMultiple = query => service.Queries.Count == 1
                ? Page(true, "cookie-1", new Entity("account", Guid.NewGuid()))
                : Page(false, null, new Entity("account", Guid.NewGuid()));

            var records = common.RetrieveAllWithFetchXml("<fetch><entity name='account' /></fetch>").ToList();

            Assert.AreEqual(2, records.Count);
            var second = ((FetchExpression)service.Queries[1]).Query;
            StringAssert.Contains(second, "page=\"2\"");
            StringAssert.Contains(second, "paging-cookie=\"cookie-1\"");
        }

        [TestMethod]
        public void RetrieveAllWithFetchXml_StopsPagingWhenTheCallerStops()
        {
            service.OnRetrieveMultiple = query => Page(true, "c", new Entity("account", Guid.NewGuid()), new Entity("account", Guid.NewGuid()));

            var records = common.RetrieveAllWithFetchXml("<fetch><entity name='account' /></fetch>").Take(3).ToList();

            Assert.AreEqual(3, records.Count);
            Assert.AreEqual(2, service.Queries.Count);
        }

        [TestMethod]
        public void RetrieveAllWithFetchXml_TopIsRunOnceWithoutPaging()
        {
            const string fetch = "<fetch top='5'><entity name='account' /></fetch>";
            service.OnRetrieveMultiple = query => Page(true, "c", new Entity("account", Guid.NewGuid()));

            Assert.AreEqual(1, common.RetrieveAllWithFetchXml(fetch).Count());
            Assert.AreEqual(fetch, ((FetchExpression)service.Queries.Single()).Query);
        }

        [TestMethod]
        public void ConcatenateFromQuery_JoinsFormattedValuesUpToTheTop()
        {
            service.OnRetrieveMultiple = query => Collection(
                new Entity("account") { ["revenue"] = new Money(1234.5m) },
                new Entity("account"),
                new Entity("account") { ["revenue"] = new Money(10m) },
                new Entity("account") { ["revenue"] = new Money(99m) });

            Assert.AreEqual("1,234.50; 10.00", common.ConcatenateFromQuery("<fetch><entity name='account' /></fetch>", "revenue", "; ", "N2", 2));
        }

        [TestMethod]
        public void ConcatenateFromQuery_NoValuesIsNull()
        {
            Assert.IsNull(common.ConcatenateFromQuery("<fetch><entity name='account' /></fetch>", "name", ",", string.Empty, 0));
        }

        [TestMethod]
        public void RetrieveFirstWithFetchXml_ReadsOneRecord()
        {
            var first = new Entity("account", Guid.NewGuid());
            service.OnRetrieveMultiple = query => Page(true, "c", first);

            Assert.AreSame(first, common.RetrieveFirstWithFetchXml("<fetch><entity name='account' /></fetch>"));
            StringAssert.Contains(((FetchExpression)service.Queries.Single()).Query, "count=\"1\"");
            Assert.IsNull(new Common(new FakeOrganizationService()).RetrieveFirstWithFetchXml("<fetch top='1'><entity name='account' /></fetch>"));
        }

        [TestMethod]
        public void RetrieveAllIds_ReadsEveryPage()
        {
            var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            service.OnRetrieveMultiple = query => ((QueryExpression)query).PageInfo.PageNumber == 1
                ? Page(true, "c", new Entity("account", ids[0]), new Entity("account", ids[1]))
                : Page(false, null, new Entity("account", ids[2]));

            CollectionAssert.AreEqual(ids, common.RetrieveAllIds(new QueryExpression("account")));
        }

        [TestMethod]
        public void CountRecords_AddsUpEveryPage()
        {
            service.OnRetrieveMultiple = query => ((QueryExpression)query).PageInfo.PageNumber == 1
                ? Page(true, "c", new Entity("account"), new Entity("account"))
                : Page(false, null, new Entity("account"));

            Assert.AreEqual(3, common.CountRecords(new QueryExpression("account")));
        }

        #endregion

        #region Other requests

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

        #endregion

        #region Teams and roles

        [TestMethod]
        public void TeamMembers_AreAddedAndRemoved()
        {
            var teamId = Guid.NewGuid();
            service.OnExecute = r => new OrganizationResponse();

            common.AddTeamMember(teamId, UserId);
            common.RemoveTeamMember(teamId, UserId);

            var add = (AddMembersTeamRequest)service.Executed[0];
            var remove = (RemoveMembersTeamRequest)service.Executed[1];
            Assert.AreEqual(teamId, add.TeamId);
            CollectionAssert.AreEqual(new[] { UserId }, add.MemberIds);
            Assert.AreEqual(teamId, remove.TeamId);
            CollectionAssert.AreEqual(new[] { UserId }, remove.MemberIds);
        }

        [TestMethod]
        public void IsMemberOfTeam_DependsOnTheMembership()
        {
            Assert.IsFalse(common.IsMemberOfTeam(TeamId, UserId));

            service.OnRetrieveMultiple = query => Collection(new Entity("team", TeamId));

            Assert.IsTrue(common.IsMemberOfTeam(TeamId, UserId));
        }

        [TestMethod]
        public void AddRole_AssociatesTheBusinessUnitCopyOfTheRole()
        {
            var businessUnitRoleId = Guid.NewGuid();
            SetUpRoleLookup(businessUnitRoleId, alreadyAssigned: false);

            common.AddRole(new EntityReference("team", TeamId), Guid.NewGuid());

            var call = service.Associated.Single();
            Assert.AreEqual(TeamId, call.Record.Id);
            Assert.AreEqual("teamroles_association", call.Relationship.SchemaName);
            Assert.AreEqual(businessUnitRoleId, call.Related.Single().Id);
        }

        [TestMethod]
        public void AddRole_SkipsARoleTheUserAlreadyHas()
        {
            SetUpRoleLookup(Guid.NewGuid(), alreadyAssigned: true);

            common.AddRole(new EntityReference("systemuser", UserId), Guid.NewGuid());

            Assert.AreEqual(0, service.Associated.Count);
        }

        [TestMethod]
        public void RemoveRole_DisassociatesFromTheUser()
        {
            var businessUnitRoleId = Guid.NewGuid();
            SetUpRoleLookup(businessUnitRoleId, alreadyAssigned: true);

            common.RemoveRole(new EntityReference("systemuser", UserId), Guid.NewGuid());

            var call = service.Disassociated.Single();
            Assert.AreEqual("systemuserroles_association", call.Relationship.SchemaName);
            Assert.AreEqual(businessUnitRoleId, call.Related.Single().Id);
        }

        [TestMethod]
        public void AddRole_UnknownRoleDoesNothing()
        {
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["businessunitid"] = new EntityReference("businessunit", Guid.NewGuid()) };

            common.AddRole(new EntityReference("team", TeamId), Guid.NewGuid());

            Assert.AreEqual(0, service.Associated.Count);
        }

        private void SetUpRoleLookup(Guid businessUnitRoleId, bool alreadyAssigned)
        {
            var rootRole = new EntityReference("role", Guid.NewGuid());
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["businessunitid"] = new EntityReference("businessunit", Guid.NewGuid()) };
            service.OnRetrieveMultiple = query =>
            {
                var expression = (QueryExpression)query;

                if (expression.EntityName == "role" && expression.Criteria.Conditions.Any(c => c.AttributeName == "roleid"))
                {
                    return Collection(new Entity("role", Guid.NewGuid()) { ["parentrootroleid"] = rootRole });
                }

                if (expression.EntityName == "role")
                {
                    return Collection(new Entity("role", businessUnitRoleId) { ["roleid"] = businessUnitRoleId });
                }

                return alreadyAssigned ? Collection(new Entity(expression.EntityName, Guid.NewGuid())) : Collection();
            };
        }
        #endregion

        #region Users, email, queues and settings

        [TestMethod]
        public void UserHasRole_DependsOnTheAssignment()
        {
            Assert.IsFalse(common.UserHasRole(UserId, Guid.NewGuid()));

            service.OnRetrieveMultiple = query => Collection(new Entity("role", Guid.NewGuid()));

            Assert.IsTrue(common.UserHasRole(UserId, Guid.NewGuid()));
        }

        [TestMethod]
        public void AddressEmailToTeam_SetsEveryMemberAsRecipient()
        {
            var emailId = Guid.NewGuid();
            var members = new[] { Guid.NewGuid(), Guid.NewGuid() };
            service.OnRetrieveMultiple = query => Collection(members.Select(id => new Entity("systemuser", id)).ToArray());

            Assert.AreEqual(2, common.AddressEmailToTeam(emailId, TeamId));

            var email = service.Updated.Single();
            Assert.AreEqual(emailId, email.Id);
            CollectionAssert.AreEqual(members, email.GetAttributeValue<EntityCollection>("to").Entities.Select(p => p.GetAttributeValue<EntityReference>("partyid").Id).ToArray());
        }

        [TestMethod]
        public void AddressEmailToTeam_EmptyTeamLeavesTheEmail()
        {
            Assert.AreEqual(0, common.AddressEmailToTeam(Guid.NewGuid(), TeamId));
            Assert.AreEqual(0, service.Updated.Count);
        }

        [TestMethod]
        public void SendEmailToUsersInRole_AddressesAndSends()
        {
            var emailId = Guid.NewGuid();
            service.OnRetrieveMultiple = query => Collection(new Entity("systemuser", UserId));
            service.OnExecute = r => new OrganizationResponse();

            common.SendEmailToUsersInRole(new EntityReference("role", Guid.NewGuid()), new EntityReference("email", emailId));

            Assert.AreEqual(UserId, service.Updated.Single().GetAttributeValue<EntityCollection>("to").Entities.Single().GetAttributeValue<EntityReference>("partyid").Id);
            Assert.AreEqual(emailId, ((SendEmailRequest)service.Executed.Single()).EmailId);
        }

        [TestMethod]
        public void PickFromQueue_PicksEachItemForTheWorker()
        {
            var items = new[] { new Entity("queueitem", Guid.NewGuid()), new Entity("queueitem", Guid.NewGuid()) };
            service.OnRetrieveMultiple = query => Collection(items);
            service.OnExecute = r => new OrganizationResponse();

            Assert.AreEqual(2, common.PickFromQueue(Guid.NewGuid(), UserId, true, 2));

            var picks = service.Executed.Cast<PickFromQueueRequest>().ToList();
            CollectionAssert.AreEqual(items.Select(i => i.Id).ToArray(), picks.Select(r => r.QueueItemId).ToArray());
            Assert.IsTrue(picks.All(r => r.WorkerId == UserId && r.RemoveQueueItem));
            Assert.AreEqual(2, ((QueryExpression)service.Queries.Single()).TopCount);
        }

        [TestMethod]
        public void OrganizationSettings_AreReadAndWrittenTyped()
        {
            var organization = new Entity("organization", Guid.NewGuid()) { ["maxuploadfilesize"] = 5242880 };
            service.OnRetrieveMultiple = query => Collection(organization);

            Assert.AreEqual(5242880, common.GetOrganizationSetting("maxuploadfilesize"));
            Assert.IsTrue(common.SetOrganizationSetting("maxuploadfilesize", "10485760"));

            var update = service.Updated.Single();
            Assert.AreEqual(organization.Id, update.Id);
            Assert.AreEqual(10485760, update["maxuploadfilesize"]);
        }

        [TestMethod]
        public void SetState_SendsStateAndStatus()
        {
            service.OnExecute = r => new OrganizationResponse();
            var record = new EntityReference("account", RecordId);

            common.SetState(record, 1, 2);

            var request = service.Executed.Single();
            Assert.AreEqual("SetState", request.RequestName);
            Assert.AreEqual(record, request["EntityMoniker"]);
            Assert.AreEqual(1, ((OptionSetValue)request["State"]).Value);
            Assert.AreEqual(2, ((OptionSetValue)request["Status"]).Value);
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
        #endregion

        #region Single requests

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
        #endregion

        #region AI functions

        [TestMethod]
        public void AIClassify_SendsTextAndCategories()
        {
            service.OnExecute = r => new OrganizationResponse { Results = { ["Classification"] = "Billing" } };

            Assert.AreEqual("Billing", common.AIClassify("Invoice is wrong", new[] { "Billing", "Support" }));

            var request = service.Executed.Single();
            Assert.AreEqual("AIClassify", request.RequestName);
            Assert.AreEqual("Invoice is wrong", request["Text"]);
            CollectionAssert.AreEqual(new[] { "Billing", "Support" }, (string[])request["Categories"]);
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
        #endregion

        private static string UrlFor(string entityName)
        {
            return $"https://org.crm.dynamics.com/main.aspx?etn={entityName}&id={RecordId}";
        }

        private static RetrieveMetadataChangesResponse MetadataResponse(string schemaName)
        {
            return new RetrieveMetadataChangesResponse
            {
                Results = { ["EntityMetadata"] = new EntityMetadataCollection { new EntityMetadata { SchemaName = schemaName } } }
            };
        }

        private static EntityMetadata MetadataWithTypeCode(int objectTypeCode)
        {
            // ObjectTypeCode has no public setter
            var metadata = new EntityMetadata();
            typeof(EntityMetadata).GetProperty("ObjectTypeCode").SetValue(metadata, (int?)objectTypeCode);

            return metadata;
        }

        private static EntityMetadata EntityWithAttributes(string primaryIdAttribute, params AttributeMetadata[] attributes)
        {
            // the metadata classes have no public setters for these
            var metadata = new EntityMetadata();
            typeof(EntityMetadata).GetProperty("PrimaryIdAttribute").SetValue(metadata, primaryIdAttribute);
            typeof(EntityMetadata).GetProperty("Attributes").SetValue(metadata, attributes);

            return metadata;
        }

        private static T Attribute<T>(string logicalName) where T : AttributeMetadata, new()
        {
            var attribute = new T { LogicalName = logicalName };
            typeof(AttributeMetadata).GetProperty("IsValidForCreate").SetValue(attribute, (bool?)true);
            typeof(AttributeMetadata).GetProperty("IsValidForUpdate").SetValue(attribute, (bool?)true);
            typeof(AttributeMetadata).GetProperty("IsPrimaryId").SetValue(attribute, (bool?)false);
            typeof(AttributeMetadata).GetProperty("IsPrimaryName").SetValue(attribute, (bool?)false);
            typeof(AttributeMetadata).GetProperty("AttributeTypeName").SetValue(attribute, AttributeTypeDisplayName.StringType);

            return attribute;
        }

        private static RetrieveAttributeResponse AttributeResponse(bool isSecured)
        {
            return new RetrieveAttributeResponse
            {
                Results = { ["AttributeMetadata"] = new MoneyAttributeMetadata { IsSecured = isSecured, MetadataId = Guid.NewGuid() } }
            };
        }

        private static RetrieveProcessInstancesResponse InstancesResponse(params Entity[] instances)
        {
            return new RetrieveProcessInstancesResponse { Results = { ["Processes"] = Collection(instances) } };
        }

        private static Entity Instance(Guid processId)
        {
            return new Entity("opportunitysalesprocess", Guid.NewGuid())
            {
                ["processid"] = new EntityReference("workflow", processId),
                ["name"] = "Sales process"
            };
        }

        private static EntityCollection Collection(params Entity[] entities)
        {
            return new EntityCollection(entities.ToList());
        }

        private static EntityCollection Page(bool moreRecords, string cookie, params Entity[] entities)
        {
            var page = Collection(entities);
            page.MoreRecords = moreRecords;
            page.PagingCookie = cookie;

            return page;
        }

        private static OptionSetValueCollection Options(params int[] values)
        {
            return new OptionSetValueCollection(values.Select(v => new OptionSetValue(v)).ToList());
        }

        private static int[] Values(Entity entity, string attributeName)
        {
            return entity.GetAttributeValue<OptionSetValueCollection>(attributeName).Select(v => v.Value).ToArray();
        }
    }
}
