using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Force Calculate Rollup Field")]
    public class CalculateRollupField : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("FieldName")]
        [Default("")]
        public InArgument<string> FieldName { get; set; }

        [RequiredArgument]
        [Input("Parent Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> ParentRecordURL { get; set; }

        [Input("Copy Result To Field")]
        [Default("")]
        public InArgument<string> CopyResultToField { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.CalculateRollupField(common.GetRecordReference(ParentRecordURL.Get(executionContext), "Parent Record URL"), FieldName.Get(executionContext), CopyResultToField.Get(executionContext));
        }
    }
}
