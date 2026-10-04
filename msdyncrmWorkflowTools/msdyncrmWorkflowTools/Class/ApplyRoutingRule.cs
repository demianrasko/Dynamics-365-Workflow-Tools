using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class ApplyRoutingRule : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Incident Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> IncidentRecordURL { get; set; }
        #endregion
        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var incidentRecordUrl= IncidentRecordURL.Get(executionContext);
            if (string.IsNullOrEmpty(incidentRecordUrl))
            {
                return;
            }

            var parsedUrl = Utility.ParseRecordUrl(incidentRecordUrl);
            var parentObjectTypeCode = parsedUrl.ObjectTypeCode;
            var parentId = parsedUrl.Id;
            
            objCommon.Trace("ParentObjectTypeCode=" + parentObjectTypeCode + "--ParentId=" + parentId);
            #endregion

            #region "ApplyRoutingRuleRequest Execution"
            var entityName = objCommon.GetEntityNameFromCode(parentObjectTypeCode);
            
            var request = new ApplyRoutingRuleRequest
            {
                Target = new EntityReference(entityName, new Guid(parentId))
            };
            
            objCommon.service.Execute(request);
            
            #endregion
        }
    }
}
