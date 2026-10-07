using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Get SharePoint Location URL")]
    public class GetSharepointLocationURL : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL")]
        public InArgument<string> RecordURL { get; set; }

        [Output("SharepointLocationURL")]
        public OutArgument<string> SharepointLocationURL { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            SharepointLocationURL.Set(executionContext, common.GetSharepointLocationUrl(RecordURL.Get(executionContext)));
        }
    }
}
