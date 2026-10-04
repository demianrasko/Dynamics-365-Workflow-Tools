using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class GetAppRecordUrl : WorkflowActivityBase
    {
        #region "Parameter Definition"
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

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var recordUrl = RecordURL.Get(executionContext);
            var appModuleUniqueName = AppModuleUniqueName.Get(executionContext);
            #endregion

            var appRecordUrl = common.GetAppRecordUrl(recordUrl, appModuleUniqueName);

            AppRecordUrl.Set(executionContext, appRecordUrl);
        }
    }
}
