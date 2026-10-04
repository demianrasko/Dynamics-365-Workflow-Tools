using System;
using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class ApplyRoutingRule : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Incident Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> IncidentRecordURL { get; set; }
        #endregion
        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var incidentRecordUrl= IncidentRecordURL.Get(executionContext);
            if (string.IsNullOrEmpty(incidentRecordUrl))
            {
                return;
            }

            var parsedUrl = Utility.ParseRecordUrl(incidentRecordUrl);
            var parentObjectTypeCode = parsedUrl.ObjectTypeCode;
            var parentId = parsedUrl.Id;
            
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + parentObjectTypeCode + "--ParentId=" + parentId);
            #endregion

            #region "ApplyRoutingRuleRequest Execution"
            var entityName = objCommon.GetEntityNameFromCode(parentObjectTypeCode, objCommon.service);
            
            var request = new ApplyRoutingRuleRequest
            {
                Target = new EntityReference(entityName, new Guid(parentId))
            };
            
            objCommon.service.Execute(request);
            
            #endregion
        }
    }
}
