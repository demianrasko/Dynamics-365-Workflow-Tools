using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class SendEmailFromTemplateToUsersInRole : WorkflowActivityBase
    {
        [Input("Security Role")]
        [RequiredArgument]
        [ReferenceTarget("role")]
        public InArgument<EntityReference> SecurityRoleLookup
        {
            get;
            set;
        }

        [Input("Email Template")]
        [RequiredArgument]
        [ReferenceTarget("template")]
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
            common.Trace("Init");

            common.SendEmailFromTemplateToUsersInRole(securityRoleLookup,emailTemplateLookup);
        }
    }
}
