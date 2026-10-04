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



        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var securityRoleLookup = SecurityRoleLookup.Get(executionContext);
            objCommon.Trace(string.Format("marketingList: {0} ", securityRoleLookup.Id.ToString()));

            var emailTemplateLookup = EmailTemplateLookup.Get(executionContext);
            objCommon.Trace(string.Format("campaign: {0} ", emailTemplateLookup.Id.ToString()));


            #endregion
            objCommon.Trace("Init");

            objCommon.SendEmailFromTemplateToUsersInRole(securityRoleLookup,emailTemplateLookup);


        }


    }
}
