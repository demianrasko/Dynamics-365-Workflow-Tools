using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class GetAppModuleID : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Application Unique Name")]
        [Default("")]
        public InArgument<string> AppModuleUniqueName { get; set; }

        [Output("App Module ID")]
        public OutArgument<string> AppModuleId { get; set; }

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"
            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var appModuleUniqueName = AppModuleUniqueName.Get(executionContext);
            #endregion

            
            var appModuleId = objCommon.GetAppModuleId(appModuleUniqueName);
                
            AppModuleId.Set(executionContext, appModuleId);

        }
        

    }
}
