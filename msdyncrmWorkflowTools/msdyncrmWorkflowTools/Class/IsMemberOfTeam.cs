using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Is Member Of Team")]
    public class IsMemberOfTeam : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget(EntityNames.SystemUser)]
        public InArgument<EntityReference> User { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget(EntityNames.Team)]
        public InArgument<EntityReference> Team { get; set; }

        [Output("Result")]
        public OutArgument<bool> Result { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var user = User.Get(executionContext);
            var team = Team.Get(executionContext);

            var isMember = common.IsMemberOfTeam(team.Id, user.Id);

            Result.Set(executionContext, isMember);
        }
    }
}
