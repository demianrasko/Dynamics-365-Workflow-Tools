using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class SendEmail : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Email to send")]
        [ReferenceTarget(EntityNames.Email)]
        public InArgument<EntityReference> SourceEmail
        { get; set; }

        [Output("Email Subject")]
        public OutArgument<string> Subject { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            Subject.Set(executionContext, common.SendEmail(SourceEmail.Get(executionContext).Id));
        }
    }
}
