using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;

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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            #region "Read Parameters"
            var recordURL = RecordURL.Get(executionContext);
            var appModuleUniqueName = AppModuleUniqueName.Get(executionContext);
            #endregion

            
            var appRecordUrl = objCommon.GetAppRecordUrl(recordURL, appModuleUniqueName);

            AppRecordUrl.Set(executionContext, appRecordUrl);

        }
        

    }
}
