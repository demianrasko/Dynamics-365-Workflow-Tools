using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CheckUserInTeam : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget(EntityNames.Team)]
        public InArgument<EntityReference> Team { get; set; }

        [Input("User")]
        [ReferenceTarget(EntityNames.SystemUser)]
        public InArgument<EntityReference> User { get; set; }

        [Output("isUserInTeam")]
        public OutArgument<bool> isUserInTeam { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var team = Team.Get(executionContext);
            var userId = User.Get(executionContext)?.Id ?? common.Context.InitiatingUserId;

            isUserInTeam.Set(executionContext, common.IsMemberOfTeam(team.Id, userId));
        }
    }
}
