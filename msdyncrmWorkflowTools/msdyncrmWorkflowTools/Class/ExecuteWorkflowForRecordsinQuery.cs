using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public class ExecuteWorkflowForRecordsinQuery : CodeActivity
    {
        [Input("Process")]
        [ReferenceTarget("workflow")]
        public InArgument<EntityReference> Process { get; set; }


        [Input("Query")]
        public InArgument<string> Query { get; set; }


        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String query = this.Query.Get(executionContext);
            EntityReference process = this.Process.Get(executionContext);
            #endregion

            #region "SetProcess Execution"
            EntityCollection entityCollection = new EntityCollection();
            RunQuery(objCommon.service, query, ref entityCollection);
            foreach (Entity ent in entityCollection.Entities)
            {
                ExecuteWorkflowRequest wfRequest = new ExecuteWorkflowRequest();
                wfRequest.EntityId = ent.Id;
                wfRequest.WorkflowId = process.Id;
                ExecuteWorkflowResponse wfResponse = (ExecuteWorkflowResponse)objCommon.service.Execute(wfRequest);
            }
            #endregion

        }
        public bool RunQuery(IOrganizationService service, string fetchXML, ref EntityCollection result)
        {
            if (!string.IsNullOrEmpty(fetchXML))
            {
                result = service.RetrieveMultiple((QueryBase)new FetchExpression(fetchXML));
                if (result != null && ((IEnumerable<Entity>)result.Entities).Any<Entity>())
                    return true;
            }
            return false;
        }
    }
}
