using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace msdyncrmWorkflowTools.Class
{
    public class ExecuteWorkflowByID : CodeActivity
    {
        [RequiredArgument]
        [Input("Record ID")]
        [ReferenceTarget("")]
        public InArgument<String> RecordID { get; set; }

        
        [Input("Process")]
        [ReferenceTarget("workflow")]
        public InArgument<EntityReference> Process { get; set; }

 

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _RecordID = this.RecordID.Get(executionContext);
          
            
            var process = this.Process.Get(executionContext);


            #endregion

            #region "SetProcess Execution"

            var wfRequest = new ExecuteWorkflowRequest();
            wfRequest.EntityId = new Guid(_RecordID);
            wfRequest.WorkflowId = process.Id;
            var wfResponse=(ExecuteWorkflowResponse)objCommon.service.Execute(wfRequest);

            #endregion

        }
    }
}
