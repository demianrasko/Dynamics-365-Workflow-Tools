using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class GoalRecalculate : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [Input("Goal")]
        [ReferenceTarget(EntityNames.Goal)]
        public InArgument<EntityReference> Goal { get; set; }

        [Input("Goal Guid")]
        [Default("")]
        public InArgument<string> GoalGuid { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            // the Goal lookup wins; Goal Guid is used when no lookup is set
            var goal = Goal.Get(executionContext);
            var goalGuid = GoalGuid.Get(executionContext);

            Guid goalId;

            if (goal != null)
            {
                goalId = goal.Id;
            }
            else if (string.IsNullOrWhiteSpace(goalGuid))
            {
                throw new InvalidPluginExecutionException("Goal or Goal Guid is required.");
            }
            else if (!Guid.TryParse(goalGuid.Trim(), out goalId))
            {
                throw new InvalidPluginExecutionException($"Goal Guid '{goalGuid}' is not a valid GUID.");
            }

            common.RecalculateGoal(goalId);
        }
    }
}
