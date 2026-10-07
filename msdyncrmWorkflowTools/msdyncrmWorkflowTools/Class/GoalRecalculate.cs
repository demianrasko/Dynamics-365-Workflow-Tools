using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Goal Recalculate")]
    public class GoalRecalculate : WorkflowActivityBase
    {
        [Input("Goal")]
        [ReferenceTarget(EntityNames.Goal)]
        public InArgument<EntityReference> Goal { get; set; }

        [Input("Goal Guid")]
        [Default("")]
        public InArgument<string> GoalGuid { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.RecalculateGoal(Goal.Get(executionContext), GoalGuid.Get(executionContext));
        }
    }
}
