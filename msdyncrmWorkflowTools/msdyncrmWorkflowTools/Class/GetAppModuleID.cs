using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class GetAppModuleID : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Application Unique Name")]
        [Default("")]
        public InArgument<string> AppModuleUniqueName { get; set; }

        [Output("App Module ID")]
        public OutArgument<string> AppModuleId { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            #region "Read Parameters"
            var appModuleUniqueName = AppModuleUniqueName.Get(executionContext);
            #endregion

            
            var appModuleId = objCommon.GetAppModuleId(appModuleUniqueName);
                
            AppModuleId.Set(executionContext, appModuleId);

        }
        

    }
}
