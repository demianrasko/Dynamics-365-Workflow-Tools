using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class AISentimentDetect : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Text To Analyze Sentiment")]
        public InArgument<String> TextToAnalyzeSentiment { get; set; }

        [Output("Sentiment")]
        public OutArgument<String> Sentiment { get; set; }

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
            String text = this.TextToAnalyzeSentiment.Get(executionContext);
            #endregion

            if (String.IsNullOrWhiteSpace(text))
            {
                this.Failed.Set(executionContext, true);
                this.Sentiment.Set(executionContext, String.Empty);
                this.FailureMessage.Set(executionContext, "Text is empty.");
                return;
            }

            try
            {
                objCommon.tracingService.Trace(String.Format("AISentimentDetect - Text length: {0}", text.Length));

                OrganizationRequest request = new OrganizationRequest("AISentiment");
                request["Text"] = text;

                OrganizationResponse response = objCommon.service.Execute(request);

                if (!response.Results.Contains("AnalyzedSentiment"))
                {
                    this.Failed.Set(executionContext, true);
                    this.Sentiment.Set(executionContext, String.Empty);
                    this.FailureMessage.Set(executionContext, "AISentiment response missing 'AnalyzedSentiment'.");
                    return;
                }

                String analyzedSentiment = response["AnalyzedSentiment"] as String;

                this.Sentiment.Set(executionContext, analyzedSentiment ?? String.Empty);
                this.Failed.Set(executionContext, false);
                this.FailureMessage.Set(executionContext, String.Empty);
            }
            catch (Exception ex)
            {
                objCommon.tracingService.Trace(String.Format("AISentimentDetect - Error: {0}", ex.ToString()));
                this.Failed.Set(executionContext, true);
                this.Sentiment.Set(executionContext, String.Empty);
                this.FailureMessage.Set(executionContext, ex.Message);
            }
        }
    }
}
