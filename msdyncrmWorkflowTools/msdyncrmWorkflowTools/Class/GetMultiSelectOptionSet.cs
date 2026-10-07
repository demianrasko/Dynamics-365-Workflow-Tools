using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Get Multi-Select Option Set")]
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
            SelectedValues.Set(executionContext, common.GetMultiSelectOptionSetText(SourceRecordUrl.Get(executionContext), AttributeName.Get(executionContext),
                RetrieveOptionsNames.Get(executionContext), out var names));
            SelectedNames.Set(executionContext, names);
        }
    }
}
