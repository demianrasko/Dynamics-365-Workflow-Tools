using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class RemoveRoleFromTeam : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Role")]
        [ReferenceTarget(EntityNames.Role)]
        public InArgument<EntityReference> Role { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget(EntityNames.Team)]
        public InArgument<EntityReference> Team { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var roleReference = Role.Get(executionContext);
            var teamReference = Team.Get(executionContext);

            common.Trace($"RoleId: {roleReference.Id.ToString()} - TeamID: {teamReference.Id.ToString()} ");
            #endregion

            var roleId = common.GetRoleIdInBusinessUnit(new EntityReference(EntityNames.Team, teamReference.Id), roleReference.Id);

            if (roleId == null)
            {
                return;
            }

            var entRoleId = roleId.Value;

            common.Service.Disassociate(
                EntityNames.Team,
                teamReference.Id,
                new Relationship("teamroles_association"), 
                new EntityReferenceCollection { new EntityReference(EntityNames.Role, entRoleId) });
        }
    }
}
