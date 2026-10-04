using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
namespace msdyncrmWorkflowTools.Class
{
    public class SendEmailToUsersInRole : WorkflowActivityBase
    {
        [Input("Security Role")]
        [RequiredArgument]
        [ReferenceTarget("role")]
        public InArgument<EntityReference> SecurityRoleLookup
        {
            get;
            set;
        }

        [Input("Email")]
        [RequiredArgument]
        [ReferenceTarget("email")]
        public InArgument<EntityReference> Email
        {
            get;
            set;
        }



        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var email = Email.Get(executionContext);
            objCommon.Trace(string.Format("email: {0} ", email.Id.ToString()));

            var securityRoleLookup = SecurityRoleLookup.Get(executionContext);
            objCommon.Trace(string.Format("securityRoleLookup: {0} ", securityRoleLookup.Id.ToString()));


            #endregion
            objCommon.Trace("Init");

            objCommon.SendEmailToUsersInRole(securityRoleLookup, email);


        }


    }
}
