using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
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
            var parsedUrl = common.ParseRecordUrl(targetRecordUrl);

            common.Trace($"EntityName={parsedUrl.EntityName}--Id={parsedUrl.Id}");
            #endregion

            #region "ApplyRoutingRuleRequest Execution"
            var entityName = parsedUrl.EntityName;

            var target = new EntityReference(entityName, parsedUrl.Id);

            var request = new CalculatePriceRequest
            {
                Target = target
            };

            common.Service.Execute(request);
            #endregion
        }
    }
}
