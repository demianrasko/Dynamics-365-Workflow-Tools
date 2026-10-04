using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Collections.Generic;

namespace msdyncrmWorkflowTools
{
    public class SetMultiSelectOptionSet : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Target Record URL")]
        public InArgument<string> TargetRecordUrl { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        public InArgument<string> AttributeName { get; set; }

        [RequiredArgument]
        [Input("Attribute Values")]
        public InArgument<string> AttributeValues { get; set; }

        /// <summary>
        /// Indicate if the existing selected values in the target multi-select optionset attribute will be maintained.
        /// By default, it will remove the existing values and assign the new ones given by the argument "AttributesValues"
        /// </summary>
        [Input("Keep Existing Values")]
        [Default("false")]
        public InArgument<bool> KeepExistingValues { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var recordUrl = TargetRecordUrl.Get(executionContext) ?? throw new InvalidPluginExecutionException("Source URL is empty");
            var attributeName = AttributeName.Get(executionContext) ?? throw new InvalidPluginExecutionException("Attribute Name is empty");
            var attributeValues = AttributeValues.Get(executionContext) ?? throw new InvalidPluginExecutionException("Attribute Values is empty");

            common.Trace($"Record URL: '{recordUrl}', attribute: '{attributeName}', values: '{attributeValues}'");

            var target = common.GetRecordReference(recordUrl);

            var invalidValues = new List<string>();
            var values = Utility.ParseOptionSetValues(attributeValues, invalidValues);

            if (invalidValues.Count > 0)
            {
                common.Trace($"Skipped values that are not whole numbers: '{string.Join("', '", invalidValues)}'");
            }

            common.SetMultiSelectOptionSet(target, attributeName, values, KeepExistingValues.Get(executionContext));
        }
    }
}
