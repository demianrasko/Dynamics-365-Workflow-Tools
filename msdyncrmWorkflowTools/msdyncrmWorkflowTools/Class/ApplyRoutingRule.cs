using System;
using System.Activities;
using System.Linq;
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
        public InArgument<String> IncidentRecordURL { get; set; }
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

            var urlParts = incidentRecordUrl.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var parentObjectTypeCode = urlParams[0].Replace("etc=", "");
            var parentId = urlParams[1].Replace("id=", "");
            
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
