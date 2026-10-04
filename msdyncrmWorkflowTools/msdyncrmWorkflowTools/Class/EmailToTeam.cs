using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Client;



namespace msdyncrmWorkflowTools.Class
{
    public class EmailToTeam : CodeActivity
    {
        [RequiredArgument]
        [Input("Email")]
        [ReferenceTarget("email")]
        public InArgument<EntityReference> Email { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"

           
            var email = this.Email.Get(executionContext);
            var team = this.Team.Get(executionContext);

            #endregion

            #region "Query Email of team members"
            // Id of the specific Team
            var teamId = team.Id;
            // main query returing users
            var userQuery = new QueryExpression("systemuser");
            // take all columns
            userQuery.ColumnSet = new ColumnSet("systemuserid");
            // this is the intersect condition
            var teamLink = new LinkEntity("systemuser", "teammembership", "systemuserid", "systemuserid", JoinOperator.Inner);
            // this is the condition to use the specific Team
            var teamCondition = new ConditionExpression("teamid", ConditionOperator.Equal, teamId);
            // add the condition to the intersect
            teamLink.LinkCriteria.AddCondition(teamCondition);
            // add the intersect to the query
            userQuery.LinkEntities.Add(teamLink);
            //get the results
            var retrievedUsers = objCommon.service.RetrieveMultiple(userQuery);

            if (retrievedUsers.Entities.Count == 0) return;
            // fetch the results

            #endregion
            #region "Update the "To" field on the Email"
            var emailEnt = new Entity("email",email.Id);
            
            var to = new EntityCollection();

            foreach (var user in retrievedUsers.Entities)
            {
                // Id of the user
                var userId = user.Id;
              
                var to1 = new Entity("activityparty");
                to1["partyid"] = new EntityReference("systemuser", userId);
                
                to.Entities.Add(to1);

            }
            emailEnt["to"] = to;

            objCommon.service.Update(emailEnt);


            #endregion



        }
    }
}
