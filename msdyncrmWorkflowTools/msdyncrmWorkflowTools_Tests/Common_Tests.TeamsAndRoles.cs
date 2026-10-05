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
        public void TeamMembers_AreAddedAndRemoved()
        {
            var teamId = Guid.NewGuid();
            service.OnExecute = r => new OrganizationResponse();

            common.AddTeamMember(teamId, UserId);
            common.RemoveTeamMember(teamId, UserId);

            var add = (AddMembersTeamRequest)service.Executed[0];
            var remove = (RemoveMembersTeamRequest)service.Executed[1];
            Assert.AreEqual(teamId, add.TeamId);
            CollectionAssert.AreEqual(new[] { UserId }, add.MemberIds);
            Assert.AreEqual(teamId, remove.TeamId);
            CollectionAssert.AreEqual(new[] { UserId }, remove.MemberIds);
        }

        [TestMethod]
        public void IsMemberOfTeam_DependsOnTheMembership()
        {
            Assert.IsFalse(common.IsMemberOfTeam(TeamId, UserId));

            service.OnRetrieveMultiple = query => Collection(new Entity("team", TeamId));

            Assert.IsTrue(common.IsMemberOfTeam(TeamId, UserId));
        }

        [TestMethod]
        public void AddRole_AssociatesTheBusinessUnitCopyOfTheRole()
        {
            var businessUnitRoleId = Guid.NewGuid();
            SetUpRoleLookup(businessUnitRoleId, alreadyAssigned: false);

            common.AddRole(new EntityReference("team", TeamId), Guid.NewGuid());

            var call = service.Associated.Single();
            Assert.AreEqual(TeamId, call.Record.Id);
            Assert.AreEqual("teamroles_association", call.Relationship.SchemaName);
            Assert.AreEqual(businessUnitRoleId, call.Related.Single().Id);
        }

        [TestMethod]
        public void AddRole_SkipsARoleTheUserAlreadyHas()
        {
            SetUpRoleLookup(Guid.NewGuid(), alreadyAssigned: true);

            common.AddRole(new EntityReference("systemuser", UserId), Guid.NewGuid());

            Assert.AreEqual(0, service.Associated.Count);
        }

        [TestMethod]
        public void RemoveRole_DisassociatesFromTheUser()
        {
            var businessUnitRoleId = Guid.NewGuid();
            SetUpRoleLookup(businessUnitRoleId, alreadyAssigned: true);

            common.RemoveRole(new EntityReference("systemuser", UserId), Guid.NewGuid());

            var call = service.Disassociated.Single();
            Assert.AreEqual("systemuserroles_association", call.Relationship.SchemaName);
            Assert.AreEqual(businessUnitRoleId, call.Related.Single().Id);
        }

        [TestMethod]
        public void AddRole_UnknownRoleDoesNothing()
        {
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["businessunitid"] = new EntityReference("businessunit", Guid.NewGuid()) };

            common.AddRole(new EntityReference("team", TeamId), Guid.NewGuid());

            Assert.AreEqual(0, service.Associated.Count);
        }

        private void SetUpRoleLookup(Guid businessUnitRoleId, bool alreadyAssigned)
        {
            var rootRole = new EntityReference("role", Guid.NewGuid());
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["businessunitid"] = new EntityReference("businessunit", Guid.NewGuid()) };
            service.OnRetrieveMultiple = query =>
            {
                var expression = (QueryExpression)query;

                if (expression.EntityName == "role" && expression.Criteria.Conditions.Any(c => c.AttributeName == "roleid"))
                {
                    return Collection(new Entity("role", Guid.NewGuid()) { ["parentrootroleid"] = rootRole });
                }

                if (expression.EntityName == "role")
                {
                    return Collection(new Entity("role", businessUnitRoleId) { ["roleid"] = businessUnitRoleId });
                }

                return alreadyAssigned ? Collection(new Entity(expression.EntityName, Guid.NewGuid())) : Collection();
            };
        }
    }
}
