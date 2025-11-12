using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class AIDraftReply : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Text To Reply To")]
        public InArgument<String> TextToReplyTo { get; set; }

        [Output("Reply Text")]
        public OutArgument<String> ReplyText { get; set; }

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
            String text = this.TextToReplyTo.Get(executionContext);
            #endregion

            if (String.IsNullOrWhiteSpace(text))
            {
                this.Failed.Set(executionContext, true);
                this.ReplyText.Set(executionContext, String.Empty);
                this.FailureMessage.Set(executionContext, "Text is empty.");
                return;
            }

            try
            {
                objCommon.tracingService.Trace(String.Format("AIDraftReply - Text length: {0}", text.Length));

                OrganizationRequest request = new OrganizationRequest("AIReply");
                request["Text"] = text;

                OrganizationResponse response = objCommon.service.Execute(request);

                if (!response.Results.Contains("PreparedResponse"))
                {
                    this.Failed.Set(executionContext, true);
                    this.ReplyText.Set(executionContext, String.Empty);
                    this.FailureMessage.Set(executionContext, "AIReply response missing 'PreparedResponse'.");
                    return;
                }

                String preparedResponse = response["PreparedResponse"] as String;

                this.ReplyText.Set(executionContext, preparedResponse ?? String.Empty);
                this.Failed.Set(executionContext, false);
                this.FailureMessage.Set(executionContext, String.Empty);
            }
            catch (Exception ex)
            {
                objCommon.tracingService.Trace(String.Format("AIDraftReply - Error: {0}", ex.ToString()));
                this.Failed.Set(executionContext, true);
                this.ReplyText.Set(executionContext, String.Empty);
                this.FailureMessage.Set(executionContext, ex.Message);
            }
        }
    }
}
