using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class RemoveRoleFromUser : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Role")]
        [ReferenceTarget("role")]
        public InArgument<EntityReference> Role { get; set; }

        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var roleReference = Role.Get(executionContext);
            var userReference = User.Get(executionContext);

            objCommon.Trace($"RoleId: {roleReference.Id.ToString()} - UserID: {userReference.Id.ToString()} ");
            #endregion

            var roleId = Utility.GetRoleIdInBusinessUnit(objCommon.service, objCommon.tracingService, new EntityReference("systemuser", userReference.Id), roleReference.Id);

            if (roleId == null)
            {
                return;
            }

            var entRoleId = roleId.Value;

            objCommon.service.Disassociate(
                "systemuser",
                userReference.Id,
                new Relationship("systemuserroles_association"), 
                new EntityReferenceCollection { new EntityReference("role", entRoleId) });
        }
    }
}
