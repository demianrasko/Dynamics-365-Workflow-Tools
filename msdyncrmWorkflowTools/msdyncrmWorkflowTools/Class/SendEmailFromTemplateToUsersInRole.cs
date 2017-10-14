using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
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

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference securityRoleLookup = this.SecurityRoleLookup.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("marketingList: {0} ", securityRoleLookup.Id.ToString()));

            EntityReference emailTemplateLookup = this.EmailTemplateLookup.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("campaign: {0} ", emailTemplateLookup.Id.ToString()));


            #endregion
            objCommon.tracingService.Trace("Init");

            msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            commonClass.SendEmailFromTemplateToUsersInRole(securityRoleLookup,emailTemplateLookup);


        }


    }
}
