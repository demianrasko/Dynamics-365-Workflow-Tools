using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class RemoveRoleFromUser : CodeActivity
    {
        [RequiredArgument]
        [Input("Role")]
        [ReferenceTarget("role")]
        public InArgument<EntityReference> Role { get; set; }

        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var roleReference = Role.Get(executionContext);
            var userReference = User.Get(executionContext);

            objCommon.tracingService.Trace($"RoleId: {roleReference.Id.ToString()} - UserID: {userReference.Id.ToString()} ");
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
