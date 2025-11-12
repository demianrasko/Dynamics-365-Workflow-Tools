using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class AISummarizeText : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Text To Summarize")]
        public InArgument<String> TextToSummarize { get; set; }

        [Output("Summary Text")]
        public OutArgument<String> SummaryText { get; set; }

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
            String text = this.TextToSummarize.Get(executionContext);
            #endregion

            if (String.IsNullOrWhiteSpace(text))
            {
                this.Failed.Set(executionContext, true);
                this.SummaryText.Set(executionContext, String.Empty);
                this.FailureMessage.Set(executionContext, "Text is empty.");
                return;
            }

            try
            {
                objCommon.tracingService.Trace(String.Format("AISummarizeText - Text length: {0}", text.Length));

                OrganizationRequest request = new OrganizationRequest("AISummarize");
                request["Text"] = text;

                OrganizationResponse response = objCommon.service.Execute(request);

                if (!response.Results.Contains("SummarizedText"))
                {
                    this.Failed.Set(executionContext, true);
                    this.SummaryText.Set(executionContext, String.Empty);
                    this.FailureMessage.Set(executionContext, "AISummarize response missing 'SummarizedText'.");
                    return;
                }

                String summarizedText = response["SummarizedText"] as String;

                this.SummaryText.Set(executionContext, summarizedText ?? String.Empty);
                this.Failed.Set(executionContext, false);
                this.FailureMessage.Set(executionContext, String.Empty);
            }
            catch (Exception ex)
            {
                objCommon.tracingService.Trace(String.Format("AISummarizeText - Error: {0}", ex.ToString()));
                this.Failed.Set(executionContext, true);
                this.SummaryText.Set(executionContext, String.Empty);
                this.FailureMessage.Set(executionContext, ex.Message);
            }
        }
    }
}
