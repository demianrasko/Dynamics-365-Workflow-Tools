using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Common_Tests
    {
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
    }
}
