using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    public abstract partial class IntegrationTestBase
    {
        [TestMethod]
        public void Connects()
        {
            Assert.AreNotEqual(Guid.Empty, UserId);
        }

        [TestMethod]
        public void SetState_DeactivatesAndReactivatesARecord()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("account") });

            Common.SetState(account, 1, 2);
            Assert.AreEqual(1, StateOf(account));

            Common.SetState(account, 0, 1);
            Assert.AreEqual(0, StateOf(account));
        }

        [TestMethod]
        public void SetLookupAndSetMoney_UpdateTheFields()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("account") });
            var contact = CreateContact(null);

            Common.SetLookup(account, "primarycontactid", contact);
            Common.SetMoney(account, "creditlimit", 1234.56m);

            var values = Read(account, "primarycontactid", "creditlimit");
            Assert.AreEqual(contact.Id, values.GetAttributeValue<EntityReference>("primarycontactid").Id);
            Assert.AreEqual(1234.56m, values.GetAttributeValue<Money>("creditlimit").Value);
        }

        [TestMethod]
        public void SerializeRecord_WritesTheRecordAsJson()
        {
            var name = UniqueName("json");
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = name, ["numberofemployees"] = 12 });

            var record = (JObject)JObject.Parse(Common.SerializeRecord(account))[EntityNames.Account];

            Assert.AreEqual(name, (string)record[AttributeNames.Name]);
            Assert.AreEqual(12, (int)record["numberofemployees"]);
        }

        [TestMethod]
        public void DeleteRecord_DeletesIt()
        {
            var account = new EntityReference(EntityNames.Account,
                Service.Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("delete") }));

            Common.DeleteRecord(account);

            Assert.IsNull(Common.RetrieveFirstMatch(EntityNames.Account, new[] { AttributeNames.Name },
                new KeyValuePair<string, object>("accountid", account.Id)));
        }

        [TestMethod]
        public void CloneRecord_CopiesValuesAndSkipsIgnoredFields()
        {
            var name = UniqueName("clone");
            var source = Create(new Entity(EntityNames.Account)
            {
                [AttributeNames.Name] = name,
                ["accountnumber"] = "WFT-1",
                ["telephone1"] = "555-0101",
                ["numberofemployees"] = 7
            });
            Common.SetState(source, 1, 2);

            var clone = new EntityReference(EntityNames.Account, Common.CloneRecord(EntityNames.Account, source.Id, "accountnumber", "Copy of ",
                new Dictionary<string, object> { ["telephone1"] = "555-0199" }));
            DeleteAfterTest(clone);

            var values = Read(clone, AttributeNames.Name, "accountnumber", "telephone1", "numberofemployees", AttributeNames.StateCode);
            Assert.AreEqual($"Copy of {name}", values.GetAttributeValue<string>(AttributeNames.Name));
            Assert.IsNull(values.GetAttributeValue<string>("accountnumber"));
            Assert.AreEqual("555-0199", values.GetAttributeValue<string>("telephone1"));
            Assert.AreEqual(7, values.GetAttributeValue<int>("numberofemployees"));

            // status and status reason aren't copied: a copy of an inactive record starts active
            Assert.AreEqual(0, values.GetAttributeValue<OptionSetValue>(AttributeNames.StateCode).Value);
        }

        [TestMethod]
        public void DeleteRecordAuditHistory_RunsForARecord()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("audit") });
            Service.Update(new Entity(account.LogicalName, account.Id) { ["telephone1"] = "555-0102" });

            Common.DeleteRecordAuditHistory(account.LogicalName, account.Id);
        }

        [TestMethod]
        public void MultiSelectOptionSets_SetGetKeepMapAndName()
        {
            var options = TestOptions(TestChoices);
            var first = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("multi") });
            var second = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("multi") });

            Assert.AreEqual(0, Common.GetMultiSelectOptionSet(first, TestChoices).Count);

            Common.SetMultiSelectOptionSet(first, TestChoices, Options(options[0]), false);
            Common.SetMultiSelectOptionSet(first, TestChoices, Options(options[1]), true);
            CollectionAssert.AreEquivalent(new[] { options[0], options[1] }, Values(Common.GetMultiSelectOptionSet(first, TestChoices)));

            Common.SetMultiSelectOptionSets(first, new Dictionary<string, OptionSetValueCollection> { [TestChoices] = Options(options[2]) }, false);
            CollectionAssert.AreEquivalent(new[] { options[2] }, Values(Common.GetMultiSelectOptionSet(first, TestChoices)));

            Common.SetMultiSelectOptionSet(second, TestChoices, Options(options[0]), false);
            Common.MapMultiSelectOptionSets(first, new[] { TestChoices }, second, new[] { TestChoices }, true);
            CollectionAssert.AreEquivalent(new[] { options[0], options[2] }, Values(Common.GetMultiSelectOptionSet(second, TestChoices)));

            Assert.AreEqual("One,Three", Common.GetOptionSetNames(EntityNames.Account, TestChoices, Options(options[0], options[2])));
        }
    }
}
