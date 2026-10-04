using System.Activities;
using System.ServiceModel;
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
            var _state= State.Get(executionContext);
            var _status = Status.Get(executionContext);

                    
            #endregion


            #region "SetState Execution"

            try
            {
                var moniker = new EntityReference();
                moniker.LogicalName = objCommon.context.PrimaryEntityName;
                moniker.Id = objCommon.context.PrimaryEntityId;

                var request
                  = new Microsoft.Xrm.Sdk.OrganizationRequest() { RequestName = "SetState" };
                request["EntityMoniker"] = moniker;
                var state = new OptionSetValue(_state);
                var status = new OptionSetValue(_status);
                request["State"] = state;
                request["Status"] = status;

                objCommon.service.Execute(request);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                throw ex;
            }
            #endregion

        }


    }
}
