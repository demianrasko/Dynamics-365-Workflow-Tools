using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Send Email To Users In Role")]
    public class SendEmailToUsersInRole : WorkflowActivityBase
    {
        [Input("Security Role")]
        [RequiredArgument]
        [ReferenceTarget(EntityNames.Role)]
        public InArgument<EntityReference> SecurityRoleLookup
        {
            get;
            set;
        }

        [Input("Email")]
        [RequiredArgument]
        [ReferenceTarget(EntityNames.Email)]
        public InArgument<EntityReference> Email
        {
            get;
            set;
        }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.SendEmailToUsersInRole(SecurityRoleLookup.Get(executionContext), Email.Get(executionContext));
        }
    }
}
