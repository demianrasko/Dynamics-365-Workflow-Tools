using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("OrgDB Settings Update")]
    public class OrgDBSettingsUpdate : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("orgDBSetting to Update")]
        [Default("")]
        public InArgument<string> orgDBSetting { get; set; }

        [RequiredArgument]
        [Input("Value")]
        [Default("")]
        public InArgument<string> Value { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.SetOrganizationSetting(orgDBSetting.Get(executionContext), Value.Get(executionContext));
        }
    }
}
