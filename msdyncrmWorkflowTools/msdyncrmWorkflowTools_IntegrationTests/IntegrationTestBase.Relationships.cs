using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    public abstract partial class IntegrationTestBase
    {
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
                var values = Read(contact, "numberofchildren", "creditlimit", "donotemail", "preferredcontactmethodcode", "birthdate");

                Assert.AreEqual(3, values.GetAttributeValue<int>("numberofchildren"));
                Assert.AreEqual(12.5m, values.GetAttributeValue<Money>("creditlimit").Value);
                Assert.IsTrue(values.GetAttributeValue<bool>("donotemail"));
                Assert.AreEqual(2, values.GetAttributeValue<OptionSetValue>("preferredcontactmethodcode").Value);
                Assert.AreEqual(new DateTime(2001, 2, 3), values.GetAttributeValue<DateTime>("birthdate").Date);
            }

            Assert.IsFalse(Read(inactive, "numberofchildren").Contains("numberofchildren"));
        }

        [TestMethod]
        public void UpdateChildRecords_CopiesAParentField()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("parent"), ["telephone1"] = "555-0100" });
            var contact = CreateContact(account);

            Common.UpdateChildRecords("contact_customer_accounts", EntityNames.Account, account.Id, "telephone1", null, "telephone2", false);

            Assert.AreEqual("555-0100", Read(contact, "telephone2").GetAttributeValue<string>("telephone2"));
        }

        [TestMethod]
        public void CountChildRecords_CountsWithAndWithoutAFilter()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("parent") });
            CreateContact(account);
            CreateContact(account);

            Assert.AreEqual(2, Common.CountChildRecords(EntityNames.Contact, "parentcustomerid", account.Id, string.Empty));
            Assert.AreEqual(1, Common.CountChildRecords(EntityNames.Contact, "parentcustomerid", account.Id,
                "<condition attribute='lastname' operator='like' value='%0' />"));
        }

        [TestMethod]
        public void OneToMany_ChildRecordsAndRelatedIds()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("parent") });
            var contacts = new[] { CreateContact(account), CreateContact(account) };

            CollectionAssert.AreEquivalent(contacts.Select(c => c.Id).ToList(), Common.GetOneToManyRelatedIds("contact_customer_accounts", account.Id));
            CollectionAssert.AreEquivalent(contacts.Select(c => c.Id).ToList(),
                Common.GetChildRecords("contact_customer_accounts", account.Id).Entities.Select(e => e.Id).ToList());
        }

        [TestMethod]
        public void ManyToMany_AssociateFindAndDisassociate()
        {
            var team = CreateTeam();
            var roleId = RoleTheUserDoesNotHave();

            Assert.AreEqual(0, Common.GetAssociations(EntityNames.Team, team.Id, EntityNames.TeamRoles, EntityNames.Role, roleId).Entities.Count);

            Common.AssociateEntity(EntityNames.Team, team.Id, "teamroles_association", EntityNames.TeamRoles, EntityNames.Role, roleId);
            Common.AssociateEntity(EntityNames.Team, team.Id, "teamroles_association", EntityNames.TeamRoles, EntityNames.Role, roleId);

            Assert.AreEqual(1, Common.GetAssociations(EntityNames.Team, team.Id, EntityNames.TeamRoles, EntityNames.Role, roleId).Entities.Count);
            CollectionAssert.AreEqual(new[] { roleId }, Common.GetManyToManyRelatedIds("teamroles_association", EntityNames.Team, team.Id));

            Common.DisassociateEntity(team, "teamroles_association", new EntityReference(EntityNames.Role, roleId));

            Assert.AreEqual(0, Common.GetManyToManyRelatedIds("teamroles_association", EntityNames.Team, team.Id).Count);
        }
    }
}
