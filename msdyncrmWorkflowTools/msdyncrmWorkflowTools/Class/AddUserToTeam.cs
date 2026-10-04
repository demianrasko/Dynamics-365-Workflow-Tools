using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class AddUserToTeam : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var userReference = User.Get(executionContext);
            var teamReference = Team.Get(executionContext);

            objCommon.Trace($"UserID: {userReference.Id.ToString()} - TeamID: {teamReference.Id.ToString()} ");
            #endregion

            var request = new AddMembersTeamRequest
            {
                TeamId = teamReference.Id,
                MemberIds = new[] { userReference.Id }
            };

            objCommon.service.Execute(request);
        }
    }
}
