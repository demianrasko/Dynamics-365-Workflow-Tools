using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
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
            #region "Read Parameters"
            var securityRoleLookup = SecurityRoleLookup.Get(executionContext);
            common.Trace($"marketingList: {securityRoleLookup.Id.ToString()} ");

            var emailTemplateLookup = EmailTemplateLookup.Get(executionContext);
            common.Trace($"campaign: {emailTemplateLookup.Id.ToString()} ");

            #endregion

            common.SendEmailFromTemplateToUsersInRole(securityRoleLookup, emailTemplateLookup);
        }
    }
}
