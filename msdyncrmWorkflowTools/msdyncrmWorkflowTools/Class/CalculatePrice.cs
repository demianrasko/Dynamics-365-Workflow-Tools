using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

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
        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var targetRecordUrl = TargetRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(targetRecordUrl))
            {
                throw new InvalidPluginExecutionException("Target Record URL is required.");
            }
            var parsedUrl = Utility.ParseRecordUrl(targetRecordUrl);
            var parentObjectTypeCode = parsedUrl.ObjectTypeCode;
            var parentId = parsedUrl.Id;

            common.Trace($"ParentObjectTypeCode={parentObjectTypeCode}--ParentId={parentId}");
            #endregion

            #region "ApplyRoutingRuleRequest Execution"
            var entityName = common.GetEntityNameFromCode(parentObjectTypeCode);

            var target = new EntityReference(entityName, new Guid(parentId));

            var calcReq = new CalculatePriceRequest
            {
                Target = target
            };

            common.service.Execute(calcReq);

            #endregion
        }
    }
}
