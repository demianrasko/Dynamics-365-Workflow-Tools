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
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(null);
            Common objCommon = new Common();
            tracingService.Trace("Load CRM Service from context --- OK");
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
            tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);
            #endregion


            #region "ApplyRoutingRuleRequest Execution"
            string EntityName = objCommon.sGetEntityNameFromCode(ParentObjectTypeCode, service);
            ApplyRoutingRuleRequest routeRequest = new ApplyRoutingRuleRequest();
            routeRequest.Target = new EntityReference(EntityName, new Guid(ParentId));
            ApplyRoutingRuleResponse routeResponse = (ApplyRoutingRuleResponse)service.Execute(routeRequest);
            
            #endregion

        }
    }
}
