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
