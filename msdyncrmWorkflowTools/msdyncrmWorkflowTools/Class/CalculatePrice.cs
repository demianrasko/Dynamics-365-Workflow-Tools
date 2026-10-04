using System;
using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class CalculatePrice : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Target Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> TargetRecordURL { get; set; }
        #endregion
        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var targetRecordUrl = TargetRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(targetRecordUrl))
            {
                return;
            }
            var parsedUrl = Utility.ParseRecordUrl(targetRecordUrl);
            var parentObjectTypeCode = parsedUrl.ObjectTypeCode;
            var parentId = parsedUrl.Id;

            objCommon.Trace($"ParentObjectTypeCode={parentObjectTypeCode}--ParentId={parentId}");
            #endregion

            #region "ApplyRoutingRuleRequest Execution"
            var entityName = objCommon.GetEntityNameFromCode(parentObjectTypeCode);

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
