using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class RemoveUserFromTeam : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var userReference = User.Get(executionContext);
            var teamReference = Team.Get(executionContext);

            common.Trace($"UserID: {userReference.Id.ToString()} - TeamID: {teamReference.Id.ToString()} ");
            #endregion

            var request = new RemoveMembersTeamRequest
            {
                TeamId = teamReference.Id,
                MemberIds = new[] { userReference.Id}
            };

            common.service.Execute(request);
        }
    }
}
