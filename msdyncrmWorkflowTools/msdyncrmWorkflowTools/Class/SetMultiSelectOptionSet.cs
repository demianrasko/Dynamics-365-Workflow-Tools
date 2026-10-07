using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Set Multi-Select Option Set")]
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
            common.SetMultiSelectOptionSet(TargetRecordUrl.Get(executionContext), AttributeName.Get(executionContext), AttributeValues.Get(executionContext), KeepExistingValues.Get(executionContext));
        }
    }
}
