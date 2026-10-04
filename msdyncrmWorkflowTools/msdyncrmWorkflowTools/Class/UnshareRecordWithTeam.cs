using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class UnshareRecordWithTeam : WorkflowActivityBase
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Sharing Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> SharingRecordURL { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var sharingRecordUrl = SharingRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(sharingRecordUrl))
            {
                throw new InvalidPluginExecutionException("Sharing Record URL is required.");
            }

            var principal = Team.Get(executionContext);
            #endregion

            common.UnshareRecord(sharingRecordUrl, principal);
        }
    }
}
