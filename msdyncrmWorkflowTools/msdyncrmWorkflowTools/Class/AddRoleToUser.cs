using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Add Role To User")]
    public class AddRoleToUser : WorkflowActivityBase
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
            var role = Role.Get(executionContext);
            var principal = User.Get(executionContext);

            common.AddRole(new EntityReference(EntityNames.SystemUser, principal.Id), role.Id);
        }
    }
}
