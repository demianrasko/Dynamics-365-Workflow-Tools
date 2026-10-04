using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CalculatePrice : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Target Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> TargetRecordURL { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var targetRecordUrl = TargetRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(targetRecordUrl))
            {
                throw new InvalidPluginExecutionException("Target Record URL is required.");
            }

            common.CalculatePrice(common.GetRecordReference(targetRecordUrl));
        }
    }
}
