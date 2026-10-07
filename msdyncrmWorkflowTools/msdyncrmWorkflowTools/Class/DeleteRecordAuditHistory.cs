using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Delete Record Audit History")]
    public class DeleteRecordAuditHistory : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.DeleteRecordAuditHistory(common.GetRecordReference(RecordURL.Get(executionContext), "Record URL"));
        }
    }
}
