using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class RemoveRoleFromTeam : CodeActivity
    {
        [RequiredArgument]
        [Input("Role")]
        [ReferenceTarget("role")]
        public InArgument<EntityReference> Role { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var roleReference = Role.Get(executionContext);
            var teamReference = Team.Get(executionContext);

            objCommon.tracingService.Trace($"RoleId: {roleReference.Id.ToString()} - TeamID: {teamReference.Id.ToString()} ");
            #endregion

            var roleId = Utility.GetRoleIdInBusinessUnit(objCommon.service, objCommon.tracingService, new EntityReference("team", teamReference.Id), roleReference.Id);

            if (roleId == null)
            {
                return;
            }

            var entRoleId = roleId.Value;

            objCommon.service.Disassociate(
                "team",
                teamReference.Id,
                new Relationship("teamroles_association"), 
                new EntityReferenceCollection { new EntityReference("role", entRoleId) });
        }
    }
}
