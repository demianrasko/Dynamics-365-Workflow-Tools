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
    }
}
