using System;
using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class GoalRecalculate : CodeActivity
    {
        #region "Parameter Definition"
        [Input("Goal")]
        [ReferenceTarget("goal")]
        public InArgument<EntityReference> Goal { get; set; }

        [Input("Goal Guid")]
        [Default("")]
        public InArgument<string> GoalGuid { get; set; }

        #endregion
        protected override void Execute(CodeActivityContext executionContext)
        {
            

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _goal = Goal.Get(executionContext);
            var _goalguid = GoalGuid.Get(executionContext);
            if (_goal == null)
            {
                return;
            }
            

            objCommon.tracingService.Trace("GoalID=" + _goal.Id.ToString());
            #endregion


            #region "GoalRequest Execution"
            var id = "";
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
