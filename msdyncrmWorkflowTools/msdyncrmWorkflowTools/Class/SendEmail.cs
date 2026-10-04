using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;


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

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var email = SourceEmail.Get(executionContext);

            #endregion

            #region "SendEmail Execution"

            
            var ser = objCommon.service.Execute(
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
