using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Execute Workflow By ID")]
    public class ExecuteWorkflowByID : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record ID")]
        [ReferenceTarget("")]
        public InArgument<string> RecordID { get; set; }

        [Input("Process")]
        [ReferenceTarget(EntityNames.Workflow)]
        public InArgument<EntityReference> Process { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var recordId = RecordID.Get(executionContext);

            if (!Guid.TryParse(recordId, out var id))
            {
                throw new InvalidPluginExecutionException($"Record ID '{recordId}' is not a valid GUID.");
            }

            common.ExecuteWorkflow(Process.Get(executionContext).Id, new[] { id });
        }
    }
}
