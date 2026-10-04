using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class SetState : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("State")]     
        public InArgument<int> State { get; set; }

        [RequiredArgument]
        [Input("Status")]
        public InArgument<int> Status { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var stateValue= State.Get(executionContext);
            var statusValue = Status.Get(executionContext);
            #endregion

            #region "SetState Execution"

            var moniker = new EntityReference
            {
                LogicalName = objCommon.context.PrimaryEntityName,
                Id = objCommon.context.PrimaryEntityId
            };

            var request = new OrganizationRequest
            {
                RequestName = "SetState",
                ["EntityMoniker"] = moniker
            };

            request["State"] = new OptionSetValue(stateValue);
            request["Status"] = new OptionSetValue(statusValue);

            objCommon.service.Execute(request);

            #endregion
        }
    }
}
