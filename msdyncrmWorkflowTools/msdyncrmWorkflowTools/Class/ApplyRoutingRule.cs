using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Crm.Sdk.Messages;


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

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _IncidentRecordURL= this.IncidentRecordURL.Get(executionContext);
            if (_IncidentRecordURL == null || _IncidentRecordURL == "")
            {
                return;
            }
            string[] urlParts = _IncidentRecordURL.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string ParentObjectTypeCode = urlParams[0].Replace("etc=", "");
            string ParentId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);
            #endregion


            #region "ApplyRoutingRuleRequest Execution"
            string EntityName = objCommon.sGetEntityNameFromCode(ParentObjectTypeCode, objCommon.service);
            ApplyRoutingRuleRequest routeRequest = new ApplyRoutingRuleRequest();
            routeRequest.Target = new EntityReference(EntityName, new Guid(ParentId));
            ApplyRoutingRuleResponse routeResponse = (ApplyRoutingRuleResponse)objCommon.service.Execute(routeRequest);
            
            #endregion

        }
    }
}
