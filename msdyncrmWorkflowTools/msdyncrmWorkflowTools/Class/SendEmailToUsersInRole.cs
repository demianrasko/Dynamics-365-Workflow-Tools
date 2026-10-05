using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
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
            var email = Email.Get(executionContext);
            common.Trace($"email: {email.Id.ToString()} ");

            var securityRoleLookup = SecurityRoleLookup.Get(executionContext);
            common.Trace($"securityRoleLookup: {securityRoleLookup.Id.ToString()} ");

            common.SendEmailToUsersInRole(securityRoleLookup, email);
        }
    }
}
