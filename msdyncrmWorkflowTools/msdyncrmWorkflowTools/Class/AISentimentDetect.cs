using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// Detects the sentiment of a text with Dataverse AI (AISentiment). Errors are reported through the Failed and Failure Message outputs. Ported from demianrasko/Dynamics-365-Workflow-Tools#297 by rwilson504.
    /// </summary>
    [ActivityName("AI Sentiment Detect")]
    public class AISentimentDetect : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Text To Analyze Sentiment")]
        public InArgument<string> TextToAnalyzeSentiment { get; set; }

        [Output("Sentiment")]
        public OutArgument<string> Sentiment { get; set; }

        [Output("Failed")]
        [Default("false")]
        public OutArgument<bool> Failed { get; set; }

        [Output("Failure Message")]
        public OutArgument<string> FailureMessage { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var text = TextToAnalyzeSentiment.Get(executionContext);

            SetResultOrFailure(executionContext, common, () =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new InvalidPluginExecutionException("Text is empty.");
                }

                common.Trace($"Text length: {text.Length}");

                return common.AISentiment(text);
            }, Sentiment, Failed, FailureMessage);
        }
    }
}
