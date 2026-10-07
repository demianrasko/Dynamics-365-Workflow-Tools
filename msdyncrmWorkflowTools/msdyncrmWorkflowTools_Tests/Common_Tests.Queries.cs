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
    public partial class Common_Tests
    {
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
        public void CalculateAggregateDate_FillsInTheParentAndReturnsTheFirstDate()
        {
            var parentId = Guid.NewGuid();
            var date = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);
            service.OnRetrieveMultiple = query => Page(false, null, new Entity("contact") { ["latest"] = new AliasedValue("contact", "createdon", date) });

            var result = common.CalculateAggregateDate(
                "<fetch aggregate='true'><entity name='contact'><attribute name='createdon' aggregate='max' alias='latest' /><filter><condition attribute='parentcustomerid' operator='eq' value='{PARENT_GUID}' /></filter></entity></fetch>",
                parentId);

            Assert.AreEqual(date, result);
            StringAssert.Contains(((FetchExpression)service.Queries[0]).Query, parentId.ToString());
        }

        [TestMethod]
        public void CalculateAggregateDate_NoRecordIsNull()
        {
            Assert.IsNull(common.CalculateAggregateDate("<fetch><entity name='contact'><attribute name='createdon' /></entity></fetch>", Guid.NewGuid()));
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

        [TestMethod]
        public void CountRecords_CountsEveryPage()
        {
            var service = new PagingService(12345);
            var count = new Common(service).CountRecords(Common.ChildRecordsQuery("contact", "parentcustomerid", IdA));

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
        public void FirstMatch_SkipsEmptyColumnsAndFilters()
        {
            var query = Common.FirstMatchQuery("account",
                new[] { "name", string.Empty, "name" },
                new System.Collections.Generic.KeyValuePair<string, object>("accountnumber", "A-1"),
                new System.Collections.Generic.KeyValuePair<string, object>(null, "ignored"));

            Assert.AreEqual("account", query.EntityName);
            Assert.AreEqual(1, query.TopCount);
            CollectionAssert.AreEqual(new[] { "name" }, query.ColumnSet.Columns.ToArray());
            AssertCondition(query.Criteria.Conditions.Single(), "accountnumber", ConditionOperator.Equal, "A-1");
        }

        [TestMethod]
        public void FirstMatch_NullValueMatchesAnEmptyColumn()
        {
            var query = Common.FirstMatchQuery("account", new[] { "name" }, new KeyValuePair<string, object>("accountnumber", null));

            var condition = query.Criteria.Conditions.Single();
            Assert.AreEqual(ConditionOperator.Null, condition.Operator);
            Assert.AreEqual(0, condition.Values.Count);
        }

        [TestMethod]
        public void ToFilterValue_ConvertsTheTextToTheColumnsType()
        {
            // Query Values filters are typed as text; statecode = "0" used to fail (testing\Upgrade test workflows.md, 02)
            var customerId = Guid.NewGuid();
            AttributeMetadata column = null;
            service.OnExecute = r => new RetrieveAttributeResponse { Results = { ["AttributeMetadata"] = column } };

            column = new StateAttributeMetadata { LogicalName = "statecode" };
            Assert.AreEqual(0, common.ToFilterValue("account", "statecode", "0"));
            column = new IntegerAttributeMetadata { LogicalName = "numberofemployees" };
            Assert.AreEqual(25, common.ToFilterValue("account", "numberofemployees", "25"));
            Assert.IsNull(common.ToFilterValue("account", "numberofemployees", string.Empty));
            column = new MoneyAttributeMetadata { LogicalName = "creditlimit" };
            Assert.AreEqual(12.5m, common.ToFilterValue("account", "creditlimit", "12.5"));
            column = new BooleanAttributeMetadata { LogicalName = "donotemail" };
            Assert.AreEqual(true, common.ToFilterValue("account", "donotemail", "1"));
            column = new LookupAttributeMetadata(LookupFormat.None) { LogicalName = "parentcustomerid", Targets = new[] { "account", "contact" } };
            Assert.AreEqual(customerId, common.ToFilterValue("contact", "parentcustomerid", customerId.ToString()));
            column = new StringAttributeMetadata { LogicalName = "lastname" };
            Assert.AreEqual("Contact 1", common.ToFilterValue("contact", "lastname", "Contact 1"));

            var request = (RetrieveAttributeRequest)service.Executed.Last();
            Assert.AreEqual("contact", request.EntityLogicalName);
            Assert.AreEqual("lastname", request.LogicalName);
        }

        [TestMethod]
        public void RetrieveFirstMatch_ReturnsTheFirstRecordOrNull()
        {
            var first = new Entity(EntityNames.Account, Guid.NewGuid());
            service.OnRetrieveMultiple = query => Collection(first);

            var record = common.RetrieveFirstMatch(EntityNames.Account, new[] { AttributeNames.Name }, new KeyValuePair<string, object>(AttributeNames.Name, "Contoso"));

            Assert.AreSame(first, record);
            Assert.AreEqual(EntityNames.Account, ((QueryExpression)service.Queries.Single()).EntityName);

            service.OnRetrieveMultiple = query => Collection();
            Assert.IsNull(common.RetrieveFirstMatch(EntityNames.Account, new[] { AttributeNames.Name }));
        }
    }
}
