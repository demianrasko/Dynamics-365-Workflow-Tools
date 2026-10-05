using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class ExecuteWorkflowForRecordsinQuery : WorkflowActivityBase
    {
        [Input("Process")]
        [ReferenceTarget(EntityNames.Workflow)]
        public InArgument<EntityReference> Process { get; set; }

        [Input("Query")]
        public InArgument<string> Query { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var query = Query.Get(executionContext);
            var process = Process.Get(executionContext);

            if (string.IsNullOrEmpty(query))
            {
                common.Trace("No query: nothing to run.");
                return;
            }

            if (process == null)
            {
                throw new InvalidPluginExecutionException("Process is required when a Query is given.");
            }

            // the user's FetchXML, paged through every record
            var recordIds = common.RetrieveAllIds(common.FetchXmlToQueryExpression(query));
            common.Trace($"Running process {process.Id} for {recordIds.Count} records");

            common.ExecuteWorkflow(process.Id, recordIds);
        }
    }
}
