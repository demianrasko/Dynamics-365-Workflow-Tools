using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Linq;

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

            var sourceEntityReference = new DynamicUrlParser(sourceRecordUrl).ToEntityReference(common.service);
            var sourceEntity = common.service.Retrieve(sourceEntityReference.LogicalName, sourceEntityReference.Id, new ColumnSet(attributeName));
            common.Trace("Source record has been retrieved correctly. Id:{0}", sourceEntity.Id);

            var optionSetValues = sourceEntity.GetAttributeValue<OptionSetValueCollection>(attributeName);
            if (optionSetValues == null || optionSetValues.Count == 0)
            {
                common.Trace("No selected options");
                SelectedValues.Set(executionContext, string.Empty);
                return;
            }

            common.Trace("Number of selected options: {0}", optionSetValues.Count);

            var values = string.Join(",", optionSetValues.Select(o => o.Value));
            SelectedValues.Set(executionContext, values);
            common.Trace("Values have been retrieved correctly. Values: {0}", values);

            if (retrieveOptionsNames)
            {
                var labels = common.GetOptionSetLabels(sourceEntityReference.LogicalName, attributeName);
                var names = string.Join(",", optionSetValues.Select(o => labels.TryGetValue(o.Value, out var label) ? label : o.Value.ToString()));
                SelectedNames.Set(executionContext, names);
                common.Trace("Names have been retrieved correctly. Names: {0}", names);
            }
        }
    }
}
