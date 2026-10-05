using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;

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
    }
}
