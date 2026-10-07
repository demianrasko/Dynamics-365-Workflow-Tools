using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
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
        public void CalculateAggregateDate_ReadsTheDateAfterAGroupByColumn()
        {
            // the shape of upstream issue #252's query: the group-by column comes before the date
            const string fetchXml = @"<fetch aggregate='true'><entity name='contact'>
                <attribute name='parentcustomerid' groupby='true' alias='parent' />
                <attribute name='createdon' aggregate='max' alias='latest' />
                <filter><condition attribute='parentcustomerid' operator='eq' value='{PARENT_GUID}' /></filter></entity></fetch>";
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("parent") });
            var contacts = new[] { CreateContact(account), CreateContact(account) };
            var latest = contacts
                .Select(c => Service.Retrieve(EntityNames.Contact, c.Id, new ColumnSet("createdon")).GetAttributeValue<DateTime>("createdon"))
                .Max();

            Assert.AreEqual(latest, Common.CalculateAggregateDate(fetchXml, account.Id));

            var empty = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("no contacts") });
            Assert.IsNull(Common.CalculateAggregateDate(fetchXml, empty.Id));
        }

        [TestMethod]
        public void ToFilterValue_LetsQueryValuesFilterOnNumbersStatusAndLookups()
        {
            // Query Values' filter values are text; these columns need a number, a status and a GUID
            var name = UniqueName("filter");
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = name, ["numberofemployees"] = 42 });
            var contact = CreateContact(account);

            var byNumber = Common.RetrieveFirstMatch(EntityNames.Account, new[] { AttributeNames.Name },
                new KeyValuePair<string, object>(AttributeNames.Name, Common.ToFilterValue(EntityNames.Account, AttributeNames.Name, name)),
                new KeyValuePair<string, object>("numberofemployees", Common.ToFilterValue(EntityNames.Account, "numberofemployees", "42")));
            Assert.AreEqual(account.Id, byNumber?.Id);

            var byStatus = Common.RetrieveFirstMatch(EntityNames.Account, new[] { AttributeNames.Name },
                new KeyValuePair<string, object>(AttributeNames.Name, name),
                new KeyValuePair<string, object>(AttributeNames.StateCode, Common.ToFilterValue(EntityNames.Account, AttributeNames.StateCode, "0")));
            Assert.AreEqual(account.Id, byStatus?.Id);

            var byLookup = Common.RetrieveFirstMatch(EntityNames.Contact, new[] { "lastname" },
                new KeyValuePair<string, object>("parentcustomerid", Common.ToFilterValue(EntityNames.Contact, "parentcustomerid", account.Id.ToString())));
            Assert.AreEqual(contact.Id, byLookup?.Id);
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
