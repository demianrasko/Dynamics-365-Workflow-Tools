using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System.Linq;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    public abstract partial class IntegrationTestBase
    {
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
        public void Roles_AddCheckAndRemoveForATeam()
        {
            var team = CreateTeam();
            var roleId = RoleTheUserDoesNotHave();

            Assert.AreEqual(roleId, Common.GetRoleIdInBusinessUnit(team, roleId));

            Common.AddRole(team, roleId);
            Common.AddRole(team, roleId);
            CollectionAssert.AreEqual(new[] { roleId }, Common.GetManyToManyRelatedIds("teamroles_association", EntityNames.Team, team.Id));

            Common.RemoveRole(team, roleId);
            Assert.AreEqual(0, Common.GetManyToManyRelatedIds("teamroles_association", EntityNames.Team, team.Id).Count);
        }

        [TestMethod]
        public void UserHasRole_ChecksTheUsersRoles()
        {
            Assert.IsTrue(Common.UserHasRole(UserId, RoleTheUserHas()));
            Assert.IsFalse(Common.UserHasRole(UserId, RoleTheUserDoesNotHave()));
        }

        [TestMethod]
        public void RetrieveUserBuDefaultTeam_FindsTheBusinessUnitsTeam()
        {
            var team = Common.RetrieveUserBuDefaultTeam(UserId);

            Assert.IsNotNull(team);
            var values = Read(team, AttributeNames.BusinessUnitId, AttributeNames.IsDefault);
            Assert.AreEqual(BusinessUnitId, values.GetAttributeValue<EntityReference>(AttributeNames.BusinessUnitId).Id);
            Assert.IsTrue(values.GetAttributeValue<bool>(AttributeNames.IsDefault));
        }

        [TestMethod]
        public void ShareRecord_GrantsAndRevokesATeamsAccess()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("shared") });
            var team = CreateTeam();

            Common.ShareRecord(UrlFor(account), team, AccessRights.ReadAccess | AccessRights.WriteAccess);

            var shared = AccessOf(team, account);
            Assert.IsTrue(shared.HasFlag(AccessRights.ReadAccess));
            Assert.IsTrue(shared.HasFlag(AccessRights.WriteAccess));

            Common.UnshareRecord(UrlFor(account), team);

            Assert.IsFalse(AccessOf(team, account).HasFlag(AccessRights.WriteAccess));
        }

        [TestMethod]
        public void ShareSecuredField_SharesUpdatesAndRemovesFieldAccess()
        {
            TestOptions(TestSecret);
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("secured") });
            var team = CreateTeam();

            Common.ShareSecuredField(account, TestSecret, true, false, team, null);
            var share = FieldShare(account, team);
            Assert.IsTrue(share.GetAttributeValue<bool>(AttributeNames.ReadAccess));
            Assert.IsFalse(share.GetAttributeValue<bool>(AttributeNames.UpdateAccess));

            Common.ShareSecuredField(account, TestSecret, true, true, team);
            Assert.IsTrue(FieldShare(account, team).GetAttributeValue<bool>(AttributeNames.UpdateAccess));

            Common.ShareSecuredField(account, TestSecret, false, false, team);
            Assert.IsNull(FieldShare(account, team));

            // a field that isn't secured is left alone
            Common.ShareSecuredField(account, "telephone1", true, true, team);
        }

        private AccessRights AccessOf(EntityReference principal, EntityReference record)
        {
            return ((RetrievePrincipalAccessResponse)Service.Execute(new RetrievePrincipalAccessRequest { Principal = principal, Target = record })).AccessRights;
        }

        private Entity FieldShare(EntityReference record, EntityReference principal)
        {
            return Service.RetrieveMultiple(new QueryExpression(EntityNames.PrincipalObjectAttributeAccess)
            {
                ColumnSet = new ColumnSet(AttributeNames.ReadAccess, AttributeNames.UpdateAccess),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(AttributeNames.ObjectId, ConditionOperator.Equal, record.Id),
                        new ConditionExpression(AttributeNames.PrincipalId, ConditionOperator.Equal, principal.Id)
                    }
                }
            }).Entities.FirstOrDefault();
        }
    }
}
