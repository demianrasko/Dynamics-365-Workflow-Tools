using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Globalization;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Get Option Set Value")]
    public class GetOptionSetValue : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Source Record URL")]
        public InArgument<string> SourceRecordUrl { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        public InArgument<string> AttributeName { get; set; }

        [Output("Value")]
        public OutArgument<int> SelectedValue { get; set; }

        // the designer formats a whole number with the user's digit grouping (100,000) when it goes into text, such as
        // a FetchXML condition; this one is plain digits
        [Output("Value (Text)")]
        public OutArgument<string> SelectedValueText { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var sourceRecordUrl = SourceRecordUrl.Get(executionContext) ?? throw new InvalidPluginExecutionException("Source URL is empty");
            var attributeName = AttributeName.Get(executionContext) ?? throw new InvalidPluginExecutionException("Attribute Name is empty");

            common.Trace($"Source Record URL:'{sourceRecordUrl}' Attribute name:'{attributeName}'");

            var source = common.GetRecordReference(sourceRecordUrl);
            var value = common.GetOptionSetValue(source, attributeName);

            SelectedValue.Set(executionContext, value);
            SelectedValueText.Set(executionContext, value.ToString(CultureInfo.InvariantCulture));
        }
    }
}