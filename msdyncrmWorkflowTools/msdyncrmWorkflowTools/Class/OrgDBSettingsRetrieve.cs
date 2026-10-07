using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("OrgDB Settings Retrieve")]
    public class OrgDBSettingsRetrieve : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("orgDBSetting to Update")]
        [Default("")]
        public InArgument<string> orgDBSetting { get; set; }

        [Output("String Value")]
        public OutArgument<string> StringValue { get; set; }

        [Output("Numeric Value")]
        public OutArgument<decimal> NumericValue { get; set; }

        [Output("Bool Value")]
        public OutArgument<bool> BoolValue { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            StringValue.Set(executionContext, common.GetOrganizationSettingText(orgDBSetting.Get(executionContext), out var numericValue, out var boolValue));
            NumericValue.Set(executionContext, numericValue);
            BoolValue.Set(executionContext, boolValue);
        }
    }
}
