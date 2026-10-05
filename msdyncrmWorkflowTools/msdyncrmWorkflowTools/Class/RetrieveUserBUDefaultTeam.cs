using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class RetrieveUserBUDefaultTeam : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget(EntityNames.SystemUser)]
        public InArgument<EntityReference> User { get; set; }

        [Output("DefaultTeam")]
        [ReferenceTarget(EntityNames.Team)]
        public OutArgument<EntityReference> DefaultTeam { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var user = User.Get(executionContext);

            var team = common.RetrieveUserBuDefaultTeam(user.Id);

            DefaultTeam.Set(executionContext, team);
        }
    }
}
