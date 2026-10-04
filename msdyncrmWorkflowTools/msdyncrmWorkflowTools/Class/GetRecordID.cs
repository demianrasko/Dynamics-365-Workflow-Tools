using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class GetRecordID : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Record URL")]
        [Default("")]
        public InArgument<string> RecordURL { get; set; }

        [Output("Record ID")]
        public OutArgument<string> RecordID { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var recordUrl = RecordURL.Get(executionContext);

            #endregion

            var recordId = Utility.GetRecordId(recordUrl);

            RecordID.Set(executionContext, recordId);
        }
    }
}
