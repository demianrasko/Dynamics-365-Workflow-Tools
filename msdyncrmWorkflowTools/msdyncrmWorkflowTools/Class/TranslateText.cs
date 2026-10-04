using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class TranslateText : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Text To Translate")]
        [Default("")]
        public InArgument<string> TextToTranslate { get; set; }

        [RequiredArgument]
        [Input("Language")]
        [Default("")]
        public InArgument<string> Language { get; set; }

        [RequiredArgument]
        [Input("Authentication key")]
        [Default("")]
        public InArgument<string> Authenticationkey { get; set; }


        [Output("Translated Text")]
        public OutArgument<string> TranslatedText { get; set; }
        
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var textToTranslate = TextToTranslate.Get(executionContext);
            var language = Language.Get(executionContext);
            var authenticationKey = Authenticationkey.Get(executionContext);
            #endregion
            
            var res=common.TranslateText(textToTranslate, language, authenticationKey) ?? string.Empty;

            TranslatedText.Set(executionContext, res);
        }
    }
}
