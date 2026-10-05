using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// Summarizes a text with Dataverse AI (AISummarize). Errors are reported through the Failed and Failure Message outputs. Ported from demianrasko/Dynamics-365-Workflow-Tools#297 by rwilson504.
    /// </summary>
    [ActivityName("AI Summarize Text")]
    public class AISummarizeText : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Text To Summarize")]
        public InArgument<string> TextToSummarize { get; set; }

        [Output("Summary Text")]
        public OutArgument<string> SummaryText { get; set; }

        [Output("Failed")]
        [Default("false")]
        public OutArgument<bool> Failed { get; set; }

        [Output("Failure Message")]
        public OutArgument<string> FailureMessage { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var text = TextToSummarize.Get(executionContext);

            SetResultOrFailure(executionContext, common, () =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new InvalidPluginExecutionException("Text is empty.");
                }

                common.Trace($"Text length: {text.Length}");

                return common.AISummarize(text);
            }, SummaryText, Failed, FailureMessage);
        }
    }
}
