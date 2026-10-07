using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Distribute Workflow (One To Many)")]
    public class DistributeWFActivityOneToMany : WorkflowActivityBase
    {
        [Input("Relationship Name"), RequiredArgument]
        public InArgument<string> RelationshipName { get; set; }

        [ReferenceTarget(EntityNames.Workflow), Input("Distributed Workflow"), RequiredArgument]
        public InArgument<EntityReference> Workflow { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.DistributeWorkflowOneToMany(RelationshipName.Get(executionContext), Workflow.Get(executionContext), common.Context.PrimaryEntityId);
        }
    }
}
