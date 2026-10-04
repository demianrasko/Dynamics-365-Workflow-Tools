using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CheckUserInRole : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Role")]
        [ReferenceTarget(EntityNames.Role)]
        public InArgument<EntityReference> Role { get; set; }

        [Input("User")]
        [ReferenceTarget(EntityNames.SystemUser)]
        public InArgument<EntityReference> User { get; set; }

        [Output("isUserInRole")]
        public OutArgument<bool> isUserInRole { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var role = Role.Get(executionContext);
            var userId = User.Get(executionContext)?.Id ?? common.Context.InitiatingUserId;

            isUserInRole.Set(executionContext, common.UserHasRole(userId, role.Id));
        }
    }
}
