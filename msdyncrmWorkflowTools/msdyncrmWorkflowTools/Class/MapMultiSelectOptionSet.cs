using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Map Multi-Select Option Set")]
    public class MapMultiSelectOptionSet : WorkflowActivityBase
    {
        [Input("Source Record URL")]
        public InArgument<string> SourceRecordUrl { get; set; }

        [Input("Source Attributes")]
        public InArgument<string> SourceAttributes { get; set; }

        [Input("Target Record URL")]
        public InArgument<string> TargetRecordUrl { get; set; }

        [Input("Target Attributes")]
        public InArgument<string> TargetAttributes { get; set; }

        /// <summary>
        /// Indicate if the existing selected values in the target multi-select optionset attributes will be maintained.
        /// By default, it will remove the existing values and assign the new ones given by the argument "AttributesValues"
        /// </summary>
        [Input("Keep Existing Values")]
        [Default("false")]
        public InArgument<bool> KeepExistingValues { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var sourceRecordUrl = SourceRecordUrl.Get(executionContext) ?? throw new InvalidPluginExecutionException("Source URL is empty");
            var targetRecordUrl = TargetRecordUrl.Get(executionContext) ?? throw new InvalidPluginExecutionException("Target URL is empty");
            var sourceAttributes = Utility.SplitAttributeNames(SourceAttributes.Get(executionContext) ?? throw new InvalidPluginExecutionException("Source Attributes is empty"));
            var targetAttributes = Utility.SplitAttributeNames(TargetAttributes.Get(executionContext) ?? throw new InvalidPluginExecutionException("Target Attributes is empty"));

            common.MapMultiSelectOptionSets(
                common.GetRecordReference(sourceRecordUrl),
                sourceAttributes,
                common.GetRecordReference(targetRecordUrl),
                targetAttributes,
                KeepExistingValues.Get(executionContext));
        }
    }
}
