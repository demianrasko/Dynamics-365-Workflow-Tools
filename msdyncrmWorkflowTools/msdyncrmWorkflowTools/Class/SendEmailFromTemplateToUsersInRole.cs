using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
namespace msdyncrmWorkflowTools.Class
{
    public class SendEmailFromTemplateToUsersInRole : CodeActivity
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



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var securityRoleLookup = SecurityRoleLookup.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("marketingList: {0} ", securityRoleLookup.Id.ToString()));

            var emailTemplateLookup = EmailTemplateLookup.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("campaign: {0} ", emailTemplateLookup.Id.ToString()));


            #endregion
            objCommon.tracingService.Trace("Init");

            var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            commonClass.SendEmailFromTemplateToUsersInRole(securityRoleLookup,emailTemplateLookup);


        }


    }
}
