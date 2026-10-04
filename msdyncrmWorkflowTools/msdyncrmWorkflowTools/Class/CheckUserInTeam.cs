using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CheckUserInTeam : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }

        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        [Output("isUserInTeam")]
        public OutArgument<bool> isUserInTeam { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var teamReference = Team.Get(executionContext);
            var userReference = User.Get(executionContext);

            common.Trace($"TeamId: {teamReference.Id.ToString()} ");
            #endregion

            var userId = common.Context.InitiatingUserId.ToString();
            if (userReference != null)
            {
                userId = userReference.Id.ToString();
            }

            var givenTeams = common.Service.RetrieveMultiple(Queries.TeamMembership(teamReference.Id, new Guid(userId)));

            var userInTeam = (givenTeams.Entities.Count > 0);

            common.Trace("{0}", userInTeam ? "User belongs to the team." : "User does not belong to the team.");

            isUserInTeam.Set(executionContext, userInTeam);
        }
    }
}
