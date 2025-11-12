using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class AISummarizeRecord : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Record URL (Dynamic)")]
        public InArgument<String> RecordUrl { get; set; }

        [Input("Additional Record Context JSON")]
        public InArgument<String> RecordContextJson { get; set; }

        [Input("Include Catchup Changes (Lead/Opportunity only)")]
        [Default("false")]
        public InArgument<bool> IncludeCatchup { get; set; }

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
            String recordUrl = this.RecordUrl.Get(executionContext);
            String recordContext = this.RecordContextJson.Get(executionContext);
            bool includeCatchup = this.IncludeCatchup.Get(executionContext);
            #endregion

            if (String.IsNullOrWhiteSpace(recordUrl))
            {
                this.Failed.Set(executionContext, true);
                this.SummaryText.Set(executionContext, String.Empty);
                this.FailureMessage.Set(executionContext, "Record URL is required.");
                return;
            }

            try
            {
                objCommon.tracingService.Trace(String.Format("AISummarizeRecord - Received URL: {0}", recordUrl));

                DynamicUrlParser parser = new DynamicUrlParser(recordUrl);
                String logicalName = parser.GetEntityLogicalName(objCommon.service);
                String recordId = parser.Id.ToString();

                if (String.IsNullOrWhiteSpace(logicalName))
                {
                    this.Failed.Set(executionContext, true);
                    this.SummaryText.Set(executionContext, String.Empty);
                    this.FailureMessage.Set(executionContext, "Unable to resolve entity logical name from Record URL.");
                    return;
                }

                objCommon.tracingService.Trace(String.Format("AISummarizeRecord - Preparing request Entity={0}, Id={1}, IncludeCatchup={2}, HasContext={3}", logicalName, recordId, includeCatchup, !String.IsNullOrWhiteSpace(recordContext)));

                OrganizationRequest request = new OrganizationRequest("AISummarizeRecord");
                request["EntityLogicalName"] = logicalName;
                request["Id"] = recordId;

                if (includeCatchup)
                {
                    request["IsMergedCatchupAndSummary"] = true;
                }

                if (!String.IsNullOrWhiteSpace(recordContext))
                {
                    request["RecordContext"] = recordContext;
                }

                objCommon.tracingService.Trace(String.Format("AISummarizeRecord - Executing request with parameters: {0}", String.Join(", ", request.Parameters.Keys)));

                OrganizationResponse response = objCommon.service.Execute(request);

                if (!response.Results.Contains("SummarizedText"))
                {
                    this.Failed.Set(executionContext, true);
                    this.SummaryText.Set(executionContext, String.Empty);
                    this.FailureMessage.Set(executionContext, "AISummarizeRecord response missing 'SummarizedText'.");
                    return;
                }

                String summarizedText = response["SummarizedText"] as String;

                this.SummaryText.Set(executionContext, summarizedText ?? String.Empty);
                this.Failed.Set(executionContext, false);
                this.FailureMessage.Set(executionContext, String.Empty);
                objCommon.tracingService.Trace(String.Format("AISummarizeRecord - Summary length: {0}", summarizedText == null ? 0 : summarizedText.Length));
            }
            catch (Exception ex)
            {
                objCommon.tracingService.Trace(String.Format("AISummarizeRecord - Error: {0}", ex.ToString()));
                this.Failed.Set(executionContext, true);
                this.SummaryText.Set(executionContext, String.Empty);
                this.FailureMessage.Set(executionContext, ex.Message);
            }
        }
    }
}
