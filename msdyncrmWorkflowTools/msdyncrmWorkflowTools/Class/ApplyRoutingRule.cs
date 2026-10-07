using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Apply Routing Rule")]
    public class ApplyRoutingRule : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Incident Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> IncidentRecordURL { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.ApplyRoutingRule(common.GetRecordReference(IncidentRecordURL.Get(executionContext), "Incident Record URL"));
        }
    }
}
