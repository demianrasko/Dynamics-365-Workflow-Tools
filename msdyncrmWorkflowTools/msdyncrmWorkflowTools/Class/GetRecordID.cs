using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Get Record ID")]
    public class GetRecordID : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL")]
        [Default("")]
        public InArgument<string> RecordURL { get; set; }

        [Output("Record ID")]
        public OutArgument<string> RecordID { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var recordUrl = RecordURL.Get(executionContext);

            var recordId = Utility.GetRecordId(recordUrl);

            RecordID.Set(executionContext, recordId);
        }
    }
}
