using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// Translates a text with Dataverse AI (AITranslate). Errors are reported through the Failed and Failure Message
    /// outputs. Ported from demianrasko/Dynamics-365-Workflow-Tools#297 by rwilson504.
    /// </summary>
    public class AITranslateText : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Text To Translate")]
        public InArgument<string> TextToTranslate { get; set; }

        [Input("Target Language (e.g. en, fr, es)")]
        public InArgument<string> TargetLanguage { get; set; }

        [Output("Translated Text")]
        public OutArgument<string> TranslatedText { get; set; }

        [Output("Failed")]
        [Default("false")]
        public OutArgument<bool> Failed { get; set; }

        [Output("Failure Message")]
        public OutArgument<string> FailureMessage { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var text = TextToTranslate.Get(executionContext);
            var targetLanguage = TargetLanguage.Get(executionContext);

            SetResultOrFailure(executionContext, common, () =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new InvalidPluginExecutionException("Text is empty.");
                }

                common.Trace($"Text length: {text.Length}, target language: '{targetLanguage}'");

                return common.AITranslate(text, targetLanguage);
            }, TranslatedText, Failed, FailureMessage);
        }
    }
}
