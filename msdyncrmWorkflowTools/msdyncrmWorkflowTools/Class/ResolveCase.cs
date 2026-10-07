// Not in the Power Platform build: it needs Dynamics 365 tables (incident).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Resolve Case")]
    public class ResolveCase : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Case")]
        [ReferenceTarget(EntityNames.Incident)]
        public InArgument<EntityReference> Incident { get; set; }

        [Input("Case Resolution")]
        public InArgument<string> IncidentResolution { get; set; }

        [Input("Resolution Description")]
        public InArgument<string> ResolutionDescription { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.ResolveCase(Utility.Required(Incident.Get(executionContext), "Case").Id, IncidentResolution.Get(executionContext), ResolutionDescription.Get(executionContext));
        }
    }
}
#endif
