using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Execute Workflow For Records In Query")]
    public class ExecuteWorkflowForRecordsinQuery : WorkflowActivityBase
    {
        [Input("Process")]
        [ReferenceTarget(EntityNames.Workflow)]
        public InArgument<EntityReference> Process { get; set; }

        [Input("Query")]
        public InArgument<string> Query { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.ExecuteWorkflowForRecordsInQuery(Query.Get(executionContext), Process.Get(executionContext));
        }
    }
}
