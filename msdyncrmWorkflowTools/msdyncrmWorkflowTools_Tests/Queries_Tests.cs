using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class Queries_Tests
    {
        private static readonly Guid IdA = Guid.NewGuid();
        private static readonly Guid IdB = Guid.NewGuid();

        private static void AssertCondition(ConditionExpression condition, string attribute, ConditionOperator op, params object[] values)
        {
            Assert.AreEqual(attribute, condition.AttributeName);
            Assert.AreEqual(op, condition.Operator);
            CollectionAssert.AreEqual(values, condition.Values.ToArray());
        }

        [TestMethod]
        public void Associations_JoinsThroughTheIntersectEntity()
        {
            var query = Queries.Associations("account", IdA, "new_account_contact", "contact", IdB);

            Assert.AreEqual("account", query.EntityName);
            Assert.IsTrue(query.Distinct);
            var intersect = query.LinkEntities.Single();
            Assert.AreEqual("new_account_contact", intersect.LinkToEntityName);
            Assert.AreEqual("accountid", intersect.LinkFromAttributeName);
            Assert.AreEqual("accountid", intersect.LinkToAttributeName);
            AssertCondition(intersect.LinkCriteria.Conditions.Single(), "accountid", ConditionOperator.Equal, IdA);

            var related = intersect.LinkEntities.Single();
            Assert.AreEqual("contact", related.LinkToEntityName);
            Assert.AreEqual("contactid", related.LinkFromAttributeName);
            Assert.AreEqual("ac", related.EntityAlias);
            AssertCondition(related.LinkCriteria.Conditions.Single(), "contactid", ConditionOperator.Equal, IdB);
        }

        [TestMethod]
        public void ActivityParties_FiltersByActivityAndParticipation()
        {
            var query = Queries.ActivityParties(IdA, 2);

            Assert.AreEqual("activityparty", query.EntityName);
            CollectionAssert.AreEqual(new[] { "partyid" }, query.ColumnSet.Columns.ToArray());
            AssertCondition(query.Criteria.Conditions[0], "activityid", ConditionOperator.Equal, IdA);
            AssertCondition(query.Criteria.Conditions[1], "participationtypemask", ConditionOperator.Equal, 2);
        }

        [TestMethod]
        public void DefaultTeamForUser_LinksBusinessUnitToUser()
        {
            var query = Queries.DefaultTeamForUser(IdA);

            Assert.AreEqual("team", query.EntityName);
            Assert.AreEqual("name", query.Orders.Single().AttributeName);
            AssertCondition(query.Criteria.Conditions[0], "teamtype", ConditionOperator.Equal, 0);
            AssertCondition(query.Criteria.Conditions[1], "isdefault", ConditionOperator.Equal, true);

            var businessUnit = query.LinkEntities.Single();
            Assert.AreEqual("businessunit", businessUnit.LinkToEntityName);
            Assert.AreEqual(JoinOperator.Inner, businessUnit.JoinOperator);
            var user = businessUnit.LinkEntities.Single();
            Assert.AreEqual("systemuser", user.LinkToEntityName);
            AssertCondition(user.LinkCriteria.Conditions.Single(), "systemuserid", ConditionOperator.Equal, IdA);
        }

        [TestMethod]
        public void UsersInRole_FiltersEnabledUsersByRole()
        {
            var query = Queries.UsersInRole(IdA);

            Assert.AreEqual("systemuser", query.EntityName);
            AssertCondition(query.Criteria.Conditions.Single(), "accessmode", ConditionOperator.Equal, 0);
            var role = query.LinkEntities.Single().LinkEntities.Single();
            Assert.AreEqual("role", role.LinkToEntityName);
            AssertCondition(role.LinkCriteria.Conditions.Single(), "roleid", ConditionOperator.Equal, IdA);
        }

        [TestMethod]
        public void SalesLiteratureItems_PassesTheFileNamePatternUnescaped()
        {
            var query = Queries.SalesLiteratureItems("%O'Brien & Co%", IdA);

            AssertCondition(query.Criteria.Conditions[0], "filename", ConditionOperator.Like, "%O'Brien & Co%");
            AssertCondition(query.Criteria.Conditions[1], "salesliteratureid", ConditionOperator.Equal, IdA);
        }

        [TestMethod]
        public void EntityAttachments_Notes()
        {
            var query = Queries.EntityAttachments(false, "%.pdf", IdA, 3);

            Assert.AreEqual("annotation", query.EntityName);
            Assert.AreEqual(3, query.TopCount);
            Assert.AreEqual(OrderType.Descending, query.Orders.Single().OrderType);
            AssertCondition(query.Criteria.Conditions[0], "isdocument", ConditionOperator.Equal, true);
            AssertCondition(query.Criteria.Conditions[1], "objectid", ConditionOperator.Equal, IdA);
            AssertCondition(query.Criteria.Conditions[2], "filename", ConditionOperator.Like, "%.pdf");
        }

        [TestMethod]
        public void EntityAttachments_EmailAttachmentsWithoutFilterOrTop()
        {
            var query = Queries.EntityAttachments(true, null, IdA, 0);

            Assert.AreEqual("activitymimeattachment", query.EntityName);
            Assert.IsNull(query.TopCount);
            AssertCondition(query.Criteria.Conditions.Single(), "activityid", ConditionOperator.Equal, IdA);
        }

        [TestMethod]
        public void TeamMembership_FiltersTeamAndUser()
        {
            var query = Queries.TeamMembership(IdA, IdB);

            AssertCondition(query.Criteria.Conditions.Single(), "teamid", ConditionOperator.Equal, IdA);
            var user = query.LinkEntities.Single().LinkEntities.Single();
            Assert.AreEqual("systemuser", user.LinkToEntityName);
            AssertCondition(user.LinkCriteria.Conditions.Single(), "systemuserid", ConditionOperator.Equal, IdB);
        }

        [TestMethod]
        public void QueueItems_UnassignedAndTop()
        {
            var query = Queries.QueueItems(IdA, onlyUnassigned: true, top: 5);

            Assert.AreEqual(5, query.TopCount);
            AssertCondition(query.Criteria.Conditions[0], "statecode", ConditionOperator.Equal, 0);
            AssertCondition(query.Criteria.Conditions[1], "workerid", ConditionOperator.Null);
            AssertCondition(query.Criteria.Conditions[2], "queueid", ConditionOperator.Equal, IdA);

            var all = Queries.QueueItems(IdA, onlyUnassigned: false);
            Assert.IsFalse(all.Criteria.Conditions.Any(c => c.AttributeName == "workerid"));
            Assert.IsNull(all.TopCount);
        }

        [TestMethod]
        public void ChildRecords_And_OrganizationSetting()
        {
            var children = Queries.ChildRecords("contact", "parentcustomerid", IdA);
            Assert.AreEqual("contact", children.EntityName);
            AssertCondition(children.Criteria.Conditions.Single(), "parentcustomerid", ConditionOperator.Equal, IdA);

            var setting = Queries.OrganizationSetting("maxuploadfilesize");
            Assert.AreEqual("organization", setting.EntityName);
            CollectionAssert.AreEqual(new[] { "maxuploadfilesize" }, setting.ColumnSet.Columns.ToArray());
        }

        [TestMethod]
        public void CountRecords_CountsEveryPage()
        {
            var service = new PagingService(12345);
            var count = new Common(service).CountRecords(Queries.ChildRecords("contact", "parentcustomerid", IdA));

            Assert.AreEqual(12345, count);
            Assert.AreEqual(3, service.Calls);
        }

        /// <summary>Returns <c>total</c> records in pages of the requested size.</summary>
        private sealed class PagingService : IOrganizationService
        {
            private readonly int total;
            public int Calls;

            public PagingService(int total)
            {
                this.total = total;
            }

            public EntityCollection RetrieveMultiple(QueryBase query)
            {
                Calls++;
                var paging = ((QueryExpression)query).PageInfo;
                var skip = (paging.PageNumber - 1) * paging.Count;
                var take = Math.Max(0, Math.Min(paging.Count, total - skip));
                var page = new EntityCollection(Enumerable.Range(0, take).Select(_ => new Entity("contact", Guid.NewGuid())).ToList())
                {
                    MoreRecords = skip + take < total,
                    PagingCookie = $"cookie{paging.PageNumber}"
                };

                return page;
            }

            public Guid Create(Entity entity) => throw new NotSupportedException();
            public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) => throw new NotSupportedException();
            public void Update(Entity entity) => throw new NotSupportedException();
            public void Delete(string entityName, Guid id) => throw new NotSupportedException();
            public OrganizationResponse Execute(OrganizationRequest request) => throw new NotSupportedException();
            public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotSupportedException();
            public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotSupportedException();
        }

        [TestMethod]
        public void ManyToManyRelated_MatchesThePrimaryRecordOnTheIntersect()
        {
            var query = Queries.ManyToManyRelated("contact", "contactid", "contactid", "new_account_contact", "accountid", IdA);

            Assert.AreEqual("contact", query.EntityName);
            var intersect = query.LinkEntities.Single();
            Assert.AreEqual("new_account_contact", intersect.LinkToEntityName);
            Assert.AreEqual("contactid", intersect.LinkFromAttributeName);
            Assert.AreEqual("contactid", intersect.LinkToAttributeName);
            AssertCondition(intersect.LinkCriteria.Conditions.Single(), "accountid", ConditionOperator.Equal, IdA);
        }

        [TestMethod]
        public void ChildRecordsFetchXml_AddsTheUserFilterAndEscapesValues()
        {
            var fetch = Queries.ChildRecordsFetchXml("contact", "parentcustomerid", IdA, "<condition attribute='lastname' operator='eq' value='O&apos;Brien' />");
            var xml = System.Xml.Linq.XElement.Parse(fetch);
            var conditions = xml.Element("entity").Element("filter").Elements("condition").ToList();

            Assert.AreEqual("contact", (string)xml.Element("entity").Attribute("name"));
            Assert.AreEqual(2, conditions.Count);
            Assert.AreEqual(IdA.ToString(), (string)conditions[0].Attribute("value"));
            Assert.AreEqual("O'Brien", (string)conditions[1].Attribute("value"));
        }

        [TestMethod]
        public void ChildRecordsFetchXml_WithoutFilter()
        {
            var xml = System.Xml.Linq.XElement.Parse(Queries.ChildRecordsFetchXml("contact", "parentcustomerid", IdA, null));

            Assert.AreEqual(1, xml.Element("entity").Element("filter").Elements("condition").Count());
        }

        [TestMethod]
        public void FirstMatch_SkipsEmptyColumnsAndFilters()
        {
            var query = Queries.FirstMatch("account",
                new[] { "name", string.Empty, "name" },
                new System.Collections.Generic.KeyValuePair<string, object>("accountnumber", "A-1"),
                new System.Collections.Generic.KeyValuePair<string, object>(null, "ignored"));

            Assert.AreEqual("account", query.EntityName);
            Assert.AreEqual(1, query.TopCount);
            CollectionAssert.AreEqual(new[] { "name" }, query.ColumnSet.Columns.ToArray());
            AssertCondition(query.Criteria.Conditions.Single(), "accountnumber", ConditionOperator.Equal, "A-1");
        }

        [TestMethod]
        public void MarketingListMemberships_FiltersOnTheMember()
        {
            var query = Queries.MarketingListMemberships(IdA);

            Assert.AreEqual("listmember", query.EntityName);
            CollectionAssert.AreEqual(new[] { "listid" }, query.ColumnSet.Columns.ToArray());
            AssertCondition(query.Criteria.Conditions.Single(), "entityid", ConditionOperator.Equal, IdA);
        }
    }
}
