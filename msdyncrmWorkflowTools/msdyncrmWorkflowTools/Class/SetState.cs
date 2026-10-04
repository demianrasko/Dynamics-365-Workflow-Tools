using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class SetState : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("State")]     
        public InArgument<int> State { get; set; }

        [RequiredArgument]
        [Input("Status")]
        public InArgument<int> Status { get; set; }
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var stateValue= State.Get(executionContext);
            var statusValue = Status.Get(executionContext);
            #endregion

            #region "SetState Execution"

            var moniker = new EntityReference
            {
                LogicalName = common.context.PrimaryEntityName,
                Id = common.context.PrimaryEntityId
            };

            var request = new OrganizationRequest
            {
                RequestName = "SetState",
                ["EntityMoniker"] = moniker,
                ["State"] = new OptionSetValue(stateValue),
                ["Status"] = new OptionSetValue(statusValue)
            };

            common.service.Execute(request);

            #endregion
        }
    }
}
