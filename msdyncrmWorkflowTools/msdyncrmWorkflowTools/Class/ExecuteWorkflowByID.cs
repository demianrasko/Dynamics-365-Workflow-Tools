using System;
using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools.Class
{
    public class ExecuteWorkflowByID : CodeActivity
    {
        [RequiredArgument]
        [Input("Record ID")]
        [ReferenceTarget("")]
        public InArgument<string> RecordID { get; set; }
        
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
            var recordId = RecordID.Get(executionContext);
            
            var process = Process.Get(executionContext);
            #endregion

            #region "SetProcess Execution"

            var request = new ExecuteWorkflowRequest
            {
                EntityId = new Guid(recordId),
                WorkflowId = process.Id
            };

            objCommon.service.Execute(request);

            #endregion
        }
    }
}
