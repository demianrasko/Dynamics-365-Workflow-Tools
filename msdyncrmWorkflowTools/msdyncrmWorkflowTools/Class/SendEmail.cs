using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class SendEmail : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Email to send")]
        [ReferenceTarget("email")]
        public InArgument<EntityReference> SourceEmail
        { get; set; }

        [Output("Email Subject")]
        public OutArgument<string> Subject { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var email = SourceEmail.Get(executionContext);
            #endregion

            #region "SendEmail Execution"

            if (objCommon.service.Execute(
                    new SendEmailRequest
                    {
                        EmailId = email.Id,
                        IssueSend = true
                    }
                ) is SendEmailResponse ser)
            {
                Subject.Set(executionContext, ser.Subject);
            }

            #endregion
        }
    }
}
