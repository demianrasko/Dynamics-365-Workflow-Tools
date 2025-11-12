using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class AITranslateText : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Text To Translate")]
        public InArgument<String> TextToTranslate { get; set; }

        [Input("Target Language (e.g. en, fr, es)")]
        public InArgument<String> TargetLanguage { get; set; }

        [Output("Translated Text")]
        public OutArgument<String> TranslatedText { get; set; }

        [Output("Failed")]
        [Default("false")]
        public OutArgument<bool> Failed { get; set; }

        [Output("Failure Message")]
        public OutArgument<String> FailureMessage { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String textToTranslate = this.TextToTranslate.Get(executionContext);
            String targetLanguage = this.TargetLanguage.Get(executionContext);
            #endregion

            if (String.IsNullOrWhiteSpace(textToTranslate))
            {
                this.Failed.Set(executionContext, true);
                this.TranslatedText.Set(executionContext, String.Empty);
                this.FailureMessage.Set(executionContext, "Text is empty.");
                return;
            }

            try
            {
                objCommon.tracingService.Trace(String.Format("AITranslateText - Text length: {0}, Target='{1}'", textToTranslate.Length, targetLanguage));

                OrganizationRequest request = new OrganizationRequest("AITranslate");
                request["Text"] = textToTranslate;

                if (!String.IsNullOrWhiteSpace(targetLanguage))
                {
                    request["TargetLanguage"] = targetLanguage;
                }

                OrganizationResponse response = objCommon.service.Execute(request);

                if (!response.Results.Contains("TranslatedText"))
                {
                    this.Failed.Set(executionContext, true);
                    this.TranslatedText.Set(executionContext, String.Empty);
                    this.FailureMessage.Set(executionContext, "AITranslate response missing 'TranslatedText'.");
                    return;
                }

                String translatedText = response["TranslatedText"] as String;

                this.TranslatedText.Set(executionContext, translatedText ?? String.Empty);
                this.Failed.Set(executionContext, false);
                this.FailureMessage.Set(executionContext, String.Empty);
            }
            catch (Exception ex)
            {
                objCommon.tracingService.Trace(String.Format("AITranslateText - Error: {0}", ex.ToString()));
                this.Failed.Set(executionContext, true);
                this.TranslatedText.Set(executionContext, String.Empty);
                this.FailureMessage.Set(executionContext, ex.Message);
            }
        }
    }
}
