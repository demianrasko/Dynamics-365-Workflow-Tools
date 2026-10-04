using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class RemoveRoleFromTeam : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Role")]
        [ReferenceTarget("role")]
        public InArgument<EntityReference> Role { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var roleReference = Role.Get(executionContext);
            var teamReference = Team.Get(executionContext);

            common.Trace($"RoleId: {roleReference.Id.ToString()} - TeamID: {teamReference.Id.ToString()} ");
            #endregion

            var roleId = common.GetRoleIdInBusinessUnit(new EntityReference("team", teamReference.Id), roleReference.Id);

            if (roleId == null)
            {
                return;
            }

            var entRoleId = roleId.Value;

            common.service.Disassociate(
                "team",
                teamReference.Id,
                new Relationship("teamroles_association"), 
                new EntityReferenceCollection { new EntityReference("role", entRoleId) });
        }
    }
}
