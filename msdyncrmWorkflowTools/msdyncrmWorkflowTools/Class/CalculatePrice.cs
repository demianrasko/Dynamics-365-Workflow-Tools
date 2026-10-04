using System;
using System.Activities;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class CalculatePrice : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Target Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> TargetRecordURL { get; set; }
        #endregion
        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var targetRecordUrl = TargetRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(targetRecordUrl))
            {
                return;
            }
            var urlParts = targetRecordUrl.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var parentObjectTypeCode = urlParams[0].Replace("etc=", "");
            var parentId = urlParams[1].Replace("id=", "");

            objCommon.tracingService.Trace($"ParentObjectTypeCode={parentObjectTypeCode}--ParentId={parentId}");
            #endregion

            #region "ApplyRoutingRuleRequest Execution"
            var entityName = objCommon.GetEntityNameFromCode(parentObjectTypeCode, objCommon.service);

            var target = new EntityReference(entityName, new Guid(parentId));

            var calcReq = new CalculatePriceRequest
            {
                Target = target
            };

            objCommon.service.Execute(calcReq);

            #endregion
        }
    }
}
