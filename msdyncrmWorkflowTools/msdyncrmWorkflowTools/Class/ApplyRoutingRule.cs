using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
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
        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var incidentRecordUrl= IncidentRecordURL.Get(executionContext);
            if (string.IsNullOrEmpty(incidentRecordUrl))
            {
                throw new InvalidPluginExecutionException("Incident Record URL is required.");
            }

            var parsedUrl = common.ParseRecordUrl(incidentRecordUrl);

            common.Trace($"EntityName={parsedUrl.EntityName}--Id={parsedUrl.Id}");
            #endregion

            #region "ApplyRoutingRuleRequest Execution"
            var entityName = parsedUrl.EntityName;

            var request = new ApplyRoutingRuleRequest
            {
                Target = new EntityReference(entityName, parsedUrl.Id)
            };

            common.Service.Execute(request);
            #endregion
        }
    }
}
