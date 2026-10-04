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

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _TargetRecordURL = TargetRecordURL.Get(executionContext);
            if (_TargetRecordURL == null || _TargetRecordURL == "")
            {
                return;
            }
            var urlParts = _TargetRecordURL.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var ParentObjectTypeCode = urlParams[0].Replace("etc=", "");
            var ParentId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);
            #endregion


            #region "ApplyRoutingRuleRequest Execution"
            var EntityName = objCommon.sGetEntityNameFromCode(ParentObjectTypeCode, objCommon.service);


            var calcReq = new CalculatePriceRequest();
            var target = new EntityReference(EntityName,new Guid(ParentId));
            calcReq.Target = target;
            var calcRes = (CalculatePriceResponse)objCommon.service.Execute(calcReq);

            #endregion

        }
    }
}
