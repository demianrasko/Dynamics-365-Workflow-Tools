using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class GetAppModuleID : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Application Unique Name")]
        [Default("")]
        public InArgument<string> AppModuleUniqueName { get; set; }

        [Output("App Module ID")]
        public OutArgument<string> AppModuleId { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var appModuleUniqueName = AppModuleUniqueName.Get(executionContext);

            var appModuleId = common.GetAppModuleId(appModuleUniqueName);

            AppModuleId.Set(executionContext, appModuleId);
        }
    }
}
