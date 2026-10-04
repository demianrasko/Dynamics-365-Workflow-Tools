using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
namespace msdyncrmWorkflowTools.Class
{
    public class SendEmailToUsersInRole : CodeActivity
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



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var email = Email.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("email: {0} ", email.Id.ToString()));

            var securityRoleLookup = SecurityRoleLookup.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("securityRoleLookup: {0} ", securityRoleLookup.Id.ToString()));


            #endregion
            objCommon.tracingService.Trace("Init");

            var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            commonClass.SendEmailToUsersInRole(securityRoleLookup, email);


        }


    }
}
