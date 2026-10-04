using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class GetAppRecordUrl : CodeActivity
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

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"
            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var recordURL = RecordURL.Get(executionContext);
            var appModuleUniqueName = AppModuleUniqueName.Get(executionContext);
            #endregion

            var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            
            var appRecordUrl = commonClass.GetAppRecordUrl(recordURL, appModuleUniqueName);

            AppRecordUrl.Set(executionContext, appRecordUrl);

        }
        

    }
}
