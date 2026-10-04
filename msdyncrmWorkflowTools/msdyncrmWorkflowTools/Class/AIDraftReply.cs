using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// Drafts a reply to a customer message with Dataverse AI (AIReply). Errors are reported through the Failed and Failure Message outputs. Ported from demianrasko/Dynamics-365-Workflow-Tools#297 by rwilson504.
    /// </summary>
    public class AIDraftReply : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Text To Reply To")]
        public InArgument<string> TextToReplyTo { get; set; }

        [Output("Reply Text")]
        public OutArgument<string> ReplyText { get; set; }

        [Output("Failed")]
        [Default("false")]
        public OutArgument<bool> Failed { get; set; }

        [Output("Failure Message")]
        public OutArgument<string> FailureMessage { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var text = TextToReplyTo.Get(executionContext);

            SetResultOrFailure(executionContext, common, () =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new InvalidPluginExecutionException("Text is empty.");
                }

                common.Trace($"Text length: {text.Length}");

                return common.AIReply(text);
            }, ReplyText, Failed, FailureMessage);
        }
    }
}
