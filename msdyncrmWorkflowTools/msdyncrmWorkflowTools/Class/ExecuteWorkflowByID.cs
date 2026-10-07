using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
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
            common.ExecuteWorkflow(Process.Get(executionContext), RecordID.Get(executionContext));
        }
    }
}
