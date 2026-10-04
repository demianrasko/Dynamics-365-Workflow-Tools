using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class ExecuteWorkflowByID : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record ID")]
        [ReferenceTarget("")]
        public InArgument<string> RecordID { get; set; }
        
        [Input("Process")]
        [ReferenceTarget("workflow")]
        public InArgument<EntityReference> Process { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
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

            common.service.Execute(request);

            #endregion
        }
    }
}
