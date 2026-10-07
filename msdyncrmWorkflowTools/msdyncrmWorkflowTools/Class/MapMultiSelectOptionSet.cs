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
            common.MapMultiSelectOptionSets(SourceRecordUrl.Get(executionContext), SourceAttributes.Get(executionContext),
                TargetRecordUrl.Get(executionContext), TargetAttributes.Get(executionContext), KeepExistingValues.Get(executionContext));
        }
    }
}
