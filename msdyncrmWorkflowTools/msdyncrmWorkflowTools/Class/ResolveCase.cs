using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools.Class
{

    public class ResolveCase : CodeActivity
    {

        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Case")]
        [ReferenceTarget("incident")]
        public InArgument<EntityReference> Incident { get; set; }

        [Input("Case Resolution")]
        public InArgument<string> IncidentResolution { get; set; }

        [Input("Resolution Description")]
        public InArgument<string> ResolutionDescription { get; set; }

        #endregion


        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference incident = this.Incident.Get(executionContext);
            string subject = this.IncidentResolution.Get<string>(executionContext);
            string description = this.ResolutionDescription.Get<string>(executionContext);

            objCommon.tracingService.Trace(String.Format("IncidentID: {0} - Description: {1} - Subject: {2}", incident.Id.ToString(), description, subject));

            #endregion

            Entity incidentResolution = new Entity("incidentresolution");
            incidentResolution.Attributes.Add("incidentid", new EntityReference("incident", incident.Id));
            incidentResolution.Attributes.Add("subject", subject);
            incidentResolution.Attributes.Add("description", description);

            CloseIncidentRequest closeIncidentRequest = new CloseIncidentRequest
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(5)
            };

            objCommon.service.Execute(closeIncidentRequest);
        }
    }
}
