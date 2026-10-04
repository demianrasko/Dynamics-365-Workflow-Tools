using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class RetrieveUserBUDefaultTeam : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget(EntityNames.SystemUser)]
        public InArgument<EntityReference> User { get; set; }

        [Output("DefaultTeam")]
        [ReferenceTarget(EntityNames.Team)]
        public OutArgument<EntityReference> DefaultTeam { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var user = User.Get(executionContext);

            #endregion

            var team = common.RetrieveUserBuDefaultTeam(user.Id.ToString());

            DefaultTeam.Set(executionContext, team);
        }
    }
}
