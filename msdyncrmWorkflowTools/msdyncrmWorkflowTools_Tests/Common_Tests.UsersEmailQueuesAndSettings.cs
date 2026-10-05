using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Common_Tests
    {
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
    }
}
