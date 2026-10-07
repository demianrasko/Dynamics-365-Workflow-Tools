using Microsoft.Crm.Sdk.Messages;
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
        public void CreateTeam_SendsTheTeamTypeAsAnOptionSetValue()
        {
            common.CreateTeam("Sales", 1, new EntityReference("systemuser", UserId), new EntityReference("businessunit", Guid.NewGuid()));

            var team = service.Created.Single();
            Assert.AreEqual("Sales", team["name"]);
            Assert.AreEqual(1, team.GetAttributeValue<OptionSetValue>("teamtype").Value);
        }

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

        [TestMethod]
        public void UserHasRole_DependsOnTheAssignment()
        {
            Assert.IsFalse(common.UserHasRole(UserId, Guid.NewGuid()));

            service.OnRetrieveMultiple = query => Collection(new Entity("role", Guid.NewGuid()));

            Assert.IsTrue(common.UserHasRole(UserId, Guid.NewGuid()));
        }

        [TestMethod]
        public void DefaultTeamForUser_LinksBusinessUnitToUser()
        {
            var query = Common.DefaultTeamForUserQuery(IdA);

            Assert.AreEqual("team", query.EntityName);
            Assert.AreEqual("name", query.Orders.Single().AttributeName);
            AssertCondition(query.Criteria.Conditions[0], "teamtype", ConditionOperator.Equal, 0);
            AssertCondition(query.Criteria.Conditions[1], "isdefault", ConditionOperator.Equal, true);

            var businessUnit = query.LinkEntities.Single();
            Assert.AreEqual("businessunit", businessUnit.LinkToEntityName);
            Assert.AreEqual(JoinOperator.Inner, businessUnit.JoinOperator);
            var user = businessUnit.LinkEntities.Single();
            Assert.AreEqual("systemuser", user.LinkToEntityName);
            AssertCondition(user.LinkCriteria.Conditions.Single(), "systemuserid", ConditionOperator.Equal, IdA);
        }

        [TestMethod]
        public void TeamMembership_FiltersTeamAndUser()
        {
            var query = Common.TeamMembershipQuery(IdA, IdB);

            AssertCondition(query.Criteria.Conditions.Single(), "teamid", ConditionOperator.Equal, IdA);
            var user = query.LinkEntities.Single().LinkEntities.Single();
            Assert.AreEqual("systemuser", user.LinkToEntityName);
            AssertCondition(user.LinkCriteria.Conditions.Single(), "systemuserid", ConditionOperator.Equal, IdB);
        }

        [TestMethod]
        public void PrincipalRole_UsesTheTeamOrUserIntersect()
        {
            var team = Common.PrincipalRoleQuery(new EntityReference("team", IdA), IdB);
            var user = Common.PrincipalRoleQuery(new EntityReference("systemuser", IdA), IdB);

            Assert.AreEqual("teamroles", team.EntityName);
            AssertCondition(team.Criteria.Conditions[0], "teamid", ConditionOperator.Equal, IdA);
            AssertCondition(team.Criteria.Conditions[1], "roleid", ConditionOperator.Equal, IdB);
            Assert.AreEqual("systemuserroles", user.EntityName);
            AssertCondition(user.Criteria.Conditions[0], "systemuserid", ConditionOperator.Equal, IdA);
        }

        [TestMethod]
        public void UserRole_MatchesTheRootRoleForTheUser()
        {
            var query = Common.UserRoleQuery(IdA, IdB);

            Assert.AreEqual("role", query.EntityName);
            AssertCondition(query.Criteria.Conditions.Single(), "parentrootroleid", ConditionOperator.Equal, IdB);
            AssertCondition(query.LinkEntities.Single().LinkCriteria.Conditions.Single(), "systemuserid", ConditionOperator.Equal, IdA);
        }
    }
}
