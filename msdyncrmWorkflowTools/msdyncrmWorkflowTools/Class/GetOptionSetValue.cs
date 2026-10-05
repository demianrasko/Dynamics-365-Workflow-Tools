using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var sourceRecordUrl = SourceRecordUrl.Get(executionContext) ?? throw new InvalidPluginExecutionException("Source URL is empty");
            var attributeName = AttributeName.Get(executionContext) ?? throw new InvalidPluginExecutionException("Attribute Name is empty");

            common.Trace($"Source Record URL:'{sourceRecordUrl}' Attribute name:'{attributeName}'");

            var source = common.GetRecordReference(sourceRecordUrl);

            SelectedValue.Set(executionContext, common.GetOptionSetValue(source, attributeName));
        }
    }
}