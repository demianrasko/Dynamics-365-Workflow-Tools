using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace msdyncrmWorkflowTools.Class
{
    public class SendEmail : CodeActivity
    {
        [RequiredArgument]
        [Input("Email to send")]
        [ReferenceTarget("email")]
        public InArgument<EntityReference> SourceEmail
        { get; set; }

        [Output("Email Subject")]
        public OutArgument<string> Subject { get; set; }



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference email = SourceEmail.Get(executionContext);

            #endregion

            #region "SendEmail Execution"

            
            SendEmailResponse ser = objCommon.service.Execute(
                new SendEmailRequest()
                {
                    EmailId = email.Id,
                    IssueSend = true
                }
              ) as SendEmailResponse;

            if (ser != null)
            {
                Subject.Set(executionContext, ser.Subject);
            }

            #endregion

        }
    }
}
