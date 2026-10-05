using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System.Collections.Generic;
using System.Linq;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    public abstract partial class IntegrationTestBase
    {
        [TestMethod]
        public void Queries_PageThroughFetchXmlAndQueryExpressions()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("parent") });
            var contacts = Enumerable.Range(0, 5).Select(_ => CreateContact(account)).ToList();
            var fetchXml = $@"<fetch><entity name='contact'><attribute name='lastname' /><order attribute='lastname' />
                <filter><condition attribute='parentcustomerid' operator='eq' value='{account.Id}' /></filter></entity></fetch>";

            var query = Common.FetchXmlToQueryExpression(fetchXml);
            Assert.AreEqual(EntityNames.Contact, query.EntityName);

            Assert.AreEqual(5, Common.CountRecords(Common.FetchXmlToQueryExpression(fetchXml)));
            CollectionAssert.AreEquivalent(contacts.Select(c => c.Id).ToList(), Common.RetrieveAllIds(Common.FetchXmlToQueryExpression(fetchXml)));

            // 5 records in pages of 2: three pages
            Assert.AreEqual(5, Common.RetrieveAllWithFetchXml(fetchXml, 2).Count());
            Assert.AreEqual(3, Common.RetrieveAllWithFetchXml(fetchXml.Replace("<fetch>", "<fetch top='3'>"), 2).Count());

            // the fetch is ordered by last name, so the first record is the same whichever way it is read
            var first = Common.RetrieveAllWithFetchXml(fetchXml).First().Id;
            Assert.AreEqual(first, Common.RetrieveFirstWithFetchXml(fetchXml).Id);
            Assert.AreEqual(first, Common.RetrieveFirst(Common.FetchXmlToQueryExpression(fetchXml)).Id);
        }

        [TestMethod]
        public void RetrieveFirstMatch_FindsTheRecordByItsValues()
        {
            var name = UniqueName("match");
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = name, ["accountnumber"] = "WFT-42" });

            var found = Common.RetrieveFirstMatch(EntityNames.Account, new[] { "accountnumber" },
                new KeyValuePair<string, object>(AttributeNames.Name, name));

            Assert.AreEqual(account.Id, found.Id);
            Assert.AreEqual("WFT-42", found.GetAttributeValue<string>("accountnumber"));
            Assert.IsNull(Common.RetrieveFirstMatch(EntityNames.Account, new[] { "accountnumber" },
                new KeyValuePair<string, object>(AttributeNames.Name, UniqueName("missing"))));
        }
    }
}
