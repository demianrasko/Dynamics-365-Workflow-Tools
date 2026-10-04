using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// Summarizes a record with Dataverse AI (AISummarizeRecord). Errors are reported through the Failed and Failure
    /// Message outputs. Ported from demianrasko/Dynamics-365-Workflow-Tools#297 by rwilson504.
    /// </summary>
    public class AISummarizeRecord : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL (Dynamic)")]
        public InArgument<string> RecordUrl { get; set; }

        [Input("Additional Record Context JSON")]
        public InArgument<string> RecordContextJson { get; set; }

        [Input("Include Catchup Changes (Lead/Opportunity only)")]
        [Default("false")]
        public InArgument<bool> IncludeCatchup { get; set; }

        [Output("Summary Text")]
        public OutArgument<string> SummaryText { get; set; }

        [Output("Failed")]
        [Default("false")]
        public OutArgument<bool> Failed { get; set; }

        [Output("Failure Message")]
        public OutArgument<string> FailureMessage { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var recordUrl = RecordUrl.Get(executionContext);
            var recordContext = RecordContextJson.Get(executionContext);
            var includeCatchup = IncludeCatchup.Get(executionContext);

            SetResultOrFailure(executionContext, common, () =>
            {
                if (string.IsNullOrWhiteSpace(recordUrl))
                {
                    throw new InvalidPluginExecutionException("Record URL is required.");
                }

                var record = new DynamicUrlParser(recordUrl).ToEntityReference(common.Service);
                common.Trace($"EntityName={record.LogicalName}--Id={record.Id}, include catchup: {includeCatchup}");

                return common.AISummarizeRecord(record, includeCatchup, recordContext);
            }, SummaryText, Failed, FailureMessage);
        }
    }
}
