using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class SetState : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("State")]     
        public InArgument<int> State { get; set; }

        [RequiredArgument]
        [Input("Status")]
        public InArgument<int> Status { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.SetState(
                new EntityReference(common.Context.PrimaryEntityName, common.Context.PrimaryEntityId),
                State.Get(executionContext),
                Status.Get(executionContext));
        }
    }
}
