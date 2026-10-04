using System;
using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class GoalRecalculate : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [Input("Goal")]
        [ReferenceTarget("goal")]
        public InArgument<EntityReference> Goal { get; set; }

        [Input("Goal Guid")]
        [Default("")]
        public InArgument<string> GoalGuid { get; set; }

        #endregion
        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            

            #region "Read Parameters"
            var _goal = Goal.Get(executionContext);
            var _goalguid = GoalGuid.Get(executionContext);
            if (_goal == null)
            {
                return;
            }
            

            objCommon.Trace("GoalID=" + _goal.Id.ToString());
            #endregion


            #region "GoalRequest Execution"
            var id = string.Empty;
            if (_goal != null)
            {
                id = _goal.Id.ToString();
            }
            else {
                id = _goalguid;
            }

            var recalculateRequest = new RecalculateRequest()
            {
                Target = new EntityReference("goal", new Guid (id))
            };
            objCommon.service.Execute(recalculateRequest);


            #endregion

        }
    }
}
