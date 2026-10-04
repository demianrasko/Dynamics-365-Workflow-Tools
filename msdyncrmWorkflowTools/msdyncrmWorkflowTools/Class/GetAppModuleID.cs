using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var appModuleUniqueName = AppModuleUniqueName.Get(executionContext);
            #endregion

            var appModuleId = common.GetAppModuleId(appModuleUniqueName);

            AppModuleId.Set(executionContext, appModuleId);
        }
    }
}
