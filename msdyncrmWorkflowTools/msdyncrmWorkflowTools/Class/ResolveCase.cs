// Not in the Power Platform build: it needs Dynamics 365 tables (incident).
#if !POWERPLATFORM
using Microsoft.Crm.Sdk.Messages;
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
            #region "Read Parameters"
            var incident = Incident.Get(executionContext);
            if (incident == null)
            {
                throw new InvalidPluginExecutionException("Case is required.");
            }

            var subject = IncidentResolution.Get(executionContext);
            var description = ResolutionDescription.Get(executionContext);

            common.Trace($"IncidentID: {incident.Id} - Description: {description} - Subject: {subject}");
            #endregion

            var incidentResolution = new Entity(EntityNames.IncidentResolution)
            {
                ["incidentid"] = new EntityReference(EntityNames.Incident, incident.Id),
                ["subject"] = subject,
                ["description"] = description
            };

            common.Service.Execute(new CloseIncidentRequest
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(5)
            });
        }
    }
}
#endif
