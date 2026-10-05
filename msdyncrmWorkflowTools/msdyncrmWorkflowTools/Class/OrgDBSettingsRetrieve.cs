using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
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
            var setting = orgDBSetting.Get(executionContext).ToLower();
            var stringValue = common.GetOrganizationSetting(setting)?.ToString();
            common.Trace($"Organization setting {setting} = {stringValue}");

            int.TryParse(stringValue, out var numericValue);
            bool.TryParse(stringValue, out var boolValue);

            StringValue.Set(executionContext, stringValue);
            NumericValue.Set(executionContext, numericValue);
            BoolValue.Set(executionContext, boolValue);
        }
    }
}
