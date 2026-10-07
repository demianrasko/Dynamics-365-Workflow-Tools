using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// Classifies a text into one of the given categories with Dataverse AI (AIClassify). Errors are reported through
    /// the Failed and Failure Message outputs. Ported from demianrasko/Dynamics-365-Workflow-Tools#297 by rwilson504.
    /// </summary>
    [ActivityName("AI Classify Text")]
    public class AIClassifyText : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Text To Classify")]
        public InArgument<string> TextToClassify { get; set; }

        [RequiredArgument]
        [Input("Categories (Comma Separated)")]
        public InArgument<string> CategoriesCsv { get; set; }

        [Output("Classification")]
        public OutArgument<string> TopCategory { get; set; }

        [Output("Failed")]
        [Default("false")]
        public OutArgument<bool> Failed { get; set; }

        [Output("Failure Message")]
        public OutArgument<string> FailureMessage { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var text = TextToClassify.Get(executionContext);
            var categories = Utility.SplitList(CategoriesCsv.Get(executionContext), removeDuplicates: true);

            SetResultOrFailure(executionContext, common, () => common.AIClassify(text, categories), TopCategory, Failed, FailureMessage);
        }
    }
}
