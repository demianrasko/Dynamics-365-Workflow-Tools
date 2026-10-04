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
            var recordUrl = IncidentRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Incident Record URL is required.");
            }

            common.ApplyRoutingRule(common.GetRecordReference(recordUrl));
        }
    }
}
