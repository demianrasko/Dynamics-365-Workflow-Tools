// Not in the Power Platform build: it needs Dynamics 365 tables (incident).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class ResolveCase : WorkflowActivityBase
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Case")]
        [ReferenceTarget(EntityNames.Incident)]
        public InArgument<EntityReference> Incident { get; set; }

        [Input("Case Resolution")]
        public InArgument<string> IncidentResolution { get; set; }

        [Input("Resolution Description")]
        public InArgument<string> ResolutionDescription { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var incident = Incident.Get(executionContext) ?? throw new InvalidPluginExecutionException("Case is required.");

            common.ResolveCase(incident.Id, IncidentResolution.Get(executionContext), ResolutionDescription.Get(executionContext));
        }
    }
}
#endif
