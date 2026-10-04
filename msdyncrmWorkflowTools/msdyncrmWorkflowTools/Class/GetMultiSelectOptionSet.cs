using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class GetMultiSelectOptionSet : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Source Record URL")]
        public InArgument<string> SourceRecordUrl { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        public InArgument<string> AttributeName { get; set; }

        [Input("Retrieve Options Names")]
        [Default("False")]
        public InArgument<bool> RetrieveOptionsNames { get; set; }

        [Output("Selected Values")]
        public OutArgument<string> SelectedValues { get; set; }

        [Output("Selected Names")]
        public OutArgument<string> SelectedNames { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var sourceRecordUrl = SourceRecordUrl.Get(executionContext);
            if (string.IsNullOrEmpty(sourceRecordUrl))
            {
                throw new InvalidPluginExecutionException("Source URL is empty");
            }

            var attributeName = AttributeName.Get(executionContext);
            if (string.IsNullOrEmpty(attributeName))
            {
                throw new InvalidPluginExecutionException("Attribute Name is empty");
            }

            var retrieveOptionsNames = RetrieveOptionsNames.Get(executionContext);
            common.Trace($"Source Record URL:'{sourceRecordUrl}' Attribute name:'{attributeName}' Retrieve names:'{retrieveOptionsNames}'");

            var source = common.GetRecordReference(sourceRecordUrl);
            var values = common.GetMultiSelectOptionSet(source, attributeName);

            if (values.Count == 0)
            {
                common.Trace("No selected options");
                SelectedValues.Set(executionContext, string.Empty);
                return;
            }

            var selectedValues = Utility.JoinOptionSetValues(values);
            common.Trace($"Selected values: {selectedValues}");
            SelectedValues.Set(executionContext, selectedValues);

            if (!retrieveOptionsNames)
            {
                return;
            }

            var names = common.GetOptionSetNames(source.LogicalName, attributeName, values);
            common.Trace($"Selected names: {names}");
            SelectedNames.Set(executionContext, names);
        }
    }
}
