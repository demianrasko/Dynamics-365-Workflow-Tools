using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class IsMemberOfTeam : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }

        [Output("Result")]
        public OutArgument<bool> Result { get; set; }
        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var user = User.Get(executionContext);
            var team = Team.Get(executionContext);
            #endregion

            #region "Is user member of team"

            var isMember = common.IsMemberOfTeam(team.Id, user.Id);

            Result.Set(executionContext, isMember);
            
            #endregion
        }
    }
}
