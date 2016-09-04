using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace msdyncrmWorkflowTools
{
    public class GoalRecalculate : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Goal")]
        [ReferenceTarget("goal")]
        public InArgument<EntityReference> Goal { get; set; }

        
        #endregion
        protected override void Execute(CodeActivityContext executionContext)
        {
            

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference _goal = this.Goal.Get(executionContext);
            if (_goal == null)
            {
                return;
            }

           

            objCommon.tracingService.Trace("GoalID=" + _goal.Id.ToString());
            #endregion


            #region "GoalRequest Execution"
            RecalculateRequest recalculateRequest = new RecalculateRequest()
            {
                Target = new EntityReference("goal", _goal.Id)
            };
            objCommon.service.Execute(recalculateRequest);


            #endregion

        }
    }
}
