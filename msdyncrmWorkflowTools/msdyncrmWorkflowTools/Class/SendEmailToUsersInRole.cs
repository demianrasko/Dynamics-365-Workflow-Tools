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

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference email = this.Email.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("email: {0} ", email.Id.ToString()));

            EntityReference securityRoleLookup = this.SecurityRoleLookup.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("securityRoleLookup: {0} ", securityRoleLookup.Id.ToString()));


            #endregion
            objCommon.tracingService.Trace("Init");

            msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            commonClass.SendEmailToUsersInRole(securityRoleLookup, email);


        }


    }
}
