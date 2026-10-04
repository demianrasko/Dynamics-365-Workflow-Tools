using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class EmailToTeam : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Email")]
        [ReferenceTarget(EntityNames.Email)]
        public InArgument<EntityReference> Email { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget(EntityNames.Team)]
        public InArgument<EntityReference> Team { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"

            var email = Email.Get(executionContext);
            var team = Team.Get(executionContext);

            #endregion

            #region "Query Email of team members"
            var teamId = team.Id;
            var userQuery = new QueryExpression(EntityNames.SystemUser)
            {
                ColumnSet = new ColumnSet("systemuserid")
            };

            var teamLink = new LinkEntity(EntityNames.SystemUser, EntityNames.TeamMembership, "systemuserid", "systemuserid", JoinOperator.Inner);
            var teamCondition = new ConditionExpression("teamid", ConditionOperator.Equal, teamId);

            teamLink.LinkCriteria.AddCondition(teamCondition);
            userQuery.LinkEntities.Add(teamLink);

            var retrievedUsers = common.Service.RetrieveMultiple(userQuery);

            if (retrievedUsers.Entities.Count == 0)
            {
                return;
            }

            #endregion

            #region "Update the "To" field on the Email"
            var emailEnt = new Entity(EntityNames.Email,email.Id);

            var to = new EntityCollection();

            foreach (var user in retrievedUsers.Entities)
            {
                var userId = user.Id;

                var to1 = new Entity(EntityNames.ActivityParty)
                {
                    ["partyid"] = new EntityReference(EntityNames.SystemUser, userId)
                };

                to.Entities.Add(to1);
            }
            emailEnt["to"] = to;

            common.Service.Update(emailEnt);

            #endregion
        }
    }
}
