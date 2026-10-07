using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Send Email From Template To Users In Role")]
    public class SendEmailFromTemplateToUsersInRole : WorkflowActivityBase
    {
        [Input("Security Role")]
        [RequiredArgument]
        [ReferenceTarget(EntityNames.Role)]
        public InArgument<EntityReference> SecurityRoleLookup
        {
            get;
            set;
        }

        [Input("Email Template")]
        [RequiredArgument]
        [ReferenceTarget(EntityNames.Template)]
        public InArgument<EntityReference> EmailTemplateLookup
        {
            get;
            set;
        }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var securityRoleLookup = SecurityRoleLookup.Get(executionContext);
            common.Trace($"marketingList: {securityRoleLookup.Id.ToString()} ");

            var emailTemplateLookup = EmailTemplateLookup.Get(executionContext);
            common.Trace($"campaign: {emailTemplateLookup.Id.ToString()} ");

            common.SendEmailFromTemplateToUsersInRole(securityRoleLookup, emailTemplateLookup);
        }
    }
}
