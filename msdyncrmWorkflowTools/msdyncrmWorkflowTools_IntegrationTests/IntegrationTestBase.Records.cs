using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Linq;

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
        public void Metadata_TypeCodesAndRecordUrls()
        {
            Assert.AreEqual(1, Common.GetEntityTypeCode(EntityNames.Account));

            var parsed = Common.ParseRecordUrl($"https://test.crm.dynamics.com/main.aspx?etc=2&id={Guid.NewGuid()}");

            Assert.AreEqual(EntityNames.Contact, parsed.EntityName);
        }

        [TestMethod]
        public void GetEnvironmentVariable_UsesTheCurrentValueOverTheDefault()
        {
            var schemaName = $"new_WftTest{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            var definition = Create(new Entity(EntityNames.EnvironmentVariableDefinition)
            {
                [AttributeNames.SchemaName] = schemaName,
                ["displayname"] = UniqueName("variable"),
                ["type"] = new OptionSetValue(100000000), // String
                [AttributeNames.DefaultValue] = "default value"
            });

            Assert.AreEqual("default value", Common.GetEnvironmentVariable(schemaName));

            Create(new Entity(EntityNames.EnvironmentVariableValue)
            {
                [AttributeNames.SchemaName] = schemaName,
                [AttributeNames.EnvironmentVariableDefinitionId] = definition,
                [AttributeNames.Value] = "current value"
            });

            Assert.AreEqual("current value", Common.GetEnvironmentVariable(schemaName));
            Assert.IsNull(Common.GetEnvironmentVariable($"new_WftMissing{Guid.NewGuid():N}"));
        }

        [TestMethod]
        public void UpdateChildRecords_ConvertsTextToEachFieldTypeAndSkipsInactiveChildren()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("parent") });
            var active = Enumerable.Range(0, 2).Select(_ => CreateContact(account)).ToList();
            var inactive = CreateContact(account);
            Common.SetState(inactive, 1, 2);

            var updated = Common.UpdateChildRecords("contact_customer_accounts", EntityNames.Account, account.Id, null, "3", "numberofchildren", true);
            Common.UpdateChildRecords("contact_customer_accounts", EntityNames.Account, account.Id, null, "12.5", "creditlimit", true);
            Common.UpdateChildRecords("contact_customer_accounts", EntityNames.Account, account.Id, null, "1", "donotemail", true);
            Common.UpdateChildRecords("contact_customer_accounts", EntityNames.Account, account.Id, null, "2", "preferredcontactmethodcode", true);
            Common.UpdateChildRecords("contact_customer_accounts", EntityNames.Account, account.Id, null, "2001-02-03", "birthdate", true);

            Assert.AreEqual(2, updated);

            foreach (var contact in active)
            {
                var values = Service.Retrieve(contact.LogicalName, contact.Id,
                    new ColumnSet("numberofchildren", "creditlimit", "donotemail", "preferredcontactmethodcode", "birthdate"));

                Assert.AreEqual(3, values.GetAttributeValue<int>("numberofchildren"));
                Assert.AreEqual(12.5m, values.GetAttributeValue<Money>("creditlimit").Value);
                Assert.IsTrue(values.GetAttributeValue<bool>("donotemail"));
                Assert.AreEqual(2, values.GetAttributeValue<OptionSetValue>("preferredcontactmethodcode").Value);
                Assert.AreEqual(new DateTime(2001, 2, 3), values.GetAttributeValue<DateTime>("birthdate").Date);
            }

            Assert.IsFalse(Service.Retrieve(inactive.LogicalName, inactive.Id, new ColumnSet("numberofchildren")).Contains("numberofchildren"));
        }

        [TestMethod]
        public void UpdateChildRecords_CopiesAParentField()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("parent"), ["telephone1"] = "555-0100" });
            var contact = CreateContact(account);

            Common.UpdateChildRecords("contact_customer_accounts", EntityNames.Account, account.Id, "telephone1", null, "telephone2", false);

            Assert.AreEqual("555-0100", Service.Retrieve(contact.LogicalName, contact.Id, new ColumnSet("telephone2")).GetAttributeValue<string>("telephone2"));
        }

        [TestMethod]
        public void Teams_CreateAddCheckAndRemoveAMember()
        {
            var teamId = Common.CreateTeam(UniqueName("team"), 0, new EntityReference(EntityNames.SystemUser, UserId),
                new EntityReference(EntityNames.BusinessUnit, BusinessUnitId));
            DeleteAfterTest(new EntityReference(EntityNames.Team, teamId));

            Assert.IsFalse(Common.IsMemberOfTeam(teamId, UserId));


            Common.AddTeamMember(teamId, UserId);
            Assert.IsTrue(Common.IsMemberOfTeam(teamId, UserId));

            Common.RemoveTeamMember(teamId, UserId);
            Assert.IsFalse(Common.IsMemberOfTeam(teamId, UserId));
        }

        [TestMethod]
        public void CountRecords_CountsTheChildren()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("parent") });
            CreateContact(account);
            CreateContact(account);

            Assert.AreEqual(2, Common.CountChildRecords(EntityNames.Contact, "parentcustomerid", account.Id, string.Empty));
            Assert.AreEqual(1, Common.CountChildRecords(EntityNames.Contact, "parentcustomerid", account.Id,
                "<condition attribute='lastname' operator='like' value='%0' />"));
        }

        private int contactCount;

        private EntityReference CreateContact(EntityReference account)
        {
            return Create(new Entity(EntityNames.Contact)
            {
                ["lastname"] = $"{UniqueName("contact")} {contactCount++}",
                ["parentcustomerid"] = account
            });
        }

        private int StateOf(EntityReference record)
        {
            return Service.Retrieve(record.LogicalName, record.Id, new ColumnSet(AttributeNames.StateCode))
                .GetAttributeValue<OptionSetValue>(AttributeNames.StateCode).Value;
        }
    }
}
