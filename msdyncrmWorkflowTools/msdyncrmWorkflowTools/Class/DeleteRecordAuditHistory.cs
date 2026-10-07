using Microsoft.Xrm.Sdk;
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
            var recordUrl = RecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }
            var parsedUrl = common.ParseRecordUrl(recordUrl);
            common.Trace($"EntityName={parsedUrl.EntityName}--Id={parsedUrl.Id}");

            common.DeleteRecordAuditHistory(parsedUrl.EntityName, parsedUrl.Id);
        }
    }
}
