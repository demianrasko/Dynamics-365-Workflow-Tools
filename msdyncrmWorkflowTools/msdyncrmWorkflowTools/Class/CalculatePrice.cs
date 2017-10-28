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
    public class CalculatePrice : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Target Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> TargetRecordURL { get; set; }
        #endregion
        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _TargetRecordURL = this.TargetRecordURL.Get(executionContext);
            if (_TargetRecordURL == null || _TargetRecordURL == "")
            {
                return;
            }
            string[] urlParts = _TargetRecordURL.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string ParentObjectTypeCode = urlParams[0].Replace("etc=", "");
            string ParentId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);
            #endregion


            #region "ApplyRoutingRuleRequest Execution"
            string EntityName = objCommon.sGetEntityNameFromCode(ParentObjectTypeCode, objCommon.service);


            CalculatePriceRequest calcReq = new CalculatePriceRequest();
            EntityReference target = new EntityReference(EntityName,new Guid(ParentId));
            calcReq.Target = target;
            CalculatePriceResponse calcRes = (CalculatePriceResponse)objCommon.service.Execute(calcReq);

            #endregion

        }
    }
}
