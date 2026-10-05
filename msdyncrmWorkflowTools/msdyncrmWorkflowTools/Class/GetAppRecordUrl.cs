using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class GetAppRecordUrl : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL")]
        [Default("")]
        public InArgument<string> RecordURL { get; set; }

        [RequiredArgument]
        [Input("Application Unique Name")]
        [Default("")]
        public InArgument<string> AppModuleUniqueName { get; set; }

        [Output("Record URL for App Module")]
        public OutArgument<string> AppRecordUrl { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var recordUrl = RecordURL.Get(executionContext);
            var appModuleUniqueName = AppModuleUniqueName.Get(executionContext);

            var appRecordUrl = common.GetAppRecordUrl(recordUrl, appModuleUniqueName);

            AppRecordUrl.Set(executionContext, appRecordUrl);
        }
    }
}
