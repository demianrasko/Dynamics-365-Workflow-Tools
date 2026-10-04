using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class RemoveRoleFromUser : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Role")]
        [ReferenceTarget(EntityNames.Role)]
        public InArgument<EntityReference> Role { get; set; }

        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget(EntityNames.SystemUser)]
        public InArgument<EntityReference> User { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var roleReference = Role.Get(executionContext);
            var userReference = User.Get(executionContext);

            common.Trace($"RoleId: {roleReference.Id.ToString()} - UserID: {userReference.Id.ToString()} ");
            #endregion

            var roleId = common.GetRoleIdInBusinessUnit(new EntityReference(EntityNames.SystemUser, userReference.Id), roleReference.Id);

            if (roleId == null)
            {
                return;
            }

            var entRoleId = roleId.Value;

            common.Service.Disassociate(
                EntityNames.SystemUser,
                userReference.Id,
                new Relationship("systemuserroles_association"), 
                new EntityReferenceCollection { new EntityReference(EntityNames.Role, entRoleId) });
        }
    }
}
