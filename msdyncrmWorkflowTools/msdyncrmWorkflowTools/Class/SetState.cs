using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using msdyncrmWorkflowTools;
using System.ServiceModel;

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

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            int _state= this.State.Get(executionContext);
            int _status = this.Status.Get(executionContext);

                    
            #endregion


            #region "SetState Execution"

            try
            {
                EntityReference moniker = new EntityReference();
                moniker.LogicalName = objCommon.context.PrimaryEntityName;
                moniker.Id = objCommon.context.PrimaryEntityId;

                Microsoft.Xrm.Sdk.OrganizationRequest request
                  = new Microsoft.Xrm.Sdk.OrganizationRequest() { RequestName = "SetState" };
                request["EntityMoniker"] = moniker;
                OptionSetValue state = new OptionSetValue(_state);
                OptionSetValue status = new OptionSetValue(_status);
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
