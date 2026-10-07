using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Calculate Price")]
    public class CalculatePrice : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Target Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> TargetRecordURL { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.CalculatePrice(common.GetRecordReference(TargetRecordURL.Get(executionContext), "Target Record URL"));
        }
    }
}
